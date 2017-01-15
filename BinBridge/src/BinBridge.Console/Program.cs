using BinBridge.Core;
using BinBridge.Ssh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            foreach(var lfd in localFileOrDirectories)
            {
                var relativePath = lfd.Path.Substring(localBaseDirectoryLength);
                localRelativePathHash.Add(relativePath, lfd);
            }

            Dictionary<string, FileOrDirectoryInfo> remoteRelativePathHash = new Dictionary<string, FileOrDirectoryInfo>();

            var remoteBaseDirectoryLength = remoteBaseDirectory.Length;
            foreach(var rfd in remoteFileOrDirectories)
            {
                var relativePath = rfd.Path.Substring(remoteBaseDirectoryLength);
                remoteRelativePathHash.Add(relativePath, rfd);
            }


            return difference;
        }

        public static void Main(string[] args)
        {
            string host = "127.0.1.1";
            string userName = "mazong1123";
            string password = "mazong1123!@#$";

            string srcDirectory = @"E:\Workspace\github\binbridge\BinBridge\testdata";
            string destDirectory = "/home/mazong1123/binbridge/test";

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
                        Path = ld.FullName.Substring(2).Replace("\\", "/"),
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
                        Path = lf.FullName.Substring(2).Replace("\\", "/"),
                        Size = lf.Length
                    };

                    localFileAndDirectoryInfo.Add(fdi);
                }

                // Compare to find the difference.
                var difference = FindDifference(localFileAndDirectoryInfo, remoteFileAndDirectoryInfo,
                    srcDirectory.Substring(2).Replace("\\", "/"), destDirectory);

                // Delete remote file or folders.
                string fileToDelete = destDirectory + "/test.c";

                var deleteResult = command.Execute("rm -f " + fileToDelete);

                // Upload added or changed files.
                using (SftpClient sftpClient = new SftpClient(sshClient.ConnectionInfo))
                {
                    sftpClient.Connect();
                    FileStream fs = new FileStream(srcDirectory + "\\test.c", FileMode.Open);

                    sftpClient.UploadFile(fs, destDirectory + "/test.c");

                    fs.Dispose();
                }
            }
        }
    }
}
