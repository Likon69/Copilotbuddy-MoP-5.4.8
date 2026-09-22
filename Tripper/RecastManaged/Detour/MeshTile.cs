using System.Collections.Generic;
using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
    /// <summary>
    /// API port of Honorbuddy's <c>Tripper.RecastManaged.Detour.MeshTile</c>, reduced to
    /// the one member BGBuddy uses (<see cref="GetAllPolys"/>, called by IsleOfConquest and
    /// StrandOfTheAncients to rewrite gate flags).
    ///
    /// HB's original wrapped a native <c>dtMeshTile*</c>. Navigation.dll only hands out an
    /// opaque tile handle, so a tile is identified here by its map and grid coordinates,
    /// and its polygons are enumerated through <c>GetTilePolys_C</c>.
    /// </summary>
    public class MeshTile
    {
        internal MeshTile(uint mapId, int x, int y)
        {
            MapId = mapId;
            X = x;
            Y = y;
        }

        public uint MapId { get; }

        public int X { get; }

        public int Y { get; }

        /// <summary>
        /// Every polygon of this tile. Returns an empty array if the tile has been unloaded
        /// since it was fetched.
        /// </summary>
        public Poly[] GetAllPolys()
        {
            // GetTilePolys_C stops at maxRefs; grow until the buffer is not filled.
            ulong[] refs = new ulong[1024];
            int count;
            while ((count = NativeMethods.GetTilePolys_C(MapId, X, Y, refs, refs.Length)) == refs.Length)
                refs = new ulong[refs.Length * 2];

            var polys = new List<Poly>(count);
            for (int i = 0; i < count; i++)
                polys.Add(new Poly(MapId, refs[i]));
            return polys.ToArray();
        }
    }
}
