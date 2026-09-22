using Styx;
using Styx.TreeSharp;

namespace Levelbot.Decorators.Death
{
    public class DecoratorNeedToMoveToCorpse : Decorator
    {
        public DecoratorNeedToMoveToCorpse(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return StyxWoW.Me.IsGhost && StyxWoW.Me.Location.Distance(StyxWoW.Me.CorpsePoint) > 36f;
        }
    }
}
