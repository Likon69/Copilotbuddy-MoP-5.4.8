using System.Collections.Generic;
using System.Linq;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.Quest.Objectives
{
    public class UseGameObjectObjective : QuestObjective
    {
        private readonly UseObjectObjectiveInfo _overrideInfo;
        private WoWPoint? _cachedStepPosition;
        private WoWPoint? _cachedUnitPosition;
        private int _cacheRefresher;
        private Composite _branch;

        public UseGameObjectObjective(PlayerQuest quest, List<WoWQuestStep> questSteps, Styx.WoWInternals.Quest.QuestObjective questObjective, List<QuestObjective> prerequisites, WoWPoint forcedHotspot, NavType navType)
            : base(quest, questSteps, prerequisites, forcedHotspot, navType)
        {
            Objective = questObjective;
            if (OverridedQuestInfo != null)
                _overrideInfo = OverridedQuestInfo.FindUseGameObject(questObjective.ID);
        }

        public Styx.WoWInternals.Quest.QuestObjective Objective { get; private set; }

        public override bool IsCompleted
        {
            get
            {
                WoWDescriptorQuest data;
                return Quest.GetData(out data) && data.ObjectivesDone[Objective.Index] >= Objective.Count;
            }
        }

        public override bool CanComplete { get { return DonePrerequisites && GotCompletionInfo(); } }

        public override void Dispose()
        {
            if (BotPoi.Current.Type == PoiType.Hotspot)
                BotPoi.Clear("Done with use game object objective.");
        }

        public override WoWPoint GetObjectiveLocation()
        {
            List<WoWGameObject> gameObjects = (from gameObject in ObjectManager.GetObjectsOfType<WoWGameObject>()
                                               where gameObject.Entry == Objective.ID
                                               orderby gameObject.Location.Distance(StyxWoW.Me.Location)
                                               select gameObject).ToList();

            if (gameObjects.Count <= 0)
            {
                if (!_cachedStepPosition.HasValue)
                {
                    if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                    {
                        _cachedStepPosition = _overrideInfo.OverridedHotspots.FindClosestTo(StyxWoW.Me.Location);
                    }
                    else if (ForcedHotspot != WoWPoint.Empty)
                    {
                        _cachedStepPosition = ForcedHotspot;
                    }
                    else
                    {
                        WoWQuestStep questStep = GetClosestQuestStep();
                        var position = new Vector3(questStep.StepPosition.X, questStep.StepPosition.Y, 0f);
                        if (!HeightHelper.FindMeshHeight(ref position))
                        {
                            Logging.Write(QuestBotResources.UseGameObjectObjective_GetObjectiveLocation_Could_not_find_mesh_height_for_quest__0__on_step__1_, Quest.Name, questStep.PoiID);
                            _cachedStepPosition = WoWPoint.Zero;
                        }
                        else
                        {
                            _cachedStepPosition = position;
                        }
                    }
                }

                return _cachedStepPosition.Value;
            }

            if (!_cachedUnitPosition.HasValue || ++_cacheRefresher % 25 == 0)
            {
                WoWPoint myLocation = StyxWoW.Me.Location;
                WoWPoint closestLocation = gameObjects[0].Location;
                float closestDistance = myLocation.DistanceSqr(closestLocation);
                for (int i = 1; i < gameObjects.Count; i++)
                {
                    WoWPoint location = gameObjects[i].Location;
                    float distance = myLocation.DistanceSqr(location);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestLocation = location;
                    }
                }

                _cachedUnitPosition = closestLocation;
                _cacheRefresher = 0;
            }

            return _cachedUnitPosition.Value;
        }

        public override Composite CreateBranch()
        {
            return _branch ?? (_branch =
                new PrioritySelector(
                    context => (from gameObject in ObjectManager.GetObjectsOfType<WoWGameObject>()
                                where gameObject.Entry == Objective.ID
                                orderby gameObject.DistanceSqr
                                select gameObject).FirstOrDefault(),
                    new Decorator(
                        context => context != null && BotPoi.Current.Type != PoiType.Kill,
                        new PrioritySelector(
                            new Decorator(
                                context => ((WoWGameObject)context).WithinInteractRange,
                                new Sequence(
                                    new Mount.ActionLandAndDismount(),
                                    new DecoratorContinue(context => StyxWoW.Me.IsMoving,
                                        new Styx.TreeSharp.Action(context => Navigator.PlayerMover.MoveStop())),
                                    new ActionSetActivity(context => string.Format(QuestBotResources.UseGameObjectObjective_CreateBranch_Using__0__for_quest__1__, ((WoWGameObject)context).Name, Quest.Name)),
                                    new Sleep(500),
                                    new Styx.TreeSharp.Action(context => ((WoWGameObject)context).Interact()),
                                    new Sleep(500),
                                    new WaitContinue(10, context => !StyxWoW.Me.IsCasting, new Sleep(1000)),
                                    new ActionClearPoi("Successfully interacted"))),
                            new DecoratorIsNotPoiType(
                                PoiType.Interact,
                                new ActionSetPoi(true, context => new BotPoi((WoWGameObject)context, PoiType.Interact, NavType))),
                            new Sequence(
                                new ActionSetActivity(context => string.Format(QuestBotResources.UseGameObjectObjective_CreateBranch_Moving_to_game_object__0__for_quest__1__, ((WoWGameObject)context).Name, Quest.Name)),
                                new ActionMoveToPoi()))),
                    new DecoratorIsPoiType(
                        new[] { PoiType.None, PoiType.Hotspot },
                        new PrioritySelector(
                            new DecoratorIsNotPoiType(
                                PoiType.Hotspot,
                                new ActionSetPoi(true, context => new BotPoi(GetObjectiveLocation(), PoiType.Hotspot, NavType))),
                            new ActionMoveToPoi()))));
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.UseGameObjectObjective_ToString__UseGameObjectObjective_GameObjectId___0___Count___1__, Objective.ID, Objective.Count);
        }

        private bool GotCompletionInfo()
        {
            if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                return true;

            if (!QuestArea.HotspotsCreated)
                QuestArea.CreateHotspots();

            return QuestArea.Hotspots.Count > 0;
        }
    }
}
