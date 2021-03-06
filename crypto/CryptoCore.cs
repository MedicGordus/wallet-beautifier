using wallet_beautifier.crypto.fortuna;

using SHA3Core.Keccak;

using Blake2Sharp;

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto
{
    public static class CryptoCore
    {
        ///<summary>
        /// Non SHA3 variant of Keccak algorithm. THERE IS A DIFFERENCE.
        ///</summary>
        ///<returns>
        /// Hash in string format (1 byte = 2 char/hex)
        ///</returns>
        ///<remarks>
        // note that this library returns some bits as 0 if you do not create a new hash object each time
        //
        // --> this has occured specifically when parallel threading and I am worried it still happens randomly although I haven't observed it.
        //
        //  Once I find a better library I will switch.
        //
        //  I tried SeriesOne among others but they only support SHA3 variant which is useless to us.
        ///</remarks>
        internal static string ComputeKeccak256Hash(byte[] input)
        {
            var h = new Keccak(SHA3Core.Enums.KeccakBitType.K256);
            return h.Hash(input);
        }

        ///<summary>
        /// RipeMd160.
        ///</summary>
        internal static byte[] ComputeRipeMd160Hash(byte[] input)
        {
            using(RIPEMD160 hasher = new RIPEMD160Managed())
            {
                return hasher.ComputeHash(input);
            }
        }

        ///<summary>
        /// SHA2-256.
        ///</summary>
        internal static byte[] ComputeSha256Hash(byte[] input)
        {
            using (SHA256 hasher = SHA256.Create())
            {
                return hasher.ComputeHash(input);
            }
        }

        private static readonly Blake2BConfig BLAKE2B_224_CONFIG = new Blake2BConfig {
            OutputSizeInBits = 224,
            OutputSizeInBytes = 28
        };

        private static readonly Blake2BConfig BLAKE2B_256_CONFIG = new Blake2BConfig {
            OutputSizeInBits = 256,
            OutputSizeInBytes = 32
        };


/*
        ///<summary>
        /// unused.
        ///</summary>
        internal static byte[] ComputeBlake2b224Sha3256Hash(byte[] input)
        {

            return Blake2B.ComputeHash(
                Convert.FromHexString(
                    new Keccak(SHA3Core.Enums.KeccakBitType.K256).Hash(
                        input
                    )
                ),
                BLAKE2B_224_CONFIG
            );
        }
*/

        ///<summary>
        /// Blake2b-224.
        ///
        /// Used by cardano as per
        ///     https://hydra.iohk.io/build/8201171/download/1/ledger-spec.pdf
        ///</summary>
        ///<remarks>
        /// Cannot find vectors for 224, so we tested 512 and pass
        ///</remarks>
        internal static byte[] ComputeBlake2b224Hash(byte[] input)
        {
            return Blake2B.ComputeHash(input, BLAKE2B_224_CONFIG);
        }

        ///<summary>
        /// Blake2b-256.
        ///
        /// Used by cardano as per
        ///     https://hydra.iohk.io/build/8201171/download/1/ledger-spec.pdf
        ///</summary>
        ///<remarks>
        /// Cannot find vectors for 256, so we tested 512 and pass
        ///</remarks>
        internal static byte[] ComputeBlake2b256Hash(byte[] input)
        {
            return Blake2B.ComputeHash(input, BLAKE2B_256_CONFIG);
        }

        ///<remarks>
        /// Used to test BLACK2B library against vectors:
        ///     https://github.com/emilbayes/blake2b/blob/master/test-vectors.json
        ///</remarks>
        internal static byte[] ComputeBlake2b512Hash(byte[] input)
        {
            return Blake2B.ComputeHash(
                input,
                new Blake2BConfig {
                    OutputSizeInBits = 512,
                    OutputSizeInBytes = 64
                }
            );
        }
        
        ///<summary>
        /// SHA2-256, 2x.
        ///</summary>
        internal static byte[] ComputeDoubleSha256Hash(byte[] input)
        {
            using (SHA256 hasher = SHA256.Create())
            {
                return hasher.ComputeHash(hasher.ComputeHash(input));
            }
        }


        private static object rngLock = new object();

        ///<summary>
        /// CSPRNG bytes.
        ///</summary>
        public static byte[] RetrieveRandomBytes(int length)
        {
/* way the hell too slow
            byte[] output = new byte[length];

            ThreadLocalRandomNumberGenerator.Instance.GetBytes(output);

            return output;
*/

/* fortuna isn't threadsafe, but worst case we get two private keys that are the same
            lock(rngLock)
            {
                return FortunaInstance.Rng.RandomData(length);
            }
*/
            return FortunaInstance.Rng.RandomData(length);
        }

        private static SemaphoreSlim RngLock = new SemaphoreSlim(1,1);

        ///<summary>
        /// CSPRNG bytes.
        ///</summary>
        public static async Task<byte[]> RetrieveRandomBytesAsync(int length)
        {
            await RngLock.WaitAsync();
            try
            {
                return FortunaInstance.Rng.RandomData(length);
            }
            finally
            {
                RngLock.Release();
            }
        }
    }

}