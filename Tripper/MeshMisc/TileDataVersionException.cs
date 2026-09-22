using System;

namespace Tripper.MeshMisc
{
    public class TileDataVersionException : ApplicationException
    {
        public TileDataVersionException()
            : base("Tile data is wrong version!")
        {
        }
    }
}
