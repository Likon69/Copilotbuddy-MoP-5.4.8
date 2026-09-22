using System;
using System.Linq;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;

using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Tripper.Tools.Math;

using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.EOTS
{
    internal class EyeOfTheStorm : Battleground
    {
        #region Battleground Overrides

        private Blackspot _avoidFlagBlackspot;
        public override string Name { get { return BGBuddyResources.EyeOfTheStorm; } }

        public override int MapId { get { return 566; } }

        public override void Dispose()
        {
            BGBuddy.Instance.WorldStatesUpdated -= HandleWorldStatesUpdated;
            Statuses.Clear();
        }

        public override void Start()
        {
            LoadProfile();
            HandleWorldStatesUpdated();
            BGBuddy.Instance.WorldStatesUpdated += HandleWorldStatesUpdated;

            // Drop the blackspot down 50yds so we encompass the sides of the place.
            Vector3 boxCenter = Profile.Boxes[Side].First(b => b.Name == "Flag").Center;
            boxCenter.Z -= 50;
            _avoidFlagBlackspot = new Blackspot(boxCenter, 45, 100, "Avoid flag blackspot");
            BlackspotManager.AddBlackspots(new[] { _avoidFlagBlackspot });
        }

        protected override int BattlegroundRaidSize { get { return 15; } }

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
                            ret => BGBuddySettings.Instance.EotsLogicType,
                            new SwitchArgument<LogicType>(LogicType.Attack, CreateOffensiveLogic())),

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
                var lm = landmark.ToEyeOfTheStormLandmark();
                if (!Statuses.ContainsKey((int)lm.LandmarkType))
                {
                    Statuses.Add(
                        (int)lm.LandmarkType, new LandmarkInfo((int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()),this));
                }

                Statuses[(int)lm.LandmarkType] = new LandmarkInfo(
                    (int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this);
                Statuses[(int)lm.LandmarkType].Process();
            }
        }

        #endregion

        #region Offensive

        private bool AllBasesAreUncontrolled
        {
            get
                {
                    return
                        Statuses.Values.All(
                            s => s.Control == LandmarkControlType.Uncontrolled || s.Control == LandmarkControlType.Unknown);
                }
        }

        private Composite CreateOffensiveLogic()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => StyxWoW.Me.IsHorde && AllBasesAreUncontrolled,
                    new Action(ret => SetHotspot(EyeOfTheStormLandmarkType.BloodElfTower.ToString(), BGBuddyResources.StartOfGame))
                    ),
                new Decorator(
                    ret => StyxWoW.Me.IsAlliance && AllBasesAreUncontrolled,
                    new Action(ret => SetHotspot(EyeOfTheStormLandmarkType.MageTower.ToString(), BGBuddyResources.StartOfGame))
                    ),
                new Decorator(
                    ret => ClosestInBattle != null,
                    new Action(ret => SetHotspot(((EyeOfTheStormLandmarkType)ClosestInBattle.Type).ToString(), BGBuddyResources.Battle))
                    ),
                new PrioritySelector(
                    ctx => BiggestFight,
                    new Decorator(
                        ret => (WoWPoint)ret != WoWPoint.Zero,
                        new Action(ret => SetHotspot((WoWPoint)ret))
                        )),
                new Decorator(
                    ret => ClosestToDefend != null,
                    new Action(ret => SetHotspot(((EyeOfTheStormLandmarkType)ClosestToDefend.Type).ToString(), BGBuddyResources.NothingElseToDo))
                    ),
                new ActionAlwaysSucceed()
                );
        }

        #endregion
    }
}
