using System;
using System.Runtime.InteropServices;
using GreenMagic;
using Tripper.Tools.Math;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWPartyMember : IEquatable<WoWPartyMember>
    {
        internal WoWPartyMember(WoWGroupInfo.GroupMemberInfo info)
        {
            _guid = info.Guid;
            _raidInfo = info;
            _state = GetInfo(_raidInfo.Guid);
        }

        private T GetLiveOrCachedValue<T>(Func<WoWPlayer, T> liveValue, Func<T> cachedValue)
        {
            WoWPlayer woWPlayer = ToPlayer();
            if (!(woWPlayer != null))
            {
                return cachedValue();
            }
            return liveValue(woWPlayer);
        }

        public WoWSpec Specialization
        {
            get
            {
                return GetLiveOrCachedValue(player => player.Specialization, () => (WoWSpec)_state.CurrentSpecId);
            }
        }

        public ulong Guid
        {
            get
            {
                return _guid;
            }
        }

        public bool IsMainTank
        {
            get
            {
                return (_raidInfo.Flags & 2) != 0;
            }
        }

        public bool IsMainAssist
        {
            get
            {
                return (_raidInfo.Flags & 4) != 0;
            }
        }

        public bool IsOnline
        {
            get
            {
                return (_state.StatusFlags & 1) != 0;
            }
        }

        public bool PvpFlagged
        {
            get
            {
                return (_state.StatusFlags & 2) != 0;
            }
        }

        public bool Dead
        {
            get
            {
                return (_state.StatusFlags & 4) != 0;
            }
        }

        public bool Ghost
        {
            get
            {
                return (_state.StatusFlags & 8) != 0;
            }
        }

        public bool FFAPvpFlagged
        {
            get
            {
                return (_state.StatusFlags & 16) != 0;
            }
        }

        public bool GroupLeader
        {
            get
            {
                return (_state.StatusFlags & 64) != 0;
            }
        }

        public bool DNDFlagged
        {
            get
            {
                return (_state.StatusFlags & 128) != 0;
            }
        }

        public bool RAFLinked
        {
            get
            {
                return (_state.StatusFlags & 256) != 0;
            }
        }

        public WoWPowerType PowerType
        {
            get
            {
                return (WoWPowerType)_state.PowerType;
            }
        }

        public uint Health
        {
            get
            {
                return GetLiveOrCachedValue(player => player.CurrentHealth, () => _state.Health);
            }
        }

        public uint HealthMax
        {
            get
            {
                return GetLiveOrCachedValue(player => player.MaxHealth, () => _state.HealthMax);
            }
        }

        public uint Power
        {
            get
            {
                return GetLiveOrCachedValue(player => player.CurrentPower, () => (uint)_state.Power);
            }
        }

        public uint PowerMax
        {
            get
            {
                return GetLiveOrCachedValue(player => player.MaxPower, () => (uint)_state.MaxPower);
            }
        }

        public int Level
        {
            get
            {
                return GetLiveOrCachedValue(player => player.Level, () => (int)_state.Level);
            }
        }

        public int AreaTableId
        {
            get
            {
                return (int)_state.AreaTableId;
            }
        }

        public ushort ContinentId
        {
            get
            {
                return _state.ContinentId;
            }
        }

        public Vector2 Location
        {
            get
            {
                return GetLiveOrCachedValue(player =>
                {
                    WoWPoint location = player.Location;
                    return new Vector2(location.X, location.Y);
                }, () => new Vector2((float)_state.X, (float)_state.Y));
            }
        }

        public WoWPoint Location3D
        {
            get
            {
                return GetLiveOrCachedValue(player => player.Location, () => new WoWPoint((float)_state.X, (float)_state.Y, (float)_state.Z));
            }
        }

        public uint GroupNumber
        {
            get
            {
                return (uint)_raidInfo.Group;
            }
        }

        public uint RaidRank
        {
            get
            {
                return _raidInfo.Rank;
            }
        }

        public WoWPartyMember.GroupRole Role
        {
            get
            {
                return (WoWPartyMember.GroupRole)_raidInfo.Role;
            }
        }

        public bool HasRole(WoWPartyMember.GroupRole role)
        {
            return (Role & role) != WoWPartyMember.GroupRole.None;
        }

        public bool Equals(WoWPartyMember other)
        {
            return !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || other._guid == _guid);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) || (obj.GetType() == typeof(WoWPartyMember) && Equals((WoWPartyMember)obj)));
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Info: {0}", _state.ToString().Replace(",", Environment.NewLine), _raidInfo);
        }

        public static bool operator ==(WoWPartyMember left, WoWPartyMember right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WoWPartyMember left, WoWPartyMember right)
        {
            return !Equals(left, right);
        }

        public WoWPlayer ToPlayer()
        {
            return ObjectManager.GetObjectByGuid<WoWPlayer>(Guid);
        }

        internal static PartyMemberState GetInfo(ulong guid)
        {
            if (guid == 0UL)
            {
                return default(PartyMemberState);
            }
            PartyMemberState state;
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(8))
            {
                allocatedMemory.Write<ulong>(0, guid);
                IntPtr intPtr = StyxWoW.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.CGPartyMemberStateRepository__GetState, CallingConvention.Cdecl, new object[] { allocatedMemory.Address });
                if (intPtr != IntPtr.Zero)
                {
                    state = StyxWoW.Memory.Read<PartyMemberState>(intPtr);
                }
                else
                {
                    state = default(PartyMemberState);
                }
            }
            return state;
        }

        private readonly ulong _guid;

        private PartyMemberState _state;

        private WoWGroupInfo.GroupMemberInfo _raidInfo;

        [Flags]
        public enum GroupRole
        {
            None = 0,
            Leader = 1,
            Tank = 2,
            Healer = 4,
            Damage = 8
        }

        [Flags]
        internal enum PartyFlags
        {
            Valid = 1,
            PVP = 2,
            Dead = 4,
            Ghost = 8,
            FFAPVP = 16,
            GroupLeader = 64,
            AFK = 128,
            RAFLinked = 256,
            InVehicle = 512
        }

        internal struct GroupInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public IntPtr[] RaidMembers;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public IntPtr[] PartyMembers;

            public IntPtr Player;

            public uint dwordB4;

            public IntPtr dwordB8;

            public uint dwordBC;

            public uint dwordC0;

            public uint GroupSize;

            public uint PartySize;

            public uint dwordCC;

            public ulong GroupLeaderGuid;

            public uint Flags;

            public uint DungeonDifficultyID;

            public uint RaidDifficultyId;

            public uint dwordE4;

            public uint LfgDungeonId;

            public uint dwordEC;

            public uint dwordF0;

            public uint dwordF4;

            public uint dwordF8;

            public uint dwordFC;

            public uint dword100;

            public uint dword104;

            public uint dword108;

            public uint dword10C;

            public uint dword110;

            public uint BuffMask;

            public uint BuffCount;

            public IntPtr RaidTargetInfo;

            public uint dword120;

            public uint dword124;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PartyMemberState
        {
            public override string ToString()
            {
                return string.Format("Guid: {0}, Health: {1}, HealthMax: {2}, Dword10: {3}, VehicleSeat: {4}, Dword18BuffsCount: {5}, StatusFlags: {6}, OverrideDisplayPowerId: {7}, Power: {8}, MaxPower: {9}, Level: {10}, CurrentSpecId: {11}, AreaTableId: {12}, ContinentId: {13}, X: {14}, Y: {15}, Z: {16}, PartyType: {17}, PowerType: {18}, Byte35: {19}, Byte36: {20}, Byte37: {21}, PetGuid: {22}, Dword40: {23}, Dword44: {24}, Dword48: {25}, Dword4C: {26}, Dword50: {27}, Dword54: {28}, Dword58: {29}, Dword5C: {30}, Dword60: {31}, Dword64: {32}, Dword68: {33}, Dword6C: {34}, Dword70: {35}, Dword74: {36}, Dword78: {37}, Dword7C: {38}, Dword80: {39}, Dword84: {40}, Dword88: {41}, Dword8C: {42}, Dword90: {43}, Dword94: {44}, Dword98: {45}, Dword9C: {46}, DwordA0: {47}, DwordA4: {48}, DwordA8: {49}, DwordAc: {50}, DwordB0: {51}, DwordB4: {52}, DwordB8Buffs: {53}, DwordBc: {54}, DwordC0Auras: {55}, DwordC4: {56}, PhaseInfo: {57}", new object[]
                {
                    Guid, Health, HealthMax, dword10, VehicleSeat, dword18_BuffsCount, StatusFlags, OverrideDisplayPowerId, Power, MaxPower,
                    Level, CurrentSpecId, AreaTableId, ContinentId, X, Y, Z, PartyType, PowerType, byte35,
                    byte36, byte37, PetGuid, dword40, dword44, dword48, dword4C, dword50, dword54, dword58,
                    dword5C, dword60, dword64, dword68, dword6C, dword70, dword74, dword78, dword7C, dword80,
                    dword84, dword88, dword8C, dword90, dword94, dword98, dword9C, dwordA0, dwordA4, dwordA8,
                    dwordAC, dwordB0, dwordB4, dwordB8_Buffs, dwordBC, dwordC0_Auras, dwordC4, PhaseInfo
                });
            }

            public ulong Guid;

            public uint Health;

            public uint HealthMax;

            public uint dword10;

            public uint VehicleSeat;

            public uint dword18_BuffsCount;

            public ushort StatusFlags;

            public ushort OverrideDisplayPowerId;

            public ushort Power;

            public ushort MaxPower;

            public ushort Level;

            public ushort CurrentSpecId;

            public ushort AreaTableId;

            public ushort ContinentId;

            public short X;

            public short Y;

            public short Z;

            public ushort PartyType;

            public byte PowerType;

            public byte byte35;

            public byte byte36;

            public byte byte37;

            public ulong PetGuid;

            public uint dword40;

            public uint dword44;

            public uint dword48;

            public uint dword4C;

            public uint dword50;

            public uint dword54;

            public uint dword58;

            public uint dword5C;

            public uint dword60;

            public uint dword64;

            public uint dword68;

            public uint dword6C;

            public uint dword70;

            public uint dword74;

            public uint dword78;

            public uint dword7C;

            public uint dword80;

            public uint dword84;

            public uint dword88;

            public uint dword8C;

            public uint dword90;

            public uint dword94;

            public uint dword98;

            public uint dword9C;

            public uint dwordA0;

            public uint dwordA4;

            public uint dwordA8;

            public uint dwordAC;

            public uint dwordB0;

            public uint dwordB4;

            public uint dwordB8_Buffs;

            public uint dwordBC;

            public uint dwordC0_Auras;

            public uint dwordC4;

            public uint PhaseInfo;
        }
    }
}
