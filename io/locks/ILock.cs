namespace wallet_beautifier.io.locks
{
    public interface ILock
    {

        ///<summary>
        /// Lock
        ///</summary>
        void Lock();

        ///<summary>
        /// Unlock the lock
        ///</summary>
        void Unlock();

        ///<summary>
        /// Returns if the block is locked or not
        ///</summary>
        bool IsLocked();

        ///<summary>
        /// If aready locked, returns false.
        /// If not locked locks the lock and returns true.
        ///</summary>
        bool LockIfNotLocked();

        ///<summary>
        /// Helper function, to be used in a using block to lock/unlock the lock.
        /// wasAlreadyLocked is set to the value of the lock before it was locked,
        ///     true = was already locked, false = was not locked.
        ///</summary>
        AutoUnlocker GetAutoUnlocker(out bool wasAlreadyLocked);
    }
}