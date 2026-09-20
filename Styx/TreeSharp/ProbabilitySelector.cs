using System;
using System.Collections.Generic;
using System.Linq;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   Will execute random branches of logic, until one succeeds. This composite
    ///   will fail only if all branches fail as well.
    /// </summary>
    public class ProbabilitySelector : Selector
    {
        public ProbabilitySelector(params Composite[] children)
            : base(children)
        {
            Randomizer = new Random();
        }

        protected Random Randomizer { get; private set; }

        protected override IEnumerable<RunStatus> Execute(object context)
        {
            if (ContextChanger != null)
                context = ContextChanger(context);

            if (Children.Count == 0)
            {
                yield return RunStatus.Failure;
                yield break;
            }

            var childrenCopy = Children.ToList();
            var node = childrenCopy[Randomizer.Next(childrenCopy.Count)];
            childrenCopy.Remove(node);
            while (true)
            {
                if (node == null)
                    goto NEXT;

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

            NEXT:
                if (childrenCopy.Count != 0)
                {
                    node = childrenCopy[Randomizer.Next(childrenCopy.Count)];
                    childrenCopy.Remove(node);
                }
                else
                {

                    // We ran out of children, and none succeeded. Return failed.
                    yield return RunStatus.Failure;
                    yield break;
                }
            }
        }
    }
}
