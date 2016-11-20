using BinBridge.Core.SyncStrategy;
using BinBridge.Ssh;

namespace BinBridge.Core
{
    public class DirectorySyncer
    {
        private IDirectorySyncStrategy syncStrategy = null;

        protected DirectorySyncer(IDirectorySyncStrategy syncStrategy)
        {
            this.syncStrategy = syncStrategy;
        }

        public IDirectorySyncStrategy DirectorySyncStrategy
        {
            get
            {
                return this.syncStrategy;
            }

            set
            {
                this.syncStrategy = value;
            }
        }


        public void Sync(string sourceDirectory, string destDirectory, IDirectorySyncStrategy syncStrategy = null)
        {
            if (syncStrategy == null)
            {
                syncStrategy = this.syncStrategy;
            }

            if (syncStrategy == null)
            {
                throw new System.ArgumentException("Sync strategy not specified.");
            }

            syncStrategy.Sync(sourceDirectory, destDirectory);
        }
    }
}
