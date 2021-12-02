using Fortuna.crypt;
using Fortuna.rand;

using System;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class ClockEntropySource : EntropyCollector
    {
        private readonly Encryptor Encryptor = new Encryptor();

        public ClockEntropySource(Prng prng, byte sourceId) : base(prng, sourceId)
        {
            _ = GenerateEntropyAsync();
        }

        private async Task GenerateEntropyAsync()
        {
            byte[] seed = BitConverter.GetBytes(DateTime.Now.Ticks);
            byte[] key = new byte[32];
            Buffer.BlockCopy(seed, 0, key, 0, seed.Length);
            Buffer.BlockCopy(seed, 0, key, 7, seed.Length);
            Buffer.BlockCopy(seed, 0, key, 7 + 8, seed.Length);
            Buffer.BlockCopy(seed, 0, key, 7 + 8 + 8, seed.Length);
            seed = null;

            do
            {
                byte[] loopSeed = BitConverter.GetBytes(DateTime.Now.Second);

                Encryptor.UpdateKey(key);
                byte[] encryptedBytes = Encryptor.Encrypt(loopSeed);
                int delay = encryptedBytes[15];
                byte[] keyPart = encryptedBytes;
                byte[] newKey = new byte[32];
                Buffer.BlockCopy(key, 16, newKey, 0, 16);
                Buffer.BlockCopy(keyPart, 0, newKey, 16, keyPart.Length);
                key = newKey;

                await Task.Delay(delay).ConfigureAwait(false);
                EntropyReceived(BitConverter.GetBytes(DateTime.Now.Ticks));

            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }
    }
}