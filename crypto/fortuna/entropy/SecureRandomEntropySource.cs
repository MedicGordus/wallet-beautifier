using static Fortuna.FortunaCore;

using Fortuna.crypt;
using Fortuna.rand;

using System.Threading.Tasks;
using System.Security.Cryptography;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class SecureRandomEntropySource : EntropyCollector
    {
        private readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        private readonly Encryptor Encryptor = new Encryptor();
        private readonly Hasher Hasher = new Hasher();

        public SecureRandomEntropySource(Prng prng, byte sourceId) : base(prng, sourceId)
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
                Rng.GetBytes(key);
                Encryptor.UpdateKey(Hasher.Compute(AppendByteArrays(key, oldKey)));

                Rng.GetBytes(Encryptor.Encrypt(buffer));
                await Task.Delay(480).ConfigureAwait(false);
                EntropyReceived(buffer);
            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }

    }
}