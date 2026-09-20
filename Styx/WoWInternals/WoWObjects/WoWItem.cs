using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Helpers;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWItem : WoWObject
    {
        public WoWItem(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public override string Name
        {
            get { return GetItemName(); }
        }

        public override WoWPoint Location
        {
            get { return WoWPoint.Zero; }
        }

        public override double Distance
        {
            get { return 0.0; }
        }

        public override double DistanceSqr
        {
            get { return 0.0; }
        }

        public override double Distance2D
        {
            get { return 0.0; }
        }

        public override double Distance2DSqr
        {
            get { return 0.0; }
        }

        public override void Interact()
        {
            Use();
        }

        public string GetItemName()
        {
            if (string.IsNullOrEmpty(_cachedName))
            {
                int randomPropertiesId = RandomPropertiesId;
                _cachedName = randomPropertiesId != 0 ? BuildItemName(Entry, randomPropertiesId) : base.Name;
            }

            return _cachedName;
        }

        private static string BuildItemName(uint itemId, int suffixId)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            string text;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(1024))
                {
                    executor.Clear();
                    executor.AddRandomLine("push {0}", suffixId);
                    executor.AddRandomLine("push {0}", itemId);
                    executor.AddRandomLine("push 1024");
                    executor.AddRandomLine("push {0}", allocatedMemory.Address);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGItem_C__BuildItemName);
                    executor.AddRandomLine("add esp, 0x10");
                    executor.AddRandomLine("retn");
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        text = executor.Memory.ReadString(allocatedMemory.Address, Encoding.UTF8);
                    }
                }
            }

            return text;
        }

        public string ItemLink
        {
            get
            {
                if (_itemLinkCache == null)
                {
                    _itemLinkCache = new PerFrameCachedValue<string>(() =>
                    {
                        EnsureValid("ItemLink");
                        IntPtr itemLink = StyxWoW.Memory.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.CGItem_C__CreateItemLink2, CallingConvention.Cdecl, BaseAddress);
                        return StyxWoW.Memory.ReadStringUTF8(itemLink);
                    });
                }

                return _itemLinkCache;
            }
        }

        public ItemStats ItemStats
        {
            get
            {
                if (_itemStatsCache == null)
                {
                    _itemStatsCache = new PerFrameCachedValue<ItemStats>(() =>
                    {
                        EnsureValid("ItemStats");
                        ItemStats itemStats;
                        using (StyxWoW.Memory.AcquireFrame(true))
                        {
                            itemStats = ItemStats.FromLink(ItemLink);
                        }

                        return itemStats;
                    });
                }

                return _itemStatsCache;
            }
        }

        public int BagIndex
        {
            get
            {
                ulong containerGuid = ContainerGuid;
                for (uint i = 0U; i <= 10U; i += 1U)
                {
                    if (StyxWoW.Me.GetBagGuidAtIndex(i) == containerGuid)
                    {
                        return (int)i;
                    }
                }

                return -1;
            }
        }

        public int BagSlot
        {
            get
            {
                ulong[] array = BagIndex == -1 ? StyxWoW.Me.Inventory.Backpack.ItemGuids : StyxWoW.Me.GetBagAtIndex((uint)BagIndex).ItemGuids;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == Guid)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public ulong OwnerGuid
        {
            get { return GetStorageField<ulong>((uint)WoWItemFields.Owner); }
        }

        public ulong ContainerGuid
        {
            get { return GetStorageField<ulong>((uint)WoWItemFields.ContainedIn); }
        }

        public ulong CreatorGuid
        {
            get { return GetStorageField<ulong>((uint)WoWItemFields.Creator); }
        }

        public ulong GiftCreatorGuid
        {
            get { return GetStorageField<ulong>((uint)WoWItemFields.GiftCreator); }
        }

        public uint StackCount
        {
            get { return GetStorageField<uint>((uint)WoWItemFields.StackCount); }
        }

        public uint SpellCharges
        {
            get { return GetStorageField<uint>((uint)WoWItemFields.SpellCharges); }
        }

        public uint Flags
        {
            get { return GetStorageField<uint>((uint)WoWItemFields.DynamicFlags); }
        }

        public int PropertySeed
        {
            get { return GetStorageField<int>((uint)WoWItemFields.PropertySeed); }
        }

        public int RandomPropertiesId
        {
            get { return GetStorageField<int>((uint)WoWItemFields.RandomPropertiesID); }
        }

        public Styx.WoWInternals.DBC.ItemRandomSuffix RandomSuffix
        {
            get { return Styx.WoWInternals.DBC.ItemRandomSuffix.FromId((uint)Math.Abs(RandomPropertiesId)); }
        }

        public uint Durability
        {
            get { return GetStorageField<uint>((uint)WoWItemFields.Durability); }
        }

        public uint MaxDurability
        {
            get { return GetStorageField<uint>((uint)WoWItemFields.MaxDurability); }
        }

        public double DurabilityPercent
        {
            get
            {
                uint maxDurability = MaxDurability;
                if (maxDurability <= 0U)
                {
                    return 100.0;
                }

                return Math.Round((double)(Durability * 100U / maxDurability));
            }
        }

        public WoWItemEnchantment TemporaryEnchantment
        {
            get { return GetEnchantment(1U); }
        }

        public WoWItemEnchantment GetEnchantment(string name)
        {
            for (uint i = 0U; i < 12U; i += 1U)
            {
                WoWItemEnchantment enchantment = GetEnchantment(i);
                if (enchantment != null && enchantment.Name.Contains(name))
                {
                    return enchantment;
                }
            }

            return null;
        }

        public WoWItemEnchantment GetEnchantmentById(uint id)
        {
            for (uint i = 0U; i < 12U; i += 1U)
            {
                WoWItemEnchantment enchantment = GetEnchantment(i);
                if (enchantment != null && enchantment.Id == id)
                {
                    return enchantment;
                }
            }

            return null;
        }

        public WoWItemEnchantment GetEnchantment(uint index)
        {
            uint ench0 = GetStorageField<uint>((uint)WoWItemFields.Enchantment + index * 3U * 4U);
            int ench1 = GetStorageField<int>((uint)WoWItemFields.Enchantment + (index * 3U + 1U) * 4U);
            int ench2 = GetStorageField<int>((uint)WoWItemFields.Enchantment + (index * 3U + 2U) * 4U);
            return new WoWItemEnchantment(Styx.WoWInternals.DBC.SpellItemEnchantment.FromId(ench0), ench1, ench2);
        }

        public WoWItemStat GetStat(int index)
        {
            return new WoWItemStat(GetStatType(index), GetStatValue(index));
        }

        public WoWItemStatType GetStatType(int index)
        {
            return (WoWItemStatType)ItemInfo.StatId[index];
        }

        public int GetStatValue(int index)
        {
            return ItemInfo.StatValue[index];
        }

        public List<WoWItemSpell> ItemSpells
        {
            get
            {
                List<WoWItemSpell> list = new List<WoWItemSpell>();
                for (int i = 0; i < 5; i++)
                {
                    WoWItemSpell spell = GetSpell(i);
                    if (spell != null && spell.IsValid)
                    {
                        list.Add(spell);
                    }
                }

                return list;
            }
        }

        public WoWItemSpell GetSpell(int index)
        {
            try
            {
                int id = ItemInfo.SpellId[index];
                if (id == 0)
                {
                    return null;
                }

                int triggerId = ItemInfo.SpellTriggerId[index];
                int charges = ItemInfo.SpellCharges[index];
                int cooldown = ItemInfo.SpellCooldown[index];
                int category = ItemInfo.SpellCategory[index];
                int categoryCooldown = ItemInfo.SpellCategoryCooldown[index];
                WoWItemSpell spell = new WoWItemSpell(id, triggerId, charges, cooldown, category, categoryCooldown);
                if (spell.IsValid)
                {
                    return spell;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }

            return null;
        }

        public WoWSocketColor GetSocketColor(int index)
        {
            return (WoWSocketColor)ItemInfo.SocketColor[index];
        }

        public ItemInfo ItemInfo
        {
            get
            {
                ItemInfo itemInfo;
                if ((itemInfo = _cachedItemInfo) == null)
                {
                    itemInfo = (_cachedItemInfo = ItemInfo.FromId(Entry));
                }

                return itemInfo;
            }
        }

        public WoWItemQuality Quality
        {
            get { return ItemInfo.Quality; }
        }

        public void UseContainerItem()
        {
            Lua.DoString("UseContainerItem({0}, {1})", BagIndex + 1, BagSlot + 1);
        }

        public void PickUp()
        {
            Lua.DoString("PickupContainerItem({0}, {1})", BagIndex + 1, BagSlot + 1);
        }

        public bool Use(ulong targetGuid, bool forceUse)
        {
            return UseItem(Guid, targetGuid, forceUse);
        }

        public bool Use(ulong targetGuid)
        {
            return Use(targetGuid, false);
        }

        public bool Use(bool forceUse)
        {
            return Use(0UL, forceUse);
        }

        public bool Use()
        {
            return Use(0UL);
        }

        private bool HasFlag(ItemFlags flag)
        {
            return (Flags & (uint)flag) != 0U;
        }

        public bool IsSoulbound
        {
            get { return HasFlag(ItemFlags.Soulbound); }
        }

        public bool IsConjured
        {
            get { return HasFlag(ItemFlags.Conjured); }
        }

        public bool IsOpenable
        {
            get
            {
                ItemInfo itemInfo = ItemInfo;
                if (itemInfo == null)
                {
                    return HasFlag(ItemFlags.Openable);
                }

                return (itemInfo.IsOpenable && (itemInfo.LockID == 0 || HasFlag(ItemFlags.Openable))) || (itemInfo.IsGiftWrapable && IsGiftWrapped);
            }
        }

        public bool IsGiftWrapped
        {
            get { return HasFlag(ItemFlags.GiftWrapped); }
        }

        public bool IsTotem
        {
            get { return HasFlag(ItemFlags.Totem); }
        }

        public bool TriggersSpell
        {
            get { return HasFlag(ItemFlags.TriggersSpell); }
        }

        public bool HasEquipCooldown
        {
            get { return !HasFlag(ItemFlags.NoEquipCooldown); }
        }

        public bool IsWand
        {
            get { return HasFlag(ItemFlags.Wand); }
        }

        public bool IsWrappingPaper
        {
            get { return HasFlag(ItemFlags.WrappingPaper); }
        }

        public bool IsCharter
        {
            get { return HasFlag(ItemFlags.Charter); }
        }

        public bool IsReadable
        {
            get { return HasFlag(ItemFlags.Readable); }
        }

        public bool IsPvPItem
        {
            get { return HasFlag(ItemFlags.PvPItem); }
        }

        public bool CanExpire
        {
            get { return HasFlag(ItemFlags.Expires); }
        }

        public bool CanProspect
        {
            get { return HasFlag(ItemFlags.Prospectable); }
        }

        public bool IsUniqueEquipped
        {
            get { return ItemInfo != null && ItemInfo.IsUniqueEquipped; }
        }

        public bool IsThrownWeapon
        {
            get { return HasFlag(ItemFlags.ThrownWeapon); }
        }

        public bool IsAccountBound
        {
            get { return ItemInfo != null && ItemInfo.IsAccountBound; }
        }

        public bool IsEnchantScroll
        {
            get { return HasFlag(ItemFlags.EnchantScroll); }
        }

        public bool IsMillable
        {
            get { return HasFlag(ItemFlags.Millable); }
        }

        public float Cooldown
        {
            get { return Lua.GetReturnVal<float>("return GetItemCooldown(" + Entry + ")", 0U); }
        }

        public TimeSpan CooldownTimeLeft
        {
            get
            {
                if (_cooldownTimeLeftCache == null)
                {
                    _cooldownTimeLeftCache = new PerFrameCachedValue<TimeSpan>(() =>
                    {
                        double returnVal = Lua.GetReturnVal<double>(string.Format("local x,y=GetItemCooldown({0}); return x+y-GetTime()", Entry), 0U);
                        if (returnVal <= 0.0)
                        {
                            return TimeSpan.Zero;
                        }

                        return TimeSpan.FromSeconds(returnVal);
                    });
                }

                return _cooldownTimeLeftCache;
            }
        }

        public bool Usable
        {
            get
            {
                if (_usableCache == null)
                {
                    _usableCache = new PerFrameCachedValue<bool>(() => Lua.GetReturnVal<bool>("return IsUsableItem(" + Entry + ")", 0U));
                }

                return _usableCache;
            }
        }

        private static bool UseItem(ulong itemGuid, ulong targetGuid, bool forceUse)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            WoWGuid wowGuid = new WoWGuid(itemGuid);
            bool result;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(8))
                {
                    allocatedMemory.Write(0, targetGuid);
                    executor.Clear();
                    executor.AddRandomLine("push {0}", (int)WoWObjectTypeFlag.Item);
                    executor.AddRandomLine("push {0}", wowGuid.High);
                    executor.AddRandomLine("push {0}", wowGuid.Low);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrObjectPtr);
                    executor.AddRandomLine("add esp, 12");
                    executor.AddRandomLine("test eax, eax");
                    executor.AddRandomLine("jz @OUT");
                    executor.AddRandomLine("push {0}", forceUse ? 1 : 0);
                    executor.AddRandomLine("push {0}", allocatedMemory.GetAddress(0));
                    executor.AddRandomLine("mov ecx, eax");
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGItem_C__UseItem);
                    executor.AddRandomLine("@OUT:");
                    executor.AddRandomLine("retn");
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        result = StyxWoW.Memory.Read<uint>(executor.ReturnPointer) != 0U;
                    }
                }
            }

            return result;
        }

        private string _cachedName;

        private PerFrameCachedValue<string> _itemLinkCache;

        private PerFrameCachedValue<ItemStats> _itemStatsCache;

        private ItemInfo _cachedItemInfo;

        private PerFrameCachedValue<TimeSpan> _cooldownTimeLeftCache;

        private PerFrameCachedValue<bool> _usableCache;

        [Flags]
        private enum ItemFlags : uint
        {
            None = 0U,
            Soulbound = 1U,
            Conjured = 2U,
            Openable = 4U,
            GiftWrapped = 8U,
            Totem = 32U,
            TriggersSpell = 64U,
            NoEquipCooldown = 128U,
            Wand = 256U,
            WrappingPaper = 512U,
            Producer = 1024U,
            MultiLoot = 2048U,
            BriefSpellEffect = 4096U,
            Charter = 8192U,
            Readable = 16384U,
            PvPItem = 32768U,
            Expires = 65536U,
            Prospectable = 262144U,
            UniqueEquipped = 524288U,
            ThrownWeapon = 4194304U,
            AccountBound = 134217728U,
            EnchantScroll = 268435456U,
            Millable = 536870912U
        }

        public class WoWItemEnchantment
        {
            internal WoWItemEnchantment(Styx.WoWInternals.DBC.SpellItemEnchantment enchant)
                : this(enchant, 0, 0)
            {
            }

            internal WoWItemEnchantment(Styx.WoWInternals.DBC.SpellItemEnchantment enchant, int expirationTimestamp, int chargesLeft)
            {
                _enchant = enchant;
                ExpirationTimestamp = expirationTimestamp;
                ChargesLeft = chargesLeft;
            }

            public uint Id
            {
                get
                {
                    if (_enchant == null)
                    {
                        return 0U;
                    }

                    return _enchant.Id;
                }
            }

            public string Name
            {
                get
                {
                    if (_enchant == null)
                    {
                        return string.Empty;
                    }

                    return _enchant.Name;
                }
            }

            public WoWItemStat GetStat(int index)
            {
                if (_enchant == null)
                {
                    return null;
                }

                return _enchant.GetStat(index);
            }

            public int ExpirationTimestamp { get; private set; }

            public int ChargesLeft { get; private set; }

            private readonly Styx.WoWInternals.DBC.SpellItemEnchantment _enchant;
        }

        public class WoWItemSpell
        {
            public WoWItemSpell(int id, int triggerId, int charges, int cooldown, int category, int categoryCooldown)
            {
                Id = id;
                TriggerId = triggerId;
                Charges = charges;
                Cooldown = cooldown;
                Category = category;
                CategoryCooldown = categoryCooldown;
                ActualSpell = WoWSpell.FromId(id);
            }

            public int Id { get; private set; }

            public int TriggerId { get; private set; }

            public int Charges { get; private set; }

            public int Cooldown { get; private set; }

            public int Category { get; private set; }

            public int CategoryCooldown { get; private set; }

            public bool IsValid
            {
                get { return Id != 0; }
            }

            public WoWSpell ActualSpell { get; private set; }

            public override string ToString()
            {
                return string.Format("Name: {0}, Id: {1} TriggerId: {2} Charges: {3} Cooldown: {4} Category: {5} CategoryCooldown: {6} IsValid: {7}", ActualSpell != null ? ActualSpell.Name : "Null", Id, TriggerId, Charges, Cooldown, Category, CategoryCooldown, IsValid);
            }
        }

        public class WoWItemStat
        {
            public WoWItemStat(WoWItemStatType type, int value)
            {
                Type = type;
                Value = value;
            }

            public WoWItemStatType Type { get; private set; }

            public int Value { get; set; }

            public override string ToString()
            {
                return string.Format("Type: {0}, Value: {1}", Type, Value);
            }
        }
    }
}
