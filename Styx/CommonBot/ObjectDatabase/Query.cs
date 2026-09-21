using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Styx.CommonBot.Database;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.World;

namespace Styx.CommonBot.ObjectDatabase
{
    public static class Query
    {
        private static SQLiteCommand _selectNpcByName =
            Connection.CreateParameterizedCommand("SELECT * FROM npcs WHERE Name LIKE '%@NAME%' LIMIT 1");

        /// <summary>Gets a npc by name. </summary>
        /// <remarks>Created 10/1/2010.</remarks>
        /// <param name="name">The name.</param>
        /// <returns>The npc by name.</returns>
        public static NpcResult GetNpcByName(string name)
        {
            using (var reader = Connection.ExecuteReaderWithParams(_selectNpcByName, name))
            {
                if (reader.Read())
                    return new NpcResult(reader);
                return null;
            }
        }

        private static SQLiteCommand _selectNpcById =
            Connection.CreateParameterizedCommand("SELECT * FROM npcs WHERE Entry = @ENTRY LIMIT 1");

        /// <summary>Gets a npc by its ID. </summary>
        /// <remarks>Created 10/1/2010.</remarks>
        /// <param name="id">The identifier.</param>
        /// <returns>The npc by identifier.</returns>
        public static NpcResult GetNpcById(uint id)
        {
            using (var reader = Connection.ExecuteReaderWithParams(_selectNpcById, id))
            {
                if (reader.Read())
                    return new NpcResult(reader);
                return null;
            }
        }

        private static SQLiteCommand _selectNearestById =
            Connection.CreateParameterizedCommand("SELECT * FROM npcs WHERE Entry = @ENTRY AND MapId = @MAPID ORDER BY VECTORDISTANCE(X,Y,Z,@X,@Y,@Z) ASC");

        public static NpcResult GetNearestNpcById(uint id, WoWPoint searchLocation)
        {
            JbnMap map = StyxWoW.WorldScene.WorldMap.GetMapAt(searchLocation);
            if (map == null)
                return null;

            int mapId = map.MapID;

            using (var reader = Connection.ExecuteReaderWithParams(_selectNearestById, id, mapId, searchLocation.X, searchLocation.Y, searchLocation.Z))
            {
                while (reader.Read())
                {
                    NpcResult npc = new NpcResult(reader);

                    if (Blacklist.Contains(e => (e.Flags & BlacklistFlags.NpcDatabase) != 0 && (ulong)e.Entry == (ulong)npc.Entry && e.MapId == npc.MapId && e.Location.DistanceSqr(npc.Location) < 1600f))
                        continue;

                    return npc;
                }

                return null;
            }
        }

        private static SQLiteCommand _selectNearestTrainerByClass =
            Connection.CreateParameterizedCommand("SELECT * FROM npcs WHERE MapId = @MAP_ID AND TrainerClass = @TRAINER_CLASS AND Level >= @LEVEL ORDER BY VECTORDISTANCE(X,Y,Z,@X,@Y,@Z) ASC");

        private static readonly Dictionary<NpcResult, bool> _npcClassTrainerCache = new Dictionary<NpcResult, bool>();

        /// <summary>Gets the nearest trainer. </summary>
        /// <remarks>Created 10/1/2010.</remarks>
        /// <param name="mapId">Identifier for the map.</param>
        /// <param name="searchLocation">The search location.</param>
        /// <param name="searchClass">The search class.</param>
        /// <returns>The nearest trainer.</returns>
        public static NpcResult GetNearestTrainer(uint mapId, WoWPoint searchLocation, WoWClass searchClass)
        {
            int minLevel = 0;
            // We need to make sure we 'ignore' trainers that can't teach us anything.

            // 4.0.1 update: All trainers are now level 8 when they stop teaching you useful stuff. (I think their level corresponds to the level of shit they can teach you.)
            // Setting this to 10 for safety's sake
            if (StyxWoW.Me.Level > 10)
                minLevel = 10;

            var myLoc = StyxWoW.Me.Location;

            using (var reader = Connection.ExecuteReaderWithParams(_selectNearestTrainerByClass, mapId, (int)searchClass, minLevel, searchLocation.X, searchLocation.Y, searchLocation.Z))
            {
                while (reader.Read())
                {
                    var closest = new NpcResult(reader);

                    if ((closest.NpcFlags & (uint)UnitNPCFlags.ClassTrainer) == 0)
                        continue;

                    if (Blacklist.Contains(e => (e.Flags & BlacklistFlags.NpcDatabase) != 0 && (ulong)e.Entry == (ulong)closest.Entry && e.MapId == closest.MapId && e.Location.DistanceSqr(closest.Location) < 1600f))
                        continue;

                    FactionTemplate factionTemplate = FactionTemplate.FromId(closest.Faction);
                    if (factionTemplate != null && (factionTemplate.GetReactionTowards(StyxWoW.Me) < WoWUnitReaction.Neutral || StyxWoW.Me.FactionTemplate.GetReactionTowards(factionTemplate) < WoWUnitReaction.Neutral))
                        continue;

                    if (_npcClassTrainerCache.ContainsKey(closest))
                    {
                        if (!_npcClassTrainerCache[closest])
                            continue;
                    }
                    else
                    {
                        var canNavigateFully = Navigator.CanNavigateFully(myLoc, closest.Location);
                        _npcClassTrainerCache.Add(closest, canNavigateFully);

                        // Can't get to the damned thing.
                        if (!canNavigateFully && !StyxWoW.Me.IsFlying)
                            continue;
                    }

                    return closest;
                }

                return null;
            }
        }

        private static SQLiteCommand _selectNearestByFlags =
            Connection.CreateParameterizedCommand("SELECT * FROM npcs WHERE MapId = @MAP_ID AND NpcFlags & @FLAG ORDER BY VECTORDISTANCE(X,Y,Z,@X,@Y,@Z) ASC LIMIT 25");

        private static readonly Dictionary<NpcResult, bool> _npcResultCache = new Dictionary<NpcResult, bool>();

        /// <summary>Gets a nearest npc. </summary>
        /// <remarks>Created 10/1/2010.</remarks>
        /// <param name="mapId">Identifier for the map.</param>
        /// <param name="searchLocation">The search location.</param>
        /// <param name="npcFlags">The npc flags.</param>
        /// <returns>The nearest npc.</returns>
        public static NpcResult GetNearestNpc(uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags)
        {
            WoWClass myClass = StyxWoW.Me.Class;
            if ((npcFlags & UnitNPCFlags.ClassTrainer) != UnitNPCFlags.None)
            {
                return GetNearestTrainer(mapId, searchLocation, myClass);
            }

            var myLoc = StyxWoW.Me.Location;

            using (var reader = Connection.ExecuteReaderWithParams(_selectNearestByFlags, mapId, (uint)npcFlags, searchLocation.X, searchLocation.Y, searchLocation.Z))
            {
                while (reader.Read())
                {
                    var npc = new NpcResult(reader);

                    if ((npcFlags & UnitNPCFlags.ClassTrainer) != UnitNPCFlags.None && npc.TrainerClass != (int)myClass)
                        continue;

                    if (Blacklist.Contains(e => (e.Flags & BlacklistFlags.NpcDatabase) != 0 && (ulong)e.Entry == (ulong)npc.Entry && e.MapId == npc.MapId && e.Location.DistanceSqr(npc.Location) < 1600f))
                        continue;

                    if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.VendorManager.Blacklist.Any(v => v.Entry == npc.Entry))
                        continue;

                    FactionTemplate factionTemplate = FactionTemplate.FromId(npc.Faction);
                    FactionTemplate myFactionTemplate = StyxWoW.Me.FactionTemplate;
                    if (factionTemplate != null && (factionTemplate.GetReactionTowards(StyxWoW.Me) < WoWUnitReaction.Neutral || myFactionTemplate.GetReactionTowards(factionTemplate) < WoWUnitReaction.Neutral))
                        continue;

                    if (_npcResultCache.ContainsKey(npc))
                    {
                        if (!_npcResultCache[npc])
                            continue;
                    }
                    else if (!StyxWoW.Me.IsFlying)
                    {
                        var canNavigateFully = Navigator.CanNavigateFully(myLoc, npc.Location);
                        _npcResultCache.Add(npc, canNavigateFully);

                        if (!canNavigateFully)
                            continue;
                    }

                    return npc;
                }
            }
            return null;
        }

        /// <summary>Gets a nearest npc. </summary>
        /// <remarks>Created 10/1/2010.</remarks>
        /// <param name="myFaction">my faction.</param>
        /// <param name="mapId">Identifier for the map.</param>
        /// <param name="searchLocation">The search location.</param>
        /// <param name="npcFlags">The npc flags.</param>
        /// <returns>The nearest npc.</returns>
        [Obsolete("Use GetNearestNpc(uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags) overload instead.")]
        public static NpcResult GetNearestNpc(FactionTemplate myFaction, uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags)
        {
            var myLoc = StyxWoW.Me.Location;

            using (var reader = Connection.ExecuteReaderWithParams(_selectNearestByFlags, mapId, (uint)npcFlags, searchLocation.X, searchLocation.Y, searchLocation.Z))
            {
                while (reader.Read())
                {
                    var npc = new NpcResult(reader);

                    if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.VendorManager.Blacklist.Any(v => v.Entry == npc.Entry))
                        continue;

                    if (myFaction.GetReactionTowards(FactionTemplate.FromId(npc.Faction)) < WoWUnitReaction.Neutral)
                        continue;

                    if (_npcResultCache.ContainsKey(npc))
                    {
                        if (!_npcResultCache[npc])
                            continue;
                    }
                    else
                    {
                        var canNavigateFully = Navigator.CanNavigateFully(myLoc, npc.Location);
                        _npcResultCache.Add(npc, canNavigateFully);

                        if (!canNavigateFully && !StyxWoW.Me.IsFlying)
                            continue;
                    }

                    return npc;
                }
            }
            return null;
        }

        private static SQLiteCommand _selectNearestMailbox =
            Connection.CreateParameterizedCommand("SELECT * FROM mailbox WHERE map = @MAP_ID ORDER BY VECTORDISTANCE(x,y,z,@X,@Y,@Z) ASC");

        public static MailboxResult GetNearestMailbox(uint mapId, WoWPoint searchLocation)
        {
            var myLoc = StyxWoW.Me.Location;

            using (var reader = Connection.ExecuteReaderWithParams(_selectNearestMailbox, mapId, searchLocation.X, searchLocation.Y, searchLocation.Z))
            {
                while (reader.Read())
                {
                    var mailbox = new MailboxResult(reader);

                    if (Blacklist.Contains(e => (e.Flags & BlacklistFlags.NpcDatabase) != 0 && e.MapId == mailbox.MapId && e.Location == mailbox.Location))
                        continue;

                    if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.MailboxManager.Blacklist.Any(m => m.Location == mailbox.Location))
                        continue;

                    if (IsHostileFaction(mailbox.FactionId))
                        continue;

                    if (!Navigator.CanNavigateFully(myLoc, mailbox.Location) && !StyxWoW.Me.IsFlying)
                        continue;

                    return mailbox;
                }
            }
            return null;
        }

        private static bool IsHostileFaction(uint factionId)
        {
            FactionTemplate myFactionTemplate = StyxWoW.Me.FactionTemplate;
            FactionTemplate factionTemplate = FactionTemplate.FromId(factionId);
            return factionTemplate != null && (factionTemplate.GetReactionTowards(StyxWoW.Me) < WoWUnitReaction.Neutral || myFactionTemplate.GetReactionTowards(factionTemplate) < WoWUnitReaction.Neutral);
        }
    }
}
