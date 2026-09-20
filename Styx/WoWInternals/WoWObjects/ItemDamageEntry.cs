using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.WoWObjects
{
    internal struct ItemDamageEntry
    {
        public uint Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7, ArraySubType = UnmanagedType.R4)]
        public float[] Value;

        public uint Id2;
    }
}
