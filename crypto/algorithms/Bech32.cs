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
        public const string Digits = Bech32Engine.charset;
        private static CardanoBech32Wrapper C32W = new CardanoBech32Wrapper();

        public static string EncodeAddress(byte[] hashedPublicKey)
        {
            byte[] hashWithHeader = new byte[hashedPublicKey.Length + 1];

            // as per https://cips.cardano.org/cips/cip19/
            //
            //      see also https://raw.githubusercontent.com/cardano-foundation/CIPs/master/CIP-0019/CIP-0019-cardano-addresses.abnf
            //
            //  0110 = paymentkeyhash
            //  0001 = mainnet
            //
            //  0110 0001 --> 97 (97 is confirmed from the bottom of the spec on https://cips.cardano.org/cips/cip19/)
            //
            hashWithHeader[0] = 97;

            Buffer.BlockCopy(hashedPublicKey, 0, hashWithHeader, 1, hashedPublicKey.Length);

            return C32W.ConvertToBech32AddressFromBytes(hashWithHeader, AddressType.addr);
        }

        public static string EncodeCustom(byte[] data, wallet_beautifier.crypto.algorithms.bech32.AddressType addressType)
        {
            return C32W.ConvertToBech32AddressFromBytes(data, addressType);
        }
    }
}