using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using Styx.Helpers;
using Styx.Offsets;
using Styx.Patchables;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWCache;
using Tripper.Tools.Math;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWGameObject : WoWObject, IComparable<WoWUnit>, IComparable<WoWGameObject>, IComparer<WoWUnit>, IComparer<WoWGameObject>, ILootableObject
    {
        public WoWGameObject(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public override string ToString()
        {
            return string.Format("[{0}]: DisplayId:{1} State:{2} ArtKit:{3} AnimationProgress:{4} Locked:{5} IsUse:{6} Triggered:{7} Entry:{8} SubType:{9}", Name, DisplayId, State, ArtKit, AnimationProgress, Locked, InUse, Triggered, Entry, SubType);
        }

        public override WoWPoint RelativeLocation
        {
            get { return StyxWoW.Memory.Read<WoWPoint>(BaseAddress + (int)StyxWoW.Offsets.Misc.GObjectX); }
        }

        public uint DisplayId
        {
            get { return GetStorageField<uint>((uint)WoWGameObjectFields.DisplayID); }
        }

        public byte[] Bytes1
        {
            get { return BitConverter.GetBytes(GetStorageField<uint>((uint)WoWGameObjectFields.PercentHealth)); }
        }

        public ulong CreatedByGuid
        {
            get { return GetStorageField<ulong>((uint)WoWGameObjectFields.CreatedBy); }
        }

        public WoWUnit CreatedBy
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CreatedByGuid); }
        }

        private BitVector32 Flags
        {
            get { return GetStorageField<BitVector32>((uint)WoWGameObjectFields.Flags); }
        }

        public uint FlagsUint
        {
            get { return GetStorageField<uint>((uint)WoWGameObjectFields.Flags); }
        }

        public int Level
        {
            get { return GetStorageField<int>((uint)WoWGameObjectFields.Level); }
        }

        public float ParentRotation
        {
            get { return GetStorageField<float>((uint)WoWGameObjectFields.ParentRotation); }
        }

        public uint FactionTemplateId
        {
            get { return GetStorageField<uint>((uint)WoWGameObjectFields.FactionTemplate); }
        }

        public FactionTemplate FactionTemplate
        {
            get { return Styx.WoWInternals.DBC.FactionTemplate.FromId(FactionTemplateId); }
        }

        public byte AnimationState
        {
            get { return StyxWoW.Memory.Read<byte>(BaseAddress + (int)StyxWoW.Offsets.Misc.GObjectAnimationState); }
        }

        public WoWGameObjectState State
        {
            get { return (WoWGameObjectState)Bytes1[0]; }
        }

        public WoWGameObjectType SubType
        {
            get { return (WoWGameObjectType)Bytes1[1]; }
        }

        public WoWSubObject SubObj
        {
            get { return ConstructSubObj(); }
        }

        public WoWSpellFocus SpellFocus
        {
            get
            {
                int val;
                if (!GetDataSlot(GameObjectDataSlot.SpellFocusId, out val))
                {
                    return WoWSpellFocus.None;
                }

                return (WoWSpellFocus)val;
            }
        }

        public override float InteractRange
        {
            get
            {
                if (SubObj == null)
                {
                    return 4f;
                }

                return SubObj.InteractDistance;
            }
        }

        public override bool WithinInteractRange
        {
            get
            {
                if (SubObj == null)
                {
                    return base.WithinInteractRange;
                }

                return SubObj.CheckRange();
            }
        }

        public byte ArtKit
        {
            get { return Bytes1[2]; }
        }

        public byte AnimationProgress
        {
            get { return Bytes1[3]; }
        }

        public bool Locked
        {
            get { return Flags[2]; }
        }

        public bool Transport
        {
            get { return Flags[8]; }
        }

        public bool InUse
        {
            get { return Flags[1]; }
        }

        public bool Triggered
        {
            get { return Flags[64]; }
        }

        public bool IsHerb
        {
            get { return LockType == WoWLockType.Herbalism; }
        }

        public WoWLockType LockType
        {
            get
            {
                LockEntry? lockRecord = LockRecord;
                if (lockRecord != null && lockRecord.Value.Type[0] == 2U)
                {
                    return (WoWLockType)lockRecord.Value.LockProperties[0];
                }

                return WoWLockType.None;
            }
        }

        public bool IsMineral
        {
            get { return Entry != 185877U && LockType == WoWLockType.Mining; }
        }

        public bool IsChest
        {
            get { return _chests.ContainsKey(Entry); }
        }

        public string Model
        {
            get { return GetCachedModel(); }
        }

        private WoWSubObject ConstructSubObj()
        {
            IntPtr ptr = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.GObjectSubObj);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            switch (SubType)
            {
                case WoWGameObjectType.Door:
                    return new WoWDoor(ptr);
                case WoWGameObjectType.Button:
                case WoWGameObjectType.QuestGiver:
                case WoWGameObjectType.Chest:
                case WoWGameObjectType.Generic:
                case WoWGameObjectType.Trap:
                case WoWGameObjectType.SpellFocus:
                case WoWGameObjectType.Text:
                case WoWGameObjectType.Goober:
                case WoWGameObjectType.AreaDamage:
                case WoWGameObjectType.DuelArbiter:
                case WoWGameObjectType.Ritual:
                case WoWGameObjectType.Mailbox:
                case WoWGameObjectType.SpellCaster:
                case WoWGameObjectType.MeetingStone:
                case WoWGameObjectType.FlagStand:
                case WoWGameObjectType.FishingHole:
                case WoWGameObjectType.FlagDrop:
                case WoWGameObjectType.MiniGame:
                case WoWGameObjectType.CapturePoint:
                case WoWGameObjectType.GuildBank:
                    return new WoWAnimatedSubObject(ptr);
                case WoWGameObjectType.Chair:
                    return new WoWChair(ptr);
                case WoWGameObjectType.FishingNode:
                    return new WoWFishingBobber(ptr);
            }

            return new WoWSubObject(ptr);
        }

        public WoWUnitReaction GetReactionTowards(WoWUnit unit)
        {
            WoWUnit createdBy = CreatedBy;
            if (createdBy != null)
            {
                return createdBy.GetReactionTowards(unit);
            }

            Styx.WoWInternals.DBC.FactionTemplate factionTemplate = FactionTemplate;
            if (factionTemplate == null)
            {
                return WoWUnitReaction.Neutral;
            }

            return factionTemplate.GetReactionTowards(unit);
        }

        internal LockEntry? LockRecord
        {
            get
            {
                if (_lockRecordCache == null)
                {
                    _lockRecordCache = new PerFrameCachedValue<LockEntry?>(() =>
                    {
                        int val;
                        if (!GetDataSlot(GameObjectDataSlot.LockId, out val))
                        {
                            return null;
                        }

                        uint entry = (uint)val;
                        if (entry != 0)
                        {
                            WoWDb.Row row = StyxWoW.Db[ClientDb.Lock].GetRow(entry);
                            if (row != null)
                            {
                                LockEntry le = row.GetStruct<LockEntry>();
                                return le;
                            }
                        }

                        return null;
                    });
                }

                return _lockRecordCache;
            }
        }

        public uint? RequiredSkill
        {
            get
            {
                if (_requiredSkillCache == null)
                {
                    _requiredSkillCache = new PerFrameCachedValue<uint?>(() =>
                    {
                        if (IsHerb || IsMineral || IsChest)
                        {
                            int val;
                            if (!GetDataSlot(GameObjectDataSlot.LockId, out val))
                            {
                                return null;
                            }

                            uint entry = (uint)val;
                            if (entry != 0)
                            {
                                WoWDb.Row row = StyxWoW.Db[ClientDb.Lock].GetRow(entry);
                                if (row != null)
                                {
                                    return row.GetStruct<LockEntry>().RequiredSkill[0];
                                }
                            }
                        }

                        return null;
                    });
                }

                return _requiredSkillCache;
            }
        }

        public bool CanLoot
        {
            get
            {
                if (SubType != WoWGameObjectType.Chest && SubType != WoWGameObjectType.Goober)
                {
                    return false;
                }

                if (RequiredSkill != null)
                {
                    if (IsHerb)
                    {
                        WoWSkill skill = StyxWoW.Me.GetSkill(SkillLine.Herbalism);

                        int currentValue = 0;
                        if (StyxWoW.Me.Race == WoWRace.Tauren && (currentValue = skill.CurrentValue) != 0)
                        {
                            currentValue += 15;
                        }

                        if (currentValue != 0)
                        {
                            if (currentValue >= RequiredSkill && skill.MaxValue != 0)
                            {
                                return true;
                            }
                        }

                        if (skill != null && skill.CurrentValue >= RequiredSkill && skill.MaxValue != 0)
                        {
                            return true;
                        }

                        return false;
                    }

                    if (IsMineral)
                    {
                        WoWSkill skill = StyxWoW.Me.GetSkill(SkillLine.Mining);

                        if (skill != null && skill.CurrentValue >= RequiredSkill && skill.MaxValue != 0)
                        {
                            return true;
                        }

                        return false;
                    }
                }

                return (Bytes1[0] & 1) == 1 && CanUse();
            }
        }

        public override Matrix GetWorldMatrix()
        {
            return StyxWoW.Memory.Read<Matrix>(BaseAddress + (int)StyxWoW.Offsets.Misc.GObjectWorldMatrix);
        }

        private string GetCachedModel()
        {
            if (_model == null)
            {
                uint displayId = DisplayId;
                WoWDb.Row row;
                if ((row = StyxWoW.Db[ClientDb.GameObjectDisplayInfo].GetRow(displayId)) != null)
                {
                    _model = row.GetStringField(1U);
                }
                else
                {
                    _model = string.Empty;
                }
            }

            return _model;
        }

        public bool GetCachedInfo(out Styx.WoWInternals.WoWCache.WoWCache.GameObjectCacheEntry inf)
        {
            IntPtr ptr = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.GObjectCachedInfo);
            if (ptr != IntPtr.Zero)
            {
                inf = StyxWoW.Memory.Read<Styx.WoWInternals.WoWCache.WoWCache.GameObjectCacheEntry>(ptr);
                return true;
            }

            inf = default(Styx.WoWInternals.WoWCache.WoWCache.GameObjectCacheEntry);
            return false;
        }

        public bool GetDataSlot(GameObjectDataSlot dataSlot, out int value)
        {
            return GetDataSlot((uint)dataSlot, out value);
        }

        public bool GetDataSlot(GameObjectDataSlot dataSlot, out bool value)
        {
            int val;
            bool ret = GetDataSlot((uint)dataSlot, out val);
            value = val != 0;
            return ret;
        }

        public bool GetDataSlot(uint dataSlot, out int value)
        {
            Styx.WoWInternals.WoWCache.WoWCache.GameObjectCacheEntry cachedInfo;
            if (!GetCachedInfo(out cachedInfo))
            {
                value = 0;
                return false;
            }

            WoWGameObjectType type = SubType;
            if ((uint)type >= GameObjectSubTypeCount)
            {
                value = 0;
                return false;
            }

            if (_dataSlots == null)
            {
                CacheDataSlots();
            }

            uint typeId = (uint)type;
            for (int i = 0; i < _dataSlots[typeId].Length; i++)
            {
                if (_dataSlots[typeId][i] == dataSlot)
                {
                    value = cachedInfo.Properties[i];
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool CanUse()
        {
            return SubObj.CanUse();
        }

        public bool CanUseNow()
        {
            return SubObj.CanUseNow();
        }

        public bool CanUseNow(out GameError reason)
        {
            return SubObj.CanUseNow(out reason);
        }

        private static void CacheDataSlots()
        {
            _dataSlots = new uint[GameObjectSubTypeCount][];

            GameObjectDataSlotsEntry[] dataSlotDescriptors = StyxWoW.Memory.ReadArray<GameObjectDataSlotsEntry>(StyxWoW.Offsets.Statics.GameObjectDataSlots, GameObjectSubTypeCount);
            for (int i = 0; i < GameObjectSubTypeCount; i++)
            {
                int count = (int)dataSlotDescriptors[i].Count;
                int truncatedCount = count > 32 ? 32 : count;
                uint[] arr = StyxWoW.Memory.ReadArray<uint>(dataSlotDescriptors[i].Types, truncatedCount);
                _dataSlots[i] = arr;
            }
        }

        #region Implementation of IComparable<WoWGameObject> Members

        public int CompareTo(WoWGameObject other)
        {
            return DistanceSqr.CompareTo(other.DistanceSqr);
        }

        #endregion

        #region Implementation of IComparable<WoWUnit> Members

        public int CompareTo(WoWUnit other)
        {
            return DistanceSqr.CompareTo(other.DistanceSqr);
        }

        #endregion

        #region Implementation of IComparer<WoWUnit>

        public int Compare(WoWUnit x, WoWUnit y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return x.CompareTo(y);
        }

        #endregion

        #region Implementation of IComparer<WoWGameObject>

        public int Compare(WoWGameObject x, WoWGameObject y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return x.CompareTo(y);
        }

        #endregion

        private const int GameObjectSubTypeCount = 36;

        private PerFrameCachedValue<LockEntry?> _lockRecordCache;

        private PerFrameCachedValue<uint?> _requiredSkillCache;

        private string _model;

        private static uint[][] _dataSlots;

        #region Chests

        private readonly Dictionary<uint, string> _chests = new Dictionary<uint, string>
        {
            { 3658U, "Water Barrel" },
            { 106318U, "Battered Chest" },
            { 181802U, "Adamantite Bound Chest" },
            { 190552U, "Ancient Drakkari Chest" },
            { 181798U, "Fel Iron Chest" },
            { 181804U, "Felsteel Chest" },
            { 181800U, "Heavy Fel Iron Chest" },
            { 75293U, "Large Battered Chest" },
            { 2850U, "Solid Chest" },
            { 2844U, "Tattered Chest" },
            { 3715U, "Tattered Chest" }
        };

        #endregion

        public struct LockEntry
        {
            public int Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] Type;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] LockProperties;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] RequiredSkill;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] Action;
        }

        public struct LockTypeEntry
        {
            public string Name
            {
                get { return StyxWoW.Memory.ReadString(_namePtr, Encoding.UTF8); }
            }

            public string ItemStateName
            {
                get { return StyxWoW.Memory.ReadString(_itemStateNamePtr, Encoding.UTF8); }
            }

            public string ProcessName
            {
                get { return StyxWoW.Memory.ReadString(_processNamePtr, Encoding.UTF8); }
            }

            public string InternalName
            {
                get { return StyxWoW.Memory.ReadString(_internalNamePtr, Encoding.UTF8); }
            }

            public int Id;

            public IntPtr _namePtr;

            public IntPtr _itemStateNamePtr;

            public IntPtr _processNamePtr;

            public IntPtr _internalNamePtr;
        }

        private struct GameObjectDataSlotsEntry
        {
            public uint NamePtr;

            public uint Count;

            public IntPtr Types;

            private uint _unk;
        }
    }
}
