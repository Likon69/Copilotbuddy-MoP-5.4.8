using System;
using System.Collections.Generic;
using System.Linq;

using Bots.BGBuddy.HeatMap;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;

using CommonBehaviors.Actions;

using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.AB
{
    internal class ArathiBasin : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.ArathiBasin; } }

        public override int MapId { get { return 529; } }
        protected override int BattlegroundRaidSize { get { return 15; } }

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
            var randomLogic = random.NextDouble()*100;

            CurrentLogicType = randomLogic < 51 ? LogicType.Defend : LogicType.Attack;

            Logger.Write("Changed to : {0} Logic", CurrentLogicType);
        }

        #endregion

        #region Heatmap Overrides

        protected override float EnemyPlayerHeat { get { return IsHealer ? -0.13f : 0.10f; } }
        protected override float FriendlyPlayerHeat { get { return IsHealer ? 0.20f : -0.13f; } }

        protected override float ObjectiveHeat { get { return 0.60f; } }
        protected override float ObjectiveAssaultHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat : ObjectiveHeat * 0.5f; } }
        protected override float ObjectiveConflictedHeat { get { return ObjectiveHeat*1.2f; } }
        protected override float ObjectiveDefendHeat { get { return CurrentLogicType == LogicType.Attack ? ObjectiveHeat * 0.5f : ObjectiveHeat; } }

        protected override float EnemyPlayerRadius { get { return 30f; } }
        protected override float FriendlyPlayerRadius { get { return 30f; } }
        protected override float ObjectiveRadius { get { return 60f; } }

        #endregion

        #region Root Logic

        public bool IsHealer { get { return StyxWoW.Me.IsHealer; } }

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
                ArathiBasinLandmark lm = landmark.ToArathiBasinLandmark();
                if (!Statuses.ContainsKey((int) lm.LandmarkType))
                {
                    Statuses.Add(
                        (int) lm.LandmarkType,
                        new LandmarkInfo((int) lm.LandmarkType,
                            lm.ControlType,
                            GetLandmarkBox(lm.LandmarkType.ToString()),
                            this));
                }

                Statuses[(int) lm.LandmarkType] = new LandmarkInfo(
                    (int) lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this);
                Statuses[(int) lm.LandmarkType].Process();
            }
        }

        #endregion

        private ArathiBasinLandmarkType StartingBase
        {
            get
            {
                var random = new Random();
                var randomLogic = random.NextDouble()*100;

                if (randomLogic > 66)
                    return ArathiBasinLandmarkType.Blacksmith;

                if (randomLogic > 33)
                    return ArathiBasinLandmarkType.LumberMill;

                return ArathiBasinLandmarkType.GoldMine;
            }
        }

        #region Battle Logic

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
                new PrioritySelector(
                    ctx => GetHotBox(),
                    new Decorator(ret => ret != null,
                        new Action(ret => SetHotspot(((ArathiBasinLandmarkType) ((LandmarkInfo)ret).Type).ToString(), BGBuddyResources.Battle)))),

                new ActionAlwaysSucceed()
                );
        }

        #endregion
    }
}
