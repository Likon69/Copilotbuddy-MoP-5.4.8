using System.Collections.Generic;

namespace Styx.TreeSharp
{
    /// <summary>This represents a "While" loop.</summary>
    /// <remarks>Created 10/8/2010.</remarks>
    public class WhileLoop : GroupComposite
    {
        private readonly CanRunDecoratorDelegate _condition;
        private readonly RunStatus _returnStatus = RunStatus.Failure;
        public WhileLoop(CanRunDecoratorDelegate condition, params Composite[] children)
            : base(children)
        {
            _condition = condition;
        }
        public WhileLoop(RunStatus returnStatus, CanRunDecoratorDelegate condition, params Composite[] children)
            : base(children)
        {
            _condition = condition;
            _returnStatus = returnStatus;
        }

        protected override IEnumerable<RunStatus> Execute(object context)
        {

            // This is all boilerplate code pulled from PrioritySelector. Don't use it.
            if (ContextChanger != null)
                context = ContextChanger(context);

            while (_condition(context))
            {
                // Keep in mind; we ARE an enumerator here. So we do execute each child in tandem.
                foreach (Composite node in Children)
                {
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

                    // This is a loop. Meaning it runs everything IN SEQUENCE until it finishes.
                    // Ignore any success/failures. :)
                }
            }

            // We ran out of children, and none succeeded. Return failed.
            yield return _returnStatus;
        }
    }
}
