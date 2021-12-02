using Fortuna.rand;
using System.Threading;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal abstract class EntropyCollector : EntropySource
    {
        protected CancellationTokenSource Cts;

        public EntropyCollector(Prng prng, byte sourceId) : base(prng, sourceId)
        {
            Cts = new CancellationTokenSource();
        }

        internal void StopCollectingEntropy()
        {
            if (!Cts.IsCancellationRequested)
                Cts.Cancel();
        }

        ~EntropyCollector()
        {
            StopCollectingEntropy();
        }
    }
}
