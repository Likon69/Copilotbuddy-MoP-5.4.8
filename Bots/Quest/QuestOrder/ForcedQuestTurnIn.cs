using System;
using System.Diagnostics;
using System.Linq;
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
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Quest.QuestOrder
{
    public class ForcedQuestTurnIn : ForcedBehavior
    {
        private bool? _isAutoComplete;
        private static readonly Random _random = new Random();
        private readonly Stopwatch _findObjectTimer = new Stopwatch();
        private readonly Frame _questButtonFrame = new Frame("QuestTitleButton1");
        private static readonly Frame _questContinueButtonFrame = new Frame("QuestFrameCompleteButton");
        private int _counter;

        public ForcedQuestTurnIn(uint questId, string questName, uint npcId, string turnInName, WoWPoint location, NavType navType = NavType.Run, bool isExplicitLocation = true)
        {
            QuestId = questId;
            QuestName = questName;
            NpcId = npcId;
            NpcName = turnInName;
            Location = location;
            NavType = navType;
            IsExplicitLocation = isExplicitLocation;
        }

        public override bool IsDone
        {
            get
            {
                bool isDone = !StyxWoW.Me.QuestLog.ContainsQuest(QuestId);
                if (isDone)
                    StyxWoW.Me.QuestLog.AddCompletedQuest(QuestId, true);
                return isDone;
            }
        }

        public uint QuestId { get; private set; }
        public string QuestName { get; private set; }
        public uint NpcId { get; private set; }
        public string NpcName { get; private set; }
        public WoWPoint Location { get; private set; }
        public NavType NavType { get; private set; }
        public bool IsExplicitLocation { get; private set; }

        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        public override void OnFinished()
        {
            BotPoi.Clear("Quest turn in completed.");
        }

        public override void OnStart()
        {
            TreeRoot.GoalText = GetGoalText();
        }

        private string GetGoalText()
        {
            Styx.WoWInternals.Quest quest = Styx.WoWInternals.Quest.FromId(QuestId);
            if (quest != null)
            {
                if (quest.IsAutoComplete)
                    return string.Format("Turning in quest - {0} (http://wowhead.com/quest={1})", quest.Name, quest.Id);
                return string.Format("Turning in Quest - {0} (http://wowhead.com/quest={1}) : To - {2} : ID - {3}", quest.Name, quest.Id, NpcName, NpcId);
            }

            if (!string.IsNullOrEmpty(QuestName))
                return string.Format("Turning in Quest - {0} (http://wowhead.com/quest={1}) : To - {2} : ID - {3}", QuestName, QuestId, NpcName, NpcId);

            return string.Format(QuestBotResources.ForcedQuestTurnIn_GetGoalText_Turning_in_quest_with_ID__0_, QuestId);
        }

        private bool IsAutoComplete()
        {
            if (!_isAutoComplete.HasValue)
            {
                Styx.WoWInternals.Quest quest = Styx.WoWInternals.Quest.FromId(QuestId);
                if (quest == null)
                    return false;
                _isAutoComplete = quest.IsAutoComplete;
            }

            return _isAutoComplete.Value;
        }

        private Composite CreateAutoCompleteBehavior()
        {
            return new PrioritySelector(
                new Decorator(context => !Me.QuestLog.ContainsQuest(QuestId), new ActionAlwaysSucceed()),
                new Decorator(
                    context => !QuestFrame.Instance.IsVisible || QuestFrame.Instance.CurrentShownQuestId != QuestId,
                    new Sequence(
                        new Styx.TreeSharp.Action(context => Lua.DoString("ShowQuestComplete(GetQuestLogIndexByID({0}))", QuestId)),
                        new Wait(3,
                            context => QuestFrame.Instance.IsVisible && QuestFrame.Instance.CurrentShownQuestId == QuestId,
                            new ActionAlwaysSucceed()))),
                new Sequence(
                    new Sleep(1000),
                    new DecoratorContinue(
                        QuestContinueButtonShown,
                        new Sequence(
                            new Styx.TreeSharp.Action(context => PressContinueButton(context)),
                            new Sleep(1000))),
                    new DecoratorContinue(
                        QuestHasRewards,
                        new Sequence(
                            new Levelbot.Actions.General.ActionSelectReward(),
                            new Sleep(500))),
                    new Sleep(1000),
                    new Styx.TreeSharp.Action(context => QuestFrame.Instance.CompleteQuest()),
                    new SleepForLagDuration(),
                    new WaitContinue(
                        2,
                        context => QuestFrame.Instance.IsVisible && QuestFrame.Instance.CurrentShownQuestId != QuestId,
                        new Sequence(
                            new Sleep(1000),
                            new Styx.TreeSharp.Action(context => QuestFrame.Instance.AcceptQuest()),
                            new Styx.TreeSharp.Action(context => Logging.Write("Accepted auto follow up quest")),
                            new Sleep(1000))),
                    new ActionClearPoi("Quest Completed")));
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
                            NpcResult npc = Query.GetNearestNpcById(NpcId, Location);
                            if (npc == null)
                            {
                                TreeRoot.Stop(string.Format("Could not find quest giver NPC with ID {0} in database.", NpcId));
                            }
                            else
                            {
                                Location = npc.Location;
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
                new Decorator(context => !Me.QuestLog.ContainsQuest(QuestId), new ActionAlwaysSucceed()),
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
                        new ActionSelectActiveQuest((int)QuestId),
                        new Decorator(
                            QuestFrameIsVisible,
                            new Sequence(
                                new Sleep(1000),
                                new DecoratorContinue(
                                    QuestContinueButtonShown,
                                    new Sequence(
                                        new Styx.TreeSharp.Action(context => PressContinueButton(context)),
                                        new Sleep(1000))),
                                new DecoratorContinue(
                                    QuestHasRewards,
                                    new Sequence(
                                        new Levelbot.Actions.General.ActionSelectReward(),
                                        new Sleep(500))),
                                new Styx.TreeSharp.Action(context => TurnInQuest(context)),
                                new Sleep(500),
                                new Styx.TreeSharp.Action(context => ClosePreviousFrames(context)),
                                new Styx.TreeSharp.Action(ClearTarget),
                                new ActionClearPoi("Quest Completed"))),
                        new Decorator(
                            context => GossipFrame.Instance != null
                                       && GossipFrame.Instance.IsVisible
                                       && GossipFrame.Instance.ActiveQuests.Any(quest => quest.Id == QuestId),
                            new ActionAlwaysSucceed()),
                        new Decorator(
                            context => QuestFrame.Instance != null
                                       && QuestFrame.Instance.IsVisible
                                       && QuestFrame.Instance.ActiveQuests.Any(quest => quest.Id == QuestId),
                            new ActionAlwaysSucceed()))),
                new Sequence(
                    new Styx.TreeSharp.Action(context => ((WoWObject)context).Interact()),
                    new WaitContinue(1, GossipFrameOrQuestIsVisible, new ActionAlwaysSucceed())));
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
                        new ActionSetPoi(true, context => new BotPoi(PoiType.QuestTurnIn, NavType)
                        {
                            Entry = NpcId,
                            Location = Location
                        })),
                    new Decorator(context => IsAutoComplete(), CreateAutoCompleteBehavior()),
                    new Decorator(
                        context => BotPoi.Current.AsObject != null
                            ? !BotPoi.Current.AsObject.WithinInteractRange
                            : Me.Location.DistanceSqr(BotPoi.Current.Location) > 16f,
                        new ActionMoveToPoi()),
                    new Decorator(
                        context => BotPoi.Current.AsObject == null && !IsExplicitLocation,
                        CreateFindObjectBehavior()),
                    new Decorator(
                        context => BotPoi.Current.AsObject != null
                                   && BotPoi.Current.AsObject.WithinInteractRange
                                   && (!(BotPoi.Current.AsObject is WoWUnit) || ((WoWUnit)BotPoi.Current.AsObject).HasQuestCursor),
                        CreateInteractBehavior()),
                    new ActionSetActivity("Waiting for quest NPC to appear to turn in quest"),
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
                && (GossipFrame.Instance.ActiveQuests.Count == 0 || GossipFrame.Instance.ActiveQuests.All(quest => quest.Id != QuestId))
                && (QuestFrame.Instance == null
                    || !QuestFrame.Instance.IsVisible
                    || QuestFrame.Instance.CurrentShownQuestId != QuestId
                    && (QuestFrame.Instance.ActiveQuests.Count == 0 || QuestFrame.Instance.ActiveQuests.All(quest => quest.Id != QuestId))))
            {
                return true;
            }

            if (QuestFrame.Instance != null
                && QuestFrame.Instance.IsVisible
                && QuestFrame.Instance.CurrentShownQuestId != QuestId
                && (QuestFrame.Instance.ActiveQuests.Count == 0 || QuestFrame.Instance.ActiveQuests.All(quest => quest.Id != QuestId)))
            {
                return true;
            }

            return false;
        }

        private bool IsFailPoi(object context)
        {
            BotPoi poi = BotPoi.Current;
            return poi.Type != PoiType.QuestTurnIn || poi.Entry != NpcId;
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

        private bool QuestFrameIsVisible(object context)
        {
            return QuestFrame.Instance.IsVisible;
        }

        private bool GossipFrameOrQuestIsVisible(object context)
        {
            if (GossipFrame.Instance.IsVisible || _questButtonFrame.IsVisible)
                return true;

            return QuestFrame.Instance.IsVisible;
        }

        private bool QuestContinueButtonShown(object context)
        {
            return _questContinueButtonFrame.IsVisible;
        }

        private RunStatus PressContinueButton(object context)
        {
            QuestFrame.Instance.ClickContinue();
            return RunStatus.Success;
        }

        private bool QuestHasRewards(object context)
        {
            return Lua.GetReturnVal<int>("return GetNumQuestChoices()", 0u) > 0;
        }

        private RunStatus TurnInQuest(object context)
        {
            if (QuestFrame.Instance.IsVisible && QuestFrame.Instance.CurrentShownQuestId == QuestId)
            {
                if (_counter++ == 5)
                {
                    Logging.Write(QuestBotResources.ForcedQuestTurnIn_TurnInQuest_Tried_to_complete_quest_5_times_with_no_success);
                    Logging.Write(QuestBotResources.ForcedQuestTurnIn_TurnInQuest_Closing_Questframe);
                    QuestFrame.Instance.Close();
                    _counter = 0;
                    return RunStatus.Failure;
                }

                QuestFrame.Instance.CompleteQuest();
                return RunStatus.Running;
            }

            _counter = 0;
            return RunStatus.Success;
        }

        private static RunStatus ClearTarget(object context)
        {
            if (!Me.GotTarget)
                return RunStatus.Success;

            Me.ClearTarget();
            return RunStatus.Running;
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.ForcedQuestTurnIn_ToString__ForcedQuestTurnIn_QuestId___0___QuestName___1__, QuestId, QuestName);
        }
    }
}
