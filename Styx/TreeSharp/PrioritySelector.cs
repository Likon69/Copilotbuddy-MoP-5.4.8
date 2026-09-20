using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   Will execute each branch of logic in order, until one succeeds. This composite
    ///   will fail only if all branches fail as well.
    /// </summary>
    [DebuggerStepThrough]
    public class PrioritySelector : Selector
    {
        [DebuggerStepThrough]
        public PrioritySelector(params Composite[] children)
            : base(children)
        {
        }
        [DebuggerStepThrough]
        public PrioritySelector(ContextChangeHandler contextChange, params Composite[] children)
            : this(children)
        {
            ContextChanger = contextChange;
        }

        [DebuggerStepThrough]
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            if (ContextChanger != null)
                context = ContextChanger(context);

            // Keep in mind; we ARE an enumerator here. So we do execute each child in tandem.
            foreach (Composite node in Children)
            {
                // Allows us to pass in null for childrens.
                if (node == null)
                {
                    continue;
                }

                // All behaviors are 'Decorators' by default. This just makes it simple.
                // and allows us to not have another class that is nothing but a Decorator : Behavior
                // Though; it may be a good idea in the future to add.
                // Keep in mind!!!
                // Start is called EVERY time we check a behavior! REGARDLESS OF IT'S RETURN VALUE!
                // This makes sure we don't end up with a corrupted state that always returns 'Running'
                // when it's actualled 'Success' or 'Failed'
                node.Start(context);
                // If the current node is still running; return so. Don't 'break' the enumerator
                while (node.Tick(context) == RunStatus.Running)
                {
                    Selection = node;
                    yield return RunStatus.Running;
                }

                // Clear the selection... since we don't have one! Duh.
                Selection = null;
                // Call Stop to allow the node to cleanup anything. Since we don't need it anymore.
                node.Stop(context);
                // If it succeeded (since we're a selector) we return that this GroupNode
                // succeeded in executing.
                if (node.LastStatus == RunStatus.Success)
                {
                    yield return RunStatus.Success;
                    yield break;
                }

                // Still running, so continue on!
            }

            // We ran out of children, and none succeeded. Return failed.
            yield return RunStatus.Failure;
        }
    }
}
