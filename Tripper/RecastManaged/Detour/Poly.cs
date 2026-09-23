using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
    public class Poly
    {
        internal Poly(uint mapId, ulong polyRef)
        {
            MapId = mapId;
            Reference = new PolygonReference(polyRef);
        }

        public uint MapId { get; }

        public PolygonReference Reference { get; }

        public byte Area
        {
            get
            {
                NativeMethods.GetPolyArea_C(MapId, Reference.Id, out byte area);
                return area;
            }
        }

        public ushort Flags
        {
            get
            {
                NativeMethods.GetPolyFlags_C(MapId, Reference.Id, out ushort flags);
                return flags;
            }
            set => NativeMethods.SetPolyFlags_C(MapId, Reference.Id, value);
        }
    }
}
