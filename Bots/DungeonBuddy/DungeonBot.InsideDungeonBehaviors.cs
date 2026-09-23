using Styx.TreeSharp;
using Styx.WoWInternals.DBC;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private bool _resetDungeon;

        private Composite CreateInsideDungeonBehavior()
        {
            return new Decorator(
                ctx =>
                {
                    Map currentMap = Me.CurrentMap;
                    return currentMap.IsDungeon || currentMap.IsRaid || currentMap.IsScenario;
                },
                new PrioritySelector(
                    CreateDeathBehavior(),
                    CreateLootBehavior(),
                    BossManager.CreateCheckForDeadBossBehavior(),
                    CreateEncounterBehavior(),
                    CreateCombatBehavior(),
                    CreateSummonedComsumableBehavior(),
                    CreateTankAndFollowBehavior()));
        }
    }
}
