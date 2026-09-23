using System;
using Styx.Pathing;
using Tripper.Tools.Math;

namespace Tripper.RecastManaged.Detour
{
    public class NavMeshQuery
    {
        public uint MapId { get; set; }

        private static QueryFilter _appliedFilter;
        private static uint _appliedVersion;
        private static uint _appliedMapId;

        private void ApplyFilter(QueryFilter filter)
        {
            PushFilter(MapId, filter);
        }

        internal static void PushFilter(uint mapId, QueryFilter filter)
        {
            if (filter == null)
                return;

            if (ReferenceEquals(filter, _appliedFilter) && filter.Version == _appliedVersion && mapId == _appliedMapId)
                return;

            NativeMethods.SetIncludeFlags_C(filter.IncludeFlags);
            NativeMethods.SetExcludeFlags_C(filter.ExcludeFlags);
            for (int i = 0; i < QueryFilter.DT_MAX_AREAS; i++)
                NativeMethods.SetAreaCost_C(mapId, i, filter.GetAreaCost((byte)i));

            _appliedFilter = filter;
            _appliedVersion = filter.Version;
            _appliedMapId = mapId;
        }

        private static NativeMethods.XYZ_C ToWowPos(Vector3 detour)
        {
            Vector3 wow;
            Tripper.MeshMisc.GraphicalHelper.ToWow(ref detour, out wow);
            return new NativeMethods.XYZ_C(wow.X, wow.Y, wow.Z);
        }

        private static Vector3 ToDetourPos(NativeMethods.XYZ_C wowPoint)
        {
            Vector3 wow = new Vector3(wowPoint.x, wowPoint.y, wowPoint.z);
            Vector3 detour;
            Tripper.MeshMisc.GraphicalHelper.ToDetour(ref wow, out detour);
            return detour;
        }

        private static NativeMethods.XYZ_C ToWowExtents(Vector3 detour)
        {
            return new NativeMethods.XYZ_C(detour.Z, detour.X, detour.Y);
        }

        public Status FindNearestPolygon(Vector3 center, Vector3 extents, QueryFilter filter,
            out Vector3 nearestPt, out PolygonReference result)
        {
            ApplyFilter(filter);

            var centerC = ToWowPos(center);
            var extentsC = ToWowExtents(extents);

            bool ok = NativeMethods.FindNearestPolyRef_C(MapId, centerC, extentsC,
                out ulong outPolyRef, out NativeMethods.XYZ_C nearest);

            nearestPt = ok ? ToDetourPos(nearest) : default;
            result = new PolygonReference(outPolyRef);
            return ok ? Status.Success : Status.Failure;
        }

        public Status QueryPolygons(Vector3 center, Vector3 extents, QueryFilter filter,
            int maxPolys, out PolygonReference[] result)
        {
            ApplyFilter(filter);

            var centerC = ToWowPos(center);
            var extentsC = ToWowExtents(extents);

            if (maxPolys <= 0)
            {
                result = Array.Empty<PolygonReference>();
                return Status.Failure;
            }

            var buf = new ulong[maxPolys];
            int count = NativeMethods.QueryPolygons_C(MapId, centerC, extentsC, buf, maxPolys);

            if (count <= 0)
            {
                result = Array.Empty<PolygonReference>();
                return Status.Failure;
            }

            result = new PolygonReference[count];
            for (int i = 0; i < count; i++)
                result[i] = new PolygonReference(buf[i]);

            return Status.Success;
        }

        public Status Raycast(PolygonReference startRef, Vector3 startPos, Vector3 endPos,
            QueryFilter filter, int maxPolys, out PolygonReference[] result, out float t,
            out Vector3 hitNormal)
        {
            ApplyFilter(filter);

            var startC = ToWowPos(startPos);
            var endC = ToWowPos(endPos);

            if (maxPolys <= 0)
                maxPolys = 1;

            var pathBuf = new ulong[maxPolys];
            uint status = NativeMethods.Raycast_HB_C(MapId, startRef.Id, startC, endC,
                out t, out NativeMethods.XYZ_C hitNormalC, pathBuf, out int pathCount, maxPolys);

            hitNormal = ToDetourPos(hitNormalC);


            if (pathCount > 0)
            {
                result = new PolygonReference[pathCount];
                for (int i = 0; i < pathCount; i++)
                    result[i] = new PolygonReference(pathBuf[i]);
            }
            else
            {
                result = Array.Empty<PolygonReference>();
            }

            return new Status(status);
        }

        public Status ClosestPointOnPoly(PolygonReference @ref, Vector3 pos, out Vector3 closest)
        {
            var posC = ToWowPos(pos);

            bool ok = NativeMethods.ClosestPointOnPoly_C(MapId, @ref.Id, posC,
                out NativeMethods.XYZ_C closestC);

            closest = ok ? ToDetourPos(closestC) : default;
            return ok ? Status.Success : Status.Failure;
        }

        public Status FindPath(PolygonReference startRef, PolygonReference endRef,
            Vector3 startPos, Vector3 endPos, QueryFilter filter, int maxPath,
            out PolygonReference[] path)
        {
            ApplyFilter(filter);

            var startC = ToWowPos(startPos);
            var endC = ToWowPos(endPos);

            if (maxPath <= 0)
                maxPath = 1;

            var pathBuf = new ulong[maxPath];
            uint status = NativeMethods.FindPolyPath_C(MapId, startRef.Id, endRef.Id, startC, endC,
                pathBuf, out int pathCount, maxPath);

            if (pathCount > 0)
            {
                path = new PolygonReference[pathCount];
                for (int i = 0; i < pathCount; i++)
                    path[i] = new PolygonReference(pathBuf[i]);
            }
            else
            {
                path = Array.Empty<PolygonReference>();
            }

            return new Status(status);
        }

        public Status FindStraightPath(Vector3 startPos, Vector3 endPos, PolygonReference[] path,
            int maxStraightPath, out Vector3[] points, out StraightPathFlags[] flags,
            out PolygonReference[] refs)
        {
            var startC = ToWowPos(startPos);
            var endC = ToWowPos(endPos);

            int pathCount = path?.Length ?? 0;
            var pathBuf = new ulong[pathCount];
            for (int i = 0; i < pathCount; i++)
                pathBuf[i] = path[i].Id;

            if (maxStraightPath <= 0)
                maxStraightPath = 1;

            var pointsBuf = new NativeMethods.XYZ_C[maxStraightPath];
            var flagsBuf = new byte[maxStraightPath];
            var refsBuf = new ulong[maxStraightPath];

            uint status = NativeMethods.FindStraightPath_C(MapId, startC, endC, pathBuf, pathCount,
                pointsBuf, flagsBuf, refsBuf, out int count, maxStraightPath);

            if (count > 0)
            {
                points = new Vector3[count];
                flags = new StraightPathFlags[count];
                refs = new PolygonReference[count];
                for (int i = 0; i < count; i++)
                {
                    points[i] = ToDetourPos(pointsBuf[i]);
                    flags[i] = (StraightPathFlags)flagsBuf[i];
                    refs[i] = new PolygonReference(refsBuf[i]);
                }
            }
            else
            {
                points = Array.Empty<Vector3>();
                flags = Array.Empty<StraightPathFlags>();
                refs = Array.Empty<PolygonReference>();
            }

            return new Status(status);
        }

        public Status FindDistanceToWall(PolygonReference startRef, Vector3 centerPos, float maxRadius,
            QueryFilter filter, out float hitDist, out Vector3 hitPos, out Vector3 hitNormal)
        {
            ApplyFilter(filter);

            var centerC = ToWowPos(centerPos);

            float dist = NativeMethods.FindDistanceToWallFromPoly_C(MapId, startRef.Id, centerC, maxRadius,
                out NativeMethods.XYZ_C hitPointC, out NativeMethods.XYZ_C hitNormalC);

            hitDist = dist;
            hitPos = ToDetourPos(hitPointC);
            hitNormal = ToDetourPos(hitNormalC);

            return dist >= 0f ? Status.Success : Status.Failure;
        }
    }
}
