using BinBridge.Ssh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinBridge.Core.SyncStrategy
{
    public interface IDirectorySyncStrategy
    {
        void Sync(string sourceDirectory, string destDirectory);
    }
}
