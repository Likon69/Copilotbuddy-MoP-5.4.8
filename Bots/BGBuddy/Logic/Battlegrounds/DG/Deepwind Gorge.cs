using System;
using System.Linq;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;

using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.DG
{
    internal class DeepwindGorge : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.DeepwindGorge; } }

        public override int MapId { get { return 1105; } }

        public override void Dispose()
        {
            BGBuddy.Instance.WorldStatesUpdated -= HandleWorldStatesUpdated;
            Statuses.Clear();
        }

        public override void Start()
        {
            LoadProfile();
            StartingLocation = RandomizeLocation(StartingLocation, 3);
            HandleWorldStatesUpdated();
            BGBuddy.Instance.WorldStatesUpdated += HandleWorldStatesUpdated;

            var random = new Random();
            var randomLogic = random.NextDouble() * 100;

            CurrentLogicType = randomLogic < 51 ? LogicType.Defend : LogicType.Attack;

            Logger.Write("Changed to : {0} Logic", CurrentLogicType);
        }

        protected override int BattlegroundRaidSize { get { return 10; } }

        #endregion

        #region Heatmap Overrides

        protected override float EnemyPlayerHeat { get { return 0.10f; } }
        protected override float FriendlyPlayerHeat { get { return -0.13f; } }

        protected override float ObjectiveHeat { get { return 0.60f; } }
        protected override float ObjectiveAssaultHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat : ObjectiveHeat * 0.5f; } }
        protected override float ObjectiveConflictedHeat { get { return ObjectiveHeat * 1.2f; } }
        protected override float ObjectiveDefendHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat * 0.5f : ObjectiveHeat; } }

        protected override float EnemyPlayerRadius { get { return 30f; } }
        protected override float FriendlyPlayerRadius { get { return 30f; } }
        protected override float ObjectiveRadius { get { return 60f; } }

        #endregion

        #region Root Logic

        public override Composite Logic
        {
            get
            {
                return
                    new Sequence(
                        CreateBattleLogic(),
                        new Decorator(
                            ret => BotPoi.Current.Type == PoiType.Hotspot,
                            BGBuddy.CreateMoveToLocationBehavior(ret => Hotspot, true, 5))
                        );
            }
        }

        #endregion

        #region Common

        private readonly WaitTimer _updateTimer = new WaitTimer(TimeSpan.FromSeconds(2));
        private void HandleWorldStatesUpdated()
        {
            if (!_updateTimer.IsFinished)
                return;

            _updateTimer.Reset();
            // Fired whenever something changes (flag taken/returned, bases captured, etc)
            Styx.WoWInternals.Battlegrounds.LandMarks.Refresh();

            // Iterate, and refresh our landmark shit.
            foreach (WoWLandMark landmark in Styx.WoWInternals.Battlegrounds.LandMarks.LandmarkList)
            {
                var lm = landmark.ToDeepwindGorgeLandmark();
                if (!Statuses.ContainsKey((int)lm.LandmarkType))
                {
                    Statuses.Add(
                        (int)lm.LandmarkType, new LandmarkInfo((int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this));
                }

                Statuses[(int)lm.LandmarkType] = new LandmarkInfo(
                    (int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this);
                Statuses[(int)lm.LandmarkType].Process();
#if DEBUG
                Logging.Write("Name: {0}, ControlType: {1}", lm.LandmarkType, lm.ControlType);
#endif
            }
        }

        #endregion

        #region Battle Logic

        private DeepwindGorgeLandmarkType StartingBase
        {
            get
            {
                var random = new Random();
                var randomLogic = random.NextDouble() * 100;

                if (randomLogic > 50)
                    return DeepwindGorgeLandmarkType.CenterMine;

                return StyxWoW.Me.IsHorde ? DeepwindGorgeLandmarkType.GoblinMine : DeepwindGorgeLandmarkType.PandarenMine;
            }
        }

        private bool _selectedStartingBase;
        private Composite CreateBattleLogic()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => Statuses.Values.All(s => !s.ControlledByUs && s.Control != LandmarkControlType.InConflict),
                    new PrioritySelector(
                        new Decorator(ret => !_selectedStartingBase,
                            new Sequence(
                                new Action(ret => _selectedStartingBase = true),
                                new Action(ret => SetHotspot(StartingBase.ToString(), BGBuddyResources.StartOfGame)))),
                        new ActionAlwaysSucceed())),
                new Decorator(
                    ret => ShouldDefendFriendlyCartCarrier,
                    new Action(ret => SetHotspot(FriendlyFlagCarrier.Location, 10f, BGBuddyResources.DefendFriendlyFlagCarrier))
                    ),
                new Decorator(
                    ret => ShouldFocusOnEnemyCartCarrier,
                    new Action(ret => SetHotspot(EnemyFlagCarrier.Location, 10f, BGBuddyResources.EnemyFlagCarrier))
                    ),
                new PrioritySelector(
                    ctx => GetHotBox(),
                    new Decorator(ret => ret != null,
                        new Action(ret => SetHotspot(((DeepwindGorgeLandmarkType)((LandmarkInfo)ret).Type).ToString(), BGBuddyResources.Battle)))),
                new ActionAlwaysSucceed()
                );
        }

        private bool ShouldDefendFriendlyCartCarrier
        {
            get { return FriendlyFlagCarrier != null && StyxWoW.Me.SpecType == SpecType.Healer; }
        }

        private bool ShouldFocusOnEnemyCartCarrier
        {
            get { return EnemyFlagCarrier != null && EnemyFlagCarrier.PlayersAttacking() >= 2; }
        }

        #endregion
    }
}
