using Styx.TreeSharp;

namespace CommonBehaviors.Decorators
{
    public class DecoratorContextIs<T> : Decorator
    {
        public DecoratorContextIs(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return context is T;
        }
    }
}
