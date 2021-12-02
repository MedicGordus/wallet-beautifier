using wallet_beautifier.io.locks;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace wallet_beautifier.io
{
    ///<summary>
    /// Handles IO for files.
    ///</summary>    
    public class ParallelLineWriter
    {
        private StreamWriter Writer;

        private readonly int MaxFileLines;

        private int LinesWritten;

        private readonly ConcurrentBag<string> BufferedLines;

        private readonly BoolLock FileWriteLock;

        private readonly Func<string> GetNextPath;

        private Task WritingTask;

        public ParallelLineWriter(int maxFileLines, Func<string> getNextPath)
        {
            MaxFileLines = maxFileLines;
            LinesWritten = 0;
            BufferedLines = new ConcurrentBag<string>();

            FileWriteLock = BoolLock.Create();

            GetNextPath = getNextPath;

            WritingTask = Task.CompletedTask;

            Writer = null;
        }

        public void Open()
        {
            Writer = new StreamWriter(GetNextPath());
        }

        ///<summary>
        /// Closes the file and opens the specified file to write to.
        ///</summary>
        private async Task ResetAsync()
        {
            await Writer.DisposeAsync();

            Open();
        }

        public async Task WriteLinesAsync(List<string> lines)
        {
            // buffer since the file is currently being written to
            foreach(string deltaLine in lines)
            {
                BufferedLines.Add(deltaLine);
            }

            using(var u = FileWriteLock.GetAutoUnlocker(out bool wasAlreadyLocked))
            {
                if(!wasAlreadyLocked)
                {
                    WritingTask = PerformLineWritesAsync();

                    await WritingTask;
                }
            }

        }

        public async Task PerformLineWritesAsync()
        {
            while(BufferedLines.TryTake(out string line))
            {
                await Writer.WriteLineAsync(line);
                LinesWritten++;

                if(LinesWritten > MaxFileLines)
                {
                    await ResetAsync();
                    LinesWritten = 0;
                }
            }
        }


        private BoolLock ClosingLock = BoolLock.Create();
        public async Task CloseAsync()
        {
            if(Writer == null)
            {
                // this was closed already or maybe never opened?
                return;
            }

            using(ClosingLock.GetAutoUnlocker(out bool wasClosingAlready))
            {
                if(!wasClosingAlready)
                {
                    await WritingTask;

                    await PerformLineWritesAsync();

                    await Writer.DisposeAsync();
                    Writer = null;
                }
            }
        }
    }
}