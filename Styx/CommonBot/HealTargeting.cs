using System;
using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public class HealTargeting : Targeting
    {
        private static HealTargeting _instance;

        public new static HealTargeting Instance
        {
            get { return _instance ?? (_instance = new HealTargeting()); }
            set { _instance = value; }
        }

        protected override List<WoWObject> GetInitialObjectList()
        {
            return ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Cast<WoWObject>().ToList();
        }

        public override void Pulse()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                base.Pulse();
            }
        }

        protected override void DefaultRemoveTargetsFilter(List<WoWObject> units)
        {
            WoWPoint myLocation = StyxWoW.Me.Location;

            units.RemoveAll(o =>
            {
                if (!o.IsValid)
                {
                    return true;
                }

                WoWUnit unit = o as WoWUnit;
                return unit == null || myLocation.DistanceSqr(unit.Location) > CollectionRange * CollectionRange || unit.IsDead || !unit.CanSelect || !unit.IsFriendly;
            });
        }

        protected override void DefaultIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            foreach (WoWUnit unit in incomingUnits.Select(o => o.ToUnit()))
            {
                if (unit.IsPet)
                {
                    WoWPlayer owner = unit.OwnedByRoot as WoWPlayer;
                    if (owner != null && owner.IsInMyPartyOrRaid)
                    {
                        outgoingUnits.Add(unit);
                    }
                }

                WoWPlayer player = unit as WoWPlayer;
                if (player != null && player.IsInMyPartyOrRaid)
                {
                    outgoingUnits.Add(unit);
                }
            }
        }

        private static bool IsTankSpec(WoWSpec spec)
        {
            if (spec <= WoWSpec.WarriorProtection)
            {
                if (spec == WoWSpec.PaladinProtection || spec == WoWSpec.WarriorProtection)
                {
                    return true;
                }
            }
            else
            {
                if (spec == WoWSpec.DruidGuardian || spec == WoWSpec.DeathKnightBlood)
                {
                    return true;
                }

                if (spec == WoWSpec.MonkBrewmaster)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void DefaultTargetWeight(List<TargetPriority> units)
        {
            List<WoWPlayer> tanks = StyxWoW.Me.GroupInfo.RaidMembers
                .Select(member => new { Member = member, Player = member.ToPlayer() })
                .Where(pair => pair.Player != null && (pair.Member.HasRole(WoWPartyMember.GroupRole.Tank) || IsTankSpec(pair.Member.Specialization)))
                .Select(pair => pair.Player)
                .ToList();

            WoWPoint location = StyxWoW.Me.Location;

            foreach (TargetPriority targetPriority in units)
            {
                targetPriority.Score = 100.0;
                WoWUnit unit = targetPriority.Object.ToUnit();
                float predictedHealthPercent = unit.GetPredictedHealthPercent(false);
                WoWPlayer player = unit as WoWPlayer;

                if (player == null)
                {
                    targetPriority.Score -= 50.0;
                }
                else if (tanks.Contains(player) && predictedHealthPercent < 70f)
                {
                    targetPriority.Score += 20.0;
                }

                float maxHealRange = 35f + unit.CombatReach + 1f;
                float distance = location.Distance(unit.Location);
                if (distance > maxHealRange)
                {
                    targetPriority.Score -= Math.Min(Math.Pow(distance - maxHealRange, 1.6) * 0.5, 100.0);
                }

                targetPriority.Score -= predictedHealthPercent;
            }
        }
    }
}
