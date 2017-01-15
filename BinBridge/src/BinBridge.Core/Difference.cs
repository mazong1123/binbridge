using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinBridge.Core
{
    public class Difference
    {
        private List<FileOrDirectoryInfo> addedFileOrDirectories = new List<FileOrDirectoryInfo>();
        private List<FileOrDirectoryInfo> removedFileOrDirectories = new List<FileOrDirectoryInfo>();
        private List<FileOrDirectoryInfo> modifiedFiles = new List<FileOrDirectoryInfo>();

        public List<FileOrDirectoryInfo> AddedFileOrDirectories
        {
            get
            {
                return addedFileOrDirectories;
            }
        }

        public List<FileOrDirectoryInfo> RemovedFileOrDirectories
        {
            get
            {
                return removedFileOrDirectories;
            }
        }

        public List<FileOrDirectoryInfo> ModifiedFiles
        {
            get
            {
                return modifiedFiles;
            }
        }
    }
}
