using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.WoWObjects
{
    public struct DurabilityCostEntry
    {
        public uint itemLevel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)]
        public uint[] Multiplier;
    }
}
