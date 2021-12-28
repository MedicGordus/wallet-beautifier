using System;

namespace Chaos.NaCl.Internal.Ed25519Ref10
{
    internal static partial class Ed25519Operations
    {
        public static void crypto_sign_keypair(byte[] pk, int pkoffset, byte[] sk, int skoffset, byte[] seed, int seedoffset)
        {
            GroupElementP3 A;
            int i;

            Array.Copy(seed, seedoffset, sk, skoffset, 32);

            // Cardano modification to the ED25519 curve specification oi448fgu
            //
            //byte[] h = Sha512.Hash(sk, skoffset, 32);//ToDo: Remove alloc
            if(seed.Length != 32 | skoffset != 0)
            {
                throw new Exception(
                    string.Format(
                        "Cardano implementation does not allow invalid key configuration: Length = {0} (expected 32), Offset = {1} (expected 0)",
                        sk.Length,
                        skoffset
                    )
                );
            }
            byte[] h = seed;

            ScalarOperations.sc_clamp(h, 0);

            GroupOperations.ge_scalarmult_base(out A, h, 0);
            GroupOperations.ge_p3_tobytes(pk, pkoffset, ref A);

            for (i = 0; i < 32; ++i) sk[skoffset + 32 + i] = pk[pkoffset + i];

            // Cardano modification to the ED25519 curve specification oi448fgu
            //
            // CryptoBytes.Wipe(h);
        }
    }
}
