using System;
using System.Security.Cryptography;
using System.Threading;

namespace wallet_beautifier.crypto
{
    /// <summary>
    /// Convenience class for dealing with randomness.
    /// </summary>
    ///<remarks>
    /// ORIGINALLY WRITTEN BY JON SKEET
    ///
    /// https://codeblog.jonskeet.uk/2009/11/04/revisiting-randomness/
    ///
    /// Converted to crypto random for security (removed most of code)
    ///
    /// Thank you Jon!
    ///
    ///</remarks>
    public static class ThreadLocalRandomNumberGenerator
    {
        /// <summary>
        /// Random number generator
        /// </summary>
        private static readonly ThreadLocal<RandomNumberGenerator> threadRandom = new ThreadLocal<RandomNumberGenerator>(NewRandom);

        /// <summary>
        /// Creates a new instance of Random. The seed is derived
        /// from a global (static) instance of Random, rather
        /// than time.
        /// </summary>
        public static RandomNumberGenerator NewRandom() => RandomNumberGenerator.Create();

        /// <summary>
        /// Returns an instance of Random which can be used freely
        /// within the current thread.
        /// </summary>
        public static RandomNumberGenerator Instance  => threadRandom.Value;

        /// <summary>See <see cref="Random.GetBytes(byte[])" /></summary>
        public static void GetBytes(byte[] buffer)
        {
            Instance.GetBytes(buffer);
        }
    }
}