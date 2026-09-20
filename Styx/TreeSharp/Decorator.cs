using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    public class Decorator : GroupComposite
    {
        [DebuggerStepThrough]
        public Decorator(Composite decorated, CanRunDecoratorDelegate func)
            : base(decorated)
        {
            Runner = func;
        }

        [DebuggerStepThrough]
        public Decorator(CanRunDecoratorDelegate func, Composite decorated)
            : this(decorated, func)
        { }

        [DebuggerStepThrough]
        public Decorator(Composite child)
            : this(child, null)
        { }

        [DebuggerStepThrough]
        public Decorator()
            : base(null, null)
        { }

        protected CanRunDecoratorDelegate Runner { get; private set; }
        public Composite DecoratedChild { get { return Children[0]; } }

        [DebuggerStepThrough]
        protected virtual bool CanRun(object context)
        {
            return Runner != null ? Runner(context) : true;
        }

        [DebuggerStepThrough]
        public override void Start(object context)
        {
            if (Children.Count != 1)
                throw new ApplicationException("Decorators must have only one child.");
            base.Start(context);
        }

        [DebuggerStepThrough]
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            var canRun = CanRun(context);

            if (!canRun)
            {
                yield return RunStatus.Failure;
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
