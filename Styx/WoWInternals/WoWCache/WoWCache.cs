using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common;
using Styx.Patchables;
using Styx.Helpers;

namespace Styx.WoWInternals.WoWCache
{
    public class WoWCache
    {
        public WoWCache()
        {
            IntPtr db = StyxWoW.Offsets.Statics.Base_DbCache;

            for (int i = 0; i < 13; i++)
            {
                IntPtr functionIndex;
                switch ((CacheDb)i)
                {
                    case CacheDb.Creature:
                        functionIndex = StyxWoW.Offsets.Functions.DbCreatureCache_GetInfoBlockById;
                        break;
                    case CacheDb.GameObject:
                        functionIndex = StyxWoW.Offsets.Functions.DbGameObjectCache_GetInfoBlockById;
                        db = StyxWoW.Offsets.Statics.Base_DbGameObjectCache;
                        break;
                    case CacheDb.Npc:
                        functionIndex = StyxWoW.Offsets.Functions.DbNpcCache_GetInfoBlockById;
                        break;
                    case CacheDb.Name:
                        functionIndex = StyxWoW.Offsets.Functions.DbNameCache_GetInfoBlockById;
                        break;
                    case CacheDb.Guild:
                        functionIndex = StyxWoW.Offsets.Functions.DbGuildCache_GetInfoBlockById;
                        break;
                    case CacheDb.Quest:
                        functionIndex = StyxWoW.Offsets.Functions.DbQuestCache_GetInfoBlockById;
                        db = StyxWoW.Offsets.Statics.QuestCache;
                        break;
                    case CacheDb.PageText:
                        functionIndex = StyxWoW.Offsets.Functions.DbPageTextCache_GetInfoBlockById;
                        break;
                    case CacheDb.PetName:
                        functionIndex = StyxWoW.Offsets.Functions.DbPetNameCache_GetInfoBlockById;
                        break;
                    case CacheDb.Petition:
                        functionIndex = StyxWoW.Offsets.Functions.DbPetitionCache_GetInfoBlockById;
                        break;
                    case CacheDb.ItemText:
                        functionIndex = StyxWoW.Offsets.Functions.DbItemTextCache_GetInfoBlockById;
                        break;
                    case CacheDb.WoW:
                        functionIndex = (IntPtr)0;
                        db = (IntPtr)0;
                        break;
                    case CacheDb.Item:
                        functionIndex = StyxWoW.Offsets.Functions.DBItemCache_GetInfoBlockByID;
                        db = StyxWoW.Offsets.Statics.DbItemSparseCache;
                        break;
                    case CacheDb.ItemClass:
                        functionIndex = StyxWoW.Offsets.Functions.DBItemCache_GetClassInfoBlockByID;
                        db = StyxWoW.Offsets.Statics.DbItemCache;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _cacheAddresses[(CacheDb)i] = new Cache(db, functionIndex);
                db += 0x110;
            }
        }

        public Cache this[CacheDb index]
        {
            get { return _cacheAddresses[index]; }
        }

        private static InfoBlock GetInfoBlockById(IntPtr function, IntPtr cache, uint index, ulong guid, int a3, int a4, int a5)
        {
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(8))
            {
                allocatedMemory.Write<ulong>(0, guid);
                try
                {
                    IntPtr ret = StyxWoW.Memory.CallInjected<IntPtr>(function, CallingConvention.ThisCall, (uint)(int)cache, index, allocatedMemory.Address, a3, a4, a5);
                    return ret != (IntPtr)0 ? new InfoBlock(ret, index) : null;
                }
                catch (Exception ex)
                {
                    Logging.WriteDiagnostic("Exception in GetInfoBlockByID:{0} - {1} - {2}", ex.Message, ex.StackTrace, ex.Source);
                }
            }

            return null;
        }

        private readonly Dictionary<CacheDb, Cache> _cacheAddresses = new Dictionary<CacheDb, Cache>();

        #region CreatureCacheTypeFlags enum

        [Flags]
        public enum CreatureCacheTypeFlags
        {
            Tameable = 0x1,
            GhostVisible = 0x2,
            HerbSkin = 0x100,
            MineSkin = 0x200,
            Salvageable = 0x8000,
            Exotic = 0x10000,
        }

        #endregion

        #region SocketColorFlags enum

        [Flags]
        public enum SocketColorFlags
        {
            None = 0x0,
            Meta = 0x1,
            Red = 0x2,
            Yellow = 0x4,
            Blue = 0x8,
        }

        #endregion

        #region Nested Type: QuestCacheEntry

        #region QuestFlags enum

        [Flags]
        public enum QuestFlags : uint
        {
            None = 0x0,
            StayAlive = 0x1,
            PartyQuest = 0x2,
            Unk4 = 0x4,
            Shareable = 0x8,
            Daily = 0x1000,
            FlagsPVP = 0x2000,
            Weekly = 0x8000,
            AutoComplete = 0x10000,
            IntermediateItems = 0x20000,
            AutoAccept = 0x80000,
            ShowPortrait = 0x8000000,
            All = 0xFFFFFFFF,
        }

        #endregion

        #region QuestInfoTagType enum

        public enum QuestInfoTagType : uint
        {
            None = 0,
            Group = 1,
            Life = 21,
            PvP = 41,
            Raid = 62,
            Dungeon = 81,
            WorldEvent = 82,
            Legendary = 83,
            Escort = 84,
            Heroic = 85,
            Raid10 = 88,
            Raid25 = 89,
        }

        #endregion

        #region Nested type: QuestCacheEntry

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct QuestCacheEntry
        {
            public uint Id;
            public uint Method;
            public uint Level;
            public uint RequiredLevel;
            public uint AreaIdOrSortId;
            public uint SortId;
            public uint QuestType;
            public uint SuggestedPlayers;
            public uint FriendlyFactionId;
            public uint FriendlyFactionAmount;
            public uint RewardMoney;
            public uint RewardMoneyInsteadOfXP;
            public uint RewardSpellId;
            public uint RewardSpellId2;
            public uint RewardHonor;
            public uint _unk0;
            public uint RewardHonorBonus;
            public uint StartingItemId;
            public uint Flags;
            public uint Flags2;
            public uint Unk;
            public float PointX;
            public float PointY;
            public uint PointOptional;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)] public byte[] Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)] public byte[] ObjectiveText;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)] public byte[] Description;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)] public byte[] EndText;
            public uint RewardTitleId;
            public uint RewardTalentPoints;
            public uint dword1BD4;
            public uint RewardSkillId;
            public uint RewardSkillPoints;
            public uint PortraitGiverId;
            public uint PortraitTurnInId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] public byte[] PortraitGiverText;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] PortraitGiverName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] public byte[] PortraitTurnInDescription;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] PortraitTurnInName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)] public byte[] CompletionText;
            public uint dword2DE8;
            public uint dword2DEC;
            public QuestObjectivesInfo Objectives;
            public uint dword2E00;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] RewardItemCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] Arr2E14;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] ObjectiveId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] ObjectiveRequiredCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public QuestRewardChoice[] QuestRewardChoices;
            public uint dword2E74;
            public uint dword2E78;
            public uint dword2E7C;
            public uint dword2E80;
            public uint dword2E84;
            public uint dword2E88;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)] public uint[] Arr2E8C;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)] public uint[] RewardReputationFaction;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)] public uint[] Arr2EB4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] RewardCurrencyCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)] public uint[] Arr2ED8;
            public IntPtr QuestRewardChoiceCollection;
            public uint dword2EEC;
            public uint dword2EF0;

            public QuestObjectiveInfo[] GetObjectives()
            {
                List<QuestObjectiveInfo> ret = new List<QuestObjectiveInfo>();
                ret.AddRange(GetObjectiveInfos());
                ret.AddRange(GetIntermediateItemObjectives());
                return ret.ToArray();
            }

            private QuestObjectiveInfo[] GetObjectiveInfos()
            {
                QuestObjectiveInfo[] ret = new QuestObjectiveInfo[Objectives.Count];
                for (int i = 0; i < ret.Length; i++)
                {
                    ret[i] = StyxWoW.Memory.Read<QuestObjectiveInfo>(Objectives.QuestObjectives + i * MarshalCache<QuestObjectiveInfo>.Size);
                }

                return ret;
            }

            private QuestObjectiveInfo[] GetIntermediateItemObjectives()
            {
                List<QuestObjectiveInfo> ret = new List<QuestObjectiveInfo>();
                for (int i = 0; i < 4; i++)
                {
                    uint intermediateItemId = ObjectiveId[i];
                    uint intermediateItemCount = ObjectiveRequiredCount[i];
                    if (intermediateItemId > 0U && (intermediateItemId != StartingItemId || intermediateItemCount > 1U))
                    {
                        ret.Add(new QuestObjectiveInfo
                        {
                            ObjectiveId = intermediateItemId,
                            RequiredCount = intermediateItemCount,
                            ObjectiveType = Quest.QuestObjectiveType.CollectIntermediateItem,
                            Text = "Collect Intermediate Item"
                        });
                    }
                }

                return ret.ToArray();
            }
        }

        private struct QuestRewardInfo
        {
            public uint dword0;

            public uint Count;

            public IntPtr Choices;
        }

        public struct QuestRewardChoice
        {
            public uint ItemId;

            public uint Count;

            private uint dword8;
        }

        #region Nested type: QuestObjectiveInfo

        public struct QuestObjectiveInfo
        {
            public uint dword0;

            public Quest.QuestObjectiveType ObjectiveType;

            public sbyte QuestLogObjectiveIndex;

            public ushort word6;

            public uint ObjectiveId;

            public uint RequiredCount;

            public uint dword10;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Text;

            public uint dword114;

            public uint dword118;

            public IntPtr dword11C;

            public uint dword120;

            public override string ToString()
            {
                return string.Format("Dword0: {0}, ObjectiveType: {1}, Byte5QuestLogObjectiveIndex: {2}, Word6: {3}, ObjectiveId: {4}, RequiredCount: {5}, Dword10: {6}, Text: {7}, Dword114: {8}, Dword118: {9}, Dword11C: {10}, Dword120: {11}", dword0, ObjectiveType, QuestLogObjectiveIndex, word6, ObjectiveId, RequiredCount, dword10, Text, dword114, dword118, dword11C, dword120);
            }
        }

        #endregion

        #region Nested type: QuestObjectivesInfo

        public struct QuestObjectivesInfo
        {
            private readonly uint _dword0;

            public uint Count;

            public IntPtr QuestObjectives;

            private readonly uint _dwordC;
        }

        #endregion

        #endregion

        #endregion

        #region Nested type: Cache

        public class Cache
        {
            internal Cache(IntPtr address, IntPtr infoBlockAddr)
            {
                Address = address;
                InfoBlockByIdAddress = infoBlockAddr;
            }

            public InfoBlock GetInfoBlockById(uint id)
            {
                return WoWCache.GetInfoBlockById(InfoBlockByIdAddress, Address, id, 0UL, 0, 0, 0);
            }

            public readonly IntPtr Address;

            internal readonly IntPtr InfoBlockByIdAddress;

            #region Nested type: NativeCache

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct NativeCache
            {
                private readonly uint _vtable;

                private readonly uint _unk;

                public readonly NativeWowTable Base1;
            }

            #endregion
        }

        #endregion

        #region Nested type: CreatureCacheEntry

        public struct JamDynamicString
        {
            public IntPtr PtrStr;

            public int Length;
        }

        public struct CreatureCacheEntry
        {
            public uint[] QuestItems
            {
                get
                {
                    uint num = _questItems[4];
                    if (num == 4294967295U)
                    {
                        num = _questItems[0];
                        return StyxWoW.Memory.ReadArray<uint>((IntPtr)(long)(ulong)_questItems[1], (int)num);
                    }

                    return _questItems;
                }
            }

            public JamDynamicString SubName;

            public JamDynamicString IconName;

            public JamDynamicString SomeOtherName;

            public uint TypeID;

            public uint FamilyID;

            public uint Rank;

            public float HealthModifier;

            public float PowerModifier;

            public uint RacialLeader;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private readonly uint[] _questItems;

            public uint Dword50;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public CreatureCacheTypeFlags[] TypeFlags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] GroupID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] ModelID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public JamDynamicString[] Name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public JamDynamicString[] Name2;

            public uint Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] Dword9C;
        }

        #endregion

        #region Nested type: GameObjectCacheEntry

        public struct GameObjectCacheEntry
        {
            public uint[] QuestItems
            {
                get
                {
                    uint num = _QuestItems[4];
                    if (num == 4294967295U)
                    {
                        num = _QuestItems[0];
                        return StyxWoW.Memory.ReadArray<uint>((IntPtr)(long)(ulong)_QuestItems[1], (int)num);
                    }

                    return _QuestItems;
                }
            }

            internal const int NumProperties = 32;

            public int GameObjectType;

            public int DisplayId;

            public IntPtr CastBarCaptionPtr;

            public IntPtr UnkString0Ptr;

            public IntPtr UnkString1Ptr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NumProperties)]
            public int[] Properties;

            public float Scale;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private uint[] _QuestItems;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public IntPtr[] NamePtrs;
        }

        #endregion

        #region Nested type: InfoBlock

        public class InfoBlock
        {
            internal InfoBlock(IntPtr address, uint id)
            {
                Address = address;
                Id = id;
            }

            public ItemSparseEntry ItemSparse
            {
                get
                {
                    if (Address != (IntPtr)0)
                    {
                        return StyxWoW.Memory.Read<ItemSparseEntry>(Address);
                    }

                    return default(ItemSparseEntry);
                }
            }

            public ItemEntry Item
            {
                get
                {
                    if (Address != (IntPtr)0)
                    {
                        return StyxWoW.Memory.Read<ItemEntry>(Address);
                    }

                    return default(ItemEntry);
                }
            }

            public QuestCacheEntry Quest
            {
                get
                {
                    if (Address != (IntPtr)0)
                    {
                        return StyxWoW.Memory.Read<QuestCacheEntry>(Address);
                    }

                    return default(QuestCacheEntry);
                }
            }

            public GameObjectCacheEntry GameObject
            {
                get
                {
                    if (Address != (IntPtr)0)
                    {
                        return StyxWoW.Memory.Read<GameObjectCacheEntry>(Address);
                    }

                    return default(GameObjectCacheEntry);
                }
            }

            public CreatureCacheEntry Creature
            {
                get
                {
                    if (Address != (IntPtr)0)
                    {
                        return StyxWoW.Memory.Read<CreatureCacheEntry>(Address);
                    }

                    return default(CreatureCacheEntry);
                }
            }

            public uint Id { get; private set; }

            public IntPtr Address { get; private set; }

            public T GetField<T>(uint index) where T : struct
            {
                T t;
                try
                {
                    t = StyxWoW.Memory.Read<T>(Address + (int)(index * 4U));
                }
                catch
                {
                    t = default(T);
                }

                return t;
            }

            public string GetStringField(uint index)
            {
                string text;
                try
                {
                    int offset = StyxWoW.Memory.Read<int>(Address + (int)(index * 4U));
                    text = StyxWoW.Memory.ReadString(Address + offset + (int)(index * 4U), Encoding.UTF8);
                }
                catch
                {
                    text = null;
                }

                return text;
            }

            public string GetStringField<T>(string fieldName)
            {
                string text;
                try
                {
                    uint offset = (uint)(int)Marshal.OffsetOf(typeof(T), fieldName);
                    text = GetStringField(offset / 4U);
                }
                catch
                {
                    text = null;
                }

                return text;
            }

            public T GetStruct<T>() where T : struct
            {
                T t;
                try
                {
                    t = StyxWoW.Memory.Read<T>(Address);
                }
                catch
                {
                    t = default(T);
                }

                return t;
            }
        }

        #endregion

        #region Nested type: ItemEntry

        public struct ItemEntry
        {
            public int Id;

            public uint ClassId;

            public uint SubClassId;

            public int Unk;

            public int MaterialId;

            public uint DisplayInfoId;

            public uint InventoryType;

            public uint SheathType;
        }

        #endregion

        #region Nested type: ItemSparseEntry

        public struct ItemSparseEntry
        {
            public int Id;

            public int Rarity;

            public int Flags;

            public int Flags2;

            public int dword10;

            public int dword14;

            public int ItemDisplayInfoId;

            public int TypeFlags2;

            public int BuyPrice;

            public int SellPrice;

            public int EquipSlot;

            public int AllowedClasses;

            public int AllowedRaces;

            public int ItemLevel;

            public int RequiredLevel;

            public int RequiredSkill;

            public int RequiredSkillLevel;

            public int RequiredSpell;

            public int RequiredHonorRank;

            public int RequiredCityRank;

            public int RequiredReputationFaction;

            public int RequiredReputationRank;

            public int UniqueCount;

            public int MaxStackSize;

            public int BagSlots;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.I4)]
            public int[] StatType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.I4)]
            public int[] StatValue;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.I4)]
            public int[] StatUnk;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.I4)]
            public int[] Resistances;

            public int ScalingStatDistribution;

            public int DamageType;

            public int Delay;

            public float RangedModDamage;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellTrigger;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellCharges;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellCooldown;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellCategory;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.I4)]
            public int[] SpellCategoryCooldown;

            public int BondingId;

            public IntPtr Name;

            public IntPtr Name2;

            public IntPtr Name3;

            public IntPtr Name4;

            public IntPtr Description;

            public int PageTextId;

            public int unk;

            public int PageMaterialId;

            public int QuestId;

            public int LockID;

            public int SheathId;

            public int RandomPropertiesId;

            public int RandomSuffixId;

            public int ItemSetId;

            public int MaxDurabilityId;

            public int AreaId;

            public int MapId;

            public int BagFamilyId;

            public int TotemCategoryId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
            public int[] SocketColor;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
            public int[] SocketContent;

            public int SocketBonus;

            public int GemProperties;

            public float ArmorDamageModifier;

            public int Duration;

            public int ItemLimitCategory;

            public int HolidayId;

            public float StatsScalingFactor;

            public int dword204;

            public int dword208;

            public override string ToString()
            {
                return string.Format("Id: {0}, Rarity: {1}, Flags: {2}, Flags2: {3}, Dword10: {4}, Dword14: {5}, ItemDisplayInfoId: {6}, TypeFlags2: {7}, BuyPrice: {8}, SellPrice: {9}, EquipSlot: {10}, AllowedClasses: {11}, AllowedRaces: {12}, ItemLevel: {13}, RequiredLevel: {14}, RequiredSkill: {15}, RequiredSkillLevel: {16}, RequiredSpell: {17}, RequiredHonorRank: {18}, RequiredCityRank: {19}, RequiredReputationFaction: {20}, RequiredReputationRank: {21}, UniqueCount: {22}, MaxStackSize: {23}, BagSlots: {24}, StatType: {25}, StatValue: {26}, StatUnk: {27}, Resistances: {28}, ScalingStatDistribution: {29}, DamageType: {30}, Delay: {31}, RangedModDamage: {32}, SpellId: {33}, SpellTrigger: {34}, SpellCharges: {35}, SpellCooldown: {36}, SpellCategory: {37}, SpellCategoryCooldown: {38}, BondingId: {39}, Name: {40}, Name2: {41}, Name3: {42}, Name4: {43}, Description: {44}, PageTextId: {45}, Unk: {46}, PageMaterialId: {47}, QuestId: {48}, Unk2: {49}, SheathId: {50}, RandomPropertiesId: {51}, RandomSuffixId: {52}, ItemSetId: {53}, MaxDurabilityId: {54}, AreaId: {55}, MapId: {56}, BagFamilyId: {57}, TotemCategoryId: {58}, SocketColor: {59}, SocketContent: {60}, SocketBonus: {61}, GemProperties: {62}, ArmorDamageModifier: {63}, Duration: {64}, ItemLimitCategory: {65}, HolidayId: {66}, StatsScalingFactor: {67}, Dword204: {68}, Dword208: {69}",
                    Id,
                    Rarity,
                    Flags,
                    Flags2,
                    dword10,
                    dword14,
                    ItemDisplayInfoId,
                    TypeFlags2,
                    BuyPrice,
                    SellPrice,
                    EquipSlot,
                    AllowedClasses,
                    AllowedRaces,
                    ItemLevel,
                    RequiredLevel,
                    RequiredSkill,
                    RequiredSkillLevel,
                    RequiredSpell,
                    RequiredHonorRank,
                    RequiredCityRank,
                    RequiredReputationFaction,
                    RequiredReputationRank,
                    UniqueCount,
                    MaxStackSize,
                    BagSlots,
                    StatType.ToRealString<int>(),
                    StatValue.ToRealString<int>(),
                    StatUnk.ToRealString<int>(),
                    Resistances.ToRealString<int>(),
                    ScalingStatDistribution,
                    DamageType,
                    Delay,
                    RangedModDamage,
                    SpellId.ToRealString<int>(),
                    SpellTrigger.ToRealString<int>(),
                    SpellCharges.ToRealString<int>(),
                    SpellCooldown.ToRealString<int>(),
                    SpellCategory.ToRealString<int>(),
                    SpellCategoryCooldown.ToRealString<int>(),
                    BondingId,
                    Name,
                    Name2,
                    Name3,
                    Name4,
                    Description,
                    PageTextId,
                    unk,
                    PageMaterialId,
                    QuestId,
                    LockID,
                    SheathId,
                    RandomPropertiesId,
                    RandomSuffixId,
                    ItemSetId,
                    MaxDurabilityId,
                    AreaId,
                    MapId,
                    BagFamilyId,
                    TotemCategoryId,
                    SocketColor.ToRealString<int>(),
                    SocketContent.ToRealString<int>(),
                    SocketBonus,
                    GemProperties,
                    ArmorDamageModifier,
                    Duration,
                    ItemLimitCategory,
                    HolidayId,
                    StatsScalingFactor,
                    dword204,
                    dword208);
            }
        }

        #endregion
    }
}
