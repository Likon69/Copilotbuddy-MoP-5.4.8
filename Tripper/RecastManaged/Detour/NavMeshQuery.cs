using System;
using Styx.Pathing;
using Tripper.Tools.Math;

namespace Tripper.RecastManaged.Detour
{
    /// <summary>
    /// 1:1 API port of Honorbuddy 5.4.8's <c>Tripper.RecastManaged.Detour.NavMeshQuery</c>.
    ///
    /// HB's original held a native <c>dtNavMeshQuery*</c> and forwarded every member
    /// straight into <c>&lt;Module&gt;.dtNavMeshQuery.*</c> calls against it (see
    /// Tripper.RecastManaged\Detour\NavMeshQuery.cs in the HB 6.2.3 decompile). That
    /// mixed-mode assembly can no longer be loaded under .NET 10, so these bodies are
    /// rewired onto the native Navigation.dll exports declared in
    /// Styx.Pathing.NativeMethods, reached via P/Invoke instead of a C++/CLI pointer.
    ///
    /// Only the three members any caller in Styx/Pathing or Bots actually uses are
    /// ported; the rest of HB's ~20-method surface is intentionally not reproduced.
    ///
    /// Three impedance mismatches vs. HB, handled here:
    ///  - Map identity: HB's dtNavMeshQuery instance was implicitly bound to one map.
    ///    Every Navigation.dll export instead takes an explicit mapId. <see cref="MapId"/>
    ///    is exposed as a plain settable property (public, since this class has no other
    ///    way for a caller to communicate which map's mesh to query, and HB callers already
    ///    know their map id from context) that the caller must set before querying.
    ///  - Filter scope: SetIncludeFlags_C/SetExcludeFlags_C are process-global and
    ///    SetAreaCost_C is per-map, none of them per-call, whereas HB passed a QueryFilter
    ///    argument on every call. ApplyFilter pushes the QueryFilter argument's state into
    ///    the DLL immediately before each native call, so behaviourally these three methods
    ///    still look per-call from a single caller. This is a real deviation from HB:
    ///    two threads issuing queries with different filters concurrently will now
    ///    interfere with each other, where HB's per-call filter pointer would not.
    ///  - Polyref width: HB's PolygonReference.Id was <c>uint</c> (HB's Detour build used
    ///    32-bit dtPolyRef). Navigation.dll's build defines DT_POLYREF64 (see
    ///    "Navigation C++/Navigation/Navigation.vcxproj"), so its dtPolyRef is a genuine
    ///    64-bit value; PolygonReference.Id was widened to <c>ulong</c> to match (see
    ///    PolygonReference.cs), so poly refs pass straight through with no cast at either
    ///    boundary in this class.
    /// </summary>
    public class NavMeshQuery
    {
        /// <summary>
        /// The map this query operates against. Navigation.dll's exports are per-call
        /// (every function takes a mapId), unlike HB's dtNavMeshQuery which was bound to
        /// one map for its whole lifetime; the caller must set this before querying.
        /// </summary>
        public uint MapId { get; set; }

        /// <summary>
        /// Pushes <paramref name="filter"/>'s state into Navigation.dll immediately before
        /// a query: SetIncludeFlags_C/SetExcludeFlags_C (both process-global, no mapId) and
        /// SetAreaCost_C for every one of dtQueryFilter's DT_MAX_AREAS (64) area slots
        /// (per-map). See the class summary's "Filter scope" note for the resulting
        /// deviation from HB's per-call filter pointer.
        /// </summary>
        // Last filter state actually pushed into Navigation.dll. The include/exclude flags
        // are global in the DLL and the area costs are per-map, so a single (filter, version,
        // map) triple describes exactly what the DLL currently holds.
        private static QueryFilter _appliedFilter;
        private static uint _appliedVersion;
        private static uint _appliedMapId;

        private void ApplyFilter(QueryFilter filter)
        {
            PushFilter(MapId, filter);
        }

        /// <summary>
        /// Pushes a filter into Navigation.dll's own <c>_defaultFilter</c>, which every
        /// native query reads. Shared with WowNavigator.FindPath: CalculatePathEx runs
        /// against that same filter, so a path calculated without this call would ignore
        /// the caller's area costs — faction avoidance and blackspot costs among them.
        /// </summary>
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

        // ------------------------------------------------------------------
        // Coordinate space. Honorbuddy callers speak Detour space: MeshNavigator
        // runs points through NavHelper.ToNav (GraphicalHelper.ToDetour, which is
        // WoW(X,Y,Z) -> (-Y, Z, -X)) before handing them to the query.
        //
        // Navigation.dll does NOT take Detour coordinates. Its exports take raw WoW
        // coordinates and do their own conversion internally, with a different
        // convention and no sign flips -- "PathFinder.cpp:652":
        //     float point[VERTEX_SIZE] = { y, z, x }; // WoW (X,Y,Z) -> Detour (Y,Z,X)
        // and it hands results back in WoW space too (Navigation.cpp, DetourToWoW).
        // The two conventions are each self-consistent because HB's meshes and this
        // extractor's mmaps were built with their own matching transforms.
        //
        // So the shim converts at the boundary: Detour in -> WoW to the DLL, WoW out
        // -> Detour back to the caller. Passing the vectors through untouched would
        // query a mirrored position and silently return nonsense paths.
        // ------------------------------------------------------------------

        /// <summary>Detour-space position from a caller -> WoW-space for Navigation.dll.</summary>
        private static NativeMethods.XYZ_C ToWowPos(Vector3 detour)
        {
            Vector3 wow;
            Tripper.MeshMisc.GraphicalHelper.ToWow(ref detour, out wow);
            return new NativeMethods.XYZ_C(wow.X, wow.Y, wow.Z);
        }

        /// <summary>WoW-space position from Navigation.dll -> Detour space for the caller.</summary>
        private static Vector3 ToDetourPos(NativeMethods.XYZ_C wowPoint)
        {
            Vector3 wow = new Vector3(wowPoint.x, wowPoint.y, wowPoint.z);
            Vector3 detour;
            Tripper.MeshMisc.GraphicalHelper.ToDetour(ref wow, out detour);
            return detour;
        }

        /// <summary>
        /// Search-box half-extents are magnitudes, not a position: running them through
        /// ToWow would flip signs and produce a negative box. Only the axis mapping
        /// applies. Detour X/Y/Z correspond to WoW Y/Z/X, so the WoW-space extents the
        /// DLL expects are (detour.Z, detour.X, detour.Y) -- which its own
        /// <c>{ extents.Y, extents.Z, extents.X }</c> permutation
        /// ("Navigation.cpp", FindNearestPolyRef) turns back into the caller's box.
        /// </summary>
        private static NativeMethods.XYZ_C ToWowExtents(Vector3 detour)
        {
            return new NativeMethods.XYZ_C(detour.Z, detour.X, detour.Y);
        }

        /// <summary>
        /// Wraps Navigation.dll's <c>FindNearestPolyRef_C(uint mapId, XYZ_C position,
        /// XYZ_C extents, out ulong outPolyRef, out XYZ_C nearestPoint)</c>. That export
        /// returns a plain bool (no dtStatus detail bits are produced), so the returned
        /// Status is built from Status.Success/Status.Failure rather than round-tripped
        /// from a native status word.
        /// </summary>
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

        /// <summary>
        /// Wraps Navigation.dll's <c>QueryPolygons_C(uint mapId, XYZ_C center, XYZ_C
        /// extents, ulong[] outPolys, int maxPolys)</c>. That export returns a plain poly
        /// count rather than a dtStatus word (0 on both "no results" and "call failed" —
        /// Navigation.dll does not distinguish the two at this boundary), so, consistent
        /// with how NativeMethods.QueryPolygons already treats this return value elsewhere
        /// in this project, a positive count maps to Status.Success and a non-positive one
        /// maps to Status.Failure.
        /// </summary>
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

        /// <summary>
        /// Wraps Navigation.dll's <c>Raycast_HB_C(uint mapId, ulong startRef, XYZ_C
        /// startPos, XYZ_C endPos, out float outT, out XYZ_C outHitNormal, ulong[] outPath,
        /// out int outPathCount, int maxPath)</c>. Unlike FindNearestPolygon/QueryPolygons,
        /// this export returns Navigation.dll's own NavStatusFlag word, which is bit-for-bit
        /// the same layout as StatusDetailFlag (both mirror dtStatus: Failure=1&lt;&lt;31,
        /// Success=1&lt;&lt;30, InProgress=1&lt;&lt;29 plus the same detail bits — see
        /// "Navigation C++/Navigation/StatusDetailFlag.h" and NavStatus.h), so it is
        /// round-tripped directly into a Status rather than reduced to success/failure.
        /// </summary>
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

            // A normal is a direction: ToDetour is a permutation plus sign flips with no
            // translation, so it applies to directions exactly as it does to positions.
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
    }
}
