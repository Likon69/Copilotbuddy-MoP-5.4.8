using System;
using System.Diagnostics;
using System.IO;
using Styx.Common;

namespace Styx
{
    public static class Guard
    {
        internal static void AgainstInvalidExecutor()
        {
            if (StyxWoW.Memory.Executor == null)
            {
                Logging.WriteDiagnostic("Invalid Executor! StackTrace:");
                StackTrace stackTrace = new StackTrace(true);
                StackFrame[] frames = stackTrace.GetFrames();
                if (frames != null)
                {
                    for (int i = 1; i < Math.Min(frames.Length, 10); i++)
                    {
                        StackFrame stackFrame = frames[i];
                        Logging.WriteDiagnostic("\tCaller {0}: {1} in {2} line {3}", i, stackFrame.GetMethod().Name, Path.GetFileName(stackFrame.GetFileName()), stackFrame.GetFileLineNumber());
                    }
                }

                throw new InvalidExecutorException();
            }
        }
    }
}
