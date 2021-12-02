using Fortuna.rand;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class InternetEntropySource : EntropyCollector
    {
        public InternetEntropySource(Prng prng, byte sourceId, string urlToPing) : base(prng, sourceId)
        {
            Cts = new CancellationTokenSource();

            _ = GenerateEntropyAsync(urlToPing);
        }

        protected async Task GenerateEntropyAsync(string urlToPing)
        {
            const int delay = 333;
            const int tenSecondDelay = 10000;
            int bigDelay = tenSecondDelay;
            const int hourDelay = 3600000;

            using (Ping pinger = new Ping())
            {
                do
                {

                    await Task.Delay(delay).ConfigureAwait(false);

                    PingReply reply = pinger.Send(urlToPing, 6000);
                    if (reply.Status == IPStatus.Success)
                    {
                        bigDelay = tenSecondDelay;
                        EntropyReceived(BitConverter.GetBytes(reply.RoundtripTime));
                    }
                    else
                    {
                        if(bigDelay < hourDelay)
                        {
                            bigDelay = (int)(bigDelay * 1.5);
                        }
                        await Task.Delay(bigDelay, Cts.Token).ConfigureAwait(false);
                    }
                } while (Cts == null || !Cts.Token.IsCancellationRequested);
            }
        }
    }
}