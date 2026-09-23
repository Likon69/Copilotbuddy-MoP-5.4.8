using System;
using System.Collections.Generic;
using System.Linq;

using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

namespace Styx.Pathing
{
    internal class DefaultHeightProvider : ITerrainHeightProvider
    {
        private static readonly Vector3 _extents = new Vector3(1f, 50000f, 1f);

        public List<float> FindHeights(float x, float y)
        {
            if (!(Navigator.NavigationProvider is MeshNavigator meshNavigator))
                return null;

            lock (meshNavigator.Nav.MeshLock)
            {
                meshNavigator.Nav.LoadTile(TileIdentifier.GetByPosition(x, y));

                Vector3 v = NavHelper.ToNav(new Vector3(x, y, 0f));
                if (meshNavigator.Nav.MeshQuery.QueryPolygons(v, _extents, QueryFilter.Default, 256, out PolygonReference[] result).Succeeded)
                {
                    var heights = new List<float>();
                    foreach (PolygonReference polygonReference in result)
                    {
                        if (meshNavigator.Nav.MeshQuery.ClosestPointOnPolyBoundary(polygonReference, v, out Vector3 closest).Succeeded
                            && Vector3.DistanceSqr(ref closest, ref v) <= 0.005
                            && meshNavigator.Nav.MeshQuery.GetPolyHeight(polygonReference, v, out float height).Succeeded
                            && heights.All(other => Math.Abs(other - height) > 1f))
                        {
                            heights.Add(height);
                        }
                    }

                    return heights;
                }
            }

            return null;
        }
    }
}
