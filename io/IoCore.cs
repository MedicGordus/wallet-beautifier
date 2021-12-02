using wallet_beautifier.ux;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace wallet_beautifier.io
{
    ///<summary>
    /// Handles IO for .NET, all known platforms.
    ///</summary>    
    public static class IoCore
    {
        ///<summary>
        /// Returns the drive that this is executing from.
        ///</summary>
        internal static readonly string EXECUTABLE_DRIVE = (new DriveInfo(Path.GetPathRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))).RootDirectory.FullName;

        ///<summary>
        /// Returns the root directory assigned for use by this app.
        ///</summary>
        private static readonly string PATH = Path.Combine(EXECUTABLE_DRIVE, "vanwall");

        ///<summary>
        /// Returns the root directory concatenated appropriately with the subpath.
        ///</summary>
        public static string AppendOnRootPath(string subpath) => Path.Combine(PATH, subpath);

/* unused
        ///<summary>
        /// Returns the subdirectory concatenated appropriately with the subpath.
        ///</summary>
        public static string AppendOnSubdirectory(string subdirectory, string subpath) => Path.Combine(subdirectory, subpath);
*/

        public static bool EnsureAllPathsExist()
        {
            return EnsurePathExists(PATH);
        }

        public static bool EnsurePathExists(string path)
        {
            try
            {
                if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }
            catch (Exception e)
            {
                UxCore.ShareMessage(
                    MessageType.NeedToShare,
                    string.Format(
                        "IoCore.EnsurePathExists('{0}') failed with message: '{1}",
                        path,
                        e.Message
                    )
                );
            }

            return false;
        }

/* unused
        public static Task AppendLineAsync(string path, string text) => File.AppendAllTextAsync(path, text + '\n');

        public static Task AppendLinesAsync(string path, IEnumerable<string> lines) => File.AppendAllLinesAsync(path, lines);
*/
        public static ParallelLineWriter CreateLineWriter(int maxFileLines, Func<string> getNextPath) => new ParallelLineWriter(maxFileLines, getNextPath);

        public static string GetAttemptPath(string subfolder)
        {
            ulong counter = 0;
            string previousOutput = "";
            string output = GetAttemptPathByCounter(subfolder,counter);

            while(File.Exists(output))
            {
                previousOutput = output;
                counter++;
                output = GetAttemptPathByCounter(subfolder, counter);
            }

            return output;
        }
        
        private static string GetAttemptPathByCounter(string subfolder, ulong counter) => Path.Combine(
            subfolder,
            string.Format(
                "attempt-entries-{0}.log",
                string.Format(
                    "{0}",
                    counter
                ).PadLeft(10,'0')
            )
        );
    }
}