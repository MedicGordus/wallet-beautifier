using System.Threading;

namespace Fortuna.rand
{
    public abstract class EntropySource
    {
        private readonly Prng Prng;

        public readonly byte SourceId;

        private int LastPoolNumberUsed;

        public EntropySource(Prng prng, byte sourceId)
        {
            Prng = prng;
            SourceId = sourceId;
            LastPoolNumberUsed = -1;
        }

        public void EntropyReceived(byte[] data)
        {
            Prng.AddRandomEvent(SourceId, GetNextPoolNumberAndIncrement(), data);
        }

        private int GetNextPoolNumberAndIncrement()
        {
            // wrap back to (FIRST - 1) if we used the last pool number
            Interlocked.CompareExchange(ref LastPoolNumberUsed, Pool.FIRST - 1, Pool.LAST);

            // increment by one
            return Interlocked.Increment(ref LastPoolNumberUsed);
        }
    }
}
