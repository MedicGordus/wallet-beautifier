using wallet_beautifier.crypto.algorithms.bech32;

using System;

namespace wallet_beautifier.crypto.algorithms
{
    ///<summary>
    /// Best explanation for Bech here (specific to bitcoin, but good nonetheless):
    ///     https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki
    ///</summary>
    public class Bech32
    {
        public static string Digits = Bech32Engine.charset;
        private static CardanoBech32Wrapper C32W = new CardanoBech32Wrapper();

        public static string EncodeAddress(byte[] hashedPublicKey)
        {
            byte[] hashWithHeader = new byte[hashedPublicKey.Length + 1];

            // 0110 0000 --> I don't know if this is the right endiannes but if not 96 it should be 6
            hashWithHeader[0] = 96;

            Buffer.BlockCopy(hashedPublicKey, 0, hashWithHeader, 1, hashedPublicKey.Length);

            return C32W.ConvertToBech32AddressFromBytes(hashWithHeader, AddressType.addr);
        }
    }
}