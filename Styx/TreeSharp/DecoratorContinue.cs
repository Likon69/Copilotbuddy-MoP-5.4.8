using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    public class DecoratorContinue : Decorator
    {
        [DebuggerStepThrough]
        public DecoratorContinue(Composite decorated, CanRunDecoratorDelegate func)
            : base(decorated, func)
        {
        }

        [DebuggerStepThrough]
        public DecoratorContinue(CanRunDecoratorDelegate func, Composite decorated)
            : base(func, decorated)
        {
        }

        [DebuggerStepThrough]
        public DecoratorContinue(Composite child)
            : base(child)
        {
        }

        [DebuggerStepThrough]
        public DecoratorContinue()
        {
        }

        [DebuggerStepThrough]
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            if (!CanRun(context))
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
