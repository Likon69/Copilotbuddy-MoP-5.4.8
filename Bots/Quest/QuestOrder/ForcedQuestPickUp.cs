using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Bots.Quest.Actions;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Database;
using Styx.CommonBot.Frames;
using Styx.CommonBot.ObjectDatabase;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.QuestOrder
{
    public class ForcedQuestPickUp : ForcedBehavior
    {
        private readonly Stopwatch _completedTimer = new Stopwatch();
        private static readonly Random _random = new Random();
        private readonly Stopwatch _findObjectTimer = new Stopwatch();
        private static readonly Frame _questButtonFrame = new Frame("QuestTitleButton1");
        private static readonly Frame _completeQuestButton = new Frame("QuestFrameCompleteQuestButton");
        private int _lastQuestId = -1;

        public ForcedQuestPickUp(uint questId, string questName, uint giverId, string giverName, WoWPoint giverLocation, QuestObjectType? giverType, NavType navType, bool isExplicitLocation = true)
        {
            QuestId = questId;
            QuestName = questName;
            GiverId = giverId;
            GiverName = giverName;
            GiverLocation = giverLocation;
            GiverType = giverType;
            NavType = navType;
            IsExplicitLocation = isExplicitLocation;
        }

        public override bool IsDone
        {
            get
            {
                if (StyxWoW.Me.QuestLog.GetCompletedQuests().Contains(QuestId))
                    return true;

                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(QuestId);
                if (quest == null)
                    return false;

                if (quest.IsCompleted)
                {
                    if (!_completedTimer.IsRunning)
                        _completedTimer.Restart();
                    else if (_completedTimer.ElapsedMilliseconds > 1000L)
                        return true;
                }

                WoWQuestPOIInfo questInfo;
                if (!quest.GetQuestPOIInfo(out questInfo))
                    return false;

                return true;
            }
        }

        public uint QuestId { get; private set; }
        public string QuestName { get; private set; }
        public uint GiverId { get; private set; }
        public string GiverName { get; private set; }
        public WoWPoint GiverLocation { get; private set; }
        public QuestObjectType? GiverType { get; private set; }
        public NavType NavType { get; private set; }
        public bool IsExplicitLocation { get; private set; }

        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        public override void OnFinished()
        {
            BotPoi.Clear("Quest pickup completed.");
        }

        public override void OnStart()
        {
            if (StyxWoW.Me.QuestLog.GetAllQuests().Count >= 25)
            {
                Logging.Write(Colors.Red, QuestBotResources.ForcedQuestPickUp_OnStart_You_do_not_have_any_space_in_your_quest_log_);
                Logging.Write(Colors.Red, QuestBotResources.Honorbuddy_stopped);
                TreeRoot.Stop(QuestBotResources.ForcedQuestPickUp_OnStart_You_do_not_have_any_space_in_your_quest_log_);
            }

            TreeRoot.GoalText = GetGoalText();
        }

        private string GetGoalText()
        {
            Styx.WoWInternals.Quest quest = Styx.WoWInternals.Quest.FromId(QuestId);
            if (GiverType == QuestObjectType.Item)
            {
                WoWItem item = StyxWoW.Me.CarriedItems.FirstOrDefault(carriedItem => carriedItem.Entry == GiverId);
                if (item != null && !string.IsNullOrEmpty(QuestName))
                    return string.Format(QuestBotResources.ForcedQuestPickUp_GetGoalText_Picking_up_quest__0__from_item__1_, QuestName, item.Name);
            }

            string giverType = GiverType == QuestObjectType.GameObject ? "object" : "npc";
            if (quest != null)
                return string.Format("Picking Up Quest - {0} (http://wowhead.com/quest={1}) : From - {2} (http://wowhead.com/{3}={4})", quest.Name, quest.Id, GiverName, giverType, GiverId);

            if (!string.IsNullOrEmpty(QuestName))
                return string.Format("Picking Up Quest - {0} (http://wowhead.com/quest={1}) : From - {2} (http://wowhead.com/{3}={4})", QuestName, QuestId, GiverName, giverType, GiverId);

            return string.Format(QuestBotResources.ForcedQuestPickUp_GetGoalText_Picking_up_quest_with_ID__0_, QuestId);
        }

        private Composite CreateFindObjectBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    context => _findObjectTimer.IsRunning && _findObjectTimer.Elapsed.TotalMinutes > 5.0,
                    new Sequence(
                        new Styx.TreeSharp.Action(context =>
                            Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(5.0), "Quest giver not found in location")),
                        new Styx.TreeSharp.Action(context =>
                        {
                            NpcResult npc = Query.GetNearestNpcById(GiverId, GiverLocation);
                            if (npc == null)
                            {
                                TreeRoot.Stop(string.Format("Could not find quest giver NPC with ID {0} in database.", GiverId));
                            }
                            else
                            {
                                GiverLocation = npc.Location;
                                BotPoi.Clear("Npc couldn't be found in the provided location. Going for another location of the npc.");
                                _findObjectTimer.Reset();
                            }
                        }))),
                new Decorator(
                    context => !_findObjectTimer.IsRunning,
                    new Styx.TreeSharp.Action(context => _findObjectTimer.Restart())));
        }

        private Composite CreateInteractBehavior()
        {
            return new PrioritySelector(
                context => BotPoi.Current.AsObject,
                new Decorator(context => Me.QuestLog.ContainsQuest(QuestId), new ActionAlwaysSucceed()),
                new Mount.ActionLandAndDismount(),
                new Decorator(
                    context => ((WoWObject)context).DistanceSqr < 1.0,
                    new Styx.TreeSharp.Action(context =>
                    {
                        WoWObject obj = (WoWObject)context;
                        WoWPoint location = _random.Next(0, 2) < 1
                            ? WoWMathHelper.CalculatePointInFront(obj.Location, obj.Rotation, 3f)
                            : WoWMathHelper.CalculatePointBehind(obj.Location, obj.Rotation, 3f);
                        Navigator.MoveTo(location);
                    })),
                new Decorator(
                    context => StyxWoW.Me.IsMoving,
                    new Styx.TreeSharp.Action(context => WoWMovement.MoveStop())),
                new Decorator(
                    context => context is WoWUnit
                               && ((WoWUnit)context).CanSelect
                               && (StyxWoW.Me.CurrentTarget == null || StyxWoW.Me.CurrentTarget != (WoWUnit)context),
                    new Sequence(
                        new Styx.TreeSharp.Action(context => ((WoWUnit)context).Target()),
                        new Wait(2, context => StyxWoW.Me.CurrentTarget == (WoWUnit)context, new ActionAlwaysSucceed()))),
                new Decorator(
                    context => !(context is WoWItem) && !StyxWoW.Me.IsSafelyFacing((WoWObject)context),
                    new Sequence(
                        new Styx.TreeSharp.Action(context => StyxWoW.Me.SetFacing(((WoWObject)context).Location)),
                        new WaitContinue(2, context => StyxWoW.Me.IsSafelyFacing((WoWObject)context), new ActionAlwaysSucceed()))),
                new Decorator(context => NeedToCloseFrames(), new Styx.TreeSharp.Action(context => ForceCloseFrames(context))),
                new Decorator(
                    GossipFrameOrQuestIsVisible,
                    new PrioritySelector(
                        new ActionSelectAvailableQuest((int)QuestId),
                        new Decorator(
                            QuestFrameIsVisible,
                            new Sequence(
                                new DecoratorContinue(
                                    ShownQuestIsClickThrough,
                                    new Sequence(
                                        new Styx.TreeSharp.Action(context => CompleteClickThroughQuest(context)),
                                        new Styx.TreeSharp.Action(context => ClosePreviousFrames(context)))),
                                new Sleep(500),
                                new Styx.TreeSharp.Action(context => QuestFrame.Instance.AcceptQuest()),
                                new Sleep(500),
                                new Styx.TreeSharp.Action(context => ClosePreviousFrames(context)),
                                new Styx.TreeSharp.Action(context => ClearTarget(context)),
                                new ActionAlwaysSucceed())),
                        new Decorator(
                            context => GossipFrame.Instance != null
                                       && GossipFrame.Instance.IsVisible
                                       && GossipFrame.Instance.AvailableQuests.Any(quest => quest.Id == QuestId),
                            new ActionAlwaysSucceed()),
                        new Decorator(
                            context => QuestFrame.Instance != null
                                       && QuestFrame.Instance.IsVisible
                                       && QuestFrame.Instance.CurrentShownQuestId == QuestId,
                            new ActionAlwaysSucceed()))),
                new Sequence(
                    new Styx.TreeSharp.Action(context => ((WoWObject)context).Interact()),
                    new WaitContinue(2, GossipFrameOrQuestIsVisible, new Sleep(500))));
        }

        protected override Composite CreateBehavior()
        {
            return new DecoratorIsNotPoiType(
                new[]
                {
                    PoiType.Harvest,
                    PoiType.Skin,
                    PoiType.Loot,
                    PoiType.Kill,
                    PoiType.Buy,
                    PoiType.Fly,
                    PoiType.Mail,
                    PoiType.Repair,
                    PoiType.Sell
                },
                new PrioritySelector(
                    new Decorator(
                        IsFailPoi,
                        new ActionSetPoi(true, context => new BotPoi(new PickUpNode(GiverLocation, GiverId, null, GiverType, QuestId, QuestName, NavType)))),
                    new Decorator(
                        context => GiverType == QuestObjectType.Item,
                        new Sequence(
                            context => ObjectManager.GetObjectsOfType<WoWItem>().FirstOrDefault(item => item.Entry == GiverId),
                            new DecoratorContinue(
                                context => context != null,
                                new Sequence(
                                    new Styx.TreeSharp.Action(context => ((WoWItem)context).UseContainerItem()),
                                    new WaitContinue(5, QuestFrameIsVisible,
                                        new Styx.TreeSharp.Action(context => QuestFrame.Instance.AcceptQuest())),
                                    new WaitContinue(2, context => false, new ActionAlwaysSucceed()))))),
                    new Decorator(
                        context => BotPoi.Current.AsObject != null
                            ? !BotPoi.Current.AsObject.WithinInteractRange
                            : Me.Location.DistanceSqr(BotPoi.Current.Location) > 16f,
                        new ActionMoveToPoi()),
                    new Decorator(
                        context => BotPoi.Current.AsObject == null
                                   && !IsExplicitLocation
                                   && (!GiverType.HasValue || GiverType == QuestObjectType.Npc),
                        CreateFindObjectBehavior()),
                    new Decorator(
                        context => BotPoi.Current.AsObject != null
                                   && BotPoi.Current.AsObject.WithinInteractRange
                                   && (!(BotPoi.Current.AsObject is WoWUnit) || ((WoWUnit)BotPoi.Current.AsObject).HasQuestCursor),
                        CreateInteractBehavior()),
                    new ActionSetActivity("Waiting for object to appear to pick up quest from"),
                    new Decorator(
                        context => BotPoi.Current.AsObject != null,
                        new Styx.TreeSharp.Action(context =>
                        {
                            _findObjectTimer.Reset();
                            return RunStatus.Failure;
                        })),
                    new ActionIdle()));
        }

        private bool NeedToCloseFrames()
        {
            if (GossipFrame.Instance != null
                && GossipFrame.Instance.IsVisible
                && (GossipFrame.Instance.AvailableQuests.Count == 0 || GossipFrame.Instance.AvailableQuests.All(quest => quest.Id != QuestId))
                && (QuestFrame.Instance == null
                    || !QuestFrame.Instance.IsVisible
                    || QuestFrame.Instance.CurrentShownQuestId != QuestId
                    && (QuestFrame.Instance.AvailableQuests.Count == 0 || QuestFrame.Instance.AvailableQuests.All(quest => quest.Id != QuestId))))
            {
                return true;
            }

            if (QuestFrame.Instance != null
                && QuestFrame.Instance.IsVisible
                && QuestFrame.Instance.CurrentShownQuestId != QuestId
                && (QuestFrame.Instance.AvailableQuests.Count == 0 || QuestFrame.Instance.AvailableQuests.All(quest => quest.Id != QuestId)))
            {
                return true;
            }

            return false;
        }

        private bool IsFailPoi(object context)
        {
            BotPoi poi = BotPoi.Current;
            return poi.Type != PoiType.QuestPickUp || poi.Entry != GiverId;
        }

        private RunStatus ClosePreviousFrames(object context)
        {
            return ForceCloseFrames(context);
        }

        private RunStatus ForceCloseFrames(object context)
        {
            if (!GossipFrame.Instance.IsVisible && !QuestFrame.Instance.IsVisible)
                return RunStatus.Success;

            GossipFrame.Instance.Close();
            QuestFrame.Instance.Close();
            return RunStatus.Success;
        }

        private bool GossipFrameIsVisible(object context)
        {
            return GossipFrame.Instance.IsVisible || _questButtonFrame.IsVisible;
        }

        private bool GossipFrameOrQuestIsVisible(object context)
        {
            if (GossipFrame.Instance.IsVisible || _questButtonFrame.IsVisible)
                return true;

            return QuestFrame.Instance.IsVisible;
        }

        private RunStatus SelectQuestFromGossipFrame(object context)
        {
            if (!GossipFrame.Instance.IsVisible && !_questButtonFrame.IsVisible)
                return RunStatus.Success;

            List<GossipQuestEntry> gossipQuests = GossipFrame.Instance.AvailableQuests;
            int index = -1;
            if (gossipQuests.Count > 0)
            {
                for (int i = 0; i < gossipQuests.Count; i++)
                {
                    if (gossipQuests[i].Id == QuestId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                    return RunStatus.Failure;

                GossipFrame.Instance.SelectAvailableQuest(index);
                StyxWoW.Sleep(500);
                return RunStatus.Running;
            }

            return RunStatus.Success;
        }

        private bool QuestFrameIsVisible(object context)
        {
            return QuestFrame.Instance.IsVisible;
        }

        private bool ShownQuestIsClickThrough(object context)
        {
            return _completeQuestButton.IsVisible;
        }

        private RunStatus CompleteClickThroughQuest(object context)
        {
            if (_lastQuestId == -1)
                _lastQuestId = (int)QuestFrame.Instance.CurrentShownQuestId;

            if (QuestFrame.Instance.IsVisible && _lastQuestId == QuestFrame.Instance.CurrentShownQuestId)
            {
                QuestFrame.Instance.CompleteQuest();
                StyxWoW.Sleep(500);
                return RunStatus.Running;
            }

            _lastQuestId = -1;
            return RunStatus.Success;
        }

        private RunStatus ClearTarget(object context)
        {
            if (!StyxWoW.Me.GotTarget)
                return RunStatus.Success;

            StyxWoW.Me.ClearTarget();
            StyxWoW.Sleep(300);
            return RunStatus.Running;
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.ForcedQuestPickUp_ToString__ForcedQuestPickUp_QuestId___0___QuestName___1__, QuestId, QuestName);
        }
    }
}
