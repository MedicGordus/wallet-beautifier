using wallet_beautifier.crypto.fortuna;

using SHA3Core.Keccak;

using Blake2b.NetCore;
using PinnedMemory;

using System.Security.Cryptography;
using System.Text;
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

        ///<summary>
        /// Blake2b-244.
        ///</summary>
        internal static byte[] ComputeBlake2b244Hash(byte[] input)
        {
            return ComputeBlake2bHash(input, 244);
        }

        ///<summary>
        /// Blake2b-256.
        ///</summary>
        internal static byte[] ComputeBlake2b256Hash(byte[] input)
        {
            return ComputeBlake2bHash(input, 256);
        }

        ///<summary>
        /// Blake2b-244.
        ///</summary>
        private static byte[] ComputeBlake2bHash(byte[] input, int digestSize)
        {
            using (Blake2b.NetCore.Blake2b digest = new Blake2b.NetCore.Blake2b(digestSize))
            using (var hash = new PinnedMemory<byte>(new byte[digest.GetLength()]))
            using (var pinnedInput = new PinnedMemory<byte>(input, false)) // input is STILL exposed and could be leaked until garbage collect.
            {
                digest.UpdateBlock(pinnedInput, 0, pinnedInput.Length);
                digest.DoFinal(hash, 0);

                return hash.ToArray();
            }
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