using System;
using System.Runtime.InteropServices;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LootRollItemInfo
    {
        private readonly uint dword0;
        public IntPtr NextLink;
        public uint RollId;
        public uint dwordC;
        public ulong Guid;
        public uint ItemSlot;
        public uint ItemId;
        public uint RandomPropertiesId;
        public uint RandomSuffixId;
        public uint Count;
        public uint dword2C;
        public uint dword30;
        public uint dword34;
        public ulong TargetGuid;
        public byte RollNumber;
        public byte byte41;
        public byte byte42;
        public byte byte43;
        public uint RollTime;
        public uint RollFlags;
        public uint RollType;
        public short word50;
        public byte byte52;
        public byte byte53;
        public byte byte54;

        public override string ToString()
        {
            return string.Format(
                "Dword0: {0}, NextLink: {1}, RollId: {2}, DwordC: {3}, Guid: {4}, ItemSlot: {5}, ItemId: {6}, RandomPropertiesId: {7}, RandomSuffixId: {8}, Count: {9}, Dword2C: {10}, Dword30: {11}, Dword34: {12}, TargetGuid: {13}, RollNumber: {14}, Byte41: {15}, Byte42: {16}, Byte43: {17}, RollTime: {18}, RollFlags: {19}, RollType: {20}, Word50: {21}, Byte52: {22}, Byte53: {23}, Byte54: {24}",
                dword0, NextLink, RollId, dwordC, Guid, ItemSlot, ItemId, RandomPropertiesId,
                RandomSuffixId, Count, dword2C, dword30, dword34, TargetGuid, RollNumber, byte41,
                byte42, byte43, RollTime, RollFlags, RollType, word50, byte52, byte53, byte54);
        }

        public LootRollItemInfo? Next
        {
            get
            {
                if (((int)NextLink & 1) != 0 && NextLink != IntPtr.Zero)
                    return StyxWoW.Memory.Read<LootRollItemInfo>(NextLink);
                return null;
            }
        }
    }
}
