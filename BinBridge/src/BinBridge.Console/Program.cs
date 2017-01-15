using BinBridge.Core;
using BinBridge.Ssh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinBridge.Console
{
    public class Program
    {
        private static Difference FindDifference(List<FileOrDirectoryInfo> localFileOrDirectories,
            List<FileOrDirectoryInfo> remoteFileOrDirectories, string localBaseDirectory, string remoteBaseDirectory)
        {
            var difference = new Difference();

            // Extract relative path.
            Dictionary<string, FileOrDirectoryInfo> localRelativePathHash = new Dictionary<string, FileOrDirectoryInfo>();

            var localBaseDirectoryLength = localBaseDirectory.Length;
            foreach (var lfd in localFileOrDirectories)
            {
                localRelativePathHash.Add(lfd.RelativePath, lfd);
            }

            Dictionary<string, FileOrDirectoryInfo> remoteRelativePathHash = new Dictionary<string, FileOrDirectoryInfo>();

            var remoteBaseDirectoryLength = remoteBaseDirectory.Length;
            foreach (var rfd in remoteFileOrDirectories)
            {
                remoteRelativePathHash.Add(rfd.RelativePath, rfd);
            }

            foreach (var remoteRelativePath in remoteRelativePathHash.Keys)
            {
                if (!localRelativePathHash.ContainsKey(remoteRelativePath))
                {
                    // Does not exist in local file path collection. Need to be removed.
                    difference.RemovedFileOrDirectories.Add(remoteRelativePathHash[remoteRelativePath]);
                }
            }

            foreach (var localRelativePath in localRelativePathHash.Keys)
            {
                if (!remoteRelativePathHash.ContainsKey(localRelativePath))
                {
                    // Does not exist in remote file path collection. Need to be added.
                    difference.AddedFileOrDirectories.Add(localRelativePathHash[localRelativePath]);
                }
            }

            return difference;
        }

        public static void Main(string[] args)
        {
            string host = "127.0.1.1";
            string userName = "";
            string password = "";

            string srcDirectory = @"E:\Workspace\github\binbridge\BinBridge\testdata";
            string destDirectory = "/home/mazong1123/binbridge/test";

            var localBaseDirectoryLength = srcDirectory.Length;
            var remoteBaseDirectoryLength = destDirectory.Length;

            using (SshClient sshClient = new SshClient(host, userName, password))
            {
                sshClient.Connect();
                var command = sshClient.CreateCommand("find " + destDirectory + " -exec ls -ld --time-style=full-iso {} \\; 2> /dev/null");
                var remoteResult = command.Execute();

                // Assemble the remote file and directory info.
                var remoteFileAndDirectoryInfo = new List<FileOrDirectoryInfo>();

                var rows = remoteResult.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var r in rows)
                {
                    if (r.StartsWith("total "))
                    {
                        continue;
                    }

                    var cols = r.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    var path = cols[8];
                    if (path.Equals(destDirectory))
                    {
                        continue;
                    }

                    var typeString = cols[0];
                    var depth = cols[1];
                    var owner = cols[2];
                    var modifier = cols[3];
                    var size = cols[4];
                    var modifiedDate = cols[5];
                    var modifiedTime = cols[6];
                    var modifiedTimeZone = cols[7];

                    FileOrDirectoryInfo fdi = new FileOrDirectoryInfo();
                    fdi.Type = typeString.StartsWith("d") ? 1 : 0;
                    fdi.Path = path;
                    fdi.RelativePath = path.Substring(remoteBaseDirectoryLength);
                    fdi.Size = long.Parse(size);

                    // TODO: Parse last modified time.

                    remoteFileAndDirectoryInfo.Add(fdi);
                }

                DirectoryInfo di = new DirectoryInfo(srcDirectory);
                var localDirectoryList = di.GetDirectories("*", SearchOption.AllDirectories);
                var localFileList = di.GetFiles("*", SearchOption.AllDirectories);

                // Assemble local file and directory info.
                var localFileAndDirectoryInfo = new List<FileOrDirectoryInfo>();
                foreach (var ld in localDirectoryList)
                {
                    var fdi = new FileOrDirectoryInfo()
                    {
                        Type = 1,
                        LastModifiedTime = ld.LastWriteTimeUtc,
                        Path = ld.FullName,
                        RelativePath = ld.FullName.Substring(localBaseDirectoryLength).Replace("\\", "/"),
                        Size = 0
                    };

                    localFileAndDirectoryInfo.Add(fdi);
                }

                foreach (var lf in localFileList)
                {
                    var fdi = new FileOrDirectoryInfo()
                    {
                        Type = 0,
                        LastModifiedTime = lf.LastAccessTimeUtc,
                        Path = lf.FullName,
                        RelativePath = lf.FullName.Substring(localBaseDirectoryLength).Replace("\\", "/"),
                        Size = lf.Length
                    };

                    localFileAndDirectoryInfo.Add(fdi);
                }

                // Compare to find the difference.
                var difference = FindDifference(localFileAndDirectoryInfo, remoteFileAndDirectoryInfo,
                    srcDirectory.Substring(2).Replace("\\", "/"), destDirectory);

                if (difference.RemovedFileOrDirectories.Count > 0)
                {
                    // Delete remote file or folders.
                    StringBuilder deleteCommand = new StringBuilder("rm -rf ");
                    foreach (var fdr in difference.RemovedFileOrDirectories)
                    {
                        deleteCommand.Append(fdr.Path).Append(" ");
                    }

                    command.Execute(deleteCommand.ToString());
                }

                if (difference.AddedFileOrDirectories.Count > 0)
                {
                    // Upload added or changed files.
                    using (SftpClient sftpClient = new SftpClient(sshClient.ConnectionInfo))
                    {
                        sftpClient.Connect();

                        foreach (var fda in difference.AddedFileOrDirectories)
                        {
                            var destFilePath = destDirectory + fda.RelativePath.Replace("\\", "/");

                            if (fda.Type == 0)
                            {
                                FileStream fs = new FileStream(fda.Path, FileMode.Open);

                                sftpClient.UploadFile(fs, destFilePath);

                                fs.Dispose();
                            }
                            else
                            {
                                sftpClient.CreateDirectory(destFilePath);
                            }
                        }
                    }
                }

            }
        }
    }
}
