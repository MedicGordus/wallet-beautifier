using wallet_beautifier.crypto;
using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.crypto.algorithms.secp256k1;
using wallet_beautifier.crypto.coin;
using wallet_beautifier.crypto.coin.bitcoin;
using wallet_beautifier.crypto.coin.ethereum;
using wallet_beautifier.crypto.coin.dogecoin;
using wallet_beautifier.crypto.fortuna;
using wallet_beautifier.io;
using wallet_beautifier.ux;

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_beautifier
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
#if DEBUG
                /// for test purposes only

                Console.WriteLine("* * * for test purposes, command arguments are hard coded * * *");

                args = new string[] {
                    "--terms=\"doge,dog\"",
                    "--tickers=\"doge\""
                };
#endif

                UxCore.ShareMessage(MessageType.WishToShare, "Hello, wallet-beautifier starting up...");

                if(!EnsurePathExists())
                {
                    UxCore.ShareMessage(MessageType.NeedToShare, "Unable to create base paths, terminating now...'");
                    Terminate();
                    return;
                }


                UxCore.ShareMessage(MessageType.WishToShare, "Parsing arguments (exact, terms, tickers)...");

                // Create a root command with some options
                var rootCommand = new RootCommand
                {
                    new Option<bool>(
                        "--exact",
                        getDefaultValue: () => false,
                        description: "True/false if the search for terms is case sensitive."),
                    new Option<string>(
                        "--terms",
                        getDefaultValue: () => null,
                        description: "Terms to search a wallet address for."),
                    new Option<string>(
                        "--tickers",
                        getDefaultValue: () => null,
                        description: "Tickers to search for (BTC, ETH, DOGE, etc.).")
                };
                rootCommand.Description = "wallet-beautifier";

                string inputTerms = "";
                string inputTickers = "";
                bool inputCaseSensitive = false;

                // Note that the parameters of the handler method are matched according to the names of the options
                rootCommand.Handler = CommandHandler.Create<string, string, bool>((terms, tickers, caseSensitive) =>
                {
                    inputTerms = terms;
                    inputTickers = tickers;
                    inputCaseSensitive = caseSensitive;
                });

                await rootCommand.InvokeAsync(args);

                UxCore.ShareMessage(MessageType.WishToShare, "Arguments parsed.");

                UxCore.ShareMessage(MessageType.WishToShare, "Loading in selected terms (--terms, seperate by comma without space)...");
                List<string> selectedTermsToSearchFor = new List<string>();
                selectedTermsToSearchFor = inputTerms.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                if(selectedTermsToSearchFor.Count == 0)
                {
                    UxCore.ShareMessage(MessageType.NeedToShare, "No terms selected, time to exit.");
                    Terminate();
                    return;
                }

                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "Done loading in selected terms, total {0} selected.",
                        selectedTermsToSearchFor.Count
                    )
                );

                UxCore.ShareMessage(MessageType.WishToShare, "Done loading in selected tickers.");

                UxCore.ShareMessage(MessageType.WishToShare, "Loading in selected tickers (--ticker, seperate by comma without space)...");

                string[] tickersSeperated = inputTickers.Split(',', StringSplitOptions.RemoveEmptyEntries);
                List<ICoin> tickersToCheck = new List<ICoin>();



                List<ICoin> tickersAvailable = new List<ICoin> {
                    new Bitcoin(),
                    new EthereumCoin(),
                    new Dogecoin()
                };
                foreach(ICoin deltaCoin in tickersAvailable)
                {
                    if(tickersSeperated.Any(_s => _s.Trim().ToUpper() == deltaCoin.GetTicker.ToUpper()))
                    {
                        tickersToCheck.Add(deltaCoin);

                        UxCore.ShareMessage(
                            MessageType.WishToShare,
                            string.Format(
                                "Added '{0}'",
                                deltaCoin.GetTicker
                            )
                        );
                    }
                }

                if(tickersToCheck.Count == 0)
                {
                    UxCore.ShareMessage(MessageType.NeedToShare, "No tickers selected, time to exit.");
                    Terminate();
                    return;
                }

                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "Done loading in selected tickers, total {0} selected.",
                        tickersToCheck.Count
                    )

                );



                UxCore.ShareMessage(MessageType.WishToShare, "Initializing Fortuna prng...");
                await FortunaInstance.InitializePrngAsync();
                UxCore.ShareMessage(MessageType.WishToShare, "Fortuna prng is initialized and ready to generate random keys.");

                int parallelThreadsToRun = RequestNumberOfParallelThreadsToRun();

                Task t = Task.Run(() => StartGenerationAsync(parallelThreadsToRun, selectedTermsToSearchFor , tickersToCheck, inputCaseSensitive));

                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "Keys are now generating on {0} parallel threads.",
                        parallelThreadsToRun
                    )
                );

                UxCore.WaitForExit();

                UxCore.ShareMessage(MessageType.ResponseAsRequested, "Shutting down, please wait...");

                StopGeneration();

                await t;

                Terminate();
            }
            catch (Exception e)
            {
                UxCore.ShareMessage(
                    MessageType.NeedToShare,
                    string.Format(
                        "Exception caught in Main(), message = '{0}'",
                        e.Message
                    )
                );
            }
        }

        static CancellationTokenSource GenerationCts = new CancellationTokenSource();
        static Dictionary<string, List<string>> SelectedTermsToSearchFor;
        static List<ICoin> TickersToCheck;
        static bool TermsCaseSensitive;

        static async Task StartGenerationAsync(int maxNumberOfThreads, List<string> selectedTermsToSearchFor, List<ICoin> tickersToCheck, bool termsCaseSensitive)
        {
            try
            {

                List<Task> parallelTasks = new List<Task>();
                SelectedTermsToSearchFor = new Dictionary<string, List<string>>();
                TickersToCheck = tickersToCheck;
                foreach(ICoin deltaCoin in TickersToCheck)
                {
                    SelectedTermsToSearchFor.Add(deltaCoin.GetTicker, new List<string>());

                    foreach(string deltaTerm in selectedTermsToSearchFor)
                    {
                        if(deltaCoin.CharactersAreAllowedInPublicAddress(deltaTerm))
                        {
                            SelectedTermsToSearchFor[deltaCoin.GetTicker].Add(deltaTerm);
                        }
                        else
                        {
                            UxCore.ShareMessage(
                                MessageType.WantToShare,
                                string.Format(
                                    "Coin {0} does not allow for one or more characters in the term, '{1}', so this term will be skipped.",
                                    deltaCoin.GetTicker,
                                    deltaTerm
                                )
                            );
                        }
                    }
                }
                TermsCaseSensitive = termsCaseSensitive;

                while(!GenerationCts.IsCancellationRequested)
                {
                    parallelTasks.Add(Task.Run(GenerateKeysAsync));

                    if(parallelTasks.Count > maxNumberOfThreads - 1)
                    {
                        foreach(Task deltaTask in parallelTasks)
                        {
                            await deltaTask;
                        }

                        parallelTasks.Clear();
                    }
                }
            }
            catch(Exception e)
            {
                UxCore.ShareMessage(
                    MessageType.NeedToShare,
                    string.Format(
                        "Exception caught in the primary thread handling the key generation, message = '{0}'",
                        e.Message
                    )
                );
            }

            UxCore.ShareMessage(MessageType.ResponseAsRequested, "Parallel threads are shut down.");
        }

        public static async Task GenerateKeysAsync()
        {
            byte[] privateKeyBytes = await CryptoCore.RetrieveRandomBytesAsync(32);
            byte[] publicKeyBytes = GetPublicKey(privateKeyBytes);

            //
            foreach(ICoin deltaCoin in TickersToCheck)
            {
                string address;

                // this is a hack needs to be expanded when more coins are added
                if(deltaCoin.GetTicker != "ETH")
                {
                    address = deltaCoin.GenerateAddressFromHashedPublicKey(CryptoCore.ComputeRipeMd160Hash(CryptoCore.ComputeSha256Hash(publicKeyBytes)));
                }
                else
                {
                    address = deltaCoin.GenerateAddressFromHashedPublicKey(publicKeyBytes);
                }

                string addressLower = address.ToLower();

                if(SelectedTermsToSearchFor[deltaCoin.GetTicker].Count > 0)
                {
                    foreach(string deltaTermToSearchFor in SelectedTermsToSearchFor[deltaCoin.GetTicker])
                    {
                        string deltaTermToSearchForLower = deltaTermToSearchFor.ToLower();

                        if(address.Contains(deltaTermToSearchFor))
                        {
                            UxCore.ShareMessage(
                                MessageType.ResponseAsRequested,
                                string.Format(
                                    "EXACT '{0}' coin {1} (native public key)/private (Base58): {2}/{3}",
                                    deltaTermToSearchFor,
                                    deltaCoin.GetTicker,
                                    address,
                                    Base58.Encode(privateKeyBytes)
                                )
                            );
                        }
                        else if(TermsCaseSensitive && addressLower.Contains(deltaTermToSearchForLower))
                        {
                            UxCore.ShareMessage(
                                MessageType.ResponseAsRequested,
                                string.Format(
                                    "SIMILAR '{0}' coin {1} (native public key)/private (Base58): {2}/{3}",
                                    deltaTermToSearchFor,
                                    deltaCoin.GetTicker,
                                    address,
                                    Base58.Encode(privateKeyBytes)
                                )
                            );
                        }
                    }
                }
            }
        }

        ///<summary>
        ///</summary>
        private static byte[] GetPublicKey(byte[] privateKey)
        {
            using(Secp256k1 s = new Secp256k1())
            {
                Span<byte> publicKeyCreated = new Span<byte>(new byte[64]);
                s.PublicKeyCreate(publicKeyCreated, privateKey);

                Span<byte> publicKeySerialized = new Span<byte>(new byte[65]);
                s.PublicKeySerialize(publicKeySerialized, publicKeyCreated);

                return publicKeySerialized.ToArray();
            }
        }

        static void StopGeneration()
        {
            UxCore.ShareMessage(MessageType.ResponseAsRequested, "Signalling parallel threads to cancel...");
            GenerationCts.Cancel();
        }

        static int RequestNumberOfParallelThreadsToRun()
        {
            Exception caughtException = new Exception("dummy exception");

            int output = 1;

            while(caughtException != null)
            {
                caughtException = null;

                try
                {
                    UxCore.ShareMessage(MessageType.RequestingInput,"Enter number of parallel threads to generate keys on: ");
                    while (!int.TryParse(UxCore.ReadLine(), out output))
                    {
                        UxCore.ShareMessage(MessageType.RequestingInput, "Unable to parse entry, please try again; enter number of parallel threads to generate keys on: ");
                    }

                    if(output < 1)
                    {
                        caughtException = new Exception("Unable to create that many threads.");
                    }
                }
                catch(Exception e)
                {
                    UxCore.ShareMessage(
                        MessageType.NeedToShare,
                        string.Format(
                            "Exception caught while requesting number of parallel threads to generate keys on, message = '{0}'",
                            e.Message
                        )
                    );

                    caughtException = e;
                }
            }

            return output;
        }


        static bool EnsurePathExists()
        {
            if(!IoCore.EnsureAllPathsExist())
            {
                return false;
            }

            return true;
        }

        static void Terminate()
        {
            UxCore.ShareMessage(MessageType.WishToShare, "wallet-beautifier exiting, goodbye!");
        }
    }
}
