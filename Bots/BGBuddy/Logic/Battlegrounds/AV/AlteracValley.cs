using System;
using System.Linq;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Resources;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;

using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Bots.BGBuddy.Logic.Battlegrounds.AV
{
    internal class AlteracValley : Battleground
    {
        #region Battleground Overrides

        public override string Name { get { return BGBuddyResources.AlteracValley; } }

        public override int MapId { get { return 30; } }

        public override void Dispose()
        {
            BGBuddy.Instance.WorldStatesUpdated -= HandleWorldStatesUpdated;
            Statuses.Clear();
        }

        public override void Start()
        {
            LoadProfile();
            StartingLocation = RandomizeLocation(StartingLocation, 5);
            HandleWorldStatesUpdated();
            BGBuddy.Instance.WorldStatesUpdated += HandleWorldStatesUpdated;
            _startOfGame = true;
        }

        protected override int BattlegroundRaidSize { get { return 40; } }

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
                            ret => BGBuddySettings.Instance.AVLogicType,
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
                var lm = landmark.ToAlteracValleyLandmark();
                if (!Statuses.ContainsKey((int)lm.LandmarkType))
                {
                    Statuses.Add(
                        (int)lm.LandmarkType, new LandmarkInfo((int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this));
                }

                Statuses[(int)lm.LandmarkType] = new LandmarkInfo(
                    (int)lm.LandmarkType, lm.ControlType, GetLandmarkBox(lm.LandmarkType.ToString()), this);
                Statuses[(int)lm.LandmarkType].Process();
            }
        }

        #endregion

        #region Offensive

        private bool _startOfGame;
        private Composite CreateOffensiveLogic()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => _startOfGame,
                        new PrioritySelector(
                            new Decorator(
                                ret => StyxWoW.Me.IsHorde,
                                new PrioritySelector(
                                    new Decorator(
                                        ret => Statuses.ContainsKey((int)AlteracValleyLandmarkType.StonehearthOutpost) &&
                                               Statuses[(int)AlteracValleyLandmarkType.StonehearthOutpost].Box.Center.DistanceSqr(StyxWoW.Me.Location) <= 70*70 &&
                                               !ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => u.Entry == 11949 && u.IsAlive),
                                        new Sequence(
                                            new Action(ret => Logger.Write(BGBuddyResources.BalindaIsDead)),
                                            new Action(ret => _startOfGame = false))),
                                    new Action(ret => SetHotspot(AlteracValleyLandmarkType.StonehearthOutpost.ToString(), false, BGBuddyResources.BalindaStonehearth)))),
                            new Decorator(
                                ret => Statuses.ContainsKey((int)AlteracValleyLandmarkType.IcebloodGarrison) &&
                                       Statuses[(int)AlteracValleyLandmarkType.IcebloodGarrison].Box.Center.DistanceSqr(StyxWoW.Me.Location) <= 70 * 70 &&
                                       !ObjectManager.GetObjectsOfType<WoWUnit>().Any(u => u.Entry == 11947 && u.IsAlive),
                                new Sequence(
                                    new Action(ret => Logger.Write(CommonBehaviorsResources.GalvandarIsDead)),
                                    new Action(ret => _startOfGame = false))),
                            new Action(ret => SetHotspot(AlteracValleyLandmarkType.IcebloodGarrison.ToString(), false, BGBuddyResources.CaptainGalvangar)))),

                    // Horde Logic
                    new Decorator(
                        ret => StyxWoW.Me.IsHorde,
                        new PrioritySelector(
                            new Decorator(
                                ret => Statuses.Values.Count(
                                            l => ((AlteracValleyLandmarkType)l.Type & AlteracValleyLandmarkType.AllianceTowers) != 0 &&
                                                 l.Control == LandmarkControlType.Destroyed) >= 2,
                                new PrioritySelector(
                                    new Decorator(
                                        ret => ShallAssaultTo(AlteracValleyLandmarkType.StormpikeAidStation),
                                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.StormpikeAidStation.ToString(), false, BGBuddyResources.Assault))),
                                    new Action(ret => SetHotspot(AlteracValleyLandmarkType.DunBaldar.ToString(), false, BGBuddyResources.VanndarStormpike)))),
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.IcewingBunker),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.IcewingBunker.ToString(), false, BGBuddyResources.Assault))),
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.StormpikeGraveyard),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.StormpikeGraveyard.ToString(), false, BGBuddyResources.Assault))),
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.DunBaldarNorthBunker),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.DunBaldarNorthBunker.ToString(), false, BGBuddyResources.Assault))),
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.DunBaldarSouthBunker),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.DunBaldarSouthBunker.ToString(), false, BGBuddyResources.Assault))),
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.StormpikeAidStation),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.StormpikeAidStation.ToString(), false, BGBuddyResources.Assault))),
                            new PrioritySelector(
                                ctx => BiggestFight,
                                new Decorator(
                                    ret => (WoWPoint)ret != WoWPoint.Zero,
                                    new Action(ret => SetHotspot((WoWPoint)ret, 10f, BGBuddyResources.BiggestFight)))),
                            new Action(ret => SetHotspot(AlteracValleyLandmarkType.StormpikeAidStation.ToString(), CommonBehaviorsResources.NothingElseToDoWaitingTower)),
                            new ActionAlwaysSucceed()
                            )),
                    // Alliance Logic
                    new Decorator(
                        ret => Statuses.Values.Count(
                                    l => ((AlteracValleyLandmarkType)l.Type & AlteracValleyLandmarkType.HordeTowers) != 0 &&
                                         l.Control == LandmarkControlType.Destroyed) >= 2,
                        new PrioritySelector(
                            new Decorator(
                                ret => ShallAssaultTo(AlteracValleyLandmarkType.FrostwolfReliefHut),
                                new Action(ret => SetHotspot(AlteracValleyLandmarkType.FrostwolfReliefHut.ToString(), false, BGBuddyResources.Assault))),
                            new Action(ret => SetHotspot(AlteracValleyLandmarkType.FrostwolfKeep.ToString(), false, BGBuddyResources.GeneralDrekThar)))),
                    new Decorator(
                        ret => ShallAssaultTo(AlteracValleyLandmarkType.TowerPoint),
                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.TowerPoint.ToString(), false, BGBuddyResources.Assault))),
                    new Decorator(
                        ret => ShallAssaultTo(AlteracValleyLandmarkType.FrostwolfGraveyard),
                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.FrostwolfGraveyard.ToString(), false, BGBuddyResources.Assault))),
                    new Decorator(
                        ret => ShallAssaultTo(AlteracValleyLandmarkType.WestFrostwolfTower),
                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.WestFrostwolfTower.ToString(), false, BGBuddyResources.Assault))),
                    new Decorator(
                        ret => ShallAssaultTo(AlteracValleyLandmarkType.EastFrostwolfTower),
                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.EastFrostwolfTower.ToString(), false, BGBuddyResources.Assault))),
                    new Decorator(
                        ret => ShallAssaultTo(AlteracValleyLandmarkType.FrostwolfReliefHut),
                        new Action(ret => SetHotspot(AlteracValleyLandmarkType.FrostwolfReliefHut.ToString(), false, BGBuddyResources.Assault))),
                    new Decorator(
                        ret => BiggestFight != WoWPoint.Zero,
                        new Action(ret => SetHotspot(BiggestFight, 10f, BGBuddyResources.BiggestFight))),
                    new Action(ret => SetHotspot(AlteracValleyLandmarkType.FrostwolfReliefHut.ToString(), CommonBehaviorsResources.NothingElseToDoWaitingTower)),
                    new ActionAlwaysSucceed()
                    );
        }

        private LandmarkInfo Status(AlteracValleyLandmarkType landmark)
        {
            if (!Statuses.ContainsKey((int)landmark))
            {
                return null;
            }

            return Statuses[(int) landmark];
        }

        private bool ShallAssaultTo(AlteracValleyLandmarkType landmark)
        {
            var lmType = (int)landmark;

            if (!Statuses.ContainsKey(lmType))
                return false;

            var status = Statuses[lmType];

            if (!status.ControlledByEnemy)
                return false;

            return true;
        }

        #endregion
    }
}
