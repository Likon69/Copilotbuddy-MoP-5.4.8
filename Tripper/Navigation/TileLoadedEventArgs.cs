using System;
using Tripper.MeshMisc;

namespace Tripper.Navigation
{
    public class TileLoadedEventArgs : EventArgs
    {
        public TileIdentifier Tile { get; set; }
    }
}
