using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary> Creates a Composite that will wait while the defined condition is false. </summary>
    /// <remarks> Created 2/7/2012. </remarks>
    public class Wait : Decorator
    {
        private TimeSpan? _timeout;
        public TimeSpan Timeout
        {
            get
            {
                if (_timeout == null)
                {
                    return new TimeSpan(0, 0, _timeoutRetriever());
                }
                return _timeout.Value;
            }
            set { _timeout = value; }
        }

        [DebuggerStepThrough]
        public Wait(TimeSpan timeout, CanRunDecoratorDelegate runFunc, Composite child)
            : base(child, runFunc)
        {
            Timeout = timeout;
        }

        [DebuggerStepThrough]
        public Wait(int timeoutSeconds, CanRunDecoratorDelegate runFunc, Composite child)
            : base(child, runFunc)
        {
            Timeout = new TimeSpan(0, 0, timeoutSeconds);
        }

        [DebuggerStepThrough]
        public Wait(int timeoutSeconds, Composite child)
            : base(child, null)
        {
            Timeout = new TimeSpan(0, 0, timeoutSeconds);
        }

        [DebuggerStepThrough]
        public Wait(TimeSpan timeout, Composite child)
            : base(child, null)
        {
            Timeout = timeout;
        }

        private WaitGetTimeoutDelegate _timeoutRetriever;
        public Wait(WaitGetTimeoutDelegate timeoutRetriever, Composite child)
            : base(child)
        {
            _timeoutRetriever = timeoutRetriever;
        }

        public Wait(WaitGetTimeoutDelegate timeoutRetriever, CanRunDecoratorDelegate runFunc, Composite child)
            : base(child, runFunc)
        {
            _timeoutRetriever = timeoutRetriever;
        }

        protected DateTime End;

        [DebuggerStepThrough]
        public override void Start(object context)
        {
            End = DateTime.Now.Add(Timeout);
            base.Start(context);
        }

        [DebuggerStepThrough]
        public override void Stop(object context)
        {
            End = DateTime.MinValue;
            base.Stop(context);
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
                yield return RunStatus.Failure;
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
