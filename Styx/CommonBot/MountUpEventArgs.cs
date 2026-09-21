using System;

namespace Styx.CommonBot
{
    public class MountUpEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a boolean indicating whether to cancel this mount up.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// The destination
        /// </summary>
        public WoWPoint Destination { get; internal set; }
    }
}
