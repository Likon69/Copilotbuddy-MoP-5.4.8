using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class LocalPlayer : WoWPlayer
    {
        public LocalPlayer(IntPtr baseAddress)
            : base(baseAddress)
        {
        }
    }
}
