using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    public class WaitContinue : Wait
    {
        public WaitContinue(WaitGetTimeoutDelegate timeoutRetriever, Composite child)
            : base(timeoutRetriever, child)
        {
        }

        [DebuggerStepThrough]
        public WaitContinue(TimeSpan timeout, CanRunDecoratorDelegate runFunc, Composite child)
            : base(timeout, runFunc, child)
        {
        }

        [DebuggerStepThrough]
        public WaitContinue(int timeoutSeconds, CanRunDecoratorDelegate runFunc, Composite child)
            : base(timeoutSeconds, runFunc, child)
        {
        }

        [DebuggerStepThrough]
        public WaitContinue(int timeoutSeconds, Composite child)
            : base(timeoutSeconds, child)
        {
        }

        [DebuggerStepThrough]
        public WaitContinue(TimeSpan timeout, Composite child)
            : base(timeout, child)
        {
        }

        public WaitContinue(WaitGetTimeoutDelegate timeoutRetriever, CanRunDecoratorDelegate runFunc, Composite child)
            : base(timeoutRetriever, runFunc, child)
        {
        }

        [DebuggerStepThrough]
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            DateTime now;
            while ((now = DateTime.Now) < End)
            {
                if (Runner != null)
                {
                    if (Runner(context))
                        break;
                }
                else
                {
                    if (CanRun(context))
                        break;
                }

                yield return RunStatus.Running;
            }

            if (now >= End)
            {
                yield return RunStatus.Success;
                yield break;
            }

            if (DecoratedChild == null)
            {
                yield return RunStatus.Success;
                yield break;
            }

            DecoratedChild.Start(context);
            while (DecoratedChild.Tick(context) == RunStatus.Running)
            {
                yield return RunStatus.Running;
            }

            DecoratedChild.Stop(context);
            if (DecoratedChild.LastStatus == RunStatus.Failure)
            {
                yield return RunStatus.Failure;
                yield break;
            }

            yield return RunStatus.Success;
            yield break;
        }
    }
}
