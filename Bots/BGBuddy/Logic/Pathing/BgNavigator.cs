using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Styx;
using Styx.Common;
using Styx.Helpers;

using Styx.Pathing;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Tripper.MeshMisc;

namespace Bots.BGBuddy.Logic.Pathing
{
    internal class BgNavigator : MeshNavigator
    {
        private new MeshMovePath CurrentMovePath { get; set; }
        private float PathPrecisionSqr { get { return PathPrecision * PathPrecision; } }
        private readonly Dictionary<PolygonReference, byte> _polyCache = new Dictionary<PolygonReference, byte>();
        private readonly Random _rand = new Random(Guid.NewGuid().GetHashCode());
        private bool _randomized;

        public BgNavigator()
        {
            Nav.OnTileLoaded += Nav_OnTileLoaded;
        }

        private void Nav_OnTileLoaded(object sender, TileLoadedEventArgs e)
        {
            _randomized = false;
            Battleground curBg = BGBuddy.CurrentBattleground;
            if (curBg != null && curBg.Profile != null)
            {
                BlackspotManager.AddBlackspots(curBg.Profile.Blackspots);
            }
        }

        public override MoveResult MoveTo(WoWPoint location)
        {
            if (StyxWoW.Me.Location.DistanceSqr(location) < PathPrecisionSqr)
            {
                Clear();
                return MoveResult.ReachedDestination;
            }

            if (CurrentMovePath == null ||
                (CurrentMovePath.Path.End.DistanceSqr(location) > PathPrecisionSqr &&
                (!StyxWoW.Me.CurrentMap.IsBattleground || CurrentMovePath.Index == 0 ||
                ((CurrentMovePath.Path.Flags[CurrentMovePath.Index - 1] & StraightPathFlags.OffmeshConnection) == 0))))
            {
                PathFindResult path = Nav.FindPath(StyxWoW.Me.Location, location);
                if (!path.Succeeded)
                {
                    if (path.Aborted)
                        Logging.WriteDiagnostic(Colors.Red, "Path search from {0} to {1} was aborted due to combat (time used: {2})", path.Start, path.End, GetTimeString(path.Elapsed));
                    else
                        Logging.WriteDiagnostic(Colors.Red, "Could not generate path from {0} to {1} (time used: {2}) @ {3}", path.Start, path.End, GetTimeString(path.Elapsed), path.FailStep);

                    return MoveResult.PathGenerationFailed;
                }

                if (path.IsPartialPath)
                {
                    Logging.WriteDiagnostic(Colors.Orange, "Could not generate full path from {0} to {1} (time used: {2})", path.Start, path.End, GetTimeString(path.Elapsed));
                }
                else if (path.Elapsed.TotalMilliseconds > 50)
                {
                    Logging.WriteDiagnostic("Successfully generated path from {0} to {1} in {2}", path.Start, path.End, GetTimeString(path.Elapsed));
                }

                if (!_randomized && !path.IsPartialPath)
                {
                    var shallRandomize = path.Start.DistanceSqr(path.End) > 40*40;
                    // Randomize polygons here

                    for (int index = 0; index < path.Polygons.Length; index++)
                    {
                        var polyRef = path.Polygons[index];
                        // return all polygon areas to old values first. So we don't end up making all areas water in long term
                        if (_polyCache.ContainsKey(polyRef))
                        {
                            Nav.Mesh.SetPolyArea(polyRef, _polyCache[polyRef]);
                            _polyCache.Remove(polyRef);
                        }

                        var chance = _rand.NextDouble() <= 0.40d;

                        ushort flags;
                        Nav.Mesh.GetPolyFlags(polyRef, out flags);

                        byte area;
                        Nav.Mesh.GetPolyArea(polyRef, out area);

                        var notSpecialArea = area == (byte) AreaType.Road ||
                                             area == (byte) AreaType.Ground;

                        if (chance && shallRandomize && notSpecialArea)
                        {
                            // cache area first
                            _polyCache.Add(polyRef, area);

                            // Set area type to water so we avoid this polygon on next path generation
                            Nav.Mesh.SetPolyArea(polyRef, (byte) AreaType.Misc8);
                        }
                    }

                    Logging.WriteDiagnostic("Path randomized");
                    _randomized = true;
                    return MoveResult.PathGenerated;
                }

                _randomized = false;
                CurrentMovePath = new MeshMovePath(path);
                MovePath(CurrentMovePath);
                return MoveResult.PathGenerated;
            }

            return MovePath(CurrentMovePath);
        }

        public override bool Clear()
        {
            CurrentMovePath = null;
            _randomized = false;
            return base.Clear();
        }

        private static string GetTimeString(TimeSpan elapsed)
        {
            if (elapsed.TotalDays >= 1)
                return string.Format("{0} days, {1} hours, {2} minutes and {3} seconds", elapsed.Days, elapsed.Hours, elapsed.Minutes, Math.Round(elapsed.Seconds + elapsed.Milliseconds / 1000f, 3));
            if (elapsed.TotalHours >= 1)
                return string.Format("{0} hours, {1} minutes and {2} seconds", elapsed.Hours, elapsed.Minutes, Math.Round(elapsed.Seconds + elapsed.Milliseconds / 1000f, 3));
            if (elapsed.TotalMinutes >= 1)
                return string.Format("{0} minutes and {1} seconds", elapsed.Minutes, Math.Round(elapsed.Seconds + elapsed.Milliseconds / 1000f, 3));
            if (elapsed.TotalSeconds >= 1)
                return string.Format("{0} seconds", Math.Round(elapsed.TotalSeconds, 3));

            return string.Format("{0} milliseconds", Math.Round(elapsed.TotalMilliseconds, 0));
        }
    }
}
