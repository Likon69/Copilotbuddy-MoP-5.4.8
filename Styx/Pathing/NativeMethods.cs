using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Styx;
using Styx.Common;

namespace Styx.Pathing
{
    /// <summary>Callback from native DLL when a navmesh tile is loaded.</summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NavTileLoadedCallback(uint mapId, int x, int y);

    /// <summary>Log callback from native DLL for diagnostic messages.</summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NavLogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

    /// <summary>
    /// Fires between the native sliced A* iterations so a long search can be abandoned.
    /// Return false to cancel the search, true to let it continue. This is what backs
    /// Honorbuddy's WowNavigator.OnPathFindProgress / PathFindProgressEventArgs.Cancel,
    /// which MeshNavigator uses to drop a pathfind still running after 4 seconds while
    /// the player is in combat or the bot has been stopped.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool NavPathFindProgressCallback();

    /// <summary>
    /// P/Invoke declarations for Navigation.dll (native C++ Recast/Detour wrapper).</summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        // ================================================================
        // Native structs (must match C layout exactly)
        // ================================================================

        [StructLayout(LayoutKind.Sequential)]
        internal struct XYZ_C
        {
            public float x;
            public float y;
            public float z;

            public XYZ_C(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
            public static implicit operator XYZ_C(WoWPoint p) => new XYZ_C(p.X, p.Y, p.Z);
            public static implicit operator WoWPoint(XYZ_C p) => new WoWPoint(p.x, p.y, p.z);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct PathResult
        {
            public IntPtr points;            // XYZ_C* array (4 bytes on x86)
            public IntPtr straightPathFlags; // byte* array (4 bytes)
            public IntPtr polyTypes;         // byte* array (4 bytes)
            public IntPtr abilityFlags;      // byte* array (4 bytes)
            public IntPtr polyRefs;          // ulong* array (4 bytes on x86)
            public int length;               // 4 bytes
            public uint status;              // NavStatusFlag bitfield (4 bytes)
            public NavPathFindStep failStep; // 4 bytes
        }

        internal enum NavPathFindStep : int
        {
            FindStartPoly = 0,
            FindEndPoly = 1,
            InitPathfind = 2,
            UpdatePathfind = 3,
            FinalizePathfind = 4,
            FindStraightPath = 5,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NavStats_C
        {
            public float PathfindTimeMs;
            public int PolysVisited;
            public float PathLength;
            public int ShortcutsApplied;
            public int StuckRecoveries;
            public int PathRecalculations;
        }

        // NavStatus flags (mirrors dtStatus bitfield)
        internal const uint NAV_FAILURE = 0x80000000;
        internal const uint NAV_SUCCESS = 0x40000000;
        internal const uint NAV_IN_PROGRESS = 0x20000000;
        internal const uint NAV_PARTIAL_RESULT = 0x10000000;
        internal const uint NAV_OUT_OF_MEMORY = 0x08000000;
        internal const uint NAV_INVALID_PARAM = 0x04000000;
        internal const uint NAV_BUFFER_TOO_SMALL = 0x02000000;
        internal const uint NAV_OUT_OF_NODES = 0x01000000;
        internal const uint NAV_WRONG_MAGIC = 0x00800000;
        internal const uint NAV_WRONG_VERSION = 0x00400000;

        // Callback delegates (must match C __stdcall signatures)
        // Note: used by public NavigationApi — declared at namespace level below

        // ================================================================
        // Loader
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Nav_LoadMaps();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Nav_UnloadMaps();

        // ================================================================
        // Basic pathfinding
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CalculatePath_C(
            uint mapId, XYZ_C start, XYZ_C end,
            [MarshalAs(UnmanagedType.I1)] bool straightPath,
            out int outLength);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreePathArr_C(IntPtr arr);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CalculatePathEx(
            uint mapId, XYZ_C start, XYZ_C end,
            [MarshalAs(UnmanagedType.I1)] bool straightPath);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreePathResult(IntPtr result);

        // ================================================================
        // Sliced pathfinding (frame-budgeted, HB-style)
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool InitSlicedFindPath(uint mapId, XYZ_C start, XYZ_C end);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool UpdateSlicedFindPath(int maxIterations);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool UpdateSlicedFindPathMs(float msBudget);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr FinalizeSlicedFindPath(int maxPathSize, out int outLength);

        // ================================================================
        // Queries
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Raycast_C(
            uint mapId, XYZ_C start, XYZ_C end,
            out XYZ_C hitPos, out float tHit);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool HasLineOfSight_C(uint mapId, XYZ_C start, XYZ_C end);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool FindNearestPoint_C(uint mapId, XYZ_C position, out XYZ_C nearest);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool FindNearestPointEx_C(
            uint mapId, XYZ_C position,
            float extentX, float extentY, float extentZ,
            out XYZ_C nearest);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool FindRandomPoint_C(
            uint mapId, XYZ_C center, float radius,
            out XYZ_C randomPoint);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsPointOnNavMesh_C(
            uint mapId, XYZ_C point, float tolerance);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern float FindDistanceToWall_C(
            uint mapId, XYZ_C position, float maxRadius,
            out XYZ_C hitPoint);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern float FindDistanceToWallEx_C(
            uint mapId, XYZ_C position, float maxRadius,
            out XYZ_C hitPoint, out XYZ_C outHitNormal);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern float FindDistanceToWallFromPoly_C(
            uint mapId, ulong polyRef, XYZ_C position, float maxRadius,
            out XYZ_C hitPoint, out XYZ_C outHitNormal);

        // ================================================================
        // Low-level Detour helpers
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool FindNearestPolyRef_C(
            uint mapId, XYZ_C position, XYZ_C extents,
            out ulong outPolyRef, out XYZ_C nearestPoint);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GetPolyHeight_C(
            uint mapId, ulong polyRef, XYZ_C position,
            out float outHeight);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ClosestPointOnPoly_C(
            uint mapId, ulong polyRef, XYZ_C position,
            out XYZ_C closestPoint);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ClosestPointOnPolyBoundary_C(
            uint mapId, ulong polyRef, XYZ_C position,
            out XYZ_C closestPoint);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int QueryPolygons_C(
            uint mapId, XYZ_C center, XYZ_C extents,
            [Out] ulong[] outPolys, int maxPolys);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FindLocalNeighbourhood_C(
            uint mapId, ulong startPolyRef, XYZ_C center, float radius,
            [Out] ulong[] outPolys, [Out] ulong[] outParents, int maxResults);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetPolyWallSegments_C(
            uint mapId, ulong polyRef,
            [Out] XYZ_C[] outSegmentStart, [Out] XYZ_C[] outSegmentEnd,
            int maxSegments);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint Raycast_HB_C(
            uint mapId, ulong startRef,
            XYZ_C startPos, XYZ_C endPos,
            out float outT, out XYZ_C outHitNormal,
            [Out] ulong[] outPath, out int outPathCount, int maxPath);

        // ================================================================
        // Tile and area filtering
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetIncludeFlags_C(ushort flags);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetExcludeFlags_C(ushort flags);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort GetIncludeFlags_C();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort GetExcludeFlags_C();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SetAreaCost_C(uint mapId, int areaType, float cost);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern float GetAreaCost_C(int areaId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void WorldToTile_C(float worldX, float worldZ, out int tileX, out int tileY);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnsureTiles_C(uint mapId, XYZ_C position, int ring);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnsureTilesDirectional_C(
            uint mapId, XYZ_C position, XYZ_C velocity, int ring);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UpdatePathFollowing_C(
            uint mapId, XYZ_C currentPos, int pathLength,
            [In] XYZ_C[] pathPoints, int currentWaypointIndex, int agentId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsTileLoaded_C(uint mapId, int x, int y);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool UnloadTile_C(uint mapId, int x, int y);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetLoadedTilesCount_C(uint mapId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetLoadedAdtCount_C(uint mapId);

        // ================================================================
        // Polygon area and flags (blackspot marking)
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint SetPolyArea_C(
            uint mapId, ulong polyRef, byte area);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint GetPolyArea_C(
            uint mapId, ulong polyRef, out byte outArea);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint SetPolyFlags_C(
            uint mapId, ulong polyRef, ushort flags);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint GetPolyFlags_C(
            uint mapId, ulong polyRef, out ushort outFlags);

        // ================================================================
        // Tile enumeration (BGBuddy gate logic)
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetTileAt_C(uint mapId, int x, int y);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetTilePolys_C(
            uint mapId, int x, int y, [Out] ulong[] outRefs, int maxRefs);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetAdtSubTiles_C(
            uint mapId, int x, int y, [Out] int[] outX, [Out] int[] outY, int maxTiles);

        // ================================================================
        // Off-mesh connections
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsOffMeshConnection_C(
            uint mapId, XYZ_C position,
            out XYZ_C outEnd, out byte outType, out uint outInteractId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void AddOffMeshConnection_C(
            uint mapId, XYZ_C start, XYZ_C end,
            float radius, byte flags, byte type, uint interactId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool LoadTileOffMesh_C(uint mapId, int tileX, int tileY);

        // ================================================================
        // Telemetry
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetNavStats_C(out NavStats_C outStats);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ResetNavStats_C();

        // ================================================================
        // Callbacks
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetTileLoadedCallback_C(NavTileLoadedCallback callback);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetNavLogCallback_C(NavLogCallback callback);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetPathFindProgressCallback_C(NavPathFindProgressCallback callback);

        // ================================================================
        // Status helpers
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint NavStatus_FailureFlag();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint NavStatus_SuccessFlag();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint NavStatus_InProgressFlag();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint NavStatus_PartialResultFlag();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool NavStatus_IsFailure(uint status);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool NavStatus_IsSuccess(uint status);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool NavStatus_IsInProgress(uint status);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool NavStatus_HasFlag(uint status, uint flag);

        // ================================================================
        // Raw object access
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetNavMeshQuery(uint mapId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetDefaultFilter();

        // ================================================================
        // Old-API exports (no _C suffix) used by CopilotBuddy NativeMethods.cs
        // ================================================================

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FindPolysAroundCircle")]
        internal static extern int FindPolysAroundCircle_C(
            uint mapId, XYZ_C center, float radius,
            [Out] XYZ_C[] results, int maxResults);
    }

    // ================================================================
    // Managed wrappers — safe, typed, handle lifecycle automatically
    // ================================================================

    /// <summary>
    /// Pathfinding result with managed ownership.  All arrays are
    /// marshalled to managed memory at construction time so the
    /// native allocations can be freed immediately.
    /// </summary>
    public class NavPathResult
    {
        public WoWPoint[] Points { get; }
        public byte[] StraightPathFlags { get; }
        public byte[] PolyTypes { get; }
        public byte[] AbilityFlags { get; }
        public ulong[] PolyRefs { get; }
        public int Length { get; }
        public uint Status { get; }
        public int FailStep { get; }

        public bool IsSuccess => (Status & NativeMethods.NAV_SUCCESS) != 0;
        public bool IsPartial => (Status & NativeMethods.NAV_PARTIAL_RESULT) != 0;

        /// <summary>Marshal a native PathResult to managed memory.</summary>
        internal unsafe static NavPathResult? FromNative(IntPtr nativePtr)
        {
            if (nativePtr == IntPtr.Zero)
                return null;

            try
            {
                var raw = Marshal.PtrToStructure<NativeMethods.PathResult>(nativePtr);
                if (raw.length <= 0)
                    return new NavPathResult(Array.Empty<WoWPoint>(), Array.Empty<byte>(),
                        Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<ulong>(),
                        0, raw.status, (int)raw.failStep);

                int n = raw.length;
                var points = new WoWPoint[n];
                var straightFlags = new byte[n];
                var polyTypes = new byte[n];
                var abilityFlags = new byte[n];
                var polyRefs = new ulong[n];

                int pointSize = Marshal.SizeOf<NativeMethods.XYZ_C>();
                for (int i = 0; i < n; i++)
                {
                    if (raw.points != IntPtr.Zero)
                    {
                        var xyz = Marshal.PtrToStructure<NativeMethods.XYZ_C>(
                            raw.points + i * pointSize);
                        points[i] = xyz;
                    }
                    if (raw.straightPathFlags != IntPtr.Zero)
                        straightFlags[i] = Marshal.ReadByte(raw.straightPathFlags, i);
                    if (raw.polyTypes != IntPtr.Zero)
                        polyTypes[i] = Marshal.ReadByte(raw.polyTypes, i);
                    if (raw.abilityFlags != IntPtr.Zero)
                        abilityFlags[i] = Marshal.ReadByte(raw.abilityFlags, i);
                    if (raw.polyRefs != IntPtr.Zero)
                        polyRefs[i] = (ulong)Marshal.ReadInt64(raw.polyRefs, i * 8);
                }

                return new NavPathResult(points, straightFlags, polyTypes,
                    abilityFlags, polyRefs, n, raw.status, (int)raw.failStep);
            }
            finally
            {
                NativeMethods.FreePathResult(nativePtr);
            }
        }

        private NavPathResult(WoWPoint[] points, byte[] straightPathFlags,
            byte[] polyTypes, byte[] abilityFlags, ulong[] polyRefs,
            int length, uint status, int failStep)
        {
            Points = points;
            StraightPathFlags = straightPathFlags;
            PolyTypes = polyTypes;
            AbilityFlags = abilityFlags;
            PolyRefs = polyRefs;
            Length = length;
            Status = status;
            FailStep = failStep;
        }
    }

    /// <summary>
    /// Safe managed wrappers around the raw NativeMethods P/Invoke calls.
    /// All methods that allocate native memory handle cleanup via try/finally.
    /// </summary>
    public static class NavigationApi
    {
        // Same vertical search envelope used by the HB/CopilotBuddy nearest-poly
        // queries. A height outside it cannot belong to the local sample and must not
        // be accepted merely because an interop call returned a non-zero value.
        private const float MaxProjectedHeightDelta = 20f;

        /// <summary>Load all mmap files from the mmaps/ directory next to the DLL.</summary>
        public static bool LoadMaps() => NativeMethods.Nav_LoadMaps();

        /// <summary>Release all loaded maps and tiles.</summary>
        public static void UnloadMaps() => NativeMethods.Nav_UnloadMaps();

        /// <summary>
        /// Basic A* pathfinding. Returns an array of waypoints, or null on failure.
        /// </summary>
        public static WoWPoint[]? CalculatePath(uint mapId, WoWPoint start, WoWPoint end, bool straightPath = true)
        {
            IntPtr ptr = NativeMethods.CalculatePath_C(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z),
                straightPath, out int length);

            if (ptr == IntPtr.Zero || length <= 0)
                return null;

            try
            {
                var result = new WoWPoint[length];
                int size = Marshal.SizeOf<NativeMethods.XYZ_C>();
                for (int i = 0; i < length; i++)
                {
                    var xyz = Marshal.PtrToStructure<NativeMethods.XYZ_C>(ptr + i * size);
                    result[i] = new WoWPoint(xyz.x, xyz.y, xyz.z);
                }
                return result;
            }
            finally
            {
                NativeMethods.FreePathArr_C(ptr);
            }
        }

        /// <summary>Full pathfinding result with metadata.</summary>
        public static NavPathResult? CalculatePathEx(uint mapId, WoWPoint start, WoWPoint end, bool straightPath = true)
        {
            NavigatorDebug.LogApi($"CalculatePathEx: map={mapId} ({start.X:F1},{start.Y:F1})->({end.X:F1},{end.Y:F1}) straight={straightPath}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            IntPtr ptr = NativeMethods.CalculatePathEx(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z),
                straightPath);
            var result = NavPathResult.FromNative(ptr);
            sw.Stop();
            if (result != null)
                NavigatorDebug.LogApi($"CalculatePathEx OK: {result.Length} points, {sw.ElapsedMilliseconds}ms");
            else
                NavigatorDebug.LogError($"CalculatePathEx FAILED in {sw.ElapsedMilliseconds}ms");
            return result;
        }

        /// <summary>Begin a sliced (multi-frame) pathfinding search.</summary>
        public static bool InitSliced(uint mapId, WoWPoint start, WoWPoint end)
        {
            return NativeMethods.InitSlicedFindPath(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z));
        }

        /// <summary>Advance sliced search by N iterations.</summary>
        public static bool UpdateSliced(int maxIterations = 100) =>
            NativeMethods.UpdateSlicedFindPath(maxIterations);

        /// <summary>Advance sliced search within time budget.</summary>
        public static bool UpdateSlicedMs(float msBudget) =>
            NativeMethods.UpdateSlicedFindPathMs(msBudget);

        /// <summary>Finalize sliced search and get the path result.</summary>
        public static WoWPoint[]? FinalizeSliced(int maxPathSize = 8192)
        {
            IntPtr ptr = NativeMethods.FinalizeSlicedFindPath(maxPathSize, out int length);
            if (ptr == IntPtr.Zero || length <= 0)
                return null;

            try
            {
                var result = new WoWPoint[length];
                int size = Marshal.SizeOf<NativeMethods.XYZ_C>();
                for (int i = 0; i < length; i++)
                {
                    var xyz = Marshal.PtrToStructure<NativeMethods.XYZ_C>(ptr + i * size);
                    result[i] = new WoWPoint(xyz.x, xyz.y, xyz.z);
                }
                return result;
            }
            finally
            {
                NativeMethods.FreePathArr_C(ptr);
            }
        }

        /// <summary>Raycast: returns true if a hit occurs along the segment.</summary>
        public static bool Raycast(uint mapId, WoWPoint start, WoWPoint end,
            out WoWPoint hitPos, out float tHit)
        {
            bool result = NativeMethods.Raycast_C(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z),
                out var h, out tHit);
            hitPos = new WoWPoint(h.x, h.y, h.z);
            return result;
        }

        /// <summary>Line-of-sight test.</summary>
        public static bool HasLineOfSight(uint mapId, WoWPoint start, WoWPoint end)
        {
            bool result = NativeMethods.HasLineOfSight_C(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z));
            NavigatorDebug.LogVerbose($"HasLineOfSight: map={mapId} -> {result}");
            return result;
        }

        /// <summary>
        /// Swept navmesh raycast from a poly resolved with custom (tight) extents.
        /// Verbatim port of CopilotBuddy Navigator.RaycastWithExtents (HB 6.2.3 method_26):
        /// FindNearestPoly(start, extents) then dtNavMeshQuery::raycast. Returns true when
        /// the query succeeded; <paramref name="hitT"/> is the normalized hit distance
        /// (>= 1.0f means the whole segment stays on the mesh). <paramref name="extents"/>
        /// are half-sizes in WoW order (X, Y horizontal, Z vertical), same as every other
        /// query in this class.
        /// </summary>
        public static bool RaycastWithExtents(uint mapId, WoWPoint start, WoWPoint end,
            WoWPoint extents, out float hitT)
        {
            hitT = 1.0f;
            try
            {
                var startC = new NativeMethods.XYZ_C(start.X, start.Y, start.Z);
                var endC = new NativeMethods.XYZ_C(end.X, end.Y, end.Z);
                var extentsC = new NativeMethods.XYZ_C(extents.X, extents.Y, extents.Z);

                // FindNearestPoly with custom (tight) extents — HB 6.2.3 method_26.
                if (!NativeMethods.FindNearestPolyRef_C(mapId, startC, extentsC, out ulong startRef, out _))
                    return false;

                var visited = new ulong[64];
                uint status = NativeMethods.Raycast_HB_C(mapId, startRef, startC, endC,
                    out hitT, out _, visited, out _, 64);

                return NativeMethods.NavStatus_IsSuccess(status);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the ordered Detour polygons crossed by a navmesh ray between two raw
        /// straight-path points. Used when inserting dense waypoints so their height and
        /// metadata come from the actual path corridor rather than a free XYZ interpolation.
        /// </summary>
        public static bool TryGetSegmentCorridor(uint mapId, WoWPoint start, WoWPoint end,
            out ulong[] polyRefs)
        {
            polyRefs = Array.Empty<ulong>();
            try
            {
                var startC = new NativeMethods.XYZ_C(start.X, start.Y, start.Z);
                var endC = new NativeMethods.XYZ_C(end.X, end.Y, end.Z);
                var extentsC = new NativeMethods.XYZ_C(3f, 3f, 20f);

                if (!NativeMethods.FindNearestPolyRef_C(
                        mapId, startC, extentsC, out ulong startRef, out _)
                    || startRef == 0)
                {
                    return false;
                }

                var visited = new ulong[128];
                uint status = NativeMethods.Raycast_HB_C(
                    mapId, startRef, startC, endC,
                    out _, out _, visited, out int count, visited.Length);

                if (!NativeMethods.NavStatus_IsSuccess(status) || count <= 0)
                    return false;

                polyRefs = new ulong[count];
                Array.Copy(visited, polyRefs, count);
                return true;
            }
            catch
            {
                polyRefs = Array.Empty<ulong>();
                return false;
            }
        }

        /// <summary>
        /// Area type of a polygon, with the same success test the corridor projection uses
        /// (<c>NavStatus_IsSuccess</c>). Leaves <paramref name="area"/> untouched on
        /// failure so a caller can pass in the value it would otherwise have used.
        /// </summary>
        public static bool TryGetPolyArea(uint mapId, ulong polyRef, ref byte area)
        {
            if (polyRef == 0)
                return false;

            uint status = NativeMethods.GetPolyArea_C(mapId, polyRef, out byte polyArea);
            if (!NativeMethods.NavStatus_IsSuccess(status))
                return false;

            area = polyArea;
            return true;
        }

        /// <summary>
        /// Every walkable height the mesh knows at this (x, y) column — the query behind
        /// the reference's <c>Navigator.FindHeights</c>, which
        /// <c>MeshHeightHelper.FindMeshHeight</c> is built on.
        ///
        /// This is the counterpart of <see cref="TryProjectHeightOnCorridor"/>, not a
        /// replacement for it. The corridor version restricts the lookup to the polygons
        /// of the path being followed, which is right while a corridor exists; it is also
        /// why it returns nothing wherever the corridor is thin, and that failure is the
        /// recurring "Dense projection unavailable on raw segment" log. This one asks the
        /// whole column and lets the caller choose — the reference picks the height
        /// closest to the player's own Z, which is what resolves a two-storey building to
        /// the storey the character is actually on.
        ///
        /// <paramref name="aroundZ"/> centres the search box, and the box height matters:
        /// dtNavMeshQuery::queryPolygons returns polygons in tile/BV order, NOT sorted by
        /// distance to the centre, so anything past maxPolys is simply dropped. The first
        /// version of this query used +/-200 yd with a cap of 64, which in a multi-storey
        /// building meant the upper floors and the roof filled the budget and the GROUND
        /// FLOOR was truncated away — it reported 33.74 and 40.29 for a point where the
        /// character was standing at 28.92 (2026-08-03 Mark Position readings). That
        /// artefact was very nearly taken as proof of a hole in the mesh, and it also fed
        /// the densification fallback, which could therefore pick a storey out of a
        /// truncated set.
        ///
        /// So the default box is now local (+/-20 yd covers two to four storeys) with a
        /// budget four times larger. A caller that genuinely wants the whole column must
        /// ask for it explicitly, and know that it is trading completeness for depth.
        /// </summary>
        public static List<float> FindHeights(uint mapId, float x, float y, float aroundZ,
            float verticalRange = 20f, float horizontalTolerance = 1f, int maxPolys = 256)
        {
            var pairs = FindHeightsWithPolys(mapId, x, y, aroundZ, verticalRange, horizontalTolerance, maxPolys);
            var result = new List<float>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
                result.Add(pairs[i].Height);
            return result;
        }

        /// <summary>
        /// Same query, keeping the polygon each height came from. A caller that promotes
        /// one of these heights into a path point needs the polygon too, to carry a real
        /// area instead of borrowing a neighbouring point's — and getting it here costs
        /// nothing, whereas a second FindNearestPolyRef would be a whole extra query.
        /// </summary>
        public static List<(float Height, ulong PolyRef)> FindHeightsWithPolys(
            uint mapId, float x, float y, float aroundZ,
            float verticalRange = 20f, float horizontalTolerance = 1f, int maxPolys = 256)
        {
            var heights = new List<(float Height, ulong PolyRef)>();

            var center = new WoWPoint(x, y, aroundZ);
            var extents = new WoWPoint(horizontalTolerance, horizontalTolerance, verticalRange);
            ulong[] polys = QueryPolygons(mapId, center, extents, maxPolys);
            if (polys.Length == 0)
                return heights;

            var probe = new NativeMethods.XYZ_C(x, y, aroundZ);
            for (int i = 0; i < polys.Length; i++)
            {
                if (!NativeMethods.GetPolyHeight_C(mapId, polys[i], probe, out float height))
                    continue;

                // Several polygons commonly tile one floor, so collapse duplicates —
                // otherwise a flat area returns the same height a dozen times and the
                // caller's "closest to my Z" scan does needless work.
                bool alreadyKnown = false;
                for (int j = 0; j < heights.Count; j++)
                {
                    if (Math.Abs(heights[j].Height - height) < 0.1f)
                    {
                        alreadyKnown = true;
                        break;
                    }
                }

                if (!alreadyKnown)
                    heights.Add((height, polys[i]));
            }

            return heights;
        }

        /// <summary>
        /// True when <paramref name="location"/> lies more than <paramref name="minDepth"/>
        /// yards below a Water navmesh polygon at the same X/Y. The extractor bakes liquid
        /// surfaces as Water-area polygons whose height IS the liquid surface
        /// (AdtFile.LiquidToNavArea), so a Water polygon well above the probe means the
        /// probe is submerged. Reference parallel: GatherbuddyBot's underwater-node
        /// blacklist, which asks the client's TraceLine(HitTestLiquid) for a liquid hit
        /// above the node — the same question, answered with data we already carry.
        /// </summary>
        public static bool IsBelowWaterSurface(uint mapId, WoWPoint location, out float depth,
            float minDepth = 2f)
        {
            depth = 0f;
            try
            {
                // Column reaching well above the probe: aroundZ +25 with ±30 vertical
                // covers water surfaces from 5 yd below to 55 yd above the location.
                var candidates = FindHeightsWithPolys(mapId, location.X, location.Y,
                    location.Z + 25f, 30f);
                for (int i = 0; i < candidates.Count; i++)
                {
                    byte area = 0;
                    if (!TryGetPolyArea(mapId, candidates[i].PolyRef, ref area))
                        continue;
                    if ((Tripper.MeshMisc.AreaType)area != Tripper.MeshMisc.AreaType.Water)
                        continue;
                    float d = candidates[i].Height - location.Z;
                    if (d > minDepth && d > depth)
                        depth = d;
                }
                if (depth > 0f)
                {
                    NavigatorDebug.LogApi(
                        $"IsBelowWaterSurface: map={mapId} ({location.X:F1},{location.Y:F1},{location.Z:F1}) " +
                        $"submerged {depth:F1}yd below a Water polygon ({candidates.Count} column heights probed)");
                }
                return depth > 0f;
            }
            catch
            {
                // An unresolved column must never veto a target on its own.
                return false;
            }
        }

        /// <summary>
        /// Projects the Z of a sample onto one of the ordered polygons in a known path
        /// corridor. Restricting the lookup to the corridor avoids snapping to the wrong
        /// floor when two walkable surfaces overlap vertically.
        /// </summary>
        public static bool TryProjectHeightOnCorridor(uint mapId, WoWPoint sample,
            ulong[] polyRefs, out WoWPoint projected, out ulong polyRef, out byte area)
        {
            projected = sample;
            polyRef = 0;
            area = 0;

            if (polyRefs == null || polyRefs.Length == 0)
                return false;

            var sampleC = new NativeMethods.XYZ_C(sample.X, sample.Y, sample.Z);
            foreach (ulong candidate in polyRefs)
            {
                if (candidate == 0)
                    continue;

                // CopilotBuddy tries every polygon visited by the Detour ray and accepts
                // only a real GetPolyHeight success. Keep that exact behavior, with one
                // Vanilla safety invariant: a local sample may not jump outside the same
                // ±20 yd vertical envelope used to resolve its corridor.
                if (!NativeMethods.GetPolyHeight_C(mapId, candidate, sampleC, out float height)
                    || float.IsNaN(height)
                    || float.IsInfinity(height)
                    || Math.Abs(height - sample.Z) > MaxProjectedHeightDelta)
                {
                    continue;
                }

                projected = new WoWPoint(sample.X, sample.Y, height);
                polyRef = candidate;

                uint areaStatus = NativeMethods.GetPolyArea_C(mapId, candidate, out byte polyArea);
                if (NativeMethods.NavStatus_IsSuccess(areaStatus))
                    area = polyArea;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Navmesh raycast that also validates the area types of every visited polygon.
        /// Verbatim port of CopilotBuddy Navigator.RaycastBlocked (HB 6.2.3 method_13):
        /// resolves the start poly, raycasts, then rejects the path if it hits a navmesh
        /// boundary OR crosses any polygon whose area is not Ground/Water/Road or the
        /// player's own faction area. Returns true if BLOCKED, false if clear.
        /// </summary>
        public static bool RaycastBlocked(uint mapId, WoWPoint start, WoWPoint end,
            out float hitT, Tripper.MeshMisc.AreaType factionArea = Tripper.MeshMisc.AreaType.Ground)
        {
            hitT = 1.0f;
            try
            {
                var startC = new NativeMethods.XYZ_C(start.X, start.Y, start.Z);
                var endC = new NativeMethods.XYZ_C(end.X, end.Y, end.Z);
                // HB 6.2.3 default query extents: ±3 horizontal, ±20 vertical (WoW order X,Y,Z).
                var extentsC = new NativeMethods.XYZ_C(3f, 3f, 20f);

                if (!NativeMethods.FindNearestPolyRef_C(mapId, startC, extentsC, out ulong startRef, out _))
                    return true;

                var path = new ulong[512];
                uint status = NativeMethods.Raycast_HB_C(mapId, startRef, startC, endC,
                    out hitT, out _, path, out int pathCount, 512);

                if (!NativeMethods.NavStatus_IsSuccess(status))
                    return true;

                // HB 6.2.3 method_13: iterate visited polys, block on any area type that is
                // not Ground/Water/Road or the player's faction area.
                for (int i = 0; i < pathCount; i++)
                {
                    if (path[i] == 0) break;
                    uint areaStatus = NativeMethods.GetPolyArea_C(mapId, path[i], out byte area);
                    if (!NativeMethods.NavStatus_IsSuccess(areaStatus))
                        return true; // can't resolve area → assume blocked

                    var areaType = (Tripper.MeshMisc.AreaType)area;
                    if (areaType != Tripper.MeshMisc.AreaType.Ground
                        && areaType != Tripper.MeshMisc.AreaType.Water
                        && areaType != Tripper.MeshMisc.AreaType.Road
                        && areaType != factionArea)
                    {
                        return true; // disallowed area type in ray path
                    }
                }

                return hitT < 1.0f; // true = hit navmesh boundary
            }
            catch
            {
                return true; // assume blocked on any error
            }
        }

        /// <summary>
        /// Distance travelled ALONG THE NAVMESH PATH from <paramref name="from"/> to
        /// <paramref name="to"/>, in yards, or null when no full path exists (partial or
        /// off-mesh). Verbatim port of CopilotBuddy Navigator.PathDistance: sum of 3D
        /// segment lengths (from → first point + between points + last point → to), capped
        /// at <paramref name="maxDistance"/>. Robust to slopes and curves, unlike a
        /// straight-line distance — used by stuck detection to measure real progress.
        /// </summary>
        public static float? PathDistance(uint mapId, WoWPoint from, WoWPoint to,
            float maxDistance = float.MaxValue)
        {
            try
            {
                var result = CalculatePathEx(mapId, from, to, straightPath: true);
                if (result == null || !result.IsSuccess || result.IsPartial || result.Points.Length == 0)
                    return null;

                var pts = result.Points;
                float dist = from.Distance(pts[0]) + pts[pts.Length - 1].Distance(to);
                if (dist > maxDistance) return maxDistance;
                for (int i = 1; i < pts.Length; i++)
                {
                    dist += pts[i - 1].Distance(pts[i]);
                    if (dist > maxDistance) return maxDistance;
                }
                return dist;
            }
            catch
            {
                return null;
            }
        }

        // Search box for point→polygon snapping, in WoW order (X, Y horizontal,
        // Z vertical) like every other query in this class. Values from the
        // reference: Extents = (3, 20, 3) in its Detour order = ±3 horizontal,
        // ±20 vertical (CopilotBuddy Navigator.cs:100-102, matching HB 6.2.3
        // WowNavigator.Extents). The generous vertical span is deliberate — a
        // unit's position often sits above its polygon — and callers that must
        // not confuse floors guard that separately (the |ΔZ| < 3 endpoint test
        // and the line-of-sight check before melee range).
        private const float SnapExtentHorizontal = 3f;
        private const float SnapExtentVertical = 20f;

        /// <summary>Snap a point to nearest valid navmesh position.</summary>
        public static bool FindNearestPoint(uint mapId, WoWPoint position,
            out WoWPoint nearest)
        {
            // Snap through FindNearestPolyRef, exactly like the reference
            // (CopilotBuddy Navigator.FindNearestPolyRef:1102 / FindNearestPoint:688,
            // both calling FindNearestPoly* with an explicit search box, Extents =
            // ±3 horizontal / ±20 vertical to match HB 6.2.3 WowNavigator.Extents).
            //
            // The two no-poly-ref entry points this port used before —
            // FindNearestPoint_C and FindNearestPointEx_C — do not marshal: they
            // returned success with the out point either zeroed or filled with
            // garbage (logged 2026-07-30: "snapped=(0,0)" and
            // "snapped=(0, 8.28e29)"), so every destination became the world
            // origin, no path could be built and every target got blacklisted.
            // This entry point is the one already proven in RaycastWithExtents.
            var extents = new NativeMethods.XYZ_C(
                SnapExtentHorizontal, SnapExtentHorizontal, SnapExtentVertical);
            bool ok = NativeMethods.FindNearestPolyRef_C(mapId,
                new NativeMethods.XYZ_C(position.X, position.Y, position.Z),
                extents, out ulong polyRef, out var n);

            // No polygon found ⇒ no snap, whatever the return code says.
            if (ok && polyRef == 0UL)
                ok = false;

            nearest = ok ? new WoWPoint(n.x, n.y, n.z) : position;
            NavigatorDebug.LogApi($"FindNearestPoint: map={mapId} ({position.X:F1},{position.Y:F1}) -> ok={ok} snapped=({nearest.X:F1},{nearest.Y:F1})");
            return ok;
        }

        /// <summary>Find a random navigable point around a center.</summary>
        public static bool FindRandomPoint(uint mapId, WoWPoint center, float radius,
            out WoWPoint randomPoint)
        {
            bool ok = NativeMethods.FindRandomPoint_C(mapId,
                new NativeMethods.XYZ_C(center.X, center.Y, center.Z),
                radius, out var r);
            randomPoint = ok ? new WoWPoint(r.x, r.y, r.z) : center;
            return ok;
        }

        /// <summary>Check if point is on the navmesh.</summary>
        public static bool IsPointOnNavMesh(uint mapId, WoWPoint point, float tolerance = 2.0f)
        {
            return NativeMethods.IsPointOnNavMesh_C(mapId,
                new NativeMethods.XYZ_C(point.X, point.Y, point.Z), tolerance);
        }

        /// <summary>Distance to nearest wall from position.</summary>
        public static float FindDistanceToWall(uint mapId, WoWPoint position,
            float maxRadius, out WoWPoint hitPoint)
        {
            float d = NativeMethods.FindDistanceToWall_C(mapId,
                new NativeMethods.XYZ_C(position.X, position.Y, position.Z),
                maxRadius, out var h);
            hitPoint = new WoWPoint(h.x, h.y, h.z);
            return d;
        }

        public static int FindPolysAroundCircle(uint mapId, WoWPoint center,
            float radius, WoWPoint[] results, int maxResults)
        {
            var nativeResults = new NativeMethods.XYZ_C[maxResults];
            int count = NativeMethods.FindPolysAroundCircle_C(mapId,
                new NativeMethods.XYZ_C(center.X, center.Y, center.Z),
                radius, nativeResults, maxResults);
            for (int i = 0; i < count && i < maxResults; i++)
                results[i] = new WoWPoint(nativeResults[i].x, nativeResults[i].y, nativeResults[i].z);
            return count;
        }

        /// <summary>Update path following — returns new waypoint index after shortcuts.</summary>
        public static int UpdatePathFollowing(uint mapId, WoWPoint currentPos,
            WoWPoint[] pathPoints, int currentIndex, int agentId = 0)
        {
            var nativePoints = new NativeMethods.XYZ_C[pathPoints.Length];
            for (int i = 0; i < pathPoints.Length; i++)
                nativePoints[i] = new NativeMethods.XYZ_C(pathPoints[i].X, pathPoints[i].Y, pathPoints[i].Z);

            return NativeMethods.UpdatePathFollowing_C(mapId,
                new NativeMethods.XYZ_C(currentPos.X, currentPos.Y, currentPos.Z),
                pathPoints.Length, nativePoints, currentIndex, agentId);
        }

        /// <summary>Preload tiles around a position.</summary>
        public static void EnsureTiles(uint mapId, WoWPoint position, int ring = 1)
        {
            NativeMethods.EnsureTiles_C(mapId,
                new NativeMethods.XYZ_C(position.X, position.Y, position.Z), ring);
        }

        /// <summary>Set area cost for pathfinding (higher = avoided).</summary>
        public static bool SetAreaCost(uint mapId, int areaType, float cost)
        {
            return NativeMethods.SetAreaCost_C(mapId, areaType, cost);
        }

        /// <summary>Set global include/exclude flags for pathfinding.</summary>
        public static void SetIncludeFlags(ushort flags) => NativeMethods.SetIncludeFlags_C(flags);
        public static void SetExcludeFlags(ushort flags) => NativeMethods.SetExcludeFlags_C(flags);

        // ── Blackspots ────────────────────────────────────────────
        // NAV_BLOCKED (area index 12) has a default cost of 100.0 in the native
        // Navigation constructor — the pathfinder routes around such polys unless
        // there is no alternative. Marking blackspot polygons with this area is
        // the HB-style, non-destructive way to forbid a region at runtime without
        // touching the mmap files on disk.
        public const byte NAV_AREA_BLOCKED = 12;

        /// <summary>Set a polygon's area type (see NAV_AREA_* / AreaType).</summary>
        public static bool SetPolyArea(uint mapId, ulong polyRef, byte area)
            => NativeMethods.SetPolyArea_C(mapId, polyRef, area) != 0;

        /// <summary>
        /// Query the polygons whose center lies within an axis-aligned box of
        /// half-size <paramref name="extents"/> around <paramref name="center"/>.
        /// Returns the poly refs found (empty on failure or if no tiles are loaded).
        /// </summary>
        public static ulong[] QueryPolygons(uint mapId, WoWPoint center, WoWPoint extents, int maxPolys = 256)
        {
            if (maxPolys <= 0) return Array.Empty<ulong>();
            var buf = new ulong[maxPolys];
            int n = NativeMethods.QueryPolygons_C(mapId,
                new NativeMethods.XYZ_C(center.X, center.Y, center.Z),
                new NativeMethods.XYZ_C(extents.X, extents.Y, extents.Z),
                buf, maxPolys);
            if (n <= 0) return Array.Empty<ulong>();
            if (n < maxPolys) Array.Resize(ref buf, n);
            return buf;
        }

        /// <summary>
        /// Mark every navmesh polygon within <paramref name="radius"/> of
        /// <paramref name="center"/> as NAV_BLOCKED so pathfinding avoids it.
        /// Loads the surrounding tiles first (blackspots on unloaded tiles are
        /// silently no-ops in Detour). Returns the number of polygons blocked.
        /// </summary>
        public static int BlacklistArea(uint mapId, WoWPoint center, float radius)
        {
            if (radius <= 0f) radius = 1f;
            EnsureTiles(mapId, center, 1);
            // Vertical extent generous enough to cover multi-level terrain at the spot.
            var extents = new WoWPoint(radius, radius, MathF.Max(radius, 20f));
            var polys = QueryPolygons(mapId, center, extents);
            int blocked = 0;
            foreach (var poly in polys)
                if (poly != 0 && SetPolyArea(mapId, poly, NAV_AREA_BLOCKED))
                    blocked++;
            return blocked;
        }

        /// <summary>Tile state queries.</summary>
        public static bool IsTileLoaded(uint mapId, int x, int y) =>
            NativeMethods.IsTileLoaded_C(mapId, x, y);

        public static bool UnloadTile(uint mapId, int x, int y) =>
            NativeMethods.UnloadTile_C(mapId, x, y);

        public static int GetLoadedTilesCount(uint mapId) =>
            NativeMethods.GetLoadedTilesCount_C(mapId);

        /// <summary>Convert world coordinates to ADT tile indices.</summary>
        public static void WorldToTile(float x, float z, out int tileX, out int tileY)
        {
            NativeMethods.WorldToTile_C(x, z, out tileX, out tileY);
        }

        /// <summary>Get navigation statistics for the last pathfinding call.</summary>
        public static NavStats GetNavStats()
        {
            NativeMethods.GetNavStats_C(out var s);
            return new NavStats
            {
                PathfindTimeMs = s.PathfindTimeMs,
                PolysVisited = s.PolysVisited,
                PathLength = s.PathLength,
                ShortcutsApplied = s.ShortcutsApplied,
                StuckRecoveries = s.StuckRecoveries,
                PathRecalculations = s.PathRecalculations,
            };
        }

        public static void ResetNavStats() => NativeMethods.ResetNavStats_C();

        /// <summary>Register tile-loaded callback (for logging or UI updates).</summary>
        public static void SetTileLoadedCallback(NavTileLoadedCallback cb) =>
            NativeMethods.SetTileLoadedCallback_C(cb);

        /// <summary>Register log callback for diagnostic messages from the DLL.</summary>
        public static void SetLogCallback(NavLogCallback cb) =>
            NativeMethods.SetNavLogCallback_C(cb);
    }

    /// <summary>Managed navigation statistics for display/debugging.</summary>
    public class NavStats
    {
        public float PathfindTimeMs { get; set; }
        public int PolysVisited { get; set; }
        public float PathLength { get; set; }
        public int ShortcutsApplied { get; set; }
        public int StuckRecoveries { get; set; }
        public int PathRecalculations { get; set; }

        public override string ToString() =>
            $"Pathfind: {PathfindTimeMs:F2}ms, Polys: {PolysVisited}, " +
            $"Length: {PathLength:F1}yd, Shortcuts: {ShortcutsApplied}, " +
            $"StuckRecoveries: {StuckRecoveries}, Recalcs: {PathRecalculations}";
    }
}
