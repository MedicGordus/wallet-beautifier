using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Fortuna
{
    internal static class FortunaCore
    {
        internal const int SEED_LENGTH = 64;

        internal static byte[] AppendByteArrays(byte[] array1, byte[] array2)
        {
            byte[] output = new byte[array1.Length + array2.Length];

            Buffer.BlockCopy(array1, 0, output, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, output, array1.Length, array2.Length);

            return output;
        }

        internal static byte[] TruncateByteArray(byte[] buffer, long desiredLength)
        {
            byte[] output = new byte[desiredLength];
            Buffer.BlockCopy(buffer, 0, output, 0, output.Length);
            return output;
        }

        internal static async Task WriteToFileAsync(string path, byte[] contents)
        {
            await File.WriteAllBytesAsync(path, contents).ConfigureAwait(false);
        }

        internal static async Task<byte[]> ReadFromFileAsync(string path)
        {
            return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        }

        internal static async Task<bool> FileExistsAndContainsValidContentsAsync(string path)
        {
            if (!File.Exists(path))
                return false;

            if ((await ReadFromFileAsync(path).ConfigureAwait(false)).Length != SEED_LENGTH)
                return false;

            return true;
        }
    }
}
