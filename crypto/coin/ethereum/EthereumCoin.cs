using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.io;

using System;

namespace wallet_beautifier.crypto.coin.ethereum
{
    public class EthereumCoin : ACoin<EthereumCoin>, ICoin
    {
        public string GetCommonName => "Ethereum";

        public string GetTicker => "ETH";

        private readonly static string SUBFOLDER = IoCore.AppendOnRootPath("attempts-ethereum");

        public string GetAttemptPath => SUBFOLDER;

        public CurveType GetCurveType => CurveType.Secp256k1;

        public PostCalculationType GetPostCalculationType => PostCalculationType.Keccak256;

        public EthereumCoin() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }
        
        public string GenerateAddressFromCalculatedPublicKey(string keccakString)
        {
            return "0x" + keccakString.Substring(keccakString.Length - 40);
        }

        public byte[] TweakPrivateKey(byte[] privateKey) => privateKey;

        public bool CharactersAreAllowedInPublicAddress(string address, bool termsCaseSensitive)
        {
            string digitsToCheck;
            string addressToCheck;

            if(termsCaseSensitive)
            {
                digitsToCheck = Base58.Hexdigits;
                addressToCheck = address;
            }
            else
            {
                digitsToCheck = Base58.Hexdigits.ToUpper();
                addressToCheck = address.ToUpper();
            }

            foreach(char deltaChar in addressToCheck)
            {
                if(!digitsToCheck.Contains(deltaChar))
                {
                    return false;
                }
            }

            return true;
        }
    }
}