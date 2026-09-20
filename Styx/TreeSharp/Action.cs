using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   The base Action class. A simple, easy to use, way to execute actions, and return their status of execution.
    ///   These are normally considered 'atoms' in that they are executed in their entirety.
    /// </summary>
    public class Action : Composite
    {
        [DebuggerStepThrough]
        public Action() { }

        [DebuggerStepThrough]
        public Action(ActionDelegate action)
        {
            Runner = action;
        }

        [DebuggerStepThrough]
        public Action(ActionSucceedDelegate action)
        {
            SucceedRunner = action;
        }

        public ActionDelegate Runner { get; private set; }
        public ActionSucceedDelegate SucceedRunner { get; private set; }

        /// <summary>
        ///   Runs this action, and returns a <see cref = "RunStatus" /> describing it's current state of execution.
        ///   If this method is not overriden, it returns <see cref = "RunStatus.Failure" />.
        /// </summary>
        /// <returns></returns>
        protected virtual RunStatus Run(object context)
        {
            return RunStatus.Failure;
        }

        [DebuggerStepThrough]
        protected RunStatus RunAction(object context)
        {
            if (Runner != null)
                return Runner(context);
            if (SucceedRunner != null)
            {
                SucceedRunner(context);
                return RunStatus.Success;
            }
            RunStatus retn = Run(context);

            return retn;
        }

        [DebuggerStepThrough]
        protected override sealed IEnumerable<RunStatus> Execute(object context)
        {
            while ((LastStatus = RunAction(context)) == RunStatus.Running)
                yield return RunStatus.Running;

            yield return LastStatus.Value;
        }
    }
}
