using System.Threading;

namespace wallet_beautifier.io.locks
{
    public class BoolLock : ILock
    {
        private const int UNLOCKED = 0;
        private const int LOCKED = 1;

        ///<summary>
        /// This is the lock, when set to 1 it is locked,
        /// when set to 0 it is unlocked.
        ///</summary>
        private int LockInt;

        ///<summary>
        /// Hidden constructor.
        ///</summary>
        private BoolLock(int value)
        {
            LockInt = value;
        }

        ///<summary>
        /// Lock
        ///</summary>
        public void Lock() => Interlocked.Exchange(ref LockInt, LOCKED);

        ///<summary>
        /// Unlock the lock
        ///</summary>
        public void Unlock() => Interlocked.Exchange(ref LockInt, UNLOCKED);

        ///<summary>
        /// Returns if the block is locked or not
        ///</summary>
        public bool IsLocked() => LockInt == LOCKED;


        ///<summary>
        /// If aready locked, returns false.
        /// If not locked locks the lock and returns true.
        ///</summary>
        public bool LockIfNotLocked()
        {
            int oldValue = Interlocked.Exchange(ref LockInt, LOCKED);

            return oldValue == UNLOCKED;
        }

        ///<summary>
        /// Helper function, to be used in a using block to lock/unlock the lock.
        /// wasAlreadyLocked is set to the value of the lock before it was locked,
        ///     true = was already locked, false = was not locked.
        ///</summary>
        public AutoUnlocker GetAutoUnlocker(out bool wasAlreadyLocked) => AutoUnlocker.Create(this, out wasAlreadyLocked);

        ///<summary>
        /// Factory constructor.
        ///</summary>
        public static BoolLock Create(bool startLocked = false) => new BoolLock(startLocked == true ? LOCKED : UNLOCKED);
    }
}