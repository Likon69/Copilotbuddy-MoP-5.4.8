using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.WoWObjects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct CGBag
    {
        public readonly uint SlotCount;

        public readonly IntPtr ItemGuidArray;

        public readonly ulong Guid;

        public readonly byte IsInventory;
    }
}
