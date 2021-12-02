using System.Security.Cryptography;

namespace Fortuna.crypt
{
    internal class Hasher
    {

        protected SHA256 Hash;

        public const int HASH_BYTE_LENGTH = 32;

        internal Hasher()
        {
            Hash = SHA256.Create();
        }

        internal byte[] Compute(byte[] buffer)
        {
            return Hash.ComputeHash(buffer);
        }
    }
}
