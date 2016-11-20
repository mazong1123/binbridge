using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinBridge.Ssh;
using System.IO;
using System.IO.Compression;

namespace BinBridge.Core.SyncStrategy
{
    public class FullSyncStrategy : IDirectorySyncStrategy
    {
        public void Sync(string sourceDirectory, string destDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
