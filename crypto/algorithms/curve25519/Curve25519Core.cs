using curve25519;
using org.whispersystems.curve25519;

using System;

namespace wallet_beautifier.crypto.algorithms.curve25519
{

    public static class Curve25519Core
    {
        ///<remarks>
        /// We tested a custom ISha512 which returned input as output and it made no difference to pubkey generation from a private key
        ///</remarks>
        private static CSharpCurve25519Provider Provider = new CSharpCurve25519Provider();

        private static Curve25519 curve25519 = Curve25519.getInstance(Curve25519.CSHARP);

        ///<summary>
        ///</summary>
        ///<remarks>
        /// fails vectors listed here 
        ///     https://datatracker.ietf.org/doc/html/draft-ietf-tls-curve25519-01
        ///
        ///     and
        ///
        ///     https://datatracker.ietf.org/doc/html/rfc8032#section-7.1
        ///
        /// although it passes it's own test:
        ///
        ///     https://github.com/signal-csharp/curve25519-dotnet/blob/master/curve25519-dotnet-tests/BasicTests.cs
        ///
        ///     as well as an external test:
        ///
        ///     https://github.com/signalapp/curve25519-java/blob/master/tests/src/main/java/org/whispersystems/curve25519/Curve25519Test.java
        ///</remarks>
        public static byte[] CalculatePublicKey(byte[] privateKey)
        {
            return Provider.generatePublicKey(TweakPrivateKey(privateKey));
        }

        public static byte[] TweakPrivateKey(byte[] privateKey)
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

            return tweakedPrivateKey;
        }
    }
}