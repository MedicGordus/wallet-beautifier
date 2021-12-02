
using static Fortuna.FortunaCore;

using Fortuna.crypt;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fortuna.rand
{
    public class Prng
    {
        protected const long MAX_MILLISECONDS_TO_WAIT_FOR_INITIAL_SEED_ENTROPY = 60000;

        private Pool[] Pools;

        private int ReseedCounter;

        private Generator Generator;

        private long LastReseedTicks;

        /// <summary>
        /// documentation names this method as Initialize
        /// </summary>
        /// <returns></returns>
        public static Prng InitializeBlank()
        {
            Hasher hasher = new Hasher();

            Pool[] pools = new Pool[32];
            for(int delta = 0; delta < 32; delta++)
            {
                pools[delta] = new Pool(hasher);
            }

            Generator generator = new Generator(hasher);
            return new Prng()
            {
                Generator = generator,
                ReseedCounter = -1,
                Pools = pools,
                LastReseedTicks = 0
            };

        }

        /// <summary>
        /// documentation names this method as UpdateSeedFile
        /// </summary>
        /// <returns></returns>
        public static async Task SeedFromFileAsync(string filePath, Prng prng, long millisecondsToWaitForInitialSeedEntropy = MAX_MILLISECONDS_TO_WAIT_FOR_INITIAL_SEED_ENTROPY)
        {
            byte[] seed = await prng.ObtainSeedAsync(filePath, millisecondsToWaitForInitialSeedEntropy).ConfigureAwait(false);

            if (seed.Length != SEED_LENGTH)
                throw new Exception(string.Format("seed obtained, '{0}' may be corrupted, length was '{1}', when expected length is {2}",seed, seed.Length, SEED_LENGTH));

            prng.Generator.Reseed(seed);
            await prng.WriteSeedFileAsync(filePath).ConfigureAwait(false);

            prng.ReseedCounter = 0;
        }

        protected async Task<byte[]> ObtainSeedAsync(string filePath, long millisecondsToWaitForInitialSeedEntropy)
        {
            if (!await FileExistsAndContainsValidContentsAsync(filePath))
                await WaitForEntropyBasedSeedAsync(filePath, millisecondsToWaitForInitialSeedEntropy).ConfigureAwait(false);

            byte[] output = await ReadFromFileAsync(filePath).ConfigureAwait(false);

            return output;
        }

        protected async Task WaitForEntropyBasedSeedAsync(string filePath, long millisecondsToWaitForInitialSeedEntropy)
        {
            const int MILLISECONDS_BETWEEN_CHECKS = 1000;
            long millisecondsWaited = 0;

            while(!Pools[0].ReadyToReseed() && ReseedTimeLimiterTranspired())
            {
                await Task.Delay(MILLISECONDS_BETWEEN_CHECKS).ConfigureAwait(false);
                millisecondsWaited += MILLISECONDS_BETWEEN_CHECKS;

                if(millisecondsWaited > millisecondsToWaitForInitialSeedEntropy)
                {
                    throw new Exception(string.Format("waited {0} milliseconds and timed out due to not enough entropy being generated for initial seed within time constraint", millisecondsWaited));
                }
            }
            Generator.Reseed(new byte[32]);
            await WriteSeedFileAsync(filePath).ConfigureAwait(false);
        }

        protected async Task WriteSeedFileAsync(string filePath)
        {
            await WriteToFileAsync(filePath, Generator.GenerateRandomData(SEED_LENGTH)).ConfigureAwait(false);
        }

        public bool ReadyToGenerateRandomData()
        {
            return ReseedCounter != -1 && (Pools[0].ReadyToReseed() && ReseedTimeLimiterTranspired());
        }

        public byte[] RandomData(long desiredLength)
        {
            if (Pools[0].ReadyToReseed() && ReseedTimeLimiterTranspired())
            {

                // in rare instance when not loaded from file but somehow this is called, we try to push reseed beyond 0 so later it won't crash
                if(ReseedCounter == -1)
                    Interlocked.Increment(ref ReseedCounter);

                // wrap reseed counter if it reaches maxvalue
                Interlocked.CompareExchange(ref ReseedCounter, 0, int.MaxValue);

                // increment by one
                Interlocked.Increment(ref ReseedCounter);

                byte[] seed = new byte[] { };
                for(int delta = 0; delta <= 32; delta++)
                {
                    if((ReseedCounter ^ (int)Math.Pow(2, delta)) == delta)
                    {
                        seed = AppendByteArrays(seed, Pools[delta].RetrieveValueAndReset());
                    }
                }

                Generator.Reseed(seed);
                LastReseedTicks = DateTime.Now.Ticks;
            }

            if (ReseedCounter == 0)
                throw new Exception("unable to continue, seeding has not occured");

            return Generator.GenerateRandomData(desiredLength);
        }

        protected bool ReseedTimeLimiterTranspired()
        {
            const long MINIMUM_MILLISECONDS_BEFORE_RESEED = 100;
            long timeStamp = DateTime.Now.Ticks;
            return ((timeStamp - LastReseedTicks) > TimeSpan.TicksPerMillisecond * MINIMUM_MILLISECONDS_BEFORE_RESEED);
        }

        public void AddRandomEvent(byte source, int poolNumber, byte[] eventData)
        {
            if (eventData.Length == 0 | eventData.Length > 32)
                throw new Exception(string.Format("event data length invalid, '{0}', expected length: 1 - 32", eventData.Length));

            if (poolNumber < 0 | poolNumber > 31)
                throw new Exception(string.Format("pool number invalid, '{0}', expected pool number: 0 - 31", poolNumber));

            Pools[poolNumber].AppendEventData(AppendByteArrays(new byte[] { source, (byte)eventData.Length }, eventData));
        }

    }
}
