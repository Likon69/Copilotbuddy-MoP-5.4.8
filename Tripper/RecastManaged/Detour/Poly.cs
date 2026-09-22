using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
    /// <summary>
    /// API port of Honorbuddy's <c>Tripper.RecastManaged.Detour.Poly</c>, reduced to
    /// <see cref="Area"/> and <see cref="Flags"/> — the members BGBuddy's gate logic uses.
    ///
    /// HB's original wrapped a native <c>dtPoly*</c> and read/wrote its fields in place.
    /// Here a polygon is only a <c>(mapId, polyRef)</c> pair and each accessor goes
    /// straight to Navigation.dll, so a write lands in the live mesh exactly as before.
    /// </summary>
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
