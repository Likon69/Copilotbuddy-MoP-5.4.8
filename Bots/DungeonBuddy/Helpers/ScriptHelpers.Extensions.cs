using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Styx;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#if USE_DUNGEONBUDDY_DLL
using Bots.DungeonBuddy.Enums;
namespace Bots.DungeonBuddy.Helpers
#else
    using Bots.DungeonBuddy.Enums;
    namespace Bots.DungeonBuddy.Helpers
#endif
{
    public static partial class ScriptHelpers
    {
        /// <summary>Determines whether the bot is tank.</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the bot is tank; otherwise, <c>false</c>.</returns>
        public static bool IsTank(this LocalPlayer me)
        {
            return me.IsLeader();
        }

        /// <summary>
        /// Determines whether the bot should lead group... same as IsTank for backwards compatibility with scripts.
        /// </summary>
        /// <param name="me">Me.</param>
        /// <returns>
        ///   <c>true</c> if the bot should lead group; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeader(this LocalPlayer me)
        {
            GroupMember groupMember = GroupMembers.FirstOrDefault(member => member.Guid == me.Guid);
            if (groupMember != null)
                return groupMember.IsLeader();
            if (!IsFarming && !me.Specialization.IsTank())
                return me.Role.HasFlag(WoWPartyMember.GroupRole.Tank);
            return true;
        }


        /// <summary>Determines whether the bot is healer.</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the bot is healer; otherwise, <c>false</c>.</returns>
        public static bool IsHealer(this LocalPlayer me)
        {
            if ((me.Role & WoWPartyMember.GroupRole.Healer) <= WoWPartyMember.GroupRole.None)
                return me.Specialization.IsHealer();
            return true;
        }

        /// <summary>Determines whether the bot is follower.</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the bote is follower; otherwise, <c>false</c>.</returns>
        public static bool IsFollower(this LocalPlayer me) { return !StyxWoW.Me.IsLeader(); }

        /// <summary>Determines whether the bot is DPS.</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if bot is DPS; otherwise, <c>false</c>.</returns>
        public static bool IsDps(this LocalPlayer me)
        {
            if (!me.Role.HasFlag(WoWPartyMember.GroupRole.Damage))
                return me.Specialization.IsDps();
            return true;
        }

        /// <summary>My party role.</summary>
        /// <param name="me">Me.</param>
        /// <returns></returns>
        public static GroupRoleFlags Roles(this LocalPlayer me)
        {
            var role = GroupRoleFlags.None;

            if (me.IsLeader())
                role |= GroupRoleFlags.Leader;
            if (me.IsFollower())
                role |= GroupRoleFlags.Follower;

            if (me.IsTank())
                return role | GroupRoleFlags.Tank | GroupRoleFlags.Melee;

            if (me.IsHealer)
                return role | GroupRoleFlags.Healer | GroupRoleFlags.Ranged;

            if (me.IsRange())
                return role | GroupRoleFlags.Ranged | GroupRoleFlags.Dps;
            return role | GroupRoleFlags.Melee | GroupRoleFlags.Dps;
        }

        /// <summary>Determines whether the specified role has flag.</summary>
        /// <param name="role">The role.</param>
        /// <param name="flag">The flag.</param>
        /// <returns><c>true</c> if the specified role has flag; otherwise, <c>false</c>.</returns>
        /// <summary>Returns 'true' if player primarily uses melee range abilites</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the specified me is melee; otherwise, <c>false</c>.</returns>
        public static bool IsMelee(this LocalPlayer me)
        {
            return me.Specialization.IsMelee();
        }

        /// <summary>Returns 'true' if player primarily uses ranged abilites</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the specified me is range; otherwise, <c>false</c>.</returns>
        public static bool IsRange(this LocalPlayer me)
        {
            return !me.IsMelee();
        }

        /// <summary>Returns 'true' if player primarily uses melee range abilites and has a DPS role</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the specified me is melee and dps; otherwise, <c>false</c>.</returns>
        public static bool IsMeleeDps(this LocalPlayer me)
        {
            return me.IsDps() && me.IsMelee();
        }
        /// <summary>Returns 'true' if player primarily uses ranged abilites and has a DPS role</summary>
        /// <param name="me">Me.</param>
        /// <returns><c>true</c> if the specified me is range and dps; otherwise, <c>false</c>.</returns>
        public static bool IsRangeDps(this LocalPlayer me)
        {
            return me.IsDps() && me.IsRange();
        }

        public static bool HasRole(this LocalPlayer me, GroupRoleFlags role)
        {
            return (me.Roles() & role) == role;
        }

        #region GroupMember Role extentions

        /// <summary>party role.</summary>
        /// <param name="partyMember">The party member.</param>
        /// <returns></returns>
        public static GroupRoleFlags Roles(this GroupMember partyMember)
        {
            var role = GroupRoleFlags.None;
            var isLeader = partyMember.IsLeader();

            // tank should always be the leader.. ignore group leader.
            if (isLeader)
                role |= GroupRoleFlags.Leader;
            else
                role |= GroupRoleFlags.Follower;

            if (partyMember.IsTank)
                return role | GroupRoleFlags.Tank | GroupRoleFlags.Melee;

            if (partyMember.IsHealer)
                return role | GroupRoleFlags.Healer | GroupRoleFlags.Ranged;

            if (partyMember.IsRange)
                return role | GroupRoleFlags.Ranged | GroupRoleFlags.Dps;

            if (partyMember.IsMelee)
                return role | GroupRoleFlags.Melee | GroupRoleFlags.Dps;

            return role;
        }

        public static bool HasRole(this GroupMember groupMember, GroupRoleFlags role)
        {
            return (groupMember.Roles() & role) == role;
        }

        /// <summary>Determines whether the specified party member is tank.</summary>
        /// <param name="groupMember">The party member.</param>
        /// <returns></returns>
        public static bool IsLeader(this GroupMember groupMember)
        {
            if (groupMember.IsTank)
                return true;

            // if noone in party is tank spec then check for max health
            if (!InRoleBasedContent && !GroupMembers.Any(g => g.IsTank))
            {
                bool allHealerGroup = GroupMembers.All(member => member.IsHealer);

                var highestHpMember = GroupMembers.Where(g => !g.IsHealer || allHealerGroup).OrderByDescending(m => m.MaxHealth).FirstOrDefault();
                return highestHpMember != null && highestHpMember.Guid == groupMember.Guid;
            }
            return false;
        }

        public static string UnitId(this WoWPlayer player)
        {
            return GetUnitIdFromGuid(player.Guid);
        }

        static string GetUnitIdFromGuid(ulong guid)
        {
            if (guid == StyxWoW.Me.Guid)
                return "player";
            if (guid == StyxWoW.Me.CurrentTargetGuid)
                return "target";

            if (StyxWoW.Me.GroupInfo.IsInParty)
            {
                bool isInRaid = StyxWoW.Me.GroupInfo.IsInRaid;
                var guids = isInRaid ? StyxWoW.Me.GroupInfo.RaidMemberGuids : StyxWoW.Me.GroupInfo.PartyMemberGuids;
                var index = Array.IndexOf(guids, guid);

                if (index >= 0)
                    return (StyxWoW.Me.GroupInfo.IsInRaid ? "raid" : "party") + (index + 1);
            }

            return "none";
        }

        #endregion

        /// <summary>Melee range of unit</summary>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public static float MeleeRange(this WoWUnit unit)
        {
            if (unit == null)
                return 0f;
            if (unit.IsPlayer)
                return 3.5f;

            return Math.Max(4.5f, StyxWoW.Me.CombatReach + 1f + unit.CombatReach);
        }

        /// <summary>Max range of a spell on unit. Calculates hitbox</summary>
        /// <param name="spell">The spell.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public static float ActualMaxRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.IsMeleeSpell)
                return unit.MeleeRange();

            if (spell.MaxRange == 0f)
                return 0;

            return unit != null ? spell.MaxRange + unit.CombatReach + 1f : spell.MaxRange;
        }

        /// <summary>Min range of a spell on unit. Calculates hitbox.</summary>
        /// <param name="spell">The spell.</param>
        /// <param name="unit">The unit.</param>
        /// <returns></returns>
        public static float ActualMinRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.MinRange == 0)
                return 0;
            return unit != null ? spell.MinRange + unit.CombatReach + 1.6666667f : spell.MinRange;
        }

        /// <summary>Cancels the specified aura.</summary>
        /// <param name="aura">The aura.</param>
        public static void Cancel(this WoWAura aura)
        {
            if (aura != null && aura.Cancellable)
                Lua.DoString("for i=1,40 do local id=select(11,UnitAura('player',i)) if id == {0} then CancelUnitBuff('player',i) break end end", aura.SpellId);
        }

        /// <summary>Determines whether the specified targeting list is empty.</summary>
        /// <param name="targeting">The targeting.</param>
        /// <returns><c>true</c> if the specified targeting list is empty; otherwise, <c>false</c>.</returns>
        public static bool IsEmpty(this Targeting targeting) { return targeting == null || !targeting.TargetList.Any(); }

        /// <summary>
        /// Determines whether bot has a POI set.
        /// </summary>
        /// <param name="poi">The poi.</param>
        /// <returns></returns>
        public static bool NoPoi(this BotPoi poi) { return poi.Type == PoiType.None; }

        public static bool IsType(this PoiType poiType, params PoiType[] poiTypes)
        {
            return poiTypes.Contains(poiType);
        }

        public static bool HasQuestAvailable(this WoWUnit unit, bool lowLevel = false, bool repeatable = false)
        {
            switch (unit.QuestGiverStatus)
            {
                case QuestGiverStatus.LowLevelAvailable:
                    return lowLevel;
                case QuestGiverStatus.LowLevelAvailableRepeatable:
                    return lowLevel && repeatable;
                case QuestGiverStatus.AvailableRepeatable:
                    return repeatable;
                case QuestGiverStatus.Available:
                    return true;
                default:
                    return false;
            }
        }

        public static bool HasQuestTurnin(this WoWUnit unit, bool lowLevel = true, bool repeatable = false)
        {
            switch (unit.QuestGiverStatus)
            {
                case QuestGiverStatus.TurnInRepeatable:
                    return repeatable;
                case QuestGiverStatus.TurnInInvisible:
                case QuestGiverStatus.TurnIn:
                    return true;
                case QuestGiverStatus.LowLevelTurnInRepeatable:
                    return lowLevel && repeatable;
                default:
                    return false;
            }
        }

        #region WoWPoint Helpers

        /// <summary>Returns the dot product.</summary>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns></returns>
        public static float Dot(this WoWPoint point1, WoWPoint point2)
        {
            return point1.X * point2.X + point1.Y * point2.Y + point1.Z * point2.Z;
        }

        /// <summary>Returns the cross product.</summary>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns></returns>
        public static WoWPoint Cross(this WoWPoint point1, WoWPoint point2)
        {
            return new WoWPoint(point1.Y * point2.Z - point1.Z * point2.Y, point1.Z * point2.X - point1.X * point2.Z, point1.X * point2.Y - point1.Y * point2.X);
        }

        /// <summary>Gets the nearest point on line.</summary>
        /// <param name="point">The point.</param>
        /// <param name="lineStart">The line start.</param>
        /// <param name="lineEnd">The line end.</param>
        /// <returns></returns>
        public static WoWPoint GetNearestPointOnLine(this WoWPoint point, WoWPoint lineStart, WoWPoint lineEnd)
        {
            WoWPoint startToEnd = lineEnd - lineStart;
            WoWPoint startToPoint = point - lineStart;
            var magnitudeSqr = lineStart.DistanceSqr(lineEnd);
            var dot = startToEnd.Dot(startToPoint);
            var normalizedDistance = dot / magnitudeSqr;
            return new WoWPoint(lineStart.X + startToEnd.X * normalizedDistance, lineStart.Y + startToEnd.Y * normalizedDistance, lineStart.Z + startToEnd.Z * normalizedDistance);
        }

        /// <summary>Gets the nearest point on segment.</summary>
        /// <param name="point">The point.</param>
        /// <param name="segmentStart">The segment start point.</param>
        /// <param name="segmentEnd">The segment end point.</param>
        /// <returns></returns>
        /// 
        /// 
        public static WoWPoint GetNearestPointOnSegment(this WoWPoint point, WoWPoint segmentStart, WoWPoint segmentEnd)
        {
            var magnitudeSqr = segmentStart.DistanceSqr(segmentEnd);
            if (magnitudeSqr == 0.0)
                return segmentStart;
            //const float t = dot(p - v, w - v) / l2;

            WoWPoint startToPoint = point - segmentStart;
            WoWPoint startToEnd = segmentEnd - segmentStart;

            float t = startToPoint.Dot(startToEnd) / magnitudeSqr;

            if (t < 0f)
                return segmentStart;
            else if (t > 1)
                return segmentEnd;

            return new WoWPoint(segmentStart.X + startToEnd.X * t, segmentStart.Y + startToEnd.Y * t, segmentStart.Z + startToEnd.Z * t);
        }

        /// <summary>
        /// Determines whether point is left of a line.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="lineStart">The line start.</param>
        /// <param name="lineEnd">The line end.</param>
        /// <returns><c>true</c> if  point left of line; otherwise, <c>false</c>.</returns>
        public static bool IsPointLeftOfLine(this WoWPoint point, WoWPoint lineStart, WoWPoint lineEnd)
        {
            return ((lineEnd.X - lineStart.X) * (point.Y - lineStart.Y) -
                    (lineEnd.Y - lineStart.Y) * (point.X - lineStart.X)) > 0;
        }

        /// <summary>Returns the distance of a MeshNavigator generated path between 'from' and 'to' </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>The distance</returns>
        public static float? PathDistance(this WoWPoint from, WoWPoint to, float maxDistance = float.MaxValue)
        {
            return DungeonBot.DungeonNavigator.PathDistance(from, to, maxDistance);
        }
        #endregion
    }
}
