using Styx.CommonBot.Frames;
using Styx.TreeSharp;

namespace CommonBehaviors.Decorators
{
    public class DecoratorFrameIsVisible<T> : Decorator where T : Frame, new()
    {
        private readonly T _frame = new T();

        public DecoratorFrameIsVisible(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return _frame.IsVisible;
        }
    }
}
