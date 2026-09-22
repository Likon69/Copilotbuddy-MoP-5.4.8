using System;
using System.Collections.Generic;
using System.Linq;
using Bots.BGBuddy.Helpers;
using Styx;
using Styx.CommonBot;

using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.BGBuddy
{
    /*
     * Targeting works like so, in order of being called
     *
     * GetInitialObjectList - Return a list of initial objects for the targeting to use.
     * RemoveTargetsFilter - Remove anything that doesn't belong in the list.
     * IncludeTargetsFilter - If you want to include units regardless of the remove filter
     * WeighTargetsFilter - Weigh each target in the list.
     *
     */

    partial class BGBuddy
    {
        public void SetTargetingFilters()
        {
            Targeting.Instance = new BgTargeting();
        }

        public void RemoveTargetingFilters()
        {
            Targeting.Instance = new Targeting();
        }

        #region Nested type: BgTargeting

        public class BgTargeting : Targeting
        {
            public BgTargeting()
            {
                AllowEvents = false;
            }

            protected override sealed List<WoWObject> GetInitialObjectList()
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>(true, false).Where(p => !p.IsFriendly).Cast<WoWObject>().ToList();
            }

            private static readonly HashSet<ulong> UsedSiegeWeapons = new HashSet<ulong>();

            protected override sealed void DefaultRemoveTargetsFilter(List<WoWObject> units)
            {
                var meMounted = StyxWoW.Me.Mounted;
                var myGuid = StyxWoW.Me.Guid;
                var raidMemberGuids = StyxWoW.Me.GroupInfo.RaidMemberGuids;
                UsedSiegeWeapons.Clear();
                units.RemoveAll(o =>
                    {
                        var u = o as WoWUnit;

                        if (u == null)
                            return true;

                        // Npc checks are different
                        if (!u.IsPlayer)
                        {
                            var curTarGuid = u.CurrentTargetGuid;

                            if (!u.IsAlive)
                                return true;

                            var distSqr = u.DistanceSqr;
                            // Battleground Demolishers in SotA
                            if (u.Entry == 28781)
                            {
                                if (distSqr < 100 * 100)
                                {
                                    return false;
                                }
                            }

                            if (meMounted)
                                return true;

                            if (distSqr > 40 * 40)
                                return true;

                            if (Blacklist.Contains(u.Guid, BlacklistFlags.Combat))
                                return true;

                            if (curTarGuid == 0)
                                return true;

                            if (curTarGuid != myGuid && !raidMemberGuids.Contains(curTarGuid))
                                return true;

                            if (u.IsTotem)
                                return true;

                            if (u.IsPet || u.OwnedByRoot != null)
                                return true;

                            if (u.IsNonCombatPet)
                                return true;

                            return false;
                        }

                        // Deal with dead people... zomg movie reference);
                        if (u.IsDead)
                            return true;

                        if (u.Shapeshift == ShapeshiftForm.SpiritOfRedemption)
                            return true;

                        var transportGuid = u.TransportGuid;
                        if (transportGuid != 0)
                            UsedSiegeWeapons.Add(transportGuid);

                        // Ignore shit thats far away please...)
                        if (u.DistanceSqr > 60 * 60)
                            return true;

                        if (u.Mounted)
                            return true;

                        // Note: Update this!
                        //if (IsCrowdControlled(u) && !u.ActiveAuras.Keys.Any(a => a.ToLowerInvariant().Contains("flag")))
                        //    return true;

                        if (Blacklist.Contains(u.Guid, BlacklistFlags.Combat) && u.CurrentTargetGuid != myGuid)
                            return true;

                        if (u.IsPet)
                            return true;

                        if (u.IsTotem)
                            return true;

                        if (u.IsNonCombatPet)
                            return true;

                        if (u.IsOnTransport)
                            return true;

                        return false;
                    });
            }

            protected override sealed void DefaultIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
            {
                // Sigh...
                foreach (WoWObject iu in incomingUnits)
                {
                    outgoingUnits.Add(iu);
                }
            }

            private static readonly HashSet<uint> SiegeWeapons = new HashSet<uint>
                                                                    {
                                                                        28781, // Battleground Demolisher - SotA
                                                                        34802, // Glaive Thrower - IoC
                                                                        35273, // Glaive Thrower - IoC
                                                                        35069, // Siege Engine - IoC
                                                                        34776, // Siege Engine - IoC
                                                                        34775, // Demolisher - IoC
                                                                    };

            protected override void DefaultTargetWeight(List<TargetPriority> units)
            {
                const double BASE_CAPTURE_MODIFIER = 300;
                const double FLAG_CARRIER_MODIFIER = 200;
                const double CURRENT_TARGET_MODIFIER = 150;
                const double CASTED_HEALING_MODIFIER = 100;
                const double ATTACKER_MODIFIER = 50;
                const double MANA_USER_MODIFIER = 50;
                const double DISTANCE_MODIFIER = 3;
                const double HEALTH_MODIFIER = 1;

                var Me = StyxWoW.Me;
                var spec = Me.SpecType;

                var underAttack = ObjectManager.GetObjectsOfType<WoWPlayer>().Any(u => u.IsTargetingMeOrPet);
                foreach (var p in units)
                {
                    var u = p.Object.ToUnit();

                    // We really, *really* want to avoid these.
                    if (u.IsTotem || u.IsPet || u.IsNonCombatPet)
                    {
                        p.Score = -100000f;
                        continue;
                    }

                    var guid = u.Guid;
                    var distance = u.Distance;
                    // Npcs are last in prio.
                    if (!u.IsPlayer)
                    {
                        if (SiegeWeapons.Contains(u.Entry) && UsedSiegeWeapons.Contains(guid) &&
                            !underAttack && spec != SpecType.Healer && distance < 70)
                        {
                            p.Score = 5000f;
                            continue;
                        }

                        p.Score -= distance;
                        continue;
                    }

                    p.Score = 1000f;

                    if (u.ActiveAuras.Values.Any(a => a.Name.ToLowerInvariant().Contains("flag")))
                        p.Score += FLAG_CARRIER_MODIFIER;

                    // Deepwind Gorge Cart Carriers
                    if (u.ActiveAuras.Values.Any(a => a.Name.ToLowerInvariant().Contains("cart")))
                        p.Score += FLAG_CARRIER_MODIFIER;

                    // Not switching target too often, give some weight to current target
                    if (guid == Me.CurrentTargetGuid)
                        p.Score += CURRENT_TARGET_MODIFIER;

                    // Players attacking the unit
                    int attackers = GetPvPAttackersNumber(u);
                    p.Score += attackers * ATTACKER_MODIFIER;

                    // Distance Stuff
                    if (spec == SpecType.RangedDps)
                    {
                        // Range dps should act like a turret. Decrease the score of units out of spell range
                        if (distance > 35)
                        {
                            p.Score -= distance * DISTANCE_MODIFIER;
                        }
                    }
                    // Melee dps stuff here
                    else if (spec != SpecType.RangedDps)
                    {
                        // Shall decrease the weight of units further then 20 yards.
                        // We really don't want to run between targets all the time.
                        if (distance > 20)
                        {
                            p.Score -= distance * DISTANCE_MODIFIER * 3;
                        }
                        // Targets in melee range should have a lot more weight.
                        else if (distance > 10)
                        {
                            p.Score -= distance * DISTANCE_MODIFIER;
                        }
                    }

                    // Health
                    p.Score -= u.HealthPercent * HEALTH_MODIFIER;


                    // Give more weight to mana users.
                    var powerType = u.PowerType;
                    if (powerType == WoWPowerType.Mana)
                        p.Score += MANA_USER_MODIFIER;

                    //Give weight to anyone that is currently trying to capture bases
                    if (u.CastingSpellId == 98322) // Capturing ID : 98322
                        p.Score += BASE_CAPTURE_MODIFIER;

                    //Give weight to anyone that casts a healing spell
                    if (u.IsCastingHealingSpell)
                        p.Score += CASTED_HEALING_MODIFIER;


                }
            }

            private static bool IsCrowdControlled(WoWUnit unit)
            {
                var auras = unit.Auras.Values;

                return auras.Select(a => a.Spell.Mechanic).Any(
                    a => a == WoWSpellMechanic.Banished ||
                         a == WoWSpellMechanic.Charmed ||
                         a == WoWSpellMechanic.Horrified ||
                         a == WoWSpellMechanic.Incapacitated ||
                         a == WoWSpellMechanic.Polymorphed ||
                         a == WoWSpellMechanic.Sapped ||
                         a == WoWSpellMechanic.Shackled ||
                         a == WoWSpellMechanic.Asleep ||
                         a == WoWSpellMechanic.Frozen ||
                         a == WoWSpellMechanic.Invulnerable ||
                         a == WoWSpellMechanic.Invulnerable2 ||
                         a == WoWSpellMechanic.Turned
                    );
            }

            private static int GetPvPAttackersNumber(WoWUnit target)
            {
                IOrderedEnumerable<WoWUnit> tars = (
                                                       from u in ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                                                       where
                                                           u.CurrentTarget == target &&
                                                           u.IsFriendly != target.IsFriendly
                                                       orderby u.DistanceSqr ascending
                                                       select u
                                                   );
                return tars.Count();
            }
        }

        #endregion
    }
}
