using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.io;

using System;

namespace wallet_beautifier.crypto.coin.bitcoin
{
    public class Bitcoin : ACoin<Bitcoin>, ICoin
    {
        public string GetCommonName => "Bitcoin";

        public string GetTicker => "BTC";

        private readonly static string SUBFOLDER = IoCore.AppendOnRootPath("attempts-bitcoin");

        public string GetAttemptPath => SUBFOLDER;

        public ByteStage GetByteStage => ByteStage.Sha256RipeMd160;

        public Bitcoin() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }

        public string GenerateAddressFromHashedPublicKey(byte[] publicKeyHashed)
        {
            if(publicKeyHashed.Length != 20) throw new ArgumentException("publicKey size incorrect, must be exactly 20 bytes as it should have been hashed like so: RipeMd160(Sha256(input)).");

            // this appends the 0 byte we need for versioning mainnet
            byte[] bitcoinBytes = new byte[publicKeyHashed.Length + 1];
            Buffer.BlockCopy(publicKeyHashed, 0, bitcoinBytes, 1, publicKeyHashed.Length);
            
            // create checksum
            byte[] checkSumBytes = CryptoCore.ComputeDoubleSha256Hash(bitcoinBytes);
            
            // concatenate on the end of the ripemd160
            byte[] addressBytes = new byte[bitcoinBytes.Length + 4];
            Buffer.BlockCopy(bitcoinBytes, 0, addressBytes, 0, bitcoinBytes.Length);
            Buffer.BlockCopy(checkSumBytes, 0, addressBytes, bitcoinBytes.Length, 4);
            
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