using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
    public class NavMesh
    {
        public uint MapId { get; set; }

        public Status GetPolyArea(PolygonReference @ref, out byte area)
        {
            uint status = NativeMethods.GetPolyArea_C(MapId, @ref.Id, out area);
            return new Status(status);
        }

        public Status SetPolyArea(PolygonReference @ref, byte area)
        {
            uint status = NativeMethods.SetPolyArea_C(MapId, @ref.Id, area);
            return new Status(status);
        }

        public Status GetPolyFlags(PolygonReference @ref, out ushort flags)
        {
            uint status = NativeMethods.GetPolyFlags_C(MapId, @ref.Id, out flags);
            return new Status(status);
        }

        public MeshTile GetTileAt(int x, int y)
        {
            if (NativeMethods.GetTileAt_C(MapId, x, y) == System.IntPtr.Zero)
                return null;
            return new MeshTile(MapId, x, y);
        }
    }
}
