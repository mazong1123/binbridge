using BinBridge.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinBridge.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string host = "127.0.1.1";
            string userName = "mazong1123";
            string password = "mazong1123!@#$";

            string srcDirectory = "";
            string destDirectory = "/home/mazong1123/binbridge/test";

            using (SshClient sshClient = new SshClient(host, userName, password))
            {
                sshClient.Connect();
                var command = sshClient.CreateCommand("find " + destDirectory + " -type f -exec ls -l --time-style=full-iso {} \\;");
                var remoteResult = command.Execute();


            }
        }
    }
}
