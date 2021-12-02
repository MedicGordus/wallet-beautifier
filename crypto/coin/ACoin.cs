using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.io;
using wallet_beautifier.io.locks;

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.coin
{
    public class KeyPair
    {
        public readonly string Address;

        public readonly byte[] PrivateKey;

        public KeyPair(string address, byte[] privateKey)
        {
            Address = address;
            PrivateKey = privateKey;
        }
/* don't overly complicate with constructor
        public static KeyPair Create(byte[] privateKey,string address) => new KeyPair(privateKey, address);
*/
    }

    public abstract class ACoin<S> where S : ICoin
    {
        public const int MAX_BUFFER_LINES = 50;

        public const int MAX_FILE_LINES = 50000;

        private static ConcurrentBag<KeyPair> KeyPairs;

        private static ParallelLineWriter LineWriter;

        public static BoolLock WritingLock = BoolLock.Create(); 

        protected ACoin(Func<string> getNextPath)
        {
            KeyPairs = new ConcurrentBag<KeyPair>();
            LineWriter = IoCore.CreateLineWriter(MAX_FILE_LINES, getNextPath);
        }

        public void Open() => LineWriter.Open();

        public async Task BufferKeyPairAsync(string address, byte[] privateKey)
        {
            KeyPairs.Add(new KeyPair(address, privateKey));

            if(KeyPairs.Count >= MAX_BUFFER_LINES)
            {
                using (WritingLock.GetAutoUnlocker(out bool wasAlreadyWriting))
                if(!wasAlreadyWriting)
                {
                    List<string> lineList = new List<string>();

                    while(KeyPairs.TryTake(out KeyPair pair))
                    {
                        lineList.Add(
                            string.Format(
                                "{0},{1}",
                                pair.Address,
                                Base58.Encode(pair.PrivateKey)
                            )
                        );
                    }

                    KeyPairs.Clear();

                    await LineWriter.WriteLinesAsync(lineList);
                }
            }
        }
        
        public Task CloseAsync() => LineWriter.CloseAsync();
    }
}