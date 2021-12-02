using static Fortuna.FortunaCore;

using Fortuna.crypt;

using System;
using System.Numerics;

namespace Fortuna.rand
{
    internal class Generator
    {

        protected GeneratorState State;

        internal Generator(Hasher hasher)
        {
            State = InitializeGeneratorState(hasher);
        }

        internal void Reseed(byte[] seed)
        {
            State.ComputeNewKey(seed);

            State.IncrementCounter();
        }

        protected byte[] GenerateBlocks(int numberOfBlocks)
        {
            State.ThrowIfNotSeeded();

            byte[] output = new byte[] { };

            for (int delta = 1; delta <= numberOfBlocks ; delta++)
            {
                output = AppendByteArrays(output, State.GenerateOutputFromCurrentCounter());

                State.IncrementCounter();
            }

            return output;
        }

        internal byte[] GenerateRandomData(long desiredByteLength)
        {
            if (desiredByteLength > Math.Pow(2,20))
                throw new Exception("cannot generate length over 2^20");

            int numberOfBlocks = int.Parse(Math.Ceiling(desiredByteLength / 16d).ToString());

            byte[] output = TruncateByteArray(GenerateBlocks(numberOfBlocks), desiredByteLength);

            State.SwitchToKey(GenerateBlocks(2));

            return output;
        }

        internal static GeneratorState InitializeGeneratorState(Hasher hasher)
        {
            return new GeneratorState(hasher);
        }
    }

    internal class GeneratorState
    {
        /// <summary>
        /// as specified in the Fortuna documentation (9.4.2), we limit the counter to 16 bytes
        /// </summary>
        private readonly BigInteger COUNTER_MAX = BigInteger.Subtract(BigInteger.Pow(2, 128), 1);

        protected readonly Hasher Hasher;

        protected readonly Encryptor Encryptor = new Encryptor();

        internal byte[] Key { get; private set; }

        protected BigInteger HiddenCounter;

        internal GeneratorState(Hasher hasher)
        {
            Hasher = hasher;
            Key = new byte[32];
            Encryptor.UpdateKey(Key);
            HiddenCounter = 0;
        }

        internal void IncrementCounter()
        {
            // note that because of the generator constriant of 2^20, this should never occur
            if (HiddenCounter.Equals(COUNTER_MAX))
                HiddenCounter = 0;

            HiddenCounter++;
        }

        internal void ComputeNewKey(byte[] seed)
        {
            Key = Hasher.Compute(AppendByteArrays(Key, seed));
            Encryptor.UpdateKey(Key);
        }

        internal void SwitchToKey(byte[] key)
        {
            Key = key;
            Encryptor.UpdateKey(Key);
        }

        internal void ThrowIfNotSeeded()
        {
            if (HiddenCounter == 0)
                throw new Exception("generator has not been seeded.");
        }

        /// <summary>
        /// note that we are not following the exact specification in the Fortuna specification (9.4.2),
        ///     but the rearrangement of bytes has no effect and therefore the extra cycles required to
        ///     follow specification are being left out.
        /// </summary>
        /// <returns></returns>
        internal byte[] GenerateOutputFromCurrentCounter()
        {
            // endianness is not important
            byte[] counterBytes = HiddenCounter.ToByteArray();

            // ensure that the counter bytes are only 16 bytes
            byte[] bytesToEncrypt = new byte[16];
            Buffer.BlockCopy(counterBytes, 0, bytesToEncrypt, 0, counterBytes.Length);

            return Encryptor.Encrypt(counterBytes);
        }
    }
}
