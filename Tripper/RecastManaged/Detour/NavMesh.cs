using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
    /// <summary>
    /// 1:1 API port of Honorbuddy 5.4.8's <c>Tripper.RecastManaged.Detour.NavMesh</c>.
    ///
    /// HB's original held a native <c>dtNavMesh*</c> and forwarded every member straight
    /// into <c>&lt;Module&gt;.dtNavMesh.*</c> calls against it (see
    /// Tripper.RecastManaged\Detour\NavMesh.cs in the HB 6.2.3 decompile). That mixed-mode
    /// assembly can no longer be loaded under .NET 10, so this body is rewired onto the
    /// native Navigation.dll export declared in Styx.Pathing.NativeMethods, reached via
    /// P/Invoke instead of a C++/CLI pointer.
    ///
    /// Only the members any caller in Styx/Pathing or Bots actually uses are ported; the
    /// rest of HB's surface is intentionally not reproduced.
    ///
    /// Map identity: HB's dtNavMesh instance was implicitly bound to one map. Every
    /// Navigation.dll export instead takes an explicit mapId, so <see cref="MapId"/> is
    /// exposed as a plain settable property (public, for the same reason as
    /// NavMeshQuery.MapId — there is no other channel for the caller to say which map's
    /// mesh to query) that the caller must set before calling GetPolyArea.
    /// </summary>
    public class NavMesh
    {
        /// <summary>
        /// The map this mesh operates against. Navigation.dll's exports are per-call
        /// (every function takes a mapId), unlike HB's dtNavMesh which was bound to one
        /// map for its whole lifetime; the caller must set this before querying.
        /// </summary>
        public uint MapId { get; set; }

        /// <summary>
        /// Wraps Navigation.dll's <c>GetPolyArea_C(uint mapId, ulong polyRef, out byte
        /// outArea)</c>. That export returns Navigation.dll's own NavStatusFlag word,
        /// which is bit-for-bit the same layout as StatusDetailFlag (both mirror dtStatus:
        /// Failure=1&lt;&lt;31, Success=1&lt;&lt;30, InProgress=1&lt;&lt;29 plus the same
        /// detail bits — see "Navigation C++/Navigation/StatusDetailFlag.h" and
        /// NavStatus.h), so it is round-tripped directly into a Status.
        /// </summary>
        public Status GetPolyArea(PolygonReference @ref, out byte area)
        {
            uint status = NativeMethods.GetPolyArea_C(MapId, @ref.Id, out area);
            return new Status(status);
        }

        /// <summary>
        /// Wraps Navigation.dll's <c>SetPolyArea_C(uint mapId, ulong polyRef, byte area)</c>.
        /// Same status round-trip as <see cref="GetPolyArea"/>.
        /// </summary>
        public Status SetPolyArea(PolygonReference @ref, byte area)
        {
            uint status = NativeMethods.SetPolyArea_C(MapId, @ref.Id, area);
            return new Status(status);
        }

        /// <summary>
        /// Wraps Navigation.dll's <c>GetPolyFlags_C(uint mapId, ulong polyRef, out ushort
        /// outFlags)</c>. Same status round-trip as <see cref="GetPolyArea"/>.
        /// </summary>
        public Status GetPolyFlags(PolygonReference @ref, out ushort flags)
        {
            uint status = NativeMethods.GetPolyFlags_C(MapId, @ref.Id, out flags);
            return new Status(status);
        }
    }
}
