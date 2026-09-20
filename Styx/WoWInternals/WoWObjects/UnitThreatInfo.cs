using System;
using System.Runtime.InteropServices;
using GreenMagic;
using Styx.Patchables;

namespace Styx.WoWInternals.WoWObjects
{
    public class UnitThreatInfo
    {
        private UnitThreatInfo(NativeThreatEntry entry)
        {
            _entry = entry;
        }

        public byte RawPercent
        {
            get { return _entry.RawPercent; }
        }

        public ulong TargetGuid
        {
            get { return _entry.UnitGuid; }
        }

        public ThreatStatus ThreatStatus
        {
            get { return (ThreatStatus)_entry.Status; }
        }

        public uint ThreatValue
        {
            get { return (uint)_entry.RawThreat; }
        }

        public override string ToString()
        {
            return string.Format("TargetGuid: {0:X016}, RawPercent: {1}, ThreatStatus: {2}, ThreatValue: {3}", new object[] { TargetGuid, RawPercent, ThreatStatus, ThreatValue });
        }

        internal static UnitThreatInfo GetForUnit(WoWUnit mob, WoWUnit unit)
        {
            IntPtr address = mob.BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitThreatInfo;
            NativeThreatInfo threatInfo = StyxWoW.Memory.Read<NativeThreatInfo>(address);
            ulong guid = unit.Guid;
            IntPtr result;
            bool found = FindThreatEntry(threatInfo.ThreatTable, (uint)guid, guid, out result);
            if (threatInfo.PrimaryTargetGuid != 0UL && found)
            {
                return new UnitThreatInfo(StyxWoW.Memory.Read<NativeThreatEntry>(result));
            }
            return new UnitThreatInfo(new NativeThreatEntry
            {
                Status = (byte)(found ? 1 : 0),
                RawPercent = 0,
                RawThreat = 0,
                UnitGuid = guid,
                Hash = (uint)guid
            });
        }

        private static bool FindThreatEntry(NativeWowTable table, uint hash, ulong guid, out IntPtr result)
        {
            result = (IntPtr)0;
            if (table.BucketMask == 4294967295U)
            {
                return false;
            }
            ExternalProcessMemory memory = StyxWoW.Memory;
            uint bucketIndex = hash & table.BucketMask;
            NativeWowTableBucket bucket = memory.Read<NativeWowTableBucket>(table.Buckets + (int)(bucketIndex * (uint)MarshalCache<NativeWowTableBucket>.Size));
            IntPtr current = bucket.First;
            while (((int)current & 1) == 0 && current != IntPtr.Zero)
            {
                NativeThreatEntry entry = memory.Read<NativeThreatEntry>(current);
                if (entry.Hash == hash && entry.UnitGuid == guid)
                {
                    result = current;
                    return true;
                }
                current = memory.Read<IntPtr>(current + 4 + (int)bucket.NextOffset);
            }
            return false;
        }

        private readonly NativeThreatEntry _entry;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NativeThreatEntry
        {
            public uint Hash;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private readonly byte[] _unk2;

            public ulong UnitGuid;

            public byte Status;

            public byte RawPercent;

            private readonly ushort _unk3;

            public int RawThreat;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NativeThreatInfo
        {
            [MarshalAs(UnmanagedType.U8)]
            public readonly ulong PrimaryTargetGuid;

            public readonly NativeWowTable ThreatTable;
        }
    }
}
