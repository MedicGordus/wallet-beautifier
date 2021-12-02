using static Fortuna.FortunaCore;

using Fortuna.crypt;
using Fortuna.rand;

using System;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class InsecureRandomEntropySource : EntropyCollector
    {
        private readonly Random Random = new Random();
        private readonly Encryptor Encryptor = new Encryptor();
        private readonly Hasher Hasher = new Hasher();

        public InsecureRandomEntropySource(Prng prng, byte sourceId) : base(prng, sourceId)
        {
            _ = GenerateEntropyAsync();
        }

        protected async Task GenerateEntropyAsync()
        {
            byte[] buffer = new byte[32];
            byte[] key = new byte[32];

            do
            {
                byte[] oldKey = key;
                Random.NextBytes(key);
                Encryptor.UpdateKey(Hasher.Compute(AppendByteArrays(key,oldKey)));

                Random.NextBytes(Encryptor.Encrypt(buffer));
                await Task.Delay(670).ConfigureAwait(false);
                EntropyReceived(buffer);
            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }
    }
}