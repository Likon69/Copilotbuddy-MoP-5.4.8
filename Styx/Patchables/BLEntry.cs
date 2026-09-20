using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Styx.Patchables
{
    internal struct BLEntry
    {
        public string Name
        {
            get { return StyxWoW.Memory.ReadString(NamePtr, Encoding.UTF8); }
        }

        public override string ToString()
        {
            return string.Format("Row: {0}, Instances: {1}, InstanceType: {2}, CanJoinAsGroup: {3}, Name: {4}, Dword24: {5}, WorldStateId: {6}, MinLevel: {7}, MaxLevel: {8}, RatedBattlegroundPlayers: {9}, MinWargamePlayers: {10}, MaxWargamePlayers: {11}",
                Row, Instances, InstanceType, CanJoinAsGroup, Name, dword24, WorldStateId, MinLevel, MaxLevel, RatedBattlegroundPlayers,
                MinWargamePlayers, MaxWargamePlayers);
        }

        public uint Row;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] Instances;

        public uint InstanceType;

        public uint CanJoinAsGroup;

        public IntPtr NamePtr;

        public uint dword24;

        public uint WorldStateId;

        public uint MinLevel;

        public uint MaxLevel;

        public uint RatedBattlegroundPlayers;

        public uint MinWargamePlayers;

        public uint MaxWargamePlayers;
    }
}
