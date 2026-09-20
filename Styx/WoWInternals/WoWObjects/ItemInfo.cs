using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GreenMagic;
using Styx.Patchables;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWCache;

namespace Styx.WoWInternals.WoWObjects
{
    public class ItemInfo
    {
        private ItemInfo(uint id, Styx.WoWInternals.WoWCache.WoWCache.InfoBlock sparseInfoBlock, Styx.WoWInternals.WoWCache.WoWCache.InfoBlock classInfoBlock)
        {
            Id = id;
            _sparseInfo = sparseInfoBlock.ItemSparse;
            _classInfo = classInfoBlock.Item;
            _sellPrice = StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.GetItemSellPrice, CallingConvention.ThisCall, new object[] { sparseInfoBlock.Address });
            Name = sparseInfoBlock.GetStringField<Styx.WoWInternals.WoWCache.WoWCache.ItemSparseEntry>("Name");
            Description = sparseInfoBlock.GetStringField<Styx.WoWInternals.WoWCache.WoWCache.ItemSparseEntry>("Description");
        }

        public Styx.WoWInternals.WoWCache.WoWCache.ItemSparseEntry InternalInfo
        {
            get { return _sparseInfo; }
        }

        public uint Id { get; private set; }

        public int TypeFlags
        {
            get { return _sparseInfo.Flags; }
        }

        public int SubClassId
        {
            get { return (int)_classInfo.SubClassId; }
        }

        public float RangeModifier
        {
            get { return _sparseInfo.RangedModDamage; }
        }

        public int AllowedClasses
        {
            get { return _sparseInfo.AllowedClasses; }
        }

        public int AllowedRaces
        {
            get { return _sparseInfo.AllowedRaces; }
        }

        public int BookStationaryId
        {
            get { return _sparseInfo.PageMaterialId; }
        }

        public int SheathId
        {
            get { return _sparseInfo.SheathId; }
        }

        public int RandomPropertiesId
        {
            get { return _sparseInfo.RandomPropertiesId; }
        }

        public int RandomSuffixId
        {
            get { return _sparseInfo.RandomSuffixId; }
        }

        public int ItemSetId
        {
            get { return _sparseInfo.ItemSetId; }
        }

        public int ItemAreaId
        {
            get { return _sparseInfo.AreaId; }
        }

        public int ItemMapId
        {
            get { return _sparseInfo.MapId; }
        }

        public int BagFamily
        {
            get { return _sparseInfo.BagFamilyId; }
        }

        public int TotemCategory
        {
            get { return _sparseInfo.TotemCategoryId; }
        }

        public float ArmorDamageModifier
        {
            get { return _sparseInfo.ArmorDamageModifier; }
        }

        public int[] StatId
        {
            get { return _sparseInfo.StatType; }
        }

        public int[] StatValue
        {
            get { return _sparseInfo.StatValue; }
        }

        public bool IsAccountBound
        {
            get { return (_sparseInfo.Flags & 134217728) != 0; }
        }

        public bool IsBattleNetAccountBound
        {
            get { return (_sparseInfo.Flags & 134217728) != 0 && (_sparseInfo.Flags2 & 131072) != 0; }
        }

        public bool IsGarroshHeirloomWeapon
        {
            get { return (_sparseInfo.Flags2 & 512) != 0; }
        }

        public bool IsOpenable
        {
            get { return (_sparseInfo.Flags & 4) != 0; }
        }

        public int LockID
        {
            get { return _sparseInfo.LockID; }
        }

        public int ScalingStatDistributionID
        {
            get { return _sparseInfo.ScalingStatDistribution; }
        }

        public Styx.WoWInternals.DBC.ScalingStatDistribution ScalingStatDistribution
        {
            get { return Styx.WoWInternals.DBC.ScalingStatDistribution.FromId((uint)ScalingStatDistributionID); }
        }

        public bool IsUniqueEquipped
        {
            get { return (_sparseInfo.Flags & 524288) != 0; }
        }

        public bool IsGiftWrapable
        {
            get { return (_sparseInfo.Flags & 512) != 0; }
        }

        public float[] DamageMin
        {
            get { return new float[3]; }
        }

        public float[] DamageMax
        {
            get { return new float[3]; }
        }

        public int DamageType
        {
            get { return _sparseInfo.DamageType; }
        }

        public int[] SpellId
        {
            get { return _sparseInfo.SpellId; }
        }

        public int[] SpellTriggerId
        {
            get { return _sparseInfo.SpellTrigger; }
        }

        public int[] SpellCharges
        {
            get { return _sparseInfo.SpellCharges; }
        }

        public int[] SpellCooldown
        {
            get { return _sparseInfo.SpellCooldown; }
        }

        public int[] SpellCategory
        {
            get { return _sparseInfo.SpellCategory; }
        }

        public int[] SpellCategoryCooldown
        {
            get { return _sparseInfo.SpellCategoryCooldown; }
        }

        public Styx.WoWInternals.WoWCache.WoWCache.SocketColorFlags[] SocketColor
        {
            get { return _sparseInfo.SocketColor.Cast<Styx.WoWInternals.WoWCache.WoWCache.SocketColorFlags>().ToArray<Styx.WoWInternals.WoWCache.WoWCache.SocketColorFlags>(); }
        }

        public string Name { get; private set; }

        public WoWItemClass ItemClass
        {
            get { return (WoWItemClass)_classInfo.ClassId; }
        }

        public WoWItemConsumableClass ConsumableClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Consumable)
                {
                    return WoWItemConsumableClass.None;
                }
                return (WoWItemConsumableClass)_classInfo.SubClassId;
            }
        }

        public WoWItemWeaponClass WeaponClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Weapon)
                {
                    return WoWItemWeaponClass.None;
                }
                return (WoWItemWeaponClass)_classInfo.SubClassId;
            }
        }

        public WoWItemGlyphClass GlyphClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Glyph)
                {
                    return WoWItemGlyphClass.None;
                }
                return (WoWItemGlyphClass)_classInfo.SubClassId;
            }
        }

        public WoWItemKeyClass KeyClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Key)
                {
                    return WoWItemKeyClass.None;
                }
                return (WoWItemKeyClass)_classInfo.SubClassId;
            }
        }

        public WoWItemMiscClass MiscClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Miscellaneous)
                {
                    return WoWItemMiscClass.None;
                }
                return (WoWItemMiscClass)_classInfo.SubClassId;
            }
        }

        public WoWItemQuiverClass QuiverClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Quiver)
                {
                    return WoWItemQuiverClass.None;
                }
                return (WoWItemQuiverClass)_classInfo.SubClassId;
            }
        }

        public WoWItemRecipeClass RecipeClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Recipe)
                {
                    return WoWItemRecipeClass.None;
                }
                return (WoWItemRecipeClass)_classInfo.SubClassId;
            }
        }

        public WoWItemTradeGoodsClass TradeGoodsClass
        {
            get
            {
                if (ItemClass != WoWItemClass.TradeGoods)
                {
                    return WoWItemTradeGoodsClass.None;
                }
                return (WoWItemTradeGoodsClass)_classInfo.SubClassId;
            }
        }

        public WoWItemProjectileClass ProjectileClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Projectile)
                {
                    return WoWItemProjectileClass.None;
                }
                return (WoWItemProjectileClass)_classInfo.SubClassId;
            }
        }

        public WoWItemArmorClass ArmorClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Armor)
                {
                    return WoWItemArmorClass.None;
                }
                return (WoWItemArmorClass)_classInfo.SubClassId;
            }
        }

        public WoWItemGemClass GemClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Gem)
                {
                    return WoWItemGemClass.None;
                }
                return (WoWItemGemClass)_classInfo.SubClassId;
            }
        }

        public WoWItemContainerClass ContainerClass
        {
            get
            {
                if (ItemClass != WoWItemClass.Container)
                {
                    return WoWItemContainerClass.None;
                }
                return (WoWItemContainerClass)_classInfo.SubClassId;
            }
        }

        public float WeaponDelay
        {
            get { return (float)_sparseInfo.Delay / 1000f; }
        }

        public float MinDamage
        {
            get { return 0f; }
        }

        public float MaxDamage
        {
            get { return 0f; }
        }

        public float DPS
        {
            get { return GetDps(); }
        }

        private float GetDamageValue(WoWDb.DbTable table, int itemLevel, int rarity)
        {
            return table.GetRow((uint)itemLevel).GetStruct<ItemDamageEntry>().Value[rarity];
        }

        private float GetDpsFromDbc()
        {
            int flags = InternalInfo.Flags2;
            int quality = (int)Quality;
            int level = Level;
            float num = 0f;
            Styx.WoWInternals.WoWCache.WoWCache.ItemEntry item = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.Item].GetInfoBlockById((uint)InternalInfo.Id).Item;
            switch (EquipSlot)
            {
            case InventoryType.Weapon:
            case InventoryType.WeaponMainHand:
            case InventoryType.WeaponOffHand:
                num = GetDamageValue(((flags & 512) != 0) ? StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageOneHandCaster] : StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageOneHand], level, quality);
                break;
            case InventoryType.Ranged:
            case InventoryType.Thrown:
            case InventoryType.RangedRight:
            {
                uint subClassId = item.SubClassId;
                switch (subClassId)
                {
                case 2U:
                case 3U:
                    break;
                default:
                    switch (subClassId)
                    {
                    case 18U:
                        break;
                    case 19U:
                        return GetDamageValue(StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageOneHandCaster], level, quality);
                    default:
                        return num;
                    }
                    break;
                }
                num = GetDamageValue(((flags & 512) != 0) ? StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageTwoHandCaster] : StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageTwoHand], level, quality);
                break;
            }
            case InventoryType.TwoHandWeapon:
                num = GetDamageValue(((flags & 512) != 0) ? StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageTwoHandCaster] : StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageTwoHand], level, quality);
                break;
            case InventoryType.Ammo:
                num = GetDamageValue(StyxWoW.Db[Styx.Patchables.ClientDb.ItemDamageAmmo], level, quality);
                break;
            }
            return num;
        }

        private float GetDps()
        {
            if (ItemClass != WoWItemClass.Weapon)
            {
                return 0f;
            }
            if (Quality == WoWItemQuality.Heirloom)
            {
                return 0f;
            }
            float num2;
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(4096))
            {
                Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlockById = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.Item].GetInfoBlockById(Id);
                Executor executor = StyxWoW.Memory.Executor;
                lock (executor.AssemblyLock)
                {
                    executor.Clear();
                    foreach (string text in StyxWoW.Memory.GenerateAsm(StyxWoW.Offsets.Functions.GetItemDps, CallingConvention.ThisCall, new object[]
                    {
                        (uint)(int)allocatedMemory.Address,
                        (int)infoBlockById.Address,
                        0
                    }))
                    {
                        executor.AddLine(text, new object[0]);
                    }
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        float num = allocatedMemory.Read<float>(0);
                        if (num <= 0f)
                        {
                            num = GetDpsFromDbc();
                        }
                        num2 = num;
                    }
                }
            }
            return num2;
        }

        public WoWItemArmorClass GetShownArmorClass()
        {
            if (ItemClass != WoWItemClass.Armor)
            {
                return WoWItemArmorClass.None;
            }
            InventoryType equipSlot = EquipSlot;
            if (equipSlot <= InventoryType.Trinket)
            {
                switch (equipSlot)
                {
                case InventoryType.Neck:
                case InventoryType.Body:
                    return WoWItemArmorClass.None;
                case InventoryType.Shoulder:
                    break;
                default:
                    switch (equipSlot)
                    {
                    case InventoryType.Finger:
                    case InventoryType.Trinket:
                        return WoWItemArmorClass.None;
                    }
                    break;
                }
            }
            else
            {
                if (equipSlot == InventoryType.Cloak)
                {
                    return WoWItemArmorClass.None;
                }
                if (equipSlot == InventoryType.Tabard)
                {
                    return WoWItemArmorClass.None;
                }
            }
            WoWItemArmorClass armorClass = ArmorClass;
            if (Quality != WoWItemQuality.Heirloom || ScalingStatDistribution == null)
            {
                return armorClass;
            }
            WoWClass[] array;
            switch (armorClass)
            {
            case WoWItemArmorClass.Mail:
                array = new WoWClass[]
                {
                    WoWClass.Hunter,
                    WoWClass.Shaman
                };
                break;
            case WoWItemArmorClass.Plate:
                array = new WoWClass[]
                {
                    WoWClass.Warrior,
                    WoWClass.Paladin
                };
                break;
            default:
                return armorClass;
            }
            if (array.Contains(StyxWoW.Me.Class) && StyxWoW.Me.Level < 40)
            {
                return armorClass - 1;
            }
            return armorClass;
        }

        public int DisplayInfoId
        {
            get { return (int)_classInfo.DisplayInfoId; }
        }

        public WoWItemQuality Quality
        {
            get { return (WoWItemQuality)_sparseInfo.Rarity; }
        }

        public int SellPrice
        {
            get { return _sellPrice; }
        }

        public int BuyPrice
        {
            get { return _sparseInfo.BuyPrice; }
        }

        public int Level
        {
            get { return _sparseInfo.ItemLevel; }
        }

        public int RequiredLevel
        {
            get { return _sparseInfo.RequiredLevel; }
        }

        public int RequiredSkillId
        {
            get { return _sparseInfo.RequiredSkill; }
        }

        public int RequiredSkillLevel
        {
            get { return _sparseInfo.RequiredSkillLevel; }
        }

        public int RequiredSpellId
        {
            get { return _sparseInfo.RequiredSpell; }
        }

        public int RequiredHonorRank
        {
            get { return _sparseInfo.RequiredHonorRank; }
        }

        public int RequiredReputationFactionId
        {
            get { return _sparseInfo.RequiredReputationFaction; }
        }

        public int RequiredReputationRank
        {
            get { return _sparseInfo.RequiredReputationRank; }
        }

        public int UniqueCount
        {
            get { return _sparseInfo.UniqueCount; }
        }

        public int MaxStackSize
        {
            get { return _sparseInfo.MaxStackSize; }
        }

        public int BagSlots
        {
            get { return _sparseInfo.BagSlots; }
        }

        public int StatsCount
        {
            get { return _sparseInfo.StatType.Count(i => i != 0); }
        }

        public int Armor
        {
            get { return 0; }
        }

        public int HolyResistance
        {
            get { return 0; }
        }

        public int FireResistance
        {
            get { return 0; }
        }

        public int NatureResistance
        {
            get { return 0; }
        }

        public int FrostResistance
        {
            get { return 0; }
        }

        public int ShadowResistance
        {
            get { return 0; }
        }

        public int ArcaneResistance
        {
            get { return 0; }
        }

        public WoWItemBondType Bond
        {
            get { return (WoWItemBondType)_sparseInfo.BondingId; }
        }

        public InventoryType InventoryType
        {
            get { return (InventoryType)_sparseInfo.EquipSlot; }
        }

        public InventoryType EquipSlot
        {
            get { return InventoryType; }
        }

        public string Description { get; private set; }

        public int BookTextId
        {
            get { return _sparseInfo.PageTextId; }
        }

        public int BeginQuestId
        {
            get { return _sparseInfo.QuestId; }
        }

        public int WeaponSpeed
        {
            get { return _sparseInfo.Delay; }
        }

        public bool IsWeapon
        {
            get { return ItemClass == WoWItemClass.Weapon; }
        }

        public Dictionary<WoWItemStatType, int> GetItemStats()
        {
            Dictionary<WoWItemStatType, int> dictionary = new Dictionary<WoWItemStatType, int>();
            for (int i = 0; i < StatsCount; i++)
            {
                dictionary[(WoWItemStatType)_sparseInfo.StatType[i]] = _sparseInfo.StatValue[i];
            }
            return dictionary;
        }

        private int GetPhysicalDamageIndex()
        {
            return 0;
        }

        public static ItemInfo FromId(uint itemId)
        {
            ItemInfo itemInfo;
            if (!ItemInfo._itemInfoCache.TryGetValue(itemId, out itemInfo))
            {
                Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlockById = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.Item].GetInfoBlockById(itemId);
                if (infoBlockById != null)
                {
                    Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlockById2 = StyxWoW.Cache[Styx.WoWInternals.WoWCache.CacheDb.ItemClass].GetInfoBlockById(itemId);
                    if (infoBlockById2 != null)
                    {
                        itemInfo = new ItemInfo(itemId, infoBlockById, infoBlockById2);
                        ItemInfo._itemInfoCache[itemId] = itemInfo;
                    }
                }
            }
            return itemInfo;
        }

        public static ItemInfo FromLink(string link)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            int num = link.IndexOf("Hitem:", StringComparison.Ordinal);
            if (num == -1)
            {
                throw new ArgumentException("Malformed item link", "link");
            }
            int num2 = num + "Hitem:".Length;
            int num3 = link.IndexOf(":", num2, StringComparison.Ordinal);
            if (num3 == -1)
            {
                throw new ArgumentException("Malformed item link", "link");
            }
            uint num4;
            if (!uint.TryParse(link.Substring(num2, num3 - num2), out num4))
            {
                throw new ArgumentException("Malformed item link", "link");
            }
            return ItemInfo.FromId(num4);
        }

        private readonly Styx.WoWInternals.WoWCache.WoWCache.ItemSparseEntry _sparseInfo;

        private readonly Styx.WoWInternals.WoWCache.WoWCache.ItemEntry _classInfo;

        private readonly int _sellPrice;

        private static readonly Dictionary<uint, ItemInfo> _itemInfoCache = new Dictionary<uint, ItemInfo>();
    }
}
