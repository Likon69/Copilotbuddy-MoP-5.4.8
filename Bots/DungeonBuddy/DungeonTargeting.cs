using System;
using System.Collections.Generic;
using System.Linq;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy
{
    internal class DungeonTargeting : Targeting
    {
        private readonly WaitTimer _combatTimer = new WaitTimer(TimeSpan.FromMilliseconds(2000.0));
        private bool _wasInCombat;
        private readonly WaitTimer _bossCombatTimer = new WaitTimer(TimeSpan.FromMilliseconds(3000.0));

        private bool InCombat
        {
            get
            {
                bool combat = StyxWoW.Me.Combat;
                if (combat && !_wasInCombat)
                {
                    _combatTimer.Reset();
                }
                _wasInCombat = combat;
                if (combat)
                {
                    return _combatTimer.IsFinished;
                }
                return false;
            }
        }

        public override void Pulse()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                base.Pulse();
            }
        }

        protected override void DefaultRemoveTargetsFilter(List<WoWObject> objects)
        {
            objects.RemoveAll(delegate(WoWObject wowObject)
            {
                if (!wowObject.IsValid)
                {
                    return true;
                }
                WoWUnit unit = wowObject as WoWUnit;
                if (unit == null)
                {
                    return true;
                }
                if (wowObject.Entry == 53488)
                {
                    return true;
                }
                return unit.IsDead || unit.IsNonCombatPet || !unit.Attackable || !unit.CanSelect ||
                       unit.ControllingPlayer != null || Blacklist.Contains(unit, BlacklistFlags.Combat) || unit.IsFriendly;
            });
        }

        protected override void DefaultIncludeTargetsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
        {
            WoWPoint location = StyxWoW.Me.Location;
            bool isLeader = StyxWoW.Me.IsLeader();
            bool isInSelectedDungeon = isLeader && DungeonManager.SelectedDungeon != null && DungeonManager.CurrentDungeon == DungeonManager.SelectedDungeon;
            if (!BossManager.BossTimer.IsRunning && incomingObjects.OfType<WoWUnit>().Any(unit => unit.IsBoss() && unit.Combat))
            {
                if (!isLeader && !InCombat)
                {
                    _bossCombatTimer.Reset();
                }
                BossManager.BossTimer.Start();
            }

            foreach (WoWUnit unit in incomingObjects.Select(wowObject => wowObject.ToUnit()))
            {
                if (unit.IsCritter && !unit.IsTargetingMeOrPet && !unit.IsTargetingMyRaidMember && unit.ThreatInfo.ThreatStatus <= ThreatStatus.UnitNotInThreatTable)
                {
                    continue;
                }
                if (isLeader && isInSelectedDungeon)
                {
                    if (!InCombat)
                    {
                        if (Blacklist.Contains(unit, BlacklistFlags.Pull))
                        {
                            continue;
                        }
                        WoWPoint unitLocation = unit.Location;
                        if ((!unit.IsTargetingMyRaidMember && (unitLocation.DistanceSqr(location) > 1600f || ProfileManager.IsPointInPullBlackspot(unitLocation) || !unit.IsHostile)) ||
                            (!unit.IsFlying && !DungeonBot.DungeonNavigator.CanNavigateDistanceFully(location, unitLocation, 40f)))
                        {
                            continue;
                        }
                    }
                    else if (unit.ThreatInfo.RawPercent <= 0 && !unit.TaggedByMe && !unit.IsTargetingMyRaidMember && !unit.IsTargetingMeOrPet)
                    {
                        continue;
                    }
                }
                else if (!unit.Combat || (!unit.TaggedByMe && !unit.IsTargetingMyRaidMember && !unit.IsTargetingMeOrPet) || (!_bossCombatTimer.IsFinished && !isLeader))
                {
                    continue;
                }
                outgoingObjects.Add(unit);
            }
        }

        protected override void DefaultTargetWeight(List<TargetPriority> priorities)
        {
            List<WoWPlayer> tanks = GroupMember.GroupMembers
                .Where(member => member.IsTank && member.Player != null)
                .Select(member => member.Player)
                .ToList();
            List<WoWPlayer> healers = GroupMember.GroupMembers
                .Where(member => member.Player != null && member.IsHealer)
                .Select(member => member.Player)
                .ToList();
            bool isTank = tanks.Any(player => player.IsMe);
            bool isMelee = StyxWoW.Me.IsMelee();
            WoWPoint location = StyxWoW.Me.Location;

            foreach (TargetPriority priority in priorities)
            {
                WoWUnit unit = priority.Object.ToUnit();
                priority.Score = 100.0;
                if (isTank)
                {
                    priority.Score -= (int)unit.ThreatInfo.RawPercent;
                    if (!unit.IsTargetingMeOrPet && unit.IsTargetingMyRaidMember)
                    {
                        priority.Score += 300.0;
                    }
                    if (unit.ThreatInfo.RawPercent < 120 && !tanks.Select(tank => unit.CurrentTargetGuid == tank.Guid).Any())
                    {
                        priority.Score += 100.0;
                        if (unit.ThreatInfo.RawPercent < 100)
                        {
                            priority.Score += 100.0;
                            if (healers.Any(healer => healer.Guid == unit.CurrentTargetGuid))
                            {
                                priority.Score += 250.0;
                            }
                        }
                    }
                    if (unit.Guid == StyxWoW.Me.CurrentTargetGuid)
                    {
                        priority.Score += 75.0;
                    }
                    if (unit.Fleeing)
                    {
                        priority.Score += 100.0;
                    }
                    priority.Score -= Math.Min(Math.Pow(unit.Location.Distance(location), 1.7), 300.0) * 2.0;
                    continue;
                }

                priority.Score += PlayersAttacking(unit) * (5.0 / GroupMember.GroupMembers.Count) * 100.0;
                if (unit.Guid == StyxWoW.Me.CurrentTargetGuid)
                {
                    priority.Score += 25.0;
                }
                if (unit.Fleeing)
                {
                    priority.Score += 100.0;
                }
                priority.Score -= unit.HealthPercent * 2.0;
                double distanceMultiplier = tanks.Count == 0 ? 2.0 : 0.5;
                float combatRange = isMelee ? unit.MeleeRange() : 35f + unit.CombatReach + 1f;
                WoWPoint unitLocation = unit.Location;
                float distance = unitLocation.Distance(location);
                if (distance > combatRange)
                {
                    priority.Score -= Math.Min(Math.Pow(distance - combatRange, 1.6), 100.0) * distanceMultiplier;
                }
                byte threat = unit.ThreatInfo.RawPercent;
                if (threat > 200)
                {
                    priority.Score -= Math.Pow(threat - 200, 1.6);
                }
                double tankMultiplier = 1.5 / tanks.Count;
                foreach (WoWPlayer tank in tanks)
                {
                    if (tank != null && tank.IsAlive)
                    {
                        float tankDistance = tank.Location.Distance(unitLocation);
                        if (tankDistance > unit.MeleeRange())
                        {
                            priority.Score -= Math.Min(Math.Pow(tankDistance - unit.MeleeRange(), 1.7), 300.0) * tankMultiplier;
                        }
                        if (unit.CurrentTargetGuid == tank.Guid)
                        {
                            priority.Score += 100.0;
                        }
                    }
                }
            }
        }

        private static int PlayersAttacking(WoWUnit unit)
        {
            return GroupMember.GroupMembers.Count(member => member.Player != null && member.Player.CurrentTargetGuid == unit.Guid);
        }
    }
}
