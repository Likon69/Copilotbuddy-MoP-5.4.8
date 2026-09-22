using System;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;

using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.BGBuddy.Logic.Battlegrounds.TP
{
    internal class TwinPeaks : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.TwinPeaks; } }

        public static MapBox AllianceBox = MapBox.fromBox(new Vector3(2178, 230, 43), new Vector3(2032, 184, 43), new Vector3(2118, 191, 44), "AllianceBox");
        public static MapBox HordeBox = MapBox.fromBox(new Vector3(1656, 384, 0), new Vector3(1537, 283, 3), new Vector3(1578, 343, 2), "HordeBox");

        public override int MapId { get { return 726; } }

        public override void Dispose()
        {
        }

        public override void Start()
        {
            LoadProfile();
            StartingLocation = RandomizeLocation(StartingLocation, 5);

            var random = new Random();
            int randomLogic = random.Next(0, 100);

            CurrentLogicType = randomLogic < 51 ? LogicType.Defend : LogicType.Attack;

            Logger.Write("Changed to : {0} Logic", CurrentLogicType);
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
                            ret => CurrentLogicType,
                            new SwitchArgument<LogicType>(LogicType.Attack, CreateOffensiveLogic()),
                            new SwitchArgument<LogicType>(LogicType.Defend, CreateDefensiveLogic())),

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
                        ret => ShouldDefendFriendlyFlagCarrier, // Healer Check
                        new Action(ret => SetHotspot(FriendlyFlagCarrier.Location, 10f, BGBuddyResources.DefendFriendlyFlagCarrier))
                        ),
                    new Decorator(
                        ret => ShouldFocusOnEnemyFlagCarrier, // All offensive move in to attack
                        new Action(ret => SetHotspot(EnemyFlagCarrier.Location, 10f, BGBuddyResources.EnemyFlagCarrier))
                        ),
                    new Decorator(
                        ret => ShouldAssultEnemyBase, // Assault Base
                        new Action(ret => SetHotspot(AssaultBaseLocation, 10f, "Moving to Assault Base"))
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

        #endregion

        #region Defensive Logic

        private Composite CreateDefensiveLogic()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => ShouldDefendFriendlyFlagCarrier,
                        new Action(ret => SetHotspot(FriendlyFlagCarrier.Location, 10f, BGBuddyResources.DefendFriendlyFlagCarrier))
                        ),
                    new Decorator(
                        ret => ShouldDefendFriendlyBase,
                        new Action(ret => SetHotspot(DefendBaseLocation, 10f, "Moving to Defend Base"))
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

        #endregion

        private bool ShouldDefendFriendlyFlagCarrier
        {
            get { return FriendlyFlagCarrier != null && ((StyxWoW.Me.SpecType == SpecType.Healer || CurrentLogicType == LogicType.Defend) && FriendlyFlagCarrier.PlayersDefending(10.0) < 5); }
        }

        private bool ShouldFocusOnEnemyFlagCarrier
        {
            get { return EnemyFlagCarrier != null; }
        }

        private bool ShouldDefendFriendlyBase
        {
            get
            {
                if (StyxWoW.Me.IsHorde)
                {
                    if (FriendlyFlagCarrier != null && !FriendlyFlagCarrier.WithinBox(HordeBox))
                        return false;

                    return FriendlyPlayersAround(HordeBox) < 4;
                }

                if (FriendlyFlagCarrier != null && !FriendlyFlagCarrier.WithinBox(AllianceBox))
                    return false;

                return FriendlyPlayersAround(AllianceBox) < 4;
            }
        }

        private bool ShouldAssultEnemyBase
        {
            get
            {
                if (StyxWoW.Me.IsHorde)
                {
                    if (EnemyFlagCarrier != null && !EnemyFlagCarrier.WithinBox(AllianceBox))
                        return false;

                    return FriendlyPlayersAround(AllianceBox) < 5 && EnemyPlayersAround(AllianceBox) < 6;
                }

                if (EnemyFlagCarrier != null && !EnemyFlagCarrier.WithinBox(HordeBox))
                    return false;

                return FriendlyPlayersAround(HordeBox) < 5 && EnemyPlayersAround(HordeBox) < 6;
            }
        }

        private WoWPoint DefendBaseLocation
        {
            get
            {
                if(StyxWoW.Me.IsHorde)
                    return new WoWPoint(1578.904, 343.8687, 2.421185);

                return new WoWPoint(2118.516, 191.6009, 44.05198);
            }
        }

        private WoWPoint AssaultBaseLocation
        {
            get
            {
                if (StyxWoW.Me.IsHorde)
                    return new WoWPoint(2118.516, 191.6009, 44.05198);

                return new WoWPoint(1578.904, 343.8687, 2.421185);
            }
        }

        public int FriendlyPlayersAround(MapBox box)
        {
            return RaidHelper.GetCountWithin(box);
        }

        public int EnemyPlayersAround(MapBox box)
        {
            return RaidHelper.GetEnemyCountWithin(box);
        }


    }
}
