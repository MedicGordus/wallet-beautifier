using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.io;

using System;

namespace wallet_beautifier.crypto.coin.dogecoin
{
    public class Dogecoin : ACoin<Dogecoin>, ICoin
    {
        public string GetCommonName => "Dogecoin";

        public string GetTicker => "DOGE";
        
        private readonly static string SUBFOLDER = IoCore.AppendOnRootPath("attempts-dogecoin");

        public string GetAttemptPath => SUBFOLDER;

        public ByteStage GetByteStage => ByteStage.Sha256RipeMd160;

        public Dogecoin() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }

        public string GenerateAddressFromHashedPublicKey(byte[] publicKeyHashed)
        {
            if(publicKeyHashed.Length != 20) throw new ArgumentException("publicKey size incorrect, must be exactly 20 bytes as it should have been hashed like so: RipeMd160(Sha256(input)).");

            // this appends the 30 byte we need for versioning mainnet
            byte[] dogecoinBytes = new byte[publicKeyHashed.Length + 1];
            dogecoinBytes[0] = 30;
            Buffer.BlockCopy(publicKeyHashed, 0, dogecoinBytes, 1, publicKeyHashed.Length);
            
            // create checksum
            byte[] checkSumBytes = CryptoCore.ComputeDoubleSha256Hash(dogecoinBytes);
            
            // concatenate on the end of the ripemd160
            byte[] addressBytes = new byte[dogecoinBytes.Length + 4];
            Buffer.BlockCopy(dogecoinBytes, 0, addressBytes, 0, dogecoinBytes.Length);
            Buffer.BlockCopy(checkSumBytes, 0, addressBytes, dogecoinBytes.Length, 4);
            
            // Base58 encode
            return Base58.Encode(addressBytes);
        }

        public bool CharactersAreAllowedInPublicAddress(string address)
        {
            foreach(char deltaChar in address)
            {
                if(!Base58.Digits.Contains(deltaChar))
                {
                    return false;
                }
            }

            return true;
        }
    }
}