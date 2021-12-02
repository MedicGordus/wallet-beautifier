using wallet_beautifier.crypto.coin.bitcoin;
using wallet_beautifier.crypto.coin.dogecoin;
using wallet_beautifier.crypto.coin.ethereum;
using wallet_beautifier.crypto.algorithms.secp256k1;
using wallet_beautifier.io;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.coin
{
    public enum ByteStage : byte
    {
        z_error = 0,

        RawBytes = 1,

        Sha256RipeMd160 = 2
    }

    public static class CoinCore
    {
        private static List<ICoin> Coins = new List<ICoin> {
            new Bitcoin(),
            new Dogecoin(),
            new EthereumCoin()
        };

        private static Dictionary<ByteStage, List<ICoin>> CoinStages;

        public static bool EnsureAllPathsExist()
        {
            foreach(ICoin deltaCoin in Coins)
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

        public static async Task StartGenerationAsync(int threadCount)
        {
            CancellationTokenSource cts = KeyGenerationStopper;

            // setup stages and open files
            CoinStages = new Dictionary<ByteStage, List<ICoin>>();
            CoinStages.Add(ByteStage.RawBytes, new List<ICoin>());
            CoinStages.Add(ByteStage.Sha256RipeMd160, new List<ICoin>());
            List<Task> parallelTasks = new List<Task>();
            foreach(ICoin deltaCoin in Coins)
            {
                CoinStages[deltaCoin.GetByteStage].Add(deltaCoin);
                deltaCoin.Open();
            }

            while(!cts.Token.IsCancellationRequested)
            {
                /* while testing we use single core
                await GenerateKeysAsync();
                */
                
                parallelTasks.Add(Task.Run(GenerateKeysAsync));

                if(parallelTasks.Count > threadCount - 1)
                {
                    foreach(Task deltaTask in parallelTasks)
                    {
                        await deltaTask;
                    }

                    parallelTasks.Clear();
                }
            }
            
            foreach(ICoin deltaCoin in Coins)
            {
                await deltaCoin.CloseAsync();
            }
        }

        public static async Task GenerateKeysAsync()
        {
            byte[] privateKeyBytes = await CryptoCore.RetrieveRandomBytesAsync(32);
            byte[] publicKeyBytes = GetPublicKey(privateKeyBytes);

            // stage: ByteStage.RawBytes
            Task taskRawBytes = StoreGeneratedKeysAsync(CoinStages[ByteStage.RawBytes], privateKeyBytes, publicKeyBytes);

            // stage: ByteStage.Sha256RipeMd160
            publicKeyBytes = CryptoCore.ComputeRipeMd160Hash(CryptoCore.ComputeSha256Hash(publicKeyBytes));
            Task taskSha256RipeMd160 =  StoreGeneratedKeysAsync(CoinStages[ByteStage.Sha256RipeMd160], privateKeyBytes, publicKeyBytes);

            // ensure everything is completed before we exit
            await taskRawBytes;
            await taskSha256RipeMd160;
        }

        private static async Task StoreGeneratedKeysAsync(List<ICoin> coins, byte[] privateKeyBytes, byte[] publicKeyBytes)
        {
            if(coins.Count == 0)
            {
                return;
            }

            foreach(ICoin deltaCoin in coins.AsParallel())
            {
                await deltaCoin.BufferKeyPairAsync(deltaCoin.GenerateAddressFromHashedPublicKey(publicKeyBytes), privateKeyBytes);
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
    }
}