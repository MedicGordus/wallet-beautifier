using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.crypto.algorithms.curve25519;
using wallet_beautifier.crypto.algorithms.secp256k1;
using wallet_beautifier.crypto.coin.bitcoin;
using wallet_beautifier.crypto.coin.cardano;
using wallet_beautifier.crypto.coin.dogecoin;
using wallet_beautifier.crypto.coin.ethereum;
using wallet_beautifier.io;
using wallet_beautifier.ux;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.coin
{
    public enum CurveType : byte
    {
        z_error = 0,
        Secp256k1 = 1,
        Curve25519 = 2
    }

    public enum PostCalculationType : byte
    {
        z_error = 0,

        None = 1,

        ///<summary>
        /// Used by BTC.
        ///</summary>
        Sha256RipeMd160AndChecksumVersion0 = 2,

        ///<summary>
        /// Used by DOGE.
        ///</summary>
        Sha256RipeMd160AndChecksumVersion30 = 3,

        ///<summary>
        /// Used by ETH.
        ///</summary>
        Keccak256 = 4,

        ///<summary>
        /// Used by ADA --> I have no idea if it uses 224 or 256, so until then we have or here.
        ///</summary>
        Black2b224 = 5
    }

    public static class CoinCore
    {
        private static Dictionary<CurveType, List<ICoin>> CoinCurves;

        public static bool EnsureAllPathsExist(List<ICoin> tickersToCheck)
        {
            foreach(ICoin deltaCoin in tickersToCheck)
            {
                if(!IoCore.EnsurePathExists(deltaCoin.GetAttemptPath))
                {
                    return false;
                }
            }

            return true;
        }

        private static CancellationTokenSource KeyGenerationStopper = new CancellationTokenSource();

        public static void StopGeneration()
        {
            KeyGenerationStopper.Cancel();

            KeyGenerationStopper = new CancellationTokenSource();
        }

        public static async Task StartGenerationAsync(int threadCount, List<string> selectedTermsToSearchFor, List<ICoin> tickersToCheck, bool termsCaseSensitive)
        {
            CancellationTokenSource cts = KeyGenerationStopper;

            // setup stages and open files
            CoinCurves = new Dictionary<CurveType, List<ICoin>>();

            List<Task> parallelTasks = new List<Task>();
            foreach(ICoin deltaCoin in tickersToCheck)
            {
                // add in the coins into their appropriate dictionaries
                CurveType curve = deltaCoin.GetCurveType;
                if(!CoinCurves.ContainsKey(curve))
                {
                    CoinCurves.Add(
                        curve,
                        new List<ICoin> {
                            deltaCoin
                        }
                    );
                }
                else
                {
                    CoinCurves[curve].Add(deltaCoin);
                }

                deltaCoin.Open();
            }

            while(!cts.Token.IsCancellationRequested)
            {
                /* while testing we use single core
                await GenerateKeysAsync();
                */
                
                parallelTasks.Add(Task.Run(GenerateAndStoreKeysAsync));

                if(parallelTasks.Count > threadCount - 1)
                {
                    foreach(Task deltaTask in parallelTasks)
                    {
                        await deltaTask;
                    }

                    parallelTasks.Clear();
                }
            }
            
            foreach(ICoin deltaCoin in tickersToCheck)
            {
                await deltaCoin.CloseAsync();
            }
        }


        public static async Task StartKeyMatchingAsync(int threadCount, List<string> selectedTermsToSearchFor, List<ICoin> tickersToCheck, bool termsCaseSensitive)
        {
            CancellationTokenSource cts = KeyGenerationStopper;

            // setup stages and open files
            CoinCurves = new Dictionary<CurveType, List<ICoin>>();

            Dictionary<string, List<string>> selectedTermsToSearchForByToken = new Dictionary<string, List<string>>();

            List<Task> parallelTasks = new List<Task>();
            foreach(ICoin deltaCoin in tickersToCheck)
            {
                // add in the coins into their appropriate dictionaries
                CurveType curve = deltaCoin.GetCurveType;
                if(!CoinCurves.ContainsKey(curve))
                {
                    CoinCurves.Add(
                        curve,
                        new List<ICoin> {
                            deltaCoin
                        }
                    );
                }
                else
                {
                    CoinCurves[curve].Add(deltaCoin);
                }
                // filter out disallowed characters
                selectedTermsToSearchForByToken.Add(deltaCoin.GetTicker, new List<string>());

                foreach(string deltaTerm in selectedTermsToSearchFor)
                {
                    if(deltaCoin.CharactersAreAllowedInPublicAddress(deltaTerm, termsCaseSensitive))
                    {
                        selectedTermsToSearchForByToken[deltaCoin.GetTicker].Add(deltaTerm);
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

            // verify we have at least one search term
            int totalTerms = (
                from
                    _item in selectedTermsToSearchForByToken
                select
                    _item.Value.Count
            ).Sum();
            if(totalTerms == 0)
            {
                UxCore.ShareMessage(
                    MessageType.ResponseAsRequested,
                    "No allowed terms found, exiting search..."
                );

                return;
            }

            // start key matching
            while(!cts.Token.IsCancellationRequested)
            {
                parallelTasks.Add(
                    Task.Run(
                        () => GenerateAndCheckKeysAsync(selectedTermsToSearchForByToken, termsCaseSensitive)
                    )
                );

                if(parallelTasks.Count > threadCount - 1)
                {
                    foreach(Task deltaTask in parallelTasks)
                    {
                        await deltaTask;
                    }

                    parallelTasks.Clear();
                }
            }
        }
        

        public static async Task GenerateAndStoreKeysAsync()
        {
            byte[] privateKeyBytes = await CryptoCore.RetrieveRandomBytesAsync(32);
            Dictionary<CurveType, Dictionary<PostCalculationType, string>> publicKeyBytesByCurveAndPostCalculationType =  new Dictionary<CurveType, Dictionary<PostCalculationType, string>>();

            foreach(KeyValuePair<CurveType, List<ICoin>> deltaCoinCurveEntry in CoinCurves)
            {
                if(!publicKeyBytesByCurveAndPostCalculationType.ContainsKey(deltaCoinCurveEntry.Key))
                {
                    publicKeyBytesByCurveAndPostCalculationType.Add(deltaCoinCurveEntry.Key, new Dictionary<PostCalculationType, string>());
                }

                foreach(ICoin deltaCoin in deltaCoinCurveEntry.Value)
                {
                    publicKeyBytesByCurveAndPostCalculationType[deltaCoinCurveEntry.Key].Add(
                        deltaCoin.GetPostCalculationType,
                        CalculatePublicKey(
                            deltaCoin.GetCurveType,
                            deltaCoin.GetPostCalculationType,
                            privateKeyBytes
                        )
                    );
                }
            }


            // ensure everything is completed before we exit
            await StoreGeneratedKeysAsync(CoinCurves, privateKeyBytes, publicKeyBytesByCurveAndPostCalculationType);
        }

        public static async Task GenerateAndCheckKeysAsync(Dictionary<string, List<string>> selectedTermsToSearchForByToken, bool termsCaseSensitive)
        {
            byte[] privateKeyBytes = await CryptoCore.RetrieveRandomBytesAsync(32);
            Dictionary<CurveType, Dictionary<PostCalculationType, string>> publicKeyBytesByCurveAndPostCalculationType =  new Dictionary<CurveType, Dictionary<PostCalculationType, string>>();

            foreach(KeyValuePair<CurveType, List<ICoin>> deltaCoinCurveEntry in CoinCurves)
            {
                if(!publicKeyBytesByCurveAndPostCalculationType.ContainsKey(deltaCoinCurveEntry.Key))
                {
                    publicKeyBytesByCurveAndPostCalculationType.Add(deltaCoinCurveEntry.Key, new Dictionary<PostCalculationType, string>());
                }

                foreach(ICoin deltaCoin in deltaCoinCurveEntry.Value)
                {
                    publicKeyBytesByCurveAndPostCalculationType[deltaCoinCurveEntry.Key].Add(
                        deltaCoin.GetPostCalculationType,
                        CalculatePublicKey(
                            deltaCoin.GetCurveType,
                            deltaCoin.GetPostCalculationType,
                            privateKeyBytes
                        )
                    );
                }
            }

            // ensure everything is completed before we exit
            CheckGeneratedKeysAndRespond(CoinCurves, privateKeyBytes, publicKeyBytesByCurveAndPostCalculationType, selectedTermsToSearchForByToken, termsCaseSensitive);
        }

        ///<summary>
        /// [unused] This method is for storing every single key that is generated.
        ///</summary>
        ///<remarks>
        /// This was used for generating tons of keys that another tool came and scraped the ones that matched criteria - you can modify code to do this for you as well.
        ///</remarks>
        private static async Task StoreGeneratedKeysAsync(Dictionary<CurveType, List<ICoin>> coinEntries, byte[] privateKeyBytes, Dictionary<CurveType, Dictionary<PostCalculationType, string>> publicKeyBytesByCurveAndPostCalculationType)
        {
            if(coinEntries.Count == 0)
            {
                return;
            }

            foreach(KeyValuePair<CurveType, List<ICoin>> deltaCoinEntry in coinEntries.AsParallel())
            {
                foreach(ICoin deltaCoin in deltaCoinEntry.Value)
                {
                    await deltaCoin.BufferKeyPairAsync(deltaCoin.GenerateAddressFromCalculatedPublicKey(publicKeyBytesByCurveAndPostCalculationType[deltaCoinEntry.Key][deltaCoin.GetPostCalculationType]), deltaCoin.TweakPrivateKey(privateKeyBytes));
                }
            }
        }

        ///<summary>
        /// .
        ///</summary>
        private static void CheckGeneratedKeysAndRespond(Dictionary<CurveType, List<ICoin>> coinEntries, byte[] privateKeyBytes, Dictionary<CurveType, Dictionary<PostCalculationType, string>> publicKeyBytesByCurveAndPostCalculationType, Dictionary<string, List<string>> selectedTermsToSearchForByToken, bool termsCaseSensitive)
        {
            if(coinEntries.Count == 0)
            {
                return;
            }

            foreach(KeyValuePair<CurveType, List<ICoin>> deltaCoinEntry in coinEntries.AsParallel())
            {
                foreach(ICoin deltaCoin in deltaCoinEntry.Value)
                {
                    string address = deltaCoin.GenerateAddressFromCalculatedPublicKey(publicKeyBytesByCurveAndPostCalculationType[deltaCoinEntry.Key][deltaCoin.GetPostCalculationType]);

                    string addressLower = null;
                    
                    if(termsCaseSensitive)
                    {
                        addressLower = address.ToLower();
                    }

                    if(selectedTermsToSearchForByToken[deltaCoin.GetTicker].Count > 0)
                    {
                        foreach(string deltaTermToSearchFor in selectedTermsToSearchForByToken[deltaCoin.GetTicker])
                        {
                            string deltaTermToSearchForLower = deltaTermToSearchFor.ToLower();

                            if(address.Contains(deltaTermToSearchFor))
                            {
                                UxCore.ShareMessage(
                                    MessageType.ResponseAsRequested,
                                    string.Format(
                                        "EXACT '{0}' coin {1} (native address/public key)/private (Base58): {2}/{3}",
                                        deltaTermToSearchFor,
                                        deltaCoin.GetTicker,
                                        address,
                                        Base58.Encode(deltaCoin.TweakPrivateKey(privateKeyBytes))
                                    )
                                );
                            }
                            else if(termsCaseSensitive && addressLower.Contains(deltaTermToSearchForLower))
                            {
                                UxCore.ShareMessage(
                                    MessageType.ResponseAsRequested,
                                    string.Format(
                                        "SIMILAR '{0}' coin {1} (native address/public key)/private (Base58): {2}/{3}",
                                        deltaTermToSearchFor,
                                        deltaCoin.GetTicker,
                                        address,
                                        Base58.Encode(deltaCoin.TweakPrivateKey(privateKeyBytes))
                                    )
                                );
                            }
                        }
                    }
                }
            }
        }

        private static string CalculatePublicKey(CurveType curveType, PostCalculationType postCalculationType, byte[] privateKey)
        {
            byte[] rawPublicKeyBytes = GetRawPublicKey(curveType, privateKey);

            string output = null;

            switch(postCalculationType)
            {
                case PostCalculationType.Sha256RipeMd160AndChecksumVersion0:
                case PostCalculationType.Sha256RipeMd160AndChecksumVersion30:
                    byte[] ripeShaHash = CryptoCore.ComputeRipeMd160Hash(CryptoCore.ComputeSha256Hash(rawPublicKeyBytes));

                    byte[] versionedHash = new byte[ripeShaHash.Length + 1];
            
                    // set version byte (note: for verison 0, it is already 0, so we do nothing)
                    if(postCalculationType == PostCalculationType.Sha256RipeMd160AndChecksumVersion30)
                    {
                        versionedHash[0] = 30;
                    }

                    // copy into the new array (with version)
                    Buffer.BlockCopy(ripeShaHash, 0, versionedHash, 1, ripeShaHash.Length);
                    
                    // create checksum
                    byte[] checkSumBytes = CryptoCore.ComputeDoubleSha256Hash(versionedHash);
                    
                    // concatenate on the end of the ripemd160
                    byte[] addressBytes = new byte[versionedHash.Length + 4];
                    Buffer.BlockCopy(versionedHash, 0, addressBytes, 0, versionedHash.Length);
                    Buffer.BlockCopy(checkSumBytes, 0, addressBytes, versionedHash.Length, 4);

                    output = Base58.Encode(addressBytes);

                    break;

                case PostCalculationType.Keccak256:
                    if(rawPublicKeyBytes.Length != 65) throw new ArgumentException("publicKey size incorrect, must be exactly 65 bytes as it should be serialized from a secp256k1 eliptical curve public key.");

                    byte[] relevantKeccakBytes = new byte[rawPublicKeyBytes.Length - 1];
                    Buffer.BlockCopy(rawPublicKeyBytes, 1, relevantKeccakBytes, 0 , relevantKeccakBytes.Length);

                    output = CryptoCore.ComputeKeccak256Hash(relevantKeccakBytes);

                    break;
                
                case PostCalculationType.Black2b224:
                    byte[] blakeHashedBytes = CryptoCore.ComputeBlake2b224Hash(rawPublicKeyBytes);
                    
                    output = Bech32.EncodeAddress(blakeHashedBytes);
                    break;
            }

            return output;
        }

        ///<summary>
        /// Calculates raw public key (byte array form), for a given curve and private key.
        ///</summary>
        private static byte[] GetRawPublicKey(CurveType curveType, byte[] privateKey)
        {
            if(curveType == CurveType.Secp256k1)
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
            else if(curveType == CurveType.Curve25519)
            {
                return Curve25519Core.CalculatePublicKey(privateKey);
            }
            else
            {
                throw new ArgumentException(
                    string.Format(
                        "Curve type '{0}' not supported, expected Secp256k1 or Curve25519",
                        curveType
                    )
                );
            }
        }
    }
}