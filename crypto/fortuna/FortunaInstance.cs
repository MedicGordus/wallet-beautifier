using Fortuna.rand;

using wallet_beautifier.io;
using wallet_beautifier.crypto.fortuna.entropy;
using wallet_beautifier.ux;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace wallet_beautifier.crypto.fortuna
{
    public static class FortunaInstance
    {
        internal static Prng Rng;
        
        private static readonly List<EntropyCollector> Entropies = new List<EntropyCollector>();

        internal static readonly string SEED_PATH = IoCore.AppendOnRootPath("fortuna.seed");

        internal static readonly string IO_PATH = IoCore.AppendOnRootPath("fortuna.io");

        private static async Task<Prng> PreparePrngAsync()
        {
            Prng output = Prng.InitializeBlank();

            List<EntropyCollector> entropyCollectors = RequestEntropySources(output);

            if(entropyCollectors == null || entropyCollectors.Count == 0)
            {
                string errorMessage = "Unable to start prng, no entropy collectors to seed from!";
                UxCore.ShareMessage(MessageType.NeedToShare, errorMessage);
                throw new ArgumentException(errorMessage);
            }

            foreach(EntropyCollector deltaEntropyCollector in entropyCollectors)
            {
                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "Adding Entropy Source, type of '{0}'...",
                        deltaEntropyCollector.GetType()
                    )
                );
                Entropies.Add(deltaEntropyCollector);
            }

            await Prng.SeedFromFileAsync(SEED_PATH, output).ConfigureAwait(false);

            return output;
        }

        private static List<EntropyCollector> RequestEntropySources(Prng prng)
        {
            bool keepAskingForMore = true;

            List<EntropyCollector> output = new List<EntropyCollector>();

            UxCore.ShareMessage(MessageType.NeedToShare, "Selecting entropy sources...");

            byte sourceId = 0;

            while(keepAskingForMore)
            {
                try
                {
                    UxCore.ShareMessage(MessageType.RequestingInput, "Select type of new entropy source ('k'eyboard, 'm'ouse, 'c'lock, 'f'ile, 'i'nsecure, 'w'eb, 's'ecure), or 'done' to continue with how many there are: ");
                    string selection = UxCore.ReadLine();

                    switch(selection.ToLower())
                    {
                        case "k":
#if NET6_0_WINDOWS_OR_GREATER
                            output.Add(new KeyboardEntropySource(prng, sourceId));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Keyboard entropy source added as requested.");
#else
                            UxCore.ShareMessage(MessageType.WantToShare, "Cannot create keyboard based entropy, right now the keyboard input library only supports Microsoft Windows.");
#endif
                            break;


                        case "m":
#if NET6_0_WINDOWS_OR_GREATER
                            output.Add(new MouseEntropySource(prng, sourceId));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Mouse entropy source added as requested.");
#else
                            UxCore.ShareMessage(MessageType.WantToShare, "Cannot create mouse based entropy, right now the mouse input library only supports Microsoft Windows.");
#endif
                            break;


                        case "c":
                            output.Add(new ClockEntropySource(prng, sourceId));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Clock entropy source added as requested.");

                            break;


                        case "f":
                            string filePath = GetInput("file path for file entropy (do not use SSD if possible, wear and tear is strong)");
                            output.Add(new FileEntropySource(prng, sourceId, filePath));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Filepath entropy source added as requested.");

                            break;


                        case "i":
                            output.Add(new InsecureRandomEntropySource(prng, sourceId));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Insecure entropy source added as requested.");

                            break;


                        case "w":
                            string webAddress = GetInput("dns address for web entropy (will ping this address)");
                            output.Add(new InternetEntropySource(prng, sourceId, webAddress));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Web entropy source added as requested.");

                            break;


                        case "s":
                            output.Add(new SecureRandomEntropySource(prng, sourceId));
                            sourceId++;
                            UxCore.ShareMessage(MessageType.WishToShare, "Secure entropy source added as requested.");

                            break;


                        case "done":
                            keepAskingForMore = false;
                            break;
                    }
                }
                catch(Exception e)
                {
                    UxCore.ShareMessage(
                        MessageType.NeedToShare,
                        string.Format(
                            "Exception caught while requesting entropy sources, message = '{0}'",
                            e.Message
                        )
                    );
                }

                // notify how many are selected
                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "{0} entropy sources are configured.",
                        output.Count
                    )
                );

                if(keepAskingForMore == false & output.Count < 5)
                {
                    // notify must select one at least
                    UxCore.ShareMessage(MessageType.WantToShare, "Cannot continue with less than five entropy sources, please try again.");
                    keepAskingForMore = true;
                }

                if(sourceId == 255)
                {
                    // notify that is the max sources allowed
                    UxCore.ShareMessage(MessageType.WantToShare, "Reached 255 sources, cannot add more.");
                    keepAskingForMore = false;
                }
            }

            return output;
        }

        
        private static string GetInput(string requestedInput)
        {
            Exception caughtException = new Exception("dummy exception");

            string output = "";

            while(caughtException != null)
            {
                caughtException = null;

                try
                {
                    UxCore.ShareMessage(
                        MessageType.ResponseAsRequested,
                        string.Format(
                            "Enter {0}: ",
                            requestedInput
                        )
                    );
                    output = UxCore.ReadLine();
                }
                catch(Exception e)
                {
                    UxCore.ShareMessage(
                        MessageType.NeedToShare,
                        string.Format(
                            "Exception caught while requesting {0}, message = '{1}'",
                            requestedInput,
                            e.Message
                        )
                    );

                    caughtException = e;
                }
            }

            return output;
        }

        internal static async Task InitializePrngAsync()
        {
            UxCore.ShareMessage(MessageType.WishToShare, "Initializing PRNG, stand by...");
            Rng = await PreparePrngAsync().ConfigureAwait(false);
            UxCore.ShareMessage(MessageType.WishToShare, "PRNG initialized.");

            UxCore.ShareMessage(MessageType.WishToShare, "Waiting for entropy to collect...");
            do
            {
                await Task.Delay(100);
            } while (!Rng.ReadyToGenerateRandomData());
            UxCore.ShareMessage(MessageType.WishToShare, "Ready to generate random data");
        }
    }
}