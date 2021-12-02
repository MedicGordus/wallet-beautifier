using static Fortuna.FortunaCore;

using Fortuna.crypt;
using Fortuna.rand;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_beautifier.crypto.fortuna.entropy
{
    internal class FileEntropySource : EntropyCollector
    {
        private const int MINIMUM_FILE_BYTE_LENGTH = 2300000;
        private readonly Encryptor Encryptor = new Encryptor();

        public FileEntropySource(Prng prng, byte sourceId, string ioPath):base(prng, sourceId)
        {
            string destinationPath;

            if(!File.Exists(ioPath))
            {
                throw new ArgumentException(
                    string.Format(
                        "File '{0}' does not exist. Unable to start file entropy source.",
                        ioPath
                    )
                );
            }

            destinationPath = File.ReadAllText(ioPath);

            if(!Directory.Exists(destinationPath))
            {
                throw new ArgumentException(
                    string.Format(
                        "Directory specified in file '{0}' ('{1}') does not exist. Unable to start file entropy source.",
                        ioPath,
                        destinationPath
                    )
                );
            }

            destinationPath = Path.Combine(destinationPath, "fortuna.io");

            _ = GenerateEntropyAsync(destinationPath);
        }

        protected async Task GenerateEntropyAsync(string filePath)
        {
            byte[] key = new byte[] {
                                        0,   1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15,
                                       16,  17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
                                    };
            Encryptor.UpdateKey(key);

            if (!File.Exists(filePath) || File.ReadAllBytes(filePath).Length < MINIMUM_FILE_BYTE_LENGTH)
            {

                byte[] buffer = new byte[] { };

                do
                {
                    buffer = AppendByteArrays(buffer, Encryptor.Encrypt(buffer));
                } while (buffer.Length < MINIMUM_FILE_BYTE_LENGTH);

                File.WriteAllBytes(filePath, TruncateByteArray(buffer, MINIMUM_FILE_BYTE_LENGTH));
            }

            do
            {
                long timeStamp = DateTime.Now.Ticks;

                byte[] buffer = File.ReadAllBytes(filePath);
                File.WriteAllBytes(filePath, Encryptor.Encrypt(buffer));

                byte[] difference = BitConverter.GetBytes(DateTime.Now.Ticks - timeStamp);

                await Task.Delay(9000, Cts.Token).ConfigureAwait(false);

                EntropyReceived(new byte[] { difference[0], difference[1] });
            } while (Cts == null || !Cts.Token.IsCancellationRequested);
        }
    }
}