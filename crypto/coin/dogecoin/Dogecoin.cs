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

        public CurveType GetCurveType => CurveType.Secp256k1;

        public PostCalculationType GetPostCalculationType => PostCalculationType.Sha256RipeMd160AndChecksumVersion30;

        public Dogecoin() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }

        public string GenerateAddressFromCalculatedPublicKey(string publicKeyCalculated)
        {
            return publicKeyCalculated;
        }

        public byte[] TweakPrivateKey(byte[] privateKey) => privateKey;

        public bool CharactersAreAllowedInPublicAddress(string address, bool termsCaseSensitive)
        {
            string digitsToCheck;
            string addressToCheck;

            if(termsCaseSensitive)
            {
                digitsToCheck = Base58.Digits;
                addressToCheck = address;
            }
            else
            {
                digitsToCheck = Base58.Digits.ToUpper();
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