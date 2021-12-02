#if NET6_0_WINDOWS_OR_GREATER

using static Fortuna.FortunaCore;

using Fortuna.crypt;
using Fortuna.rand;

using System;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class KeyboardEntropySource : EntropyCollector
    {
        private Hasher Hasher = new Hasher();
        private int[] PossibleKeys;
        private WindowsServices.KeyStates[] KeyStates;
        private long[] KeyChangeTimestamps;
        public KeyboardEntropySource(Prng prng, byte sourceId) : base(prng, sourceId)
        {
            Array possibleKeys = Enum.GetValues(typeof(WindowsServices.Keys));
            PossibleKeys = new int[possibleKeys.Length];
            KeyStates = new WindowsServices.KeyStates[possibleKeys.Length];
            KeyChangeTimestamps = new long[possibleKeys.Length];
            for (int delta = 0; delta < possibleKeys.Length; delta++)
            {
                PossibleKeys[delta] = (int)possibleKeys.GetValue(delta);
            }

            _ = GenerateEntropyAsync();
        }

        private async Task GenerateEntropyAsync()
        {
            const int delay = 33;
            do
            {
                byte[] entropy = GetKeyboardBytes();

                await Task.Delay(delay).ConfigureAwait(false);

                if (entropy != null)
                    EntropyReceived(entropy);

            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }

        private byte[] GetKeyboardBytes()
        {
            byte[] buffer = new byte [] { };

            for (int delta = 0; delta < PossibleKeys.Length; delta++)
            {
                WindowsServices.KeyStates keyState = WindowsServices.GetStateOfKey(PossibleKeys[delta]);

                if (keyState != KeyStates[delta])
                {
                    long timeStamp = DateTime.Now.Ticks;
                    byte[] difference = BitConverter.GetBytes(timeStamp - KeyChangeTimestamps[delta]);
                    byte[] keyBytes = BitConverter.GetBytes(PossibleKeys[delta]);

                    buffer = AppendByteArrays
                             (
                                buffer,
                                new byte[] { keyBytes[0], keyBytes[1], difference[0], difference[1] }
                             );
                    KeyChangeTimestamps[delta] = timeStamp;
                    KeyStates[delta] = keyState;
                }
            }

            if (buffer.Length > 0)
            {
                if (buffer.Length <= 32)
                    return buffer;
                else
                    return Hasher.Compute(buffer);
            }

            return null;
        }
    }
}
#endif
