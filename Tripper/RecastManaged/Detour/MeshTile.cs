using System.Collections.Generic;
using Styx.Pathing;

namespace Tripper.RecastManaged.Detour
{
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

        public Poly[] GetAllPolys()
        {
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
