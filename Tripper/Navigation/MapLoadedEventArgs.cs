using System;

namespace Tripper.Navigation
{
    public class MapLoadedEventArgs : EventArgs
    {
        public string[] Names { get; set; }
        public bool IsTiled { get; set; }
    }
}
