using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinBridge.Core
{
    public class FileOrDirectoryInfo
    {
        public string Path
        {
            get;
            set;
        }

        public long Size
        {
            get;
            set;
        }

        public DateTime LastModifiedTime
        {
            get;
            set;
        }

        public int Type
        {
            get;
            set;
        }
    }
}
