using Bots.Grind;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Decorators.Combat
{
    public class DecoratorNeedToFindTarget : Decorator
    {
        public DecoratorNeedToFindTarget(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            if (!LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat))
                return false;

            WoWUnit firstUnit = Targeting.Instance.FirstUnit;
            return firstUnit != null
                && LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Combat)
                && LevelBot.BehaviorFlags.HasFlag(BehaviorFlags.Pull)
                && (!LevelbotSettings.Instance.GroundMountFarmingMode || !StyxWoW.Me.Mounted);
        }

        // Dead code in HB 5.4.8: this level-range check exists but CanRun above never calls it.
        // Preserved as-is per porting rules (no bug fixes).
        private static bool IsInLevelRange(WoWUnit unit, GrindArea area)
        {
            if (area == null)
                return false;

            int level = unit.Level;
            return level >= area.TargetMinLevel && level <= area.TargetMaxLevel;
        }
    }
}
