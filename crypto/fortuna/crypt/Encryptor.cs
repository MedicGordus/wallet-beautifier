using System.Security.Cryptography;

namespace Fortuna.crypt
{
    internal class Encryptor
    {
        protected ICryptoTransform Cryptor = null;

        internal Encryptor()
        { }

        internal Encryptor(byte[] key)
        {
            UpdateKey(key);
        }

        internal void UpdateKey(byte[] key)
        {
            using (Aes encryptor = Aes.Create())
            {
                encryptor.Key = key;

                Cryptor = encryptor.CreateEncryptor();
            }
        }

        internal byte[] Encrypt(byte[] dataBlock)
        {
            if (Cryptor == null)
                throw new CryptographicException("encryptor cannot encrypt without having a key");

            return Cryptor.TransformFinalBlock(dataBlock, 0, dataBlock.Length);
        }
    }
}
