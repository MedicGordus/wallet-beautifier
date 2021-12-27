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

        public CurveType GetCurveType => CurveType.Secp256k1;

        public PostCalculationType GetPostCalculationType => PostCalculationType.Sha256RipeMd160AndChecksumVersion0;

        public Bitcoin() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }

        public string GenerateAddressFromCalculatedPublicKey(string publicKeyCalculated)
        {
            return publicKeyCalculated;
        }

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