using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWGroupInfo
    {
        private GroupInfo Info
        {
            get
            {
                IntPtr infoPtr = InfoPtr;
                if (infoPtr != IntPtr.Zero)
                {
                    return StyxWoW.Memory.Read<GroupInfo>(infoPtr);
                }
                return default(GroupInfo);
            }
        }

        private IntPtr InfoPtr
        {
            get
            {
                IntPtr intPtr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.RaidMemberInfoListPtr);
                if (intPtr == IntPtr.Zero)
                {
                    intPtr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.RaidMemberInfoListPtr2);
                }
                return intPtr;
            }
        }

        internal IntPtr TargetMarkerList
        {
            get { return Info.RaidTargetInfo; }
        }

        public bool IsInParty
        {
            get { return InfoPtr != IntPtr.Zero; }
        }

        public bool IsInRaid
        {
            get { return Flags.HasFlag(GroupFlags.Raid); }
        }

        public bool IsInBattlegroundParty
        {
            get { return Flags.HasFlag(GroupFlags.Battlegrounds); }
        }

        public bool IsInLfgParty
        {
            get { return Flags.HasFlag(GroupFlags.Lfg); }
        }

        public GroupFlags Flags
        {
            get { return (GroupFlags)Info.Flags; }
        }

        public uint GroupSize
        {
            get { return Info.GroupSize; }
        }

        public uint PartySize
        {
            get { return Info.PartySize + 1U; }
        }

        public int NumRaidMembers
        {
            get { return NativeRaidMembers.Count(); }
        }

        public int NumPartyMembers
        {
            get { return NativePartyMembers.Count(); }
        }

        public ulong GroupLeaderGuid
        {
            get { return Info.GroupLeaderGuid; }
        }

        public WoWPlayer GroupLeader
        {
            get { return ObjectManager.GetObjectByGuid<WoWPlayer>(Info.GroupLeaderGuid); }
        }

        public uint DungeonDifficultyId
        {
            get { return Info.DungeonDifficultyID; }
        }

        public uint RaidDifficultyId
        {
            get { return Info.RaidDifficultyId; }
        }

        public uint LfgDungeonId
        {
            get { return Info.LfgDungeonId & 16777215U; }
        }

        public ulong[] RaidMemberGuids
        {
            get { return NativeRaidMembers.Select(m => m.Guid).ToArray(); }
        }

        public ulong[] PartyMemberGuids
        {
            get { return NativePartyMembers.Select(m => m.Guid).ToArray(); }
        }

        public IEnumerable<WoWPartyMember> RaidMembers
        {
            get
            {
                TimeCachedValue<List<WoWPartyMember>> timeCachedValue;
                if ((timeCachedValue = _raidMembersCache) == null)
                {
                    timeCachedValue = (_raidMembersCache = new TimeCachedValue<List<WoWPartyMember>>(TimeSpan.FromMilliseconds(400.0), () => NativeRaidMembers.Select(m => new WoWPartyMember(m)).ToList()));
                }
                return timeCachedValue.Value.AsReadOnly();
            }
        }

        public IEnumerable<WoWPartyMember> PartyMembers
        {
            get
            {
                TimeCachedValue<List<WoWPartyMember>> timeCachedValue;
                if ((timeCachedValue = _partyMembersCache) == null)
                {
                    timeCachedValue = (_partyMembersCache = new TimeCachedValue<List<WoWPartyMember>>(TimeSpan.FromMilliseconds(400.0), () => NativePartyMembers.Select(m => new WoWPartyMember(m)).ToList()));
                }
                return timeCachedValue.Value.AsReadOnly();
            }
        }

        private IEnumerable<GroupMemberInfo> NativeRaidMembers
        {
            get
            {
                IntPtr[] intptr_ = Info.RaidMembers;
                if (intptr_ != null)
                {
                    foreach (IntPtr intPtr in intptr_)
                    {
                        if (intPtr != IntPtr.Zero)
                        {
                            GroupMemberInfo groupMemberInfo = default(GroupMemberInfo);
                            try
                            {
                                groupMemberInfo = StyxWoW.Memory.Read<GroupMemberInfo>(intPtr);
                            }
                            catch
                            {
                            }
                            if (groupMemberInfo.Guid != 0UL)
                            {
                                yield return groupMemberInfo;
                            }
                        }
                    }
                }
                yield break;
            }
        }

        private IEnumerable<GroupMemberInfo> NativePartyMembers
        {
            get
            {
                IntPtr[] intptr_ = Info.PartyMembers;
                if (intptr_ != null)
                {
                    foreach (IntPtr intPtr in intptr_)
                    {
                        if (intPtr != IntPtr.Zero)
                        {
                            GroupMemberInfo groupMemberInfo = default(GroupMemberInfo);
                            try
                            {
                                groupMemberInfo = StyxWoW.Memory.Read<GroupMemberInfo>(intPtr);
                            }
                            catch
                            {
                            }
                            if (groupMemberInfo.Guid != 0UL)
                            {
                                yield return groupMemberInfo;
                            }
                        }
                    }
                }
                yield break;
            }
        }

        public bool IsInCurrentRaid(ulong guid)
        {
            return NativeRaidMembers.Any(m => m.Guid == guid);
        }

        public bool IsInCurrentParty(ulong guid)
        {
            return NativePartyMembers.Any(m => m.Guid == guid);
        }

        public void PlaceWorldMarker(WorldMarker mark, WoWPoint location)
        {
            if (!IsInRaid)
            {
                return;
            }
            int num = mark - WorldMarker.Blue + 1;
            if (num <= 0 || num > 5)
            {
                throw new ArgumentOutOfRangeException("mark");
            }
            if (location == WoWPoint.Zero)
            {
                throw new ArgumentException("Location cannot be Zero", "location");
            }
            SpellManager.CastSpellById((int)mark);
            SpellManager.ClickRemoteLocation(location);
        }

        public void ClearWorldMarker(WorldMarker mark)
        {
            if (!IsInRaid)
            {
                return;
            }
            int num = mark - WorldMarker.Blue + 1;
            if (num <= 0 || num > 5)
            {
                throw new ArgumentOutOfRangeException("mark");
            }
            Lua.DoString(string.Format("ClearRaidMarker({0})", num), "WoW.lua");
        }

        private TimeCachedValue<List<WoWPartyMember>> _raidMembersCache;

        private TimeCachedValue<List<WoWPartyMember>> _partyMembersCache;

        public enum WorldMarker
        {
            Blue = 84996,
            Green,
            Purple,
            Red,
            Yellow
        }

        internal struct GroupInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public IntPtr[] RaidMembers;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public IntPtr[] PartyMembers;

            public uint uint_0;

            public IntPtr intptr_2;

            public uint uint_1;

            public uint uint_2;

            public uint GroupSize;

            public uint PartySize;

            public uint uint_5;

            public ulong GroupLeaderGuid;

            public uint Flags;

            public uint DungeonDifficultyID;

            public uint RaidDifficultyId;

            public uint uint_9;

            public uint LfgDungeonId;

            public uint uint_11;

            public uint uint_12;

            public uint uint_13;

            public uint uint_14;

            public uint uint_15;

            public uint uint_16;

            public uint uint_17;

            public uint uint_18;

            public uint uint_19;

            public uint uint_20;

            public uint uint_21;

            public uint uint_22;

            public IntPtr RaidTargetInfo;

            public uint uint_23;

            public uint uint_24;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct GroupMemberInfo
        {
            public override string ToString()
            {
                return string.Format("Group: {0}, Role: {1}, Flags: {2}, F3: {3}, Rank: {4}, Dword8: {5}, DwordC: {6}", new object[] { Group, Role, Flags, f3, Rank, dword8, dwordC });
            }

            public byte Group;

            public byte Flags;

            public byte Role;

            public byte f3;

            public uint Rank;

            public uint dword8;

            public uint dwordC;

            public ulong Guid;

            public IntPtr ParentGroupInfo;

            public uint dword1C;

            public uint dword20;

            public uint dword24;

            public uint dword28;

            public uint dword2C;
        }

        [Flags]
        public enum GroupFlags
        {
            Party = 0,
            Battlegrounds = 1,
            Raid = 2,
            RestrictedLfg = 4,
            LfgParty = 8,
            Unk10 = 16,
            Unk20 = 32,
            EveryoneIsAssist = 64,
            Lfg = 12
        }
    }
}
