﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace BinBridge.Utility
{
    public static class ZipFileEx
    {
        private const char PathSeparator = '/';

        public static Stream CreateStreamFromDirectory(string sourceDirectoryName)
        {
            return DoCreateStreamFromDirectory(sourceDirectoryName,
                      compressionLevel: null, includeBaseDirectory: false, entryNameEncoding: null);
        }

        private static void EnsureCapacity(ref char[] buffer, int min)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(min > 0);

            if (buffer.Length < min)
            {
                int newCapacity = buffer.Length * 2;
                if (newCapacity < min)
                    newCapacity = min;
                ArrayPool<char>.Shared.Return(buffer);
                buffer = ArrayPool<char>.Shared.Rent(newCapacity);
            }
        }

        private static string EntryFromPath(string entry, int offset, int length, ref char[] buffer, bool appendPathSeparator = false)
        {
            Debug.Assert(length <= entry.Length - offset);
            Debug.Assert(buffer != null);

            // Remove any leading slashes from the entry name:
            while (length > 0)
            {
                if (entry[offset] != Path.DirectorySeparatorChar &&
                    entry[offset] != Path.AltDirectorySeparatorChar)
                    break;

                offset++;
                length--;
            }

            if (length == 0)
                return appendPathSeparator ? PathSeparator.ToString() : string.Empty;

            int resultLength = appendPathSeparator ? length + 1 : length;
            EnsureCapacity(ref buffer, resultLength);
            entry.CopyTo(offset, buffer, 0, length);

            // '/' is a more broadly recognized directory separator on all platforms (eg: mac, linux)
            // We don't use Path.DirectorySeparatorChar or AltDirectorySeparatorChar because this is
            // explicitly trying to standardize to '/'
            for (int i = 0; i < length; i++)
            {
                char ch = buffer[i];
                if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar)
                    buffer[i] = PathSeparator;
            }

            if (appendPathSeparator)
                buffer[length] = PathSeparator;

            return new string(buffer, 0, resultLength);
        }

        private static Stream DoCreateStreamFromDirectory(string sourceDirectoryName,
                                          CompressionLevel? compressionLevel, bool includeBaseDirectory,
                                          Encoding entryNameEncoding)
        {
            // Rely on Path.GetFullPath for validation of sourceDirectoryName and destinationArchive

            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            sourceDirectoryName = Path.GetFullPath(sourceDirectoryName);

            var memoryStream = new MemoryStream();

            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                bool directoryIsEmpty = true;

                // Add files and directories
                DirectoryInfo di = new DirectoryInfo(sourceDirectoryName);

                string basePath = di.FullName;

                if (includeBaseDirectory && di.Parent != null)
                {
                    basePath = di.Parent.FullName;
                }

                // Windows' MaxPath (260) is used as an arbitrary default capacity, as it is likely
                // to be greater than the length of typical entry names from the file system, even
                // on non-Windows platforms. The capacity will be increased, if needed.
                const int DefaultCapacity = 260;
                char[] entryNameBuffer = ArrayPool<char>.Shared.Rent(DefaultCapacity);

                try
                {
                    foreach (FileSystemInfo file in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        directoryIsEmpty = false;

                        int entryNameLength = file.FullName.Length - basePath.Length;
                        Debug.Assert(entryNameLength > 0);

                        if (file is FileInfo)
                        {
                            // Create entry for file:
                            string entryName = EntryFromPath(file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer);
                            DoCreateEntryFromFile(archive, file.FullName, entryName, compressionLevel);
                        }
                        else
                        {
                            // Entry marking an empty dir:
                            DirectoryInfo possiblyEmpty = file as DirectoryInfo;
                            if (possiblyEmpty != null && IsDirEmpty(possiblyEmpty))
                            {
                                // FullName never returns a directory separator character on the end,
                                // but Zip archives require it to specify an explicit directory:
                                string entryName = EntryFromPath(file.FullName, basePath.Length, entryNameLength, ref entryNameBuffer, appendPathSeparator: true);
                                archive.CreateEntry(entryName);
                            }
                        }
                    }  // foreach

                    // If no entries create an empty root directory entry:
                    if (includeBaseDirectory && directoryIsEmpty)
                        archive.CreateEntry(EntryFromPath(di.Name, 0, di.Name.Length, ref entryNameBuffer, appendPathSeparator: true));
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(entryNameBuffer);
                }
            } // using archive

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }  // DoCreateStreamFromDirectory

        private static bool IsDirEmpty(DirectoryInfo possiblyEmptyDir)
        {
            using (IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(possiblyEmptyDir.FullName).GetEnumerator())
            {
                return !enumerator.MoveNext();
            }
        }

        private static ZipArchiveEntry DoCreateEntryFromFile(ZipArchive destination,
                                                              string sourceFileName, string entryName, CompressionLevel? compressionLevel)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));

            if (entryName == null)
                throw new ArgumentNullException(nameof(entryName));

            // Checking of compressionLevel is passed down to DeflateStream and the IDeflater implementation
            // as it is a pluggable component that completely encapsulates the meaning of compressionLevel.

            // Argument checking gets passed down to FileStream's ctor and CreateEntry
            Contract.Ensures(Contract.Result<ZipArchiveEntry>() != null);
            Contract.EndContractBlock();

            using (Stream fs = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false))
            {
                ZipArchiveEntry entry = compressionLevel.HasValue
                                                ? destination.CreateEntry(entryName, compressionLevel.Value)
                                                : destination.CreateEntry(entryName);

                DateTime lastWrite = File.GetLastWriteTime(sourceFileName);

                // If file to be archived has an invalid last modified time, use the first datetime representable in the Zip timestamp format
                // (midnight on January 1, 1980):
                if (lastWrite.Year < 1980 || lastWrite.Year > 2107)
                {
                    lastWrite = new DateTime(1980, 1, 1, 0, 0, 0);
                }

                entry.LastWriteTime = lastWrite;

                using (Stream es = entry.Open())
                {
                    fs.CopyTo(es);
                }

                return entry;
            }
        }
    }
}
