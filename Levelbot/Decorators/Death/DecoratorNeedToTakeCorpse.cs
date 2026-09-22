using Styx;
using Styx.TreeSharp;

namespace Levelbot.Decorators.Death
{
    public class DecoratorNeedToTakeCorpse : Decorator
    {
        public DecoratorNeedToTakeCorpse(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            return StyxWoW.Me.IsGhost && StyxWoW.Me.CorpsePoint.Distance(StyxWoW.Me.Location) <= 36f;
        }
    }
}
