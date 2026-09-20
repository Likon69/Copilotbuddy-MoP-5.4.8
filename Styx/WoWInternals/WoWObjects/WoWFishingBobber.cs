using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWFishingBobber : WoWAnimatedSubObject
    {
        internal WoWFishingBobber(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public bool IsBobbing
        {
            get { return StyxWoW.Memory.Read<int>(BaseAddress + 20) == 0; }
        }
    }
}
