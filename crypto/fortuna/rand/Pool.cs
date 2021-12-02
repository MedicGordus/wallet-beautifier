using static Fortuna.FortunaCore;

using System;
using System.Threading;
using Fortuna.crypt;

namespace Fortuna.rand
{
    internal class Pool
    {
        internal const int FIRST = 0;
        internal const int LAST = 31;

        protected const int MAX_POOL_SIZE = 2048; // count of bytes
        protected const int MIN_POOL_SIZE = 64; // 64 bytes

        protected byte[] Value;

        protected readonly object Locker = new object();
        protected readonly Hasher Hasher;

        public Pool(Hasher hasher)
        {
            Hasher = hasher;
            Value = new byte[] { };
        }

        internal bool ReadyToReseed()
        {
            return Value.Length >= MIN_POOL_SIZE;
        }

        internal byte[] RetrieveValueAndReset()
        {
            lock (Locker)
            {
                return Interlocked.Exchange(ref Value, new byte[] { });
            }
        }

        internal void AppendEventData(byte[] dataToAppend)
        {
            int lengthAddingTo = dataToAppend.Length > Hasher.HASH_BYTE_LENGTH ? dataToAppend.Length : Hasher.HASH_BYTE_LENGTH;
            if (dataToAppend.Length + lengthAddingTo > MAX_POOL_SIZE)
            {
                throw new OverflowException(
                    string.Format(
                        "Event data length too large for pool, '{0}'. Maximum size is {1}.",
                        dataToAppend.Length,
                        MAX_POOL_SIZE - lengthAddingTo
                    )
                );
            }

            lock (Locker)
            {
                if(Value.Length + dataToAppend.Length > MAX_POOL_SIZE)
                {
                    Value = AppendByteArrays(Hasher.Compute(Value), dataToAppend);
                }
                else
                {
                    Value = AppendByteArrays(Value, dataToAppend);
                }
            }
        }
    }
}
