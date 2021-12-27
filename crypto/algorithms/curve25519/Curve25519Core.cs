using curve25519;

using System;

namespace wallet_beautifier.crypto.algorithms.curve25519
{
    public static class Curve25519Core
    {
        private static CSharpCurve25519Provider Provider = new CSharpCurve25519Provider();

        public static byte[] CalculatePublicKey(byte[] privateKey)
        {
            // tweaking as per https://github.com/cardano-foundation/CIPs/blob/master/CIP-0003/Icarus.md
            //
            // deep copy
            byte[] tweakedPrivateKey = new byte[privateKey.Length];
            Buffer.BlockCopy(privateKey, 0, tweakedPrivateKey, 0, privateKey.Length);
            //
            // tweak (idk why)
            tweakedPrivateKey[0] &= 0b1111_1000;
            tweakedPrivateKey[31] &= 0b0001_1111;
            tweakedPrivateKey[31] |= 0b0100_0000;

            return Provider.generatePublicKey(tweakedPrivateKey);
        }
    }
}