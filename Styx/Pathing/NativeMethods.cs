using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Styx;
using Styx.Common;

namespace Styx.Pathing
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NavTileLoadedCallback(uint mapId, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void NavLogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool NavPathFindProgressCallback();

    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {

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

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Nav_LoadMaps();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Nav_UnloadMaps();

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

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint FindPolyPath_C(
            uint mapId, ulong startRef, ulong endRef,
            XYZ_C startPos, XYZ_C endPos,
            [Out] ulong[] outPath, out int outPathCount, int maxPath);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint FindStraightPath_C(
            uint mapId, XYZ_C startPos, XYZ_C endPos,
            ulong[] path, int pathCount,
            [Out] XYZ_C[] outPoints, [Out] byte[] outFlags, [Out] ulong[] outRefs,
            out int outCount, int maxPoints);

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

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetTileAt_C(uint mapId, int x, int y);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetTilePolys_C(
            uint mapId, int x, int y, [Out] ulong[] outRefs, int maxRefs);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetAdtSubTiles_C(
            uint mapId, int x, int y, [Out] int[] outX, [Out] int[] outY, int maxTiles);

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

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetNavStats_C(out NavStats_C outStats);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ResetNavStats_C();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetTileLoadedCallback_C(NavTileLoadedCallback callback);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetNavLogCallback_C(NavLogCallback callback);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetPathFindProgressCallback_C(NavPathFindProgressCallback callback);

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

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetNavMeshQuery(uint mapId);

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GetDefaultFilter();

        [DllImport("Navigation.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FindPolysAroundCircle")]
        internal static extern int FindPolysAroundCircle_C(
            uint mapId, XYZ_C center, float radius,
            [Out] XYZ_C[] results, int maxResults);
    }

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

    public static class NavigationApi
    {
        private const float MaxProjectedHeightDelta = 20f;

        public static bool LoadMaps() => NativeMethods.Nav_LoadMaps();

        public static void UnloadMaps() => NativeMethods.Nav_UnloadMaps();

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

        public static NavPathResult? CalculatePathEx(uint mapId, WoWPoint start, WoWPoint end, bool straightPath = true)
        {
            IntPtr ptr = NativeMethods.CalculatePathEx(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z),
                straightPath);
            return NavPathResult.FromNative(ptr);
        }

        public static bool InitSliced(uint mapId, WoWPoint start, WoWPoint end)
        {
            return NativeMethods.InitSlicedFindPath(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z));
        }

        public static bool UpdateSliced(int maxIterations = 100) =>
            NativeMethods.UpdateSlicedFindPath(maxIterations);

        public static bool UpdateSlicedMs(float msBudget) =>
            NativeMethods.UpdateSlicedFindPathMs(msBudget);

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

        public static bool HasLineOfSight(uint mapId, WoWPoint start, WoWPoint end)
        {
            return NativeMethods.HasLineOfSight_C(mapId,
                new NativeMethods.XYZ_C(start.X, start.Y, start.Z),
                new NativeMethods.XYZ_C(end.X, end.Y, end.Z));
        }

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

        public static List<float> FindHeights(uint mapId, float x, float y, float aroundZ,
            float verticalRange = 20f, float horizontalTolerance = 1f, int maxPolys = 256)
        {
            var pairs = FindHeightsWithPolys(mapId, x, y, aroundZ, verticalRange, horizontalTolerance, maxPolys);
            var result = new List<float>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
                result.Add(pairs[i].Height);
            return result;
        }

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

        public static bool IsBelowWaterSurface(uint mapId, WoWPoint location, out float depth,
            float minDepth = 2f)
        {
            depth = 0f;
            try
            {
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
                return depth > 0f;
            }
            catch
            {
                return false;
            }
        }

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

        public static bool RaycastBlocked(uint mapId, WoWPoint start, WoWPoint end,
            out float hitT, Tripper.MeshMisc.AreaType factionArea = Tripper.MeshMisc.AreaType.Ground)
        {
            hitT = 1.0f;
            try
            {
                var startC = new NativeMethods.XYZ_C(start.X, start.Y, start.Z);
                var endC = new NativeMethods.XYZ_C(end.X, end.Y, end.Z);
                var extentsC = new NativeMethods.XYZ_C(3f, 3f, 20f);

                if (!NativeMethods.FindNearestPolyRef_C(mapId, startC, extentsC, out ulong startRef, out _))
                    return true;

                var path = new ulong[512];
                uint status = NativeMethods.Raycast_HB_C(mapId, startRef, startC, endC,
                    out hitT, out _, path, out int pathCount, 512);

                if (!NativeMethods.NavStatus_IsSuccess(status))
                    return true;

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

        private const float SnapExtentHorizontal = 3f;
        private const float SnapExtentVertical = 20f;

        public static bool FindNearestPoint(uint mapId, WoWPoint position,
            out WoWPoint nearest)
        {
            var extents = new NativeMethods.XYZ_C(
                SnapExtentHorizontal, SnapExtentHorizontal, SnapExtentVertical);
            bool ok = NativeMethods.FindNearestPolyRef_C(mapId,
                new NativeMethods.XYZ_C(position.X, position.Y, position.Z),
                extents, out ulong polyRef, out var n);

            if (ok && polyRef == 0UL)
                ok = false;

            nearest = ok ? new WoWPoint(n.x, n.y, n.z) : position;
            return ok;
        }

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

        public static void EnsureTiles(uint mapId, WoWPoint position, int ring = 1)
        {
            NativeMethods.EnsureTiles_C(mapId,
                new NativeMethods.XYZ_C(position.X, position.Y, position.Z), ring);
        }

        public static bool SetAreaCost(uint mapId, int areaType, float cost)
        {
            return NativeMethods.SetAreaCost_C(mapId, areaType, cost);
        }

        public static void SetIncludeFlags(ushort flags) => NativeMethods.SetIncludeFlags_C(flags);
        public static void SetExcludeFlags(ushort flags) => NativeMethods.SetExcludeFlags_C(flags);

        public const byte NAV_AREA_BLOCKED = 12;

        public static bool SetPolyArea(uint mapId, ulong polyRef, byte area)
            => NativeMethods.SetPolyArea_C(mapId, polyRef, area) != 0;

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

        public static int BlacklistArea(uint mapId, WoWPoint center, float radius)
        {
            if (radius <= 0f) radius = 1f;
            EnsureTiles(mapId, center, 1);
            var extents = new WoWPoint(radius, radius, MathF.Max(radius, 20f));
            var polys = QueryPolygons(mapId, center, extents);
            int blocked = 0;
            foreach (var poly in polys)
                if (poly != 0 && SetPolyArea(mapId, poly, NAV_AREA_BLOCKED))
                    blocked++;
            return blocked;
        }

        public static bool IsTileLoaded(uint mapId, int x, int y) =>
            NativeMethods.IsTileLoaded_C(mapId, x, y);

        public static bool UnloadTile(uint mapId, int x, int y) =>
            NativeMethods.UnloadTile_C(mapId, x, y);

        public static int GetLoadedTilesCount(uint mapId) =>
            NativeMethods.GetLoadedTilesCount_C(mapId);

        public static void WorldToTile(float x, float z, out int tileX, out int tileY)
        {
            NativeMethods.WorldToTile_C(x, z, out tileX, out tileY);
        }

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

        public static void SetTileLoadedCallback(NavTileLoadedCallback cb) =>
            NativeMethods.SetTileLoadedCallback_C(cb);

        public static void SetLogCallback(NavLogCallback cb) =>
            NativeMethods.SetNavLogCallback_C(cb);
    }

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
