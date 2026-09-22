using System;
using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing.Avoidance
{
    public static class MeshAvoidanceManager
    {
        public static readonly List<AvoidInfo> AvoidInfos = new List<AvoidInfo>();

        public static readonly List<Avoid> Avoids = new List<Avoid>();

        public static readonly List<AvoidCluster> AvoidClusters = new List<AvoidCluster>();

        public static void Add(AvoidInfo avoid)
        {
            AvoidInfos.Add(avoid);
        }

        public static void AddRange(IEnumerable<AvoidInfo> avoids)
        {
            AvoidInfos.AddRange(avoids);
        }

        public static void Remove(AvoidInfo avoid)
        {
            AvoidInfos.Remove(avoid);
        }

        public static void RemoveAll(Predicate<AvoidInfo> match)
        {
            AvoidInfos.RemoveAll(match);
        }

        public static void Clear()
        {
            AvoidInfos.Clear();
        }

        internal static void Pulse()
        {
            if (AvoidInfos.Count == 0)
            {
                List<WoWUnit> templist = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                    .Where(u => !u.IsDead && !u.PlayerControlled)
                    .OrderBy(u => u.DistanceSqr)
                    .ToList();

                foreach (WoWUnit woWUnit in templist)
                {
                    // Honorbuddy 5.4.8: this loop body is empty (dead code preserved from the
                    // original, where the Add(new AvoidLocationInfo(...)) call inside it is
                    // commented out).
                }
            }

            List<Avoid> avoids = Avoids;

            // remove all invalid avoids from list
            bool changed = avoids.RemoveAll(a =>
            {
                if (!a.IsValid)
                {
                    return true;
                }
                // remove all avoid locations that have a custom collection selector
                if (a is AvoidLocation)
                {
                    var avoidLocInfo = a.AvoidInfo as AvoidLocationInfo;
                    if (avoidLocInfo != null && avoidLocInfo.CollectionSelector != AvoidLocationInfo.DefaultCollectionSelector)
                    {
                        return true;
                    }
                }
                return false;
            }) > 0;

            // add new AvoidObjects if any are found.
            foreach (WoWObject obj in ObjectManager.GetObjectsOfType<WoWObject>(true, true))
            {
                foreach (AvoidObjectInfo avoidObjectInfo in AvoidInfos.OfType<AvoidObjectInfo>())
                {
                    if (avoidObjectInfo.ObjectSelector(obj) && avoidObjectInfo.CanRun(obj))
                    {
                        if (!avoids.Any(a => a is AvoidObject && ((AvoidObject)a).Object == obj))
                        {
                            if (!changed)
                            {
                                changed = true;
                            }
                            avoids.Add(new AvoidObject(avoidObjectInfo, obj));
                        }
                    }
                }
            }

            // add AvoidLocations as needed
            foreach (AvoidLocationInfo avoidLocationInfo in AvoidInfos.OfType<AvoidLocationInfo>())
            {
                foreach (object content in avoidLocationInfo.CollectionSelector())
                {
                    // avoidlocationsInfos with a custom collection selector can spawn multiple instances of an AvoidLocation.
                    if (avoidLocationInfo.CanRun(null) &&
                        (avoidLocationInfo.CollectionSelector != AvoidLocationInfo.DefaultCollectionSelector ||
                         avoids.All(a => a.AvoidInfo != avoidLocationInfo)))
                    {
                        if (!changed)
                        {
                            changed = true;
                        }
                        avoids.Add(new AvoidLocation(avoidLocationInfo, content));
                    }
                }
            }

            // update the locations
            try
            {
                foreach (Avoid avoid in avoids)
                {
                    WoWPoint oldLoc = avoid.Location;
                    avoid.UpdateLocation();
                    if (oldLoc != avoid.Location && !changed)
                    {
                        changed = true;
                    }
                }
            }
            catch (Exception)
            {
                changed = true;
            }

            // group all overlapping avoids into clusters.
            if (changed)
            {
                Helpers.BuildClusters(avoids, AvoidClusters);
            }

            MoveOutOfAvoid();
        }

        public static void MoveOutOfAvoid()
        {
            AvoidCluster clusterImStandingIn = AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(StyxWoW.Me.Location)));

            if (clusterImStandingIn != null)
            {
                Navigator.GetRunStatusFromMoveResult(Helpers.MoveOutOfAvoidCluster(clusterImStandingIn));
            }
        }
    }
}
