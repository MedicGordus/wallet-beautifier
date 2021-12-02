using System;
using System.Linq;
using System.Numerics;

namespace wallet_beautifier.crypto.algorithms
{

    // copied and modified from Chainers
    public class Base58
    {
        protected const int CheckSumSizeInBytes = 4;
        public const string Hexdigits = "0123456789abcdefABCDEF";
        public const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";


        public static byte[] RemoveCheckSum(byte[] data)
        {
            var result = new byte[data.Length - CheckSumSizeInBytes];
            Buffer.BlockCopy(data, 0, result, 0, data.Length - CheckSumSizeInBytes);

            return result;
        }

        public static string Encode(byte[] data)
        {
            if(data == null)
            {
                return null;
            }

            // Decode byte[] to BigInteger
            BigInteger intData = 0;
            for (var i = 0; i < data.Length; i++)
            {
                intData = intData * 256 + data[i];
            }

            // Encode BigInteger to Base58 string
            var result = "";
            while (intData > 0)
            {
                var remainder = (int)(intData % 58);
                intData /= 58;
                result = Digits[remainder] + result;
            }

            // Append `1` for each leading 0 byte
            for (var i = 0; i < data.Length && data[i] == 0; i++)
            {
                result = '1' + result;
            }
            return result;
        }

        public static byte[] Decode(string base58)
        {
            if(base58 == null)
            {
                return null;
            }
            
            // Decode Base58 string to BigInteger 
            BigInteger intData = 0;
            for (var i = 0; i < base58.Length; i++)
            {
                var digit = Digits.IndexOf(base58[i]); //Slow
                if (digit < 0)
                    throw new FormatException($"Invalid Base58 character `{base58[i]}` at position {i}");
                intData = intData * 58 + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            var leadingZeroCount = base58.TakeWhile(c => c == '1').Count();
            var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
            var bytesWithoutLeadingZeros =
                intData.ToByteArray()
                .Reverse()// to big endian
                .SkipWhile(b => b == 0);//strip sign byte
            var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
            return result;
        }

        public static bool TryDecode(string base58, out byte[] decodedBytes)
        {
            bool output = false;
            try
            {
                decodedBytes = Decode(base58);
                output = true;
            }
            catch
            {
                decodedBytes = null;
            }

            return output;
        }

        protected static byte[] CutLastBytes(byte[] source, int cutCount)
        {
            var rez = new byte[source.Length - cutCount];
            Array.Copy(source, rez, rez.Length);
            return rez;
        }

        protected static byte[] AddLastBytes(byte[] source, int addCount)
        {
            var rez = new byte[source.Length + addCount];
            Array.Copy(source, rez, source.Length);
            return rez;
        }

        protected static byte[] CutFirstBytes(byte[] source, int cutCount)
        {
            var rez = new byte[source.Length - cutCount];
            Array.Copy(source, cutCount, rez, 0, rez.Length);
            return rez;
        }

        protected static byte[] AddFirstBytes(byte[] source, int addCount)
        {
            var rez = new byte[source.Length + addCount];
            Array.Copy(source, 0, rez, addCount, source.Length);
            return rez;
        }
    }
}