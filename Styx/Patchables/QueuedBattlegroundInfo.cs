using System;
using System.Runtime.InteropServices;
using Styx.WoWInternals;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QueuedBattlegroundInfo
    {
        public BattlegroundType Type
        {
            get { return (BattlegroundType)(MapId2 & 65535UL); }
        }

        public uint MapIdParsed
        {
            get
            {
                if (Status == BattlegroundStatus.Confirm)
                {
                    uint num = (uint)(MapId2 & 4294967295UL);
                    if ((num & 1048576U) == 0U && ((num >> 16) & 15U) != 1U)
                    {
                        if ((num & 65535U) != 32U)
                        {
                            return MapId;
                        }

                        return method_0(MapId2);
                    }
                }

                uint num2 = StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.BattlefieldStatusMapId);
                if (Status != BattlegroundStatus.Active || (ulong)num2 < (ulong)((long)StyxWoW.Db[ClientDb.Map].MinIndex) || (ulong)num2 > (ulong)((long)StyxWoW.Db[ClientDb.Map].MaxIndex))
                {
                    return method_0(MapId2);
                }

                WoWDb.Row row = StyxWoW.Db[ClientDb.Map].GetRow(num2);
                if (row != null && row.IsValid)
                {
                    return num2;
                }

                return method_0(MapId2);
            }
        }

        private uint method_0(ulong ulong_0)
        {
            uint num = (uint)(ulong_0 & 65535UL);
            if (num == 32U)
            {
                return 32U;
            }

            WoWDb.DbTable dbTable = StyxWoW.Db[ClientDb.BattlemasterList];
            if ((ulong)num >= (ulong)((long)dbTable.MinIndex) && (ulong)num <= (ulong)((long)dbTable.MaxIndex))
            {
                WoWDb.Row row = dbTable.GetRow(num);
                if (row != null && row.IsValid)
                {
                    return row.GetStruct<BLEntry>().Instances[0];
                }
            }

            return 0U;
        }

        public uint PreviousBgInfo;

        public IntPtr NextBgInfo;

        public uint BattlefieldIndex;

        public uint uintC;

        public uint uint10;

        public uint uint14;

        public uint uint18;

        public uint uint1C;

        public uint uint20;

        public uint uint24;

        public ulong MapId2;

        public uint MapId;

        public BattlegroundStatus Status;

        public uint uint38;

        public uint uint3C;

        public uint uint40;

        public uint PortalDuration;

        public uint EstimatedWaitTime;

        public uint TimeWaited;

        public uint InstanceId;

        public uint TeamSize;

        public uint dword58;

        public uint IsRatedMatch;

        public uint uint60;

        public uint uint64;

        public uint uint68;

        public uint uint6C;
    }
}
