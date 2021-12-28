using System;

namespace wallet_beautifier.crypto.algorithms.curve25519
{

    public static class Curve25519Core
    {
        ///<summary>
        ///</summary>
        ///<remarks>
        /// [INVALD NOW because of modifications] succeeded on test vectors listed here
        ///
        ///     * * * WARNING: sc_clamp.cs has been modified, search for "https://github.com/cardano-foundation/CIPs/blob/master/CIP-0003/Icarus.md" * * *
        ///
        ///     * * * WARNING: Also removed SHA512 from algorithm in keypair.cs, search for "oi448fgu" * * *
        ///
        ///     https://datatracker.ietf.org/doc/html/rfc8032#section-7.1
        ///
        /// however, unable to recreate vectors for Cardano listed here:
        ///     https://gist.github.com/KtorZ/b2e4e1459425a46df51c023fda9609c8
        ///     https://github.com/satoshilabs/slips/blob/master/slip-0023.md
        ///</remarks>
        public static byte[] CalculatePublicKey(byte[] privateKey)
        {
            // because the array is modified, we need to use a copy so caller doesn't lose the original
            byte[] deepCopyPrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, 0, deepCopyPrivateKey, 0, 32);

            return Chaos.NaCl.Ed25519.PublicKeyFromSeed(deepCopyPrivateKey);
        }

/* this is built into the implementation we are using so we don't need it anymore
        ///<summary>
        ///</summary>
        public static byte[] CalculatePublicKey(byte[] privateKey)
        {
            if(privateKey.Length > 32)
            {
                Array.Resize(ref privateKey, 32);
            }
            
            return Chaos.NaCl.Ed25519.PublicKeyFromSeed(TweakPrivateKey(privateKey));
        }
*/
        ///<summary>
        /// Used to return the actual private key used to generate the public key.
        ///</summary>
        ///<remarks>
        /// sc_clamp.cs has been modified inside the EC-curve25519 library to match this (and ADA spec)
        //      search for "https://github.com/cardano-foundation/CIPs/blob/master/CIP-0003/Icarus.md"
        ///</remarks>
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