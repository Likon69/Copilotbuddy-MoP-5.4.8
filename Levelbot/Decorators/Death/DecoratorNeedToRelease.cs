using Styx;
using Styx.TreeSharp;

namespace Levelbot.Decorators.Death
{
    public class DecoratorNeedToRelease : Decorator
    {
        public DecoratorNeedToRelease(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return !StyxWoW.Me.IsGhost && StyxWoW.Me.IsDead;
        }
    }
}
