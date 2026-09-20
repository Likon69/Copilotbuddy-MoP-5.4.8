using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.Misc
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeStabledPet
    {
        public uint PetNumber;
        public int PetLevel;
        public uint CreatureId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] PetName;
        private uint _unk2;
    }
}
