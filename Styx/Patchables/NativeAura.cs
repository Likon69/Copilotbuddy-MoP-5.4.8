using System;
using System.Runtime.InteropServices;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct NativeAura
    {
        public override string ToString()
        {
            return string.Format("NumVariableEffects: {0}, VariableEffectsPtr: {1}, Dword8: {2}, DwordC: {3}, Dword10: {4}, Dword14: {5}, Dword18: {6}, Dword1C: {7}, CreatorGuid: {8}, AuraId: {9}, Flags: {10}, StackCount: {11}, Level: {12}, Unk0: {13}, Duration: {14}, EndTime: {15}, Dword38: {16}, Dword3C: {17}", NumVariableEffects, VariableEffectsPtr, Dword8, DwordC, Dword10, Dword14, Dword18, Dword1C, CreatorGuid, AuraId, Flags, StackCount, Level, Unk0, Duration, EndTime, Dword38, Dword3C);
        }

        public int NumVariableEffects;

        public IntPtr VariableEffectsPtr;

        public uint Dword8;

        public uint DwordC;

        public uint Dword10;

        public uint Dword14;

        public uint Dword18;

        public uint Dword1C;

        public ulong CreatorGuid;

        public uint AuraId;

        public byte Flags;

        public byte StackCount;

        public byte Level;

        public byte Unk0;

        public uint Duration;

        public uint EndTime;

        public uint Dword38;

        public uint Dword3C;
    }
}
