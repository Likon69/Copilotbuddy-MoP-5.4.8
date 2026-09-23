using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Resources;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;

namespace Styx.Pathing
{
    public static class BlackspotManager
    {
        private const int MaxQueryPolygons = 8192;

        private static readonly List<Blackspot> _blackspots = new List<Blackspot>();

        public static ReadOnlyCollection<Blackspot> DynamicBlackspots
        {
            get { return _blackspots.AsReadOnly(); }
        }

        internal static void Initialize()
        {
            BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
            if (Navigator.NavigationProvider is MeshNavigator meshNavigator)
            {
                meshNavigator.Nav.OnTileLoaded += Nav_OnTileLoaded;
                meshNavigator.Nav.OnMapLoaded += Nav_OnMapLoaded;
            }
            Navigator.OnNavigationProviderChanged += Navigator_OnNavigationProviderChanged;
        }

        private static void Navigator_OnNavigationProviderChanged(object sender, NavigationProviderChangedEventArgs<INavigationProvider> e)
        {
            if (e.OldProvider is MeshNavigator oldMeshNavigator)
            {
                oldMeshNavigator.Nav.OnTileLoaded -= Nav_OnTileLoaded;
                oldMeshNavigator.Nav.OnMapLoaded -= Nav_OnMapLoaded;
            }
            if (e.NewProvider is MeshNavigator newMeshNavigator)
            {
                newMeshNavigator.Nav.OnTileLoaded += Nav_OnTileLoaded;
                newMeshNavigator.Nav.OnMapLoaded += Nav_OnMapLoaded;
            }
        }

        private static void Nav_OnMapLoaded(object sender, MapLoadedEventArgs e)
        {
            _blackspots.Clear();
            if (!e.IsTiled)
                ApplyBlackspots(GetAllCurrentBlackspots());
        }

        private static void Nav_OnTileLoaded(object sender, TileLoadedEventArgs e)
        {
            ApplyBlackspots(GetAllCurrentBlackspots());
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
            ForceReload("New profile loaded");
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
            ApplyBlackspots(filtered);
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
            if (Navigator.NavigationProvider is MeshNavigator meshNavigator)
            {
                Logging.WriteDiagnostic("BlackspotManager forcing unload of meshes{0}", string.IsNullOrWhiteSpace(reason) ? "" : " - " + reason);
                meshNavigator.Nav.UnloadAllTiles();
            }
        }

        private static void ApplyBlackspots(IEnumerable<Blackspot> blackspots)
        {
            if (blackspots == null || !(Navigator.NavigationProvider is MeshNavigator meshNavigator))
                return;

            WowNavigator nav = meshNavigator.Nav;
            lock (nav.MeshLock)
            {
                foreach (Blackspot blackspot in blackspots)
                {
                    Tripper.Tools.Math.Vector3 center = NavHelper.ToNav(blackspot.Location);
                    Tripper.Tools.Math.Vector3 extents = new Tripper.Tools.Math.Vector3(blackspot.Radius, blackspot.Height, blackspot.Radius);
                    Status status = nav.MeshQuery.QueryPolygons(center, extents, nav.QueryFilter.InternalFilter, MaxQueryPolygons, out PolygonReference[] result);
                    if (status.Failed)
                    {
                        Logging.Write("Warning: QueryPolygons failed while applying blackspot {0} with radius {1} (status: {2})", blackspot.Location, blackspot.Radius, status.Flags);
                        continue;
                    }
                    if (result.Length >= MaxQueryPolygons)
                    {
                        Logging.Write(StyxResources.Warning__Max_polygon_count_exceeded_while_applying_blackspot__0__with_radius__1___2_, blackspot.Location, blackspot.Radius, blackspot.Radius > 100f ? StyxResources._Consider_using_multiple_blackspots_instead_of_one_very_large_blackspot_ : "");
                        continue;
                    }
                    foreach (PolygonReference polygonReference in result)
                        nav.Mesh.SetPolyArea(polygonReference, (byte)Tripper.MeshMisc.AreaType.Blackspot);
                }
            }
        }
    }
}
