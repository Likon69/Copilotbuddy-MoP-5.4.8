using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;

namespace Styx.Pathing
{
    public static class BlackspotManager
    {
        private static readonly List<Blackspot> _blackspots = new List<Blackspot>();

        public static ReadOnlyCollection<Blackspot> DynamicBlackspots
        {
            get { return _blackspots.AsReadOnly(); }
        }

        internal static void Initialize()
        {
            BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
        }

        private static IEnumerable<Blackspot> GetBlackspotsForFlags(BlackspotQueryFlags flags)
        {
            if ((flags & BlackspotQueryFlags.Dynamic) != 0)
            {
                foreach (Blackspot blackspot in _blackspots)
                    yield return blackspot;
            }

            if ((flags & BlackspotQueryFlags.Static) != 0 && ProfileManager.CurrentProfile != null)
            {
                foreach (Blackspot blackspot in ProfileManager.CurrentProfile.Blackspots)
                    yield return blackspot;
            }
        }

        public static IEnumerable<Blackspot> GetAllCurrentBlackspots(BlackspotQueryFlags flags = BlackspotQueryFlags.All)
        {
            var mapId = -1;
            return GetBlackspotsForFlags(flags).Where(blackspot =>
            {
                if (blackspot.MapID == -1)
                    return true;

                if (mapId == -1)
                    mapId = (int)StyxWoW.Me.MapId;

                return blackspot.MapID == mapId;
            });
        }

        private static void Profile_OnNewProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            _blackspots.Clear();
        }

        public static bool IsBlackspotted(WoWPoint location, float radius = 0f, BlackspotQueryFlags flags = BlackspotQueryFlags.All)
        {
            return GetAllCurrentBlackspots(flags).Any(blackspot =>
            {
                float totalRadius = blackspot.Radius + radius;
                return blackspot.Location.DistanceSqr(location) <= totalRadius * totalRadius;
            });
        }

        public static void AddBlackspot(WoWPoint location, float radius, float height, string name = "")
        {
            var blackspot = new Blackspot(location, radius, height, name, -1);
            AddBlackspots(new[] { blackspot });
        }

        public static void AddBlackspots(IEnumerable<Blackspot> blackspots)
        {
            var mapId = (int)StyxWoW.Me.MapId;
            Blackspot[] filtered = blackspots.Where(blackspot => blackspot.MapID == -1 || blackspot.MapID == mapId).ToArray();
            foreach (Blackspot blackspot in filtered)
            {
                if (!_blackspots.Contains(blackspot))
                    _blackspots.Add(blackspot);
            }
        }

        public static void RemoveBlackspot(Blackspot spot)
        {
            if (_blackspots.Remove(spot))
                ForceReload("Blackspot removed");
        }

        public static void RemoveBlackspots(IEnumerable<Blackspot> spots)
        {
            Blackspot[] array = spots.ToArray();
            if (_blackspots.RemoveAll(array.Contains) > 0)
                ForceReload("Blackspots removed");
        }

        private static void ForceReload(string reason = "")
        {
        }
    }
}
