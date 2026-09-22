using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing
{
    public static class AvoidanceManager
    {
        private static readonly HashSet<uint> _avoidMobs = new HashSet<uint>();
        private static readonly Dictionary<ulong, ActiveAvoidMob> _activeAvoidMobs = new Dictionary<ulong, ActiveAvoidMob>();

        static AvoidanceManager()
        {
            BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
        }

        private static void Profile_OnNewProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            if (args.OldProfile != null && args.OldProfile.AvoidMobs != null)
            {
                RemoveAll(args.OldProfile.AvoidMobs.HashSet1);
                Navigator.Clear();
            }

            if (args.NewProfile != null && args.NewProfile.AvoidMobs != null)
            {
                AddAll(args.NewProfile.AvoidMobs.HashSet1);
                Navigator.Clear();
            }
        }

        private struct ActiveAvoidMob
        {
            public uint Id;
            public WoWPoint Location;
            public Blackspot Blackspot;

            public ActiveAvoidMob(uint id, WoWPoint loc, Blackspot blackspot)
            {
                Id = id;
                Location = loc;
                Blackspot = blackspot;
            }
        }

        public static void Pulse()
        {
            WoWPoint myLoc = StyxWoW.Me.Location;

            foreach (WoWUnit u in ObjectManager.GetObjectsOfType<WoWUnit>(false, false))
            {
                if (!_avoidMobs.Contains(u.Entry))
                {
                    continue;
                }

                // We don't want to avoid anything friendly, kthx.
                if (u.IsFriendly)
                {
                    continue;
                }

                WoWPoint uLoc = u.Location;
                float dist = myLoc.DistanceSqr(uLoc);
                ActiveAvoidMob aam;

                if (_activeAvoidMobs.TryGetValue(u.Guid, out aam))
                {
                    if (dist > 3600f)
                    {
                        // flush it out when we're more than 60 yards away from a unit again
                        _activeAvoidMobs.Remove(u.Guid);
                        BlackspotManager.RemoveBlackspot(aam.Blackspot);

                        Logging.WriteDiagnostic("Removing '{0}' as a danger - not avoiding him anymore!", u.Name);
                    }
                    else if (uLoc.DistanceSqr(_activeAvoidMobs[u.Guid].Location) > 100f) // has it moved more than 10 yards
                    {
                        // remove the old blackspot
                        BlackspotManager.RemoveBlackspot(aam.Blackspot);
                        Navigator.Clear(); // then clear so we can repath
                        var blackspot = new Blackspot(uLoc, u.GetAggroRange(StyxWoW.Me) + 10f, 10f);
                        _activeAvoidMobs[u.Guid] = new ActiveAvoidMob(aam.Id, uLoc, blackspot);

                        Logging.WriteDiagnostic("'{0}' is walking! Reavoiding!", u.Name);
                    }
                }
                else
                {
                    if (dist < 3600f)
                    {
                        var blackspot = new Blackspot(uLoc, u.GetAggroRange(StyxWoW.Me) + 10f, 10f);
                        _activeAvoidMobs.Add(u.Guid, new ActiveAvoidMob(u.Entry, uLoc, blackspot)); // begin watching this
                        Navigator.Clear(); // and regenerate the current path so it doesn't go through the enemy

                        Logging.WriteDiagnostic("Avoiding '{0}'", u.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a unit to avoid.
        /// </summary>
        /// <param name="id">The ID of the unit.</param>
        public static void Add(uint id)
        {
            _avoidMobs.Add(id);
        }

        /// <summary>
        /// Adds a bunch of units to avoid.
        /// </summary>
        /// <param name="ids">The IDs of the units.</param>
        public static void AddAll(IEnumerable<uint> ids)
        {
            foreach (uint id in ids)
            {
                _avoidMobs.Add(id);
            }
        }

        /// <summary>
        /// Removes a unit being avoided.
        /// </summary>
        /// <param name="id">The ID of the unit.</param>
        public static bool Remove(uint id)
        {
            if (_avoidMobs.Remove(id))
            {
                BlackspotManager.RemoveBlackspots(_activeAvoidMobs.Where(kvp => kvp.Value.Id == id).Select(kvp => kvp.Value.Blackspot));
                _activeAvoidMobs.RemoveAll(aam => aam.Id == id);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a bunch of units being avoided.
        /// </summary>
        /// <param name="ids">The IDs of the units.</param>
        public static void RemoveAll(IEnumerable<uint> ids)
        {
            var hashSet = new HashSet<uint>(ids);
            BlackspotManager.RemoveBlackspots(_activeAvoidMobs.Where(kvp => hashSet.Contains(kvp.Value.Id)).Select(kvp => kvp.Value.Blackspot));
            _activeAvoidMobs.RemoveAll(aam => hashSet.Contains(aam.Id));
            _avoidMobs.RemoveWhere(hashSet.Contains);
        }
    }
}
