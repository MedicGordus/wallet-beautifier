using System;

namespace wallet_beautifier.io.locks
{
    ///<summary>
    /// This class is intended to be used in a using block, to automatically lock and unlock an ILock.
    ///</summary>
    public class AutoUnlocker : IDisposable
    {
        private readonly ILock Lock;

        private readonly bool UnlockuponDisposal;

        ///<summary>
        /// Hidden constructor
        ///</summary>
        private AutoUnlocker(ILock lockInstance, bool unlockuponDisposal)
        {
            UnlockuponDisposal = unlockuponDisposal;
            Lock = lockInstance;
        }

        ///<summary>
        /// Disposal
        ///</summary>
        public void Dispose()
        {
            if(UnlockuponDisposal)
            {
                Lock.Unlock();
            }
        }

        ///<summary>
        /// Factory constructor.
        ///</summary>
        public static AutoUnlocker Create(ILock lockInstance, out bool wasAlreadyLocked)
        {
            bool thisInstanceHasLock = lockInstance.LockIfNotLocked();
            
            wasAlreadyLocked = !thisInstanceHasLock;

            return new AutoUnlocker(lockInstance, thisInstanceHasLock);
        }
    }
}