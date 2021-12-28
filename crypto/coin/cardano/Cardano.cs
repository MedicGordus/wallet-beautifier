

using wallet_beautifier.crypto.algorithms;
using wallet_beautifier.crypto.algorithms.curve25519;
using wallet_beautifier.io;

using System;

namespace wallet_beautifier.crypto.coin.cardano
{
    ///<remarks>
    /// explanation for how addresses work:
    ///     https://cips.cardano.org/cips/cip19/
    ///</remarks>
    public class Cardano : ACoin<Cardano>, ICoin
    {
        public string GetCommonName => "Cardano";

        public string GetTicker => "ADA";

        private readonly static string SUBFOLDER = IoCore.AppendOnRootPath("attempts-cardano");

        public string GetAttemptPath => SUBFOLDER;

        public CurveType GetCurveType => CurveType.Curve25519;

        public PostCalculationType GetPostCalculationType => PostCalculationType.Black2b224;

        public Cardano() : base(() => IoCore.GetAttemptPath(SUBFOLDER))
        { }

        public string GenerateAddressFromCalculatedPublicKey(string publicKeyCalculated)
        {
            return publicKeyCalculated;
        }

        public byte[] TweakPrivateKey(byte[] privateKey)
        {
            return Curve25519Core.TweakPrivateKey(privateKey);
        }

        public bool CharactersAreAllowedInPublicAddress(string address, bool termsCaseSensitive)
        {
            string digitsToCheck;
            string addressToCheck;

            if(termsCaseSensitive)
            {
                digitsToCheck = Bech32.Digits;
                addressToCheck = address;
            }
            else
            {
                digitsToCheck = Bech32.Digits.ToUpper();
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