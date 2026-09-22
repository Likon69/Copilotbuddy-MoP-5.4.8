using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Levelbot.Decorators.Death
{
    public class DecoratorInstanceRelease : DecoratorNeedToRelease
    {
        public DecoratorInstanceRelease(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            if (!StyxWoW.Me.IsGhost && StyxWoW.Me.IsDead)
            {
                return !Lua.GetReturnVal<bool>("return IsInInstance()", 0U) && !Battlegrounds.IsInsideBattleground;
            }

            return base.CanRun(context);
        }
    }
}
