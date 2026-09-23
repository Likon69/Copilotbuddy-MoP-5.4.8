using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.Quest.Actions;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
#if USE_DUNGEONBUDDY_DLL
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Profiles.Handlers;

namespace Bots.DungeonBuddy.Helpers
#else
    using Bots.DungeonBuddy.Profiles;
    using Bots.DungeonBuddy.Profiles.Handlers;
    namespace Bots.DungeonBuddy.Helpers
#endif
{
    public static partial class ScriptHelpers
    {
        /// <summary>
        ///     Talks to a NPC and then escorts the npc
        /// </summary>
        /// <param name="escortNpcId"> The escort NPC id. </param>
        /// <param name="escortNpcLocation"> The escort NPC location. </param>
        /// <param name="endLocation"> The end location. </param>
        /// <returns> </returns>
        public static Composite CreateTankTalkToThenEscortNpc(uint escortNpcId, WoWPoint escortNpcLocation, WoWPoint endLocation)
        {
            return CreateTankTalkToThenEscortNpc(escortNpcId, 1, escortNpcLocation, endLocation, 10f);
        }

        /// <summary>
        ///     Talks to a NPC and then escorts the npc
        /// </summary>
        /// <param name="escortNpcId"> The escort NPC id. </param>
        /// <param name="gossipIndex"> index of the gossip to choose (0 based) </param>
        /// <param name="escortNpcLocation"> The escort NPC location. </param>
        /// <param name="endLocation"> The end location. </param>
        /// <param name="followDistance"> The follow distance. </param>
        /// <param name="isDoneCondition"> if this condition is is 'true' then the escort is considered done.. </param>
        /// <returns> </returns>
        public static Composite CreateTankTalkToThenEscortNpc(
            uint escortNpcId, int gossipIndex, WoWPoint escortNpcLocation, WoWPoint endLocation, float followDistance, Func<object, bool> isDoneCondition = null)
        {
            WoWUnit escortNpc = null;
            return new Decorator(context =>
            {
                escortNpc = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(unit => unit.IsAlive && unit.Entry == escortNpcId);
                return !(isDoneCondition != null ? isDoneCondition(context) : escortNpc == null || escortNpc.Location.DistanceSqr(endLocation) < 100f && !escortNpc.Combat);
            }, new ActionRunCoroutine(context => TankTalkToAndEscortNpc(escortNpc, escortNpcLocation, followDistance, gossipIndex)));
        }

        /// <summary>
        ///     Move and talk to npc
        /// </summary>
        /// <param name="unitId"> The unit id. </param>
        /// <returns> </returns>
        public static Composite CreateTalkToNpc(uint unitId)
        {
            return CreateTalkToNpc(unitId, 1);
        }

        /// <summary>
        ///     Move and talk to npc, selecting gossip at specified index
        /// </summary>
        /// cccc
        /// <param name="unitId"> The unit id. </param>
        /// <param name="gossipIndex"> Index of the gossip. </param>
        /// <returns> </returns>
        public static Composite CreateTalkToNpc(uint unitId, int gossipIndex)
        {
            return CreateTalkToNpc(context => ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == unitId).OrderBy(u => u.DistanceSqr).FirstOrDefault(), gossipIndex);
        }

        public static Composite CreateTalkToNpc(Func<object, WoWUnit> unitSelector = null)
        {
            return CreateTalkToNpc(unitSelector, 1);
        }

        public static Composite CreateTalkToNpc(Func<object, WoWUnit> unitSelector, int gossipIndex)
        {
            return new ActionRunCoroutine(context => TalkToNpc(unitSelector(context), gossipIndex));
        }

        /// <summary>
        ///     Move and talk to npc. For use with Sequences.
        /// </summary>
        /// <param name="unitId"> The unit id. </param>
        /// <returns> </returns>
        /// <summary>
        ///     Move and talk to npc, selecting gossip at specified index. For use with Sequences.
        /// </summary>
        /// <param name="unitId"> The unit id. </param>
        /// <param name="gossipIndex"> Index of the gossip. </param>
        /// <returns> </returns>
        [Obsolete("Get with the times and use the coroutine version (TalkToNpc)")]
        public static Composite CreateTalkToNpcContinue(uint unitId, int gossipIndex = 0)
        {
            return CreateTalkToNpcContinue(
                context => ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == unitId).OrderBy(u => u.DistanceSqr).FirstOrDefault(), gossipIndex);
        }

        [Obsolete("Use the coroutine version")]
        public static Composite CreateTalkToNpcContinue(Func<object, WoWUnit> unitSelector, int gossipIndex = 0)
        {
            WoWUnit npc = null;
            return new Sequence(
                ctx => npc = unitSelector != null ? unitSelector(ctx) : null,
                new DecoratorContinue(ctx => npc != null && !npc.WithinInteractRange, CreateMoveToContinue(ctx => true, unitSelector, true)),
                new DecoratorContinue(
                    ctx => npc != null && npc.WithinInteractRange,
                    new Sequence(
                        new Action(ctx => npc.Interact()),
                        new WaitContinue(2, ctx => GossipFrame.Instance.IsVisible, new ActionAlwaysSucceed()),
                        new Action(ret => GossipFrame.Instance.SelectGossipOption(gossipIndex)))));
        }

        /// <summary>
        ///     picks up a quest from object
        /// </summary>
        /// <param name="objectId"> The object WoWHead id. </param>
        /// <param name="questId"> The quest id, if 0 (default) then firest availeable quest is chosen. </param>
        public static Composite CreatePickupQuest(uint objectId, uint questId = 0u)
        {
            return CreatePickupQuest(context => ObjectManager.ObjectList.FirstOrDefault(u => u.Entry == objectId), questId);
        }

        /// <summary>
        ///     picks up a quest from object
        /// </summary>
        /// <param name="objSelector"> The object selector </param>
        /// <param name="questId"> The quest id, if 0 (default) then firest availeable quest is chosen. </param>
        public static Composite CreatePickupQuest(Func<object, WoWObject> objSelector, uint questId = 0u)
        {
            return new ActionRunCoroutine(context => PickupQuest(objSelector(context), questId));
        }

        /// <summary>
        ///     Turns in a quest at object
        /// </summary>
        /// <param name="objectId"> The object WoWHead id. </param>
        /// <param name="questId"> The quest Id. If 0 (default) then first completed quest is turned in. </param>
        /// <returns> </returns>
        public static Composite CreateTurninQuest(uint objectId, uint questId = 0u)
        {
            return CreateTurninQuest(context => ObjectManager.ObjectList.FirstOrDefault(u => u.Entry == objectId), questId);
        }

        /// <summary>
        ///     Turns in a quest at object
        /// </summary>
        /// <param name="objectSelector"> The object selector. </param>
        /// <param name="questId"> The quest Id. If 0 (default) then first completed quest is turned in. </param>
        /// <returns> </returns>
        public static Composite CreateTurninQuest(Func<object, WoWObject> objectSelector, uint questId = 0u)
        {
            return new ActionRunCoroutine(context => TurninQuest(objectSelector(context), questId));
        }

        /// <summary>
        ///     Completes a popup quest.
        /// </summary>
        /// <param name="questId"> The quest id. </param>
        /// <returns> </returns>
        public static Composite CreateCompletePopupQuest(uint questId)
        {
            return new Sequence(
                new Action(ctx => Lua.DoString("ShowQuestComplete(GetQuestLogIndexByID({0}))", questId)),
                new WaitContinue(1, ctx => false, new ActionAlwaysSucceed()),
                new ActionSelectReward(),
                new WaitContinue(1, ctx => false, new ActionAlwaysSucceed()),
                new Action(ctx => QuestFrame.Instance.CompleteQuest()),
                new WaitContinue(1, ctx => false, new ActionAlwaysSucceed()),
                new Action(ctx => QuestFrame.Instance.CompleteQuest())
                //new Action(ctx => Lua.DoString("CloseQuest()"))
                );
        }

        /// <summary>
        ///     Determines whether the quest in the questlog is complete
        /// </summary>
        /// <param name="log"></param>
        /// <param name="questId">The quest id.</param>
        /// <returns>
        ///     <c>true</c> if the quest found in questlog is complete
        /// </returns>
        public static bool IsQuestInLogComplete(uint questId)
        {
            var quest = StyxWoW.Me.QuestLog.GetQuestById(questId);
            return quest != null && quest.IsCompleted;
        }

        public static bool IsQuestObjectiveComplete(int objectiveIndex, uint questId)
        {
            PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(questId);
            if (quest == null)
            {
                return false;
            }
            if (quest.IsCompleted)
            {
                return true;
            }
            WoWDescriptorQuest data;
            if (!quest.GetData(out data))
            {
                return false;
            }
            WoWCache.QuestObjectiveInfo[] objectives = quest.InternalInfo.GetObjectives();
            objectiveIndex--;
            if (objectiveIndex < 0 || objectiveIndex >= objectives.Length)
            {
                throw new IndexOutOfRangeException("objectiveIndex is out of range");
            }
            return data.ObjectivesDone[objectiveIndex] >= objectives[objectiveIndex].RequiredCount;
        }

        /// <summary>
        ///     Determines whether specific quest is turned in.
        /// </summary>
        /// <param name="questId">The quest id.</param>
        /// <returns>
        ///     <c>true</c> if specific quest is turned in; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsQuestTurnedIn(uint questId)
        {
            return StyxWoW.Me.QuestLog.GetCompletedQuests().Any(q => q == questId);
        }

        /// <summary>
        ///     Determines whether the quest is currently in questlog
        /// </summary>
        /// <param name="questId">The quest id.</param>
        /// <returns>
        ///     <c>true</c> if the quest is found in the questlog
        /// </returns>
        public static bool HasQuest(uint questId)
        {
            return StyxWoW.Me.QuestLog.GetQuestById(questId) != null;
        }

        public static async Task<bool> AbandonLowLevelQuests()
        {
            if (!DungeonBuddySettings.Instance.AbandonLowLevelQuests)
            {
                return false;
            }
            foreach (PlayerQuest quest in LowLevelQuests)
            {
                bool canAbandon = Lua.GetReturnVal<bool>(string.Format("return CanAbandonQuest({0})", quest.Id), 0u);
                if (!canAbandon)
                {
                    Logger.Write("Unable to abandon quest: {0} Id: {1}", quest.Name, quest.Id);
                    continue;
                }
                Logger.Write("Abandoning quest: {0}", quest.Name);
                StyxWoW.Me.QuestLog.AbandonQuestById(quest.Id);
                await Coroutine.Sleep(Rnd.Next(900, 2000));
                return true;
            }
            return false;
        }

        public static async Task<bool> PickupQuest(WoWObject obj, uint questId = 0u)
        {
            if (QuestAbandonTimer.IsFinished)
            {
                bool abandonedQuest = await AbandonLowLevelQuests();
                QuestAbandonTimer.Reset();
                if (abandonedQuest)
                {
                    return true;
                }
            }
            if (!DungeonBuddySettings.Instance.PickupQuests || obj == null || !obj.IsValid)
            {
                return false;
            }
            if (questId != 0 && (StyxWoW.Me.QuestLog.ContainsQuest(questId) || StyxWoW.Me.QuestLog.GetCompletedQuests().Contains(questId)))
            {
                return false;
            }
            if (obj.DistanceSqr > 16.0)
            {
                MoveResult moveResult = await CommonCoroutines.MoveTo(obj.Location, string.Format("quest pickup {0}", obj.SafeName));
                return moveResult.IsSuccessful();
            }
            if (!QuestFrame.Instance.IsVisible && !GossipFrame.Instance.IsVisible)
            {
                obj.Interact();
                await Coroutine.Wait(4000, () => QuestFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible);
            }
            if (questId == 0 || GossipFrame.Instance.AvailableQuests.Any(quest => quest.Id == questId) || GossipFrame.Instance.IsVisible)
            {
                GossipQuestEntry availableQuest = GossipFrame.Instance.AvailableQuests.FirstOrDefault(quest => quest.Id == questId || questId == 0);
                if (availableQuest != null)
                {
                    GossipFrame.Instance.SelectAvailableQuest(availableQuest.Index);
                }
                await Coroutine.Sleep(1000);
            }
            if (!QuestFrame.Instance.IsVisible)
            {
                return false;
            }
            uint currentQuestId = QuestFrame.Instance.CurrentShownQuestId;
            bool isRequestedQuestShown = currentQuestId != 0 && (currentQuestId == questId || questId == 0);
            bool hasRequestedQuest = QuestFrame.Instance.AvailableQuests.Any(quest => quest.Id == questId || questId == 0);
            if (!isRequestedQuestShown && !hasRequestedQuest)
            {
                return false;
            }
            if (currentQuestId == 0)
            {
                int questIndex = questId != 0 ? QuestFrame.Instance.AvailableQuests.First(quest => quest.Id == questId).Index : 0;
                QuestFrame.Instance.SelectAvailableQuest(questIndex);
                await Coroutine.Sleep(500);
            }
            if (QuestCompleteButton.IsVisible)
            {
                await CompleteQuestUntilFrameChanges();
            }
            QuestFrame.Instance.AcceptQuest();
            await Coroutine.Sleep(2000);
            if (QuestFrame.Instance.IsVisible)
            {
                QuestFrame.Instance.Close();
            }
            if (GossipFrame.Instance.IsVisible)
            {
                GossipFrame.Instance.Close();
            }
            return true;
        }

        public static async Task<bool> TurninQuest(WoWObject obj, uint questId = 0u)
        {
            if ((questId != 0 && !StyxWoW.Me.QuestLog.ContainsQuest(questId)) || obj == null || !obj.IsValid)
            {
                return false;
            }
            if (obj.DistanceSqr > 16.0)
            {
                MoveResult moveResult = await CommonCoroutines.MoveTo(obj.Location, string.Format("quest turnin {0}", obj.SafeName));
                return moveResult.IsSuccessful();
            }
            if (!QuestFrame.Instance.IsVisible && !GossipFrame.Instance.IsVisible)
            {
                obj.Interact();
                await Coroutine.Wait(4000, () => QuestFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible);
            }
            bool gossipVisible = GossipFrame.Instance.IsVisible;
            bool questVisible = QuestFrame.Instance.IsVisible;
            List<GossipQuestEntry> activeQuests = gossipVisible ? GossipFrame.Instance.ActiveQuests : QuestFrame.Instance.ActiveQuests;
            bool hasRequestedQuest = activeQuests.Any(quest => questId == 0 || quest.Id == questId);
            bool canSelectGossipQuest = gossipVisible && hasRequestedQuest;
            bool canSelectQuestFrameQuest = questVisible && QuestFrame.Instance.CurrentShownQuestId == 0 && hasRequestedQuest;
            if (canSelectGossipQuest || canSelectQuestFrameQuest)
            {
                GossipQuestEntry activeQuest = (from quest in activeQuests
                                                 let playerQuest = StyxWoW.Me.QuestLog.GetQuestById((uint)quest.Id)
                                                 where playerQuest != null && playerQuest.IsCompleted && (quest.Id == questId || questId == 0)
                                                 select quest).FirstOrDefault();
                if (activeQuest != null)
                {
                    if (gossipVisible)
                    {
                        GossipFrame.Instance.SelectActiveGossipQuest(activeQuest.Index);
                    }
                    else
                    {
                        QuestFrame.Instance.SelectActiveQuest(activeQuest.Index);
                    }
                    Logger.Write("Completing {0} quest", activeQuest.Name);
                    await Coroutine.Sleep(2000);
                }
            }
            uint currentQuestId = QuestFrame.Instance.CurrentShownQuestId;
            if (QuestFrame.Instance.IsVisible && currentQuestId != 0)
            {
                await Coroutine.Sleep(1000);
                if (QuestCompleteButton.IsVisible)
                {
                    QuestFrame.Instance.ClickContinue();
                    await Coroutine.Sleep(1000);
                }
                await new ActionSelectReward().ExecuteCoroutine();
                await Coroutine.Sleep(1000);
                QuestFrame.Instance.CompleteQuest();
                await Coroutine.Sleep(1000);
            }
            if (QuestFrame.Instance.IsVisible)
            {
                QuestFrame.Instance.Close();
            }
            if (GossipFrame.Instance.IsVisible)
            {
                GossipFrame.Instance.Close();
            }
            return true;
        }

        private static async Task CompleteQuestUntilFrameChanges()
        {
            uint currentQuestId = QuestFrame.Instance.CurrentShownQuestId;
            if (currentQuestId == 0)
            {
                return;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (QuestFrame.Instance.IsVisible && QuestFrame.Instance.CurrentShownQuestId == currentQuestId && stopwatch.ElapsedMilliseconds < 10000L)
            {
                QuestFrame.Instance.CompleteQuest();
                await Coroutine.Sleep(500);
            }
        }

        public static async Task<bool> TalkToNpc(WoWUnit unit, params int[] gossipIndexes)
        {
            List<int> indexes = gossipIndexes.ToList();
            if (!indexes.Any())
            {
                indexes.Add(1);
            }
            if (!IsViable(unit))
            {
                return false;
            }
            if (!unit.WithinInteractRange)
            {
                MoveResult moveResult = await CommonCoroutines.MoveTo(unit.Location, string.Format("talk to {0}", unit.SafeName));
                return moveResult.IsSuccessful();
            }
            await StopMovingIfMoving();
            if (!GossipFrame.Instance.IsVisible)
            {
                unit.Interact();
                if (!await Coroutine.Wait(2000, () => GossipFrame.Instance.IsVisible))
                {
                    return false;
                }
                await SleepForRandomUiInteractionTime();
            }
            foreach (int index in indexes)
            {
                GossipFrame.Instance.SelectGossipOption(index - 1);
                await CommonCoroutines.SleepForLagDuration();
                await SleepForRandomUiInteractionTime();
            }
            Lua.DoString("CloseGossip()");
            return true;
        }

        public static async Task<bool> TankTalkToAndEscortNpc(uint escortNpcId, WoWPoint escortNpcStartLocation, float followDistance = 10f, params int[] gossipIndexes)
        {
            WoWUnit escortNpc = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(unit => unit.IsAlive && unit.Entry == escortNpcId);
            return await TankTalkToAndEscortNpc(escortNpc, escortNpcStartLocation, followDistance, gossipIndexes);
        }

        public static async Task<bool> TankTalkToAndEscortNpc(WoWUnit escortNpc, WoWPoint escortNpcStartLocation, float followDistance = 10f, params int[] gossipIndexes)
        {
            if (!StyxWoW.Me.IsTank())
            {
                return false;
            }
            if (escortNpc == null)
            {
                if (StyxWoW.Me.Location.DistanceSqr(escortNpcStartLocation) >= 16f)
                {
                    SetLeaderMoveToPoi(escortNpcStartLocation);
                    return false;
                }
                if (Targeting.Instance.IsEmpty())
                {
                    return true;
                }
            }
            else
            {
                WoWPoint escortLocation = escortNpc.Location;
                if (escortNpc.CanGossip && escortLocation.DistanceSqr(escortNpcStartLocation) < 25f)
                {
                    if (!escortNpc.WithinInteractRange)
                    {
                        SetLeaderMoveToPoi(escortLocation);
                        return false;
                    }
                    return await TalkToNpc(escortNpc, gossipIndexes);
                }
                WoWUnit attacker = ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(unit => unit != escortNpc && unit.CurrentTargetGuid == escortNpc.Guid)
                    .OrderBy(unit => unit.DistanceSqr)
                    .FirstOrDefault();
                if (attacker != null && BotPoi.Current.Type != PoiType.Kill)
                {
                    BotPoi.Current = new BotPoi(attacker, PoiType.Kill);
                }
                if (Targeting.Instance.IsEmpty())
                {
                    float distance = escortLocation.Distance(StyxWoW.Me.Location);
                    if (distance <= followDistance && escortNpc.InLineOfSight)
                    {
                        float minimumDistance = Math.Max(followDistance / 2f, followDistance - 5f);
                        if (distance < minimumDistance)
                        {
                            await StopMovingIfMoving();
                        }
                        return true;
                    }
                    MoveResult moveResult = await CommonCoroutines.MoveTo(escortLocation, escortNpc.SafeName);
                    return moveResult.IsSuccessful();
                }
            }
            return false;
        }

        private static readonly WaitTimer QuestAbandonTimer = new WaitTimer(TimeSpan.FromMinutes(15.0));
        private static readonly Frame QuestCompleteButton = new Frame("QuestFrameCompleteButton");
    }
}
