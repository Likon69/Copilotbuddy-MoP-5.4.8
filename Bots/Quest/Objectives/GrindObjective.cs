using System;
using System.Collections.Generic;
using System.Linq;
using Bots.Grind;
using Bots.Quest.Resources;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWCache;
using Styx.WoWInternals.WoWObjects;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.Quest.Objectives
{
    public class GrindObjective : QuestObjective
    {
        private readonly KillMobObjectiveInfo _overrideInfo;
        private WoWPoint? _cachedStepPosition;

        public GrindObjective(PlayerQuest quest, List<WoWQuestStep> questSteps, Styx.WoWInternals.Quest.QuestObjective questObjective, List<QuestObjective> prerequisites, WoWPoint forcedHotspot, NavType navType)
            : base(quest, questSteps, prerequisites, forcedHotspot, navType)
        {
            Objective = questObjective;

            if (OverridedQuestInfo != null)
            {
                _overrideInfo = OverridedQuestInfo.FindKillMob(Objective.ID);
                if (_overrideInfo == null)
                {
                    foreach (KillMobObjectiveInfo overrideInfo in OverridedQuestInfo.Objectives.OfType<KillMobObjectiveInfo>().Where(info => info.Type == ObjectiveType.KillMob))
                    {
                        WoWCache.InfoBlock infoBlock = StyxWoW.Cache[CacheDb.Creature].GetInfoBlockById(overrideInfo.MobID);
                        if (infoBlock != null)
                        {
                            WoWCache.CreatureCacheEntry creature = infoBlock.Creature;
                            if (creature.GroupID[0] == Objective.ID || creature.GroupID[1] == Objective.ID)
                            {
                                _overrideInfo = overrideInfo;
                                break;
                            }
                        }
                    }
                }

                if (_overrideInfo == null && Quest.GetObjectives().Count(objective => objective.Type == Styx.WoWInternals.Quest.QuestObjectiveType.KillMob) == 1)
                    _overrideInfo = OverridedQuestInfo.Objectives.FirstOrDefault(info => info.Type == ObjectiveType.KillMob) as KillMobObjectiveInfo;
            }

            Targeting.Instance.IncludeTargetsFilter += GrindObjective_IncludeTargetsFilter;
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

        public List<WoWUnit> QuestUnits
        {
            get
            {
                return (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                        where IsKillMob(unit)
                              && unit.IsAlive
                              && !unit.TaggedByOther
                              && !Blacklist.Contains(unit.Guid, BlacklistFlags.Combat | BlacklistFlags.Pull)
                              && (unit.OwnedByUnit == null || unit.OwnedByUnit != null && !unit.OwnedByUnit.IsPlayer)
                        orderby unit.Location.Distance(StyxWoW.Me.Location)
                        select unit).ToList();
            }
        }

        public KillMobObjectiveInfo GetOverrideInfo()
        {
            return _overrideInfo;
        }

        public override void Dispose()
        {
            Targeting.Instance.IncludeTargetsFilter -= GrindObjective_IncludeTargetsFilter;
            StyxWoW.AreaManager.SetArea(null);
            if (BotPoi.Current.Type == PoiType.Hotspot)
                BotPoi.Clear("Done with grind objective.");
        }

        private void GrindObjective_IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            if (IsCompleted || StyxWoW.Me.IsBeingAttacked)
                return;

            PoiType type = BotPoi.Current.Type;
            if (type == PoiType.Buy || type == PoiType.Mail || type == PoiType.Repair || type == PoiType.Sell)
                return;

            foreach (WoWObject obj in incomingUnits)
            {
                var unit = obj as WoWUnit;
                if (unit != null && !unit.TaggedByOther && IsKillMob(unit))
                    outgoingUnits.Add(unit);
            }
        }

        public override void OnStart()
        {
            int myLevel = StyxWoW.Me.Level;
            GrindArea area;
            if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
            {
                area = new GrindArea(new HotspotManager(_overrideInfo.OverridedHotspots))
                {
                    TargetMaxLevel = _overrideInfo.TargetMaxLevel > 0 ? _overrideInfo.TargetMaxLevel : myLevel + 5,
                    TargetMinLevel = _overrideInfo.TargetMinLevel > 0 ? _overrideInfo.TargetMinLevel : Quest.Level - 5
                };
            }
            else if (ForcedHotspot != WoWPoint.Empty)
            {
                area = new GrindArea(new HotspotManager(new List<WoWPoint> { ForcedHotspot }));
            }
            else
            {
                QuestArea.CreateHotspots();
                QuestArea.TargetMaxLevel = myLevel + 5;
                QuestArea.TargetMinLevel = Quest.Level - 5;
                area = QuestArea;
            }

            StyxWoW.AreaManager.SetArea(area);
        }

        public override Composite CreateBranch()
        {
            return new DecoratorIsNotPoiType(
                new[] { PoiType.Loot, PoiType.Loot, PoiType.Skin },
                new ActionRunCoroutine(context => LevelBot.Roam(NavType)));
        }

        public override WoWPoint GetObjectiveLocation()
        {
            List<WoWUnit> units = (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
                                   where IsKillMob(unit)
                                   select unit).ToList();

            if (units.Count <= 0)
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
                            Logging.Write(QuestBotResources.GrindObjective_GetObjectiveLocation_Could_not_find_mesh_height_for_quest__0__on_step__1_, Quest.Name, questStep.PoiID);
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

            WoWPoint myLocation = StyxWoW.Me.Location;
            WoWPoint nearestLocation = units[0].Location;
            float nearestDistance = nearestLocation.DistanceSqr(myLocation);
            for (int i = 1; i < units.Count; i++)
            {
                WoWPoint location = units[i].Location;
                float distance = location.DistanceSqr(myLocation);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestLocation = location;
                }
            }

            return nearestLocation;
        }

        private bool GotCompletionInfo()
        {
            if (_overrideInfo != null && _overrideInfo.OverridedHotspots != null && _overrideInfo.OverridedHotspots.Count > 0)
                return true;

            if (!QuestArea.HotspotsCreated)
                QuestArea.CreateHotspots();

            return QuestArea.Hotspots.Count > 0;
        }

        private bool IsKillMob(WoWUnit unit)
        {
            if (unit is WoWPlayer)
                return false;

            int minLevel = _overrideInfo != null ? _overrideInfo.TargetMinLevel : int.MinValue;
            int maxLevel = _overrideInfo != null ? _overrideInfo.TargetMaxLevel : int.MaxValue;
            if (unit.Level < minLevel || unit.Level > maxLevel)
                return false;

            if (unit.Entry == Objective.ID)
                return true;

            if (_overrideInfo != null && unit.Entry == _overrideInfo.MobID)
                return true;

            WoWCache.CreatureCacheEntry cacheEntry;
            if (unit.GetCachedInfo(out cacheEntry))
                return cacheEntry.GroupID[0] == Objective.ID || cacheEntry.GroupID[1] == Objective.ID;

            return false;
        }

        public override string ToString()
        {
            return string.Format(QuestBotResources.GrindObjective_ToString__GrindObjective_MobID___0___Count___1__, _overrideInfo != null ? _overrideInfo.MobID : Objective.ID, Objective.Count);
        }
    }
}
