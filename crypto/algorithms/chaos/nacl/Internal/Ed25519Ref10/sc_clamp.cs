using System;

namespace Chaos.NaCl.Internal.Ed25519Ref10
{
    internal static partial class ScalarOperations
    {
        public static void sc_clamp(byte[] s, int offset)
        {
            s[offset + 0] &= 248;

            // tweaking as per https://github.com/cardano-foundation/CIPs/blob/master/CIP-0003/Icarus.md
            //s[offset + 31] &= 127;
            s[offset + 31] &= 31;

            s[offset + 31] |= 64;
        }
    }
}