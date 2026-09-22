using Bots.Grind;
using Styx;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.Decorators.Combat
{
    public class DecoratorNeedToFindTarget : Decorator
    {
        public DecoratorNeedToFindTarget(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            WoWUnit firstUnit = Targeting.Instance.FirstUnit;
            if (firstUnit == null)
                return false;

            if (LevelbotSettings.Instance.GroundMountFarmingMode && StyxWoW.Me.Mounted)
                return false;

            if (!LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat))
                return false;

            if (!LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Pull))
                return false;

            return true;
        }
    }
}
