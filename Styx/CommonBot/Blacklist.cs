using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Styx.Common;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public static class Blacklist
    {
        public static void Flush()
        {
            _blacklist.RemoveAll(e => e.IsFinished);
        }

        public static BlacklistEntry GetEntry(WoWObject o)
        {
            return GetEntry(o.Guid);
        }

        public static void Clear(EntryRemovalDelegate del)
        {
            foreach (BlacklistEntry entry in _blacklist.Where(e => del(e)).ToList())
            {
                _blacklist.Remove(entry);
            }
        }

        public static BlacklistEntry GetEntry(ulong guid)
        {
            BlacklistEntry entry = _blacklist.FirstOrDefault(e => e.Guid == guid);
            if (entry == null)
            {
                return null;
            }

            if (entry.IsFinished)
            {
                _blacklist.Remove(entry);
                return null;
            }

            return entry;
        }

        public static bool Contains(Func<BlacklistEntry, bool> func)
        {
            return _blacklist.Any(e => !e.IsFinished && func(e));
        }

        public static bool Contains(WoWObject o, BlacklistFlags flags)
        {
            return Contains(o.Guid, flags);
        }

        public static bool Contains(ulong guid, BlacklistFlags flags)
        {
            WoWObject wo = ObjectManager.GetObjectByGuid(guid);
            Profiles.Profile currentProfile = Profiles.ProfileManager.CurrentProfile;
            AreaManagement.GrindArea currentGrindArea = StyxWoW.AreaManager.CurrentGrindArea;
            if (wo != null)
            {
                BlacklistFlags entryFlags;
                if (currentProfile != null && currentProfile.Blacklist.TryGetValue(wo.Entry, out entryFlags) && entryFlags.HasFlag(flags))
                {
                    return true;
                }

                if (currentGrindArea != null && currentGrindArea.Blacklist.TryGetValue(wo.Entry, out entryFlags) && entryFlags.HasFlag(flags))
                {
                    return true;
                }

                int? cachedMapId = null;
                WoWPoint? cachedLocation = null;
                BlacklistEntry areaEntry = _blacklist.FirstOrDefault(e =>
                {
                    if (e.Guid != 0UL || e.Entry != wo.Entry)
                    {
                        return false;
                    }

                    WoWPoint location = (cachedLocation ?? (cachedLocation = wo.Location)).Value;
                    if (e.Location != location)
                    {
                        return false;
                    }

                    int mapId = (cachedMapId ?? (cachedMapId = GetMapId(location) ?? -1)).Value;
                    return e.MapId == mapId;
                });

                if (areaEntry != null)
                {
                    if (!areaEntry.IsFinished)
                    {
                        return true;
                    }

                    _blacklist.Remove(areaEntry);
                }
            }

            BlacklistEntry blacklistEntry = _blacklist.FirstOrDefault(e => e.Guid == guid);
            if (blacklistEntry == null)
            {
                return false;
            }

            if (blacklistEntry.IsFinished)
            {
                _blacklist.Remove(blacklistEntry);
                return false;
            }

            return (blacklistEntry.Flags & flags) != (BlacklistFlags)0;
        }

        private static int? GetMapId(WoWPoint point)
        {
            Styx.WoWInternals.World.JbnMap map = StyxWoW.WorldScene.WorldMap.GetMapAt(point);
            if (map == null)
            {
                return null;
            }

            return map.MapID;
        }

        [Obsolete("Use Contains(WoWObject, BlacklistFlags) instead.")]
        public static bool Contains(WoWObject o)
        {
            return Contains(o.Guid, BlacklistFlags.All);
        }

        [Obsolete("Use Contains(ulong, BlacklistFlags) instead.")]
        public static bool Contains(ulong guid)
        {
            return Contains(guid, BlacklistFlags.All);
        }

        [Obsolete("Use Add(ulong, BlacklistFlags, TimeSpan, Reason) instead.")]
        public static void Add(ulong guid, TimeSpan end, string reason = "")
        {
            Add(guid, BlacklistFlags.All, end, reason);
        }

        [Obsolete("Use Add(WoWObject, BlacklistFlags, TimeSpan, Reason) instead.")]
        public static void Add(WoWObject o, TimeSpan end, string reason = "")
        {
            Add(o.Guid, end, reason);
        }

        public static void Add(WoWObject obj, BlacklistFlags flags, TimeSpan duration, string reason = "")
        {
            Add(obj.Guid, flags, duration, reason);
        }

        public static void Add(ulong guid, BlacklistFlags flags, TimeSpan duration, string reason = "")
        {
            WoWObject wo = ObjectManager.GetObjectByGuid(guid);
            WoWPoint location = wo != null ? wo.Location : WoWPoint.Empty;
            uint entryId = wo != null ? wo.Entry : 0U;
            Styx.WoWInternals.World.JbnMap map = wo != null ? StyxWoW.WorldScene.WorldMap.GetMapAt(location) : StyxWoW.WorldScene.WorldMap.GetMapAt(StyxWoW.Me.Location);
            int mapId = -1;
            if (map != null)
            {
                mapId = map.MapID;
            }

            if (string.IsNullOrEmpty(reason))
            {
                StackTrace stackTrace = new StackTrace(new StackFrame(1, true));
                reason = "No reason specified. Caller: " + stackTrace;
            }

            BlacklistEntry existing = _blacklist.FirstOrDefault(e => e.Guid == guid);
            Logging.WriteVerbose("Blacklisting {0} for {1} [Type: {2}] - Reason: {3}", guid.ToString("X"), duration, flags, reason);
            if (existing != null)
            {
                TimeSpan remaining = existing.Length - existing.Timer.Elapsed;
                if (duration > remaining)
                {
                    existing.Timer.Restart();
                    existing.Length = duration;
                }

                existing.Flags |= flags;
                return;
            }

            _blacklist.Add(new BlacklistEntry
            {
                Guid = guid,
                Entry = entryId,
                Location = location,
                MapId = mapId,
                Timer = Stopwatch.StartNew(),
                Length = duration,
                Flags = flags
            });
        }

        public static void Add(uint entry, WoWPoint location, BlacklistFlags flags, TimeSpan duration, string reason = "")
        {
            Styx.WoWInternals.World.JbnMap map = StyxWoW.WorldScene.WorldMap.GetMapAt(location);
            int mapId = -1;
            if (map != null)
            {
                mapId = map.MapID;
            }

            if (string.IsNullOrEmpty(reason))
            {
                StackTrace stackTrace = new StackTrace(new StackFrame(1, true));
                reason = "No reason specified. Caller: " + stackTrace;
            }

            BlacklistEntry existing = _blacklist.FirstOrDefault(e => e.Entry == entry && e.Location == location && e.MapId == mapId);
            Logging.WriteVerbose("Blacklisting npc with entry {0} for {1} [Type: {2}] - Reason: {3}", entry, duration, flags, reason);
            if (existing != null)
            {
                TimeSpan remaining = existing.Length - existing.Timer.Elapsed;
                if (duration > remaining)
                {
                    existing.Timer.Restart();
                    existing.Length = duration;
                }

                existing.Flags |= flags;
                return;
            }

            _blacklist.Add(new BlacklistEntry
            {
                Guid = 0UL,
                Entry = entry,
                Location = location,
                MapId = mapId,
                Timer = Stopwatch.StartNew(),
                Length = duration,
                Flags = flags
            });
        }

        private static readonly List<BlacklistEntry> _blacklist = new List<BlacklistEntry>();

        public class BlacklistEntry
        {
            public ulong Guid { get; internal set; }

            public uint Entry { get; internal set; }

            public WoWPoint Location { get; internal set; }

            public int MapId { get; internal set; }

            public Stopwatch Timer { get; internal set; }

            public BlacklistFlags Flags { get; internal set; }

            public TimeSpan Length { get; internal set; }

            public bool IsFinished
            {
                get { return Timer.Elapsed >= Length; }
            }

            public DateTime Started
            {
                get { return DateTime.Now - Timer.Elapsed; }
            }
        }

        public delegate bool EntryRemovalDelegate(BlacklistEntry entry);
    }
}
