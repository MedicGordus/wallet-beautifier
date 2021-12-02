#if NET6_0_WINDOWS_OR_GREATER
using Fortuna.rand;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class MouseEntropySource : EntropyCollector
    {
        public MouseEntropySource(Prng prng, byte sourceId) : base(prng, sourceId)
        {
            _ = GenerateEntropyAsync();
        }

        private async Task GenerateEntropyAsync()
        {
            const int delay = 100;
            do
            {
                byte[] entropy = GetMousePosBytes();

                await Task.Delay(delay).ConfigureAwait(false);

                if (entropy != null)
                    EntropyReceived(entropy);

            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }

        private Point PreviousPosition = new Point();
        private long PreviousCaptureTimeStamp = 0;

        private byte[] GetMousePosBytes()
        {
            Point p = WindowsServices.GetMousePosition();

            if (PreviousPosition.X != p.X | PreviousPosition.Y != p.Y)
            {
                long timeStamp = DateTime.Now.Ticks;
                byte[] difference = BitConverter.GetBytes(timeStamp - PreviousCaptureTimeStamp);

                PreviousCaptureTimeStamp = timeStamp;
                PreviousPosition = p;

                return new byte[] { BitConverter.GetBytes(p.X)[0], BitConverter.GetBytes(p.Y)[0], difference[0], difference[1] }; ;
            }

            return null;
        }
    }
}
#endif