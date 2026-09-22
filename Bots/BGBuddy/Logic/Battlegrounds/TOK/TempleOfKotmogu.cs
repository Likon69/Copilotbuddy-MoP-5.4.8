using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;

using Styx;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;

using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.WSG
{
    internal class TempleOfKotmogu : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.TempleOfKotmogu; } }

        public override int MapId { get { return 998; } }

        public override void Dispose()
        {
        }

        public override void Start()
        {
            LoadProfile();
            StartingLocation = RandomizeLocation(StartingLocation, 5);
        }

        protected override int BattlegroundRaidSize { get { return 10; } }

        #endregion

        #region Root Logic

        public override Composite Logic
        {
            get
            {
                return
                    new Sequence(
                    // Actual logic for the bot. (Hotspot selection)
                        new Switch<LogicType>(
                            ret => BGBuddySettings.Instance.TOKLogicType,
                            new SwitchArgument<LogicType>(LogicType.Attack, CreateOffensiveLogic())),

                        new Decorator(
                            ret => BotPoi.Current.Type == PoiType.Hotspot,
                            BGBuddy.CreateMoveToLocationBehavior(ret => Hotspot, true, 5))
                        );
            }
        }

        #endregion

        #region Offensive

        private Composite CreateOffensiveLogic()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => ShouldDefendFriendlyOrbCarrier,
                        new Action(ret => SetHotspot(FriendlyOrbCarrier.Location, 10f, BGBuddyResources.DefendFriendlyFlagCarrier))
                        ),
                    new Decorator(
                        ret => ShouldFocusOnEnemyOrbCarrier,
                        new Action(ret => SetHotspot(EnemyOrbCarrier.Location, 10f, BGBuddyResources.EnemyFlagCarrier))
                        ),
                    new PrioritySelector(
                        ctx => BiggestFight,
                        new Decorator(
                            ret => (WoWPoint)ret != WoWPoint.Zero,
                            new Action(ret => SetHotspot((WoWPoint)ret))
                            )),
                    new PrioritySelector(
                        ctx => BiggestFriendlyPack,
                        new Decorator(
                            ret => (WoWPoint)ret != WoWPoint.Zero,
                            new Action(ret => SetHotspot((WoWPoint)ret, 10f, BGBuddyResources.BiggestFriendlyPack))
                            )),
                    new ActionAlwaysSucceed()
                    );
        }

        private bool ShouldDefendFriendlyOrbCarrier
        {
            get { return FriendlyOrbCarrier != null && StyxWoW.Me.SpecType == SpecType.Healer; }
        }

        private bool ShouldFocusOnEnemyOrbCarrier
        {
            get { return EnemyOrbCarrier != null && EnemyOrbCarrier.PlayersAttacking() >= 2; }
        }

        #endregion
    }
}
