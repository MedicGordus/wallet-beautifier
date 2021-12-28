using wallet_beautifier.crypto;
using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.crypto.algorithms.secp256k1;
using wallet_beautifier.crypto.coin;
using wallet_beautifier.crypto.coin.bitcoin;
using wallet_beautifier.crypto.coin.cardano;
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
                    "--terms=\"n54n\"",
                    "--tickers=\"ada\""
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
                    new Dogecoin(),
                    new Cardano()
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

                Task t = Task.Run(() => CoinCore.StartKeyMatchingAsync(parallelThreadsToRun, selectedTermsToSearchFor , tickersToCheck, inputCaseSensitive));

                UxCore.ShareMessage(
                    MessageType.WishToShare,
                    string.Format(
                        "Keys are now generating on {0} parallel threads.",
                        parallelThreadsToRun
                    )
                );

                UxCore.WaitForExit();

                UxCore.ShareMessage(MessageType.ResponseAsRequested, "Shutting down, please wait...");

                CoinCore.StopGeneration();

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
