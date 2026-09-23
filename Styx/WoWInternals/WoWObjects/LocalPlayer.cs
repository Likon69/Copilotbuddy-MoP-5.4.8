using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common.Helpers;
using Styx.CommonBot.Inventory;
using Styx.Helpers;
using Styx.Offsets;
using Styx.Patchables;
using Styx.Resources;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.Misc;
using Styx.WoWInternals.WoWCache;

namespace Styx.WoWInternals.WoWObjects
{
    public class LocalPlayer : WoWPlayer
    {
        public LocalPlayer(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        #region Glyphs

        public bool HasGlyphInSlot(int index)
        {
            return ((1 << index) & GetStorageField<int>((uint)WoWPlayerFields.GlyphSlotsEnabled)) != 0;
        }

        public int GetGlyphSlot(int index)
        {
            return GetStorageField<int>((uint)(WoWPlayerFields.GlyphSlots + index));
        }

        public int GetGlyph(int index)
        {
            return GetStorageField<int>((uint)(WoWPlayerFields.Glyphs + index));
        }

        #endregion

        #region Runes

        public int BloodRuneCount
        {
            get { return GetRuneCount(RuneType.Blood); }
        }

        public int DeathRuneCount
        {
            get { return GetRuneCount(RuneType.Death); }
        }

        public int FrostRuneCount
        {
            get { return GetRuneCount(RuneType.Frost); }
        }

        public int UnholyRuneCount
        {
            get { return GetRuneCount(RuneType.Unholy); }
        }

        public byte GetRuneCount(int index)
        {
            if (index >= 6)
            {
                throw new IndexOutOfRangeException(StyxResources.This_function_only_takes_vaues_from_0_5);
            }

            uint runeMask = StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LocalPlayerRunes);

            return (byte)((runeMask & (1 << index)) != 0 ? 1 : 0);
        }

        public byte GetRuneCount(RuneType type)
        {
            byte ret = 0;
            for (int i = 0; i < 6; i++)
            {
                if (GetRuneType(i) == type)
                {
                    ret += GetRuneCount(i);
                }
            }

            return ret;
        }

        public RuneType GetRuneType(int index)
        {
            if (index >= 6)
            {
                throw new IndexOutOfRangeException(StyxResources.This_function_only_takes_vaues_from_0_5);
            }

            return (RuneType)StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LocalPlayerRuneTypes + 4 * index);
        }

        #endregion

        #region Combo Points

        public int ComboPoints
        {
            get
            {
                ulong currentTargetGuid = CurrentTargetGuid;
                if (currentTargetGuid != 0 && currentTargetGuid == ComboPointsTarget)
                {
                    return StyxWoW.Memory.Read<byte>(StyxWoW.Offsets.Statics.LocalPlayerComboPoints);
                }

                return 0;
            }
        }

        public ulong ComboPointsTarget
        {
            get { return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.LocalPlayerComboPointsTarget); }
        }

        public int RawComboPoints
        {
            get { return StyxWoW.Memory.Read<byte>(StyxWoW.Offsets.Statics.LocalPlayerComboPoints); }
        }

        #endregion

        #region Spells

        #region Current Pending Spell

        public WoWSpell CurrentPendingCursorSpell
        {
            get
            {
                int spellId = 0;
                IntPtr ptr = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LocalPlayerPendingCursorSpell);
                if (ptr != IntPtr.Zero)
                {
                    spellId = StyxWoW.Memory.Read<int>(ptr + 32);
                }

                if (spellId != 0)
                {
                    return WoWSpell.FromId(spellId);
                }

                return null;
            }
        }

        public bool HasPendingSpell(int spellId)
        {
            WoWSpell cursorSpell = CurrentPendingCursorSpell;
            return cursorSpell != null && cursorSpell.Id == spellId;
        }

        public bool HasPendingSpell(string name)
        {
            WoWSpell cursorSpell = CurrentPendingCursorSpell;
            return cursorSpell != null && cursorSpell.Name == name;
        }

        public bool HasPendingSpell(WoWSpell spell)
        {
            WoWSpell cursorSpell = CurrentPendingCursorSpell;
            return spell != null && cursorSpell != null && cursorSpell.Id == spell.Id;
        }

        #endregion

        #region Known Spells

        public List<WoWPetSpell> PetSpells
        {
            get
            {
                uint[] spellArray = StyxWoW.Memory.ReadArray<uint>(StyxWoW.Offsets.Statics.PetSpells, 10);
                List<WoWPetSpell> ret = new List<WoWPetSpell>();
                for (int i = 0; i < 10; i++)
                {
                    ret.Add(new WoWPetSpell(spellArray[i], i));
                }

                return ret;
            }
        }

        internal List<WoWSpell> KnownSpells
        {
            get
            {
                return (from knownSpell in GetKnownSpells()
                        select WoWSpell.FromId(knownSpell)).ToList();
            }
        }

        internal IEnumerable<KnownSpell> IterateKnownSpells()
        {
            ExternalProcessMemory memory = StyxWoW.Memory;
            int numSpells = memory.Read<int>(StyxWoW.Offsets.Statics.LocalPlayerNumKnownSpells);
            IntPtr spellBookInfoPtr = memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LocalPlayerKnownSpells);

            for (int i = numSpells - 1; i >= 0; i--)
            {
                IntPtr spellPtr = memory.Read<IntPtr>(spellBookInfoPtr + i * 4);
                KnownSpell info = memory.Read<KnownSpell>(spellPtr);

                if (info.KnownType == 1 && info.Unk2 == 0)
                {
                    yield return info;
                }
            }
        }

        internal List<int> GetKnownSpells()
        {
            return IterateKnownSpells().Select(s => s.SpellId).ToList();
        }

        #endregion

        public int AutoRepeatingSpellId
        {
            get { return StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.LocalPlayerAutoRepeatingSpell); }
        }

        public bool IsAutoRepeatingSpell
        {
            get { return AutoRepeatingSpellId != 0; }
        }

        public bool CanDualWield()
        {
            if (_canDualWieldCache == null)
            {
                _canDualWieldCache = new PerFrameCachedValue<bool>(() =>
                {
                    int spellId = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.LocalPlayerDualWieldSpellId);
                    return spellId != 0 && KnowsSpell(spellId);
                });
            }

            return _canDualWieldCache;
        }

        #endregion

        #region Totems

        public List<WoWTotemInfo> Totems
        {
            get
            {
                List<WoWTotemInfo> ret = new List<WoWTotemInfo>();
                for (int i = 0; i < 4; i++)
                {
                    ret.Add(new WoWTotemInfo(i));
                }

                return ret;
            }
        }

        public List<WoWSpell> GetTotemBarSpells(int totemBarSlot)
        {
            if (totemBarSlot < 0 || totemBarSlot > 3)
            {
                throw new ArgumentException("Value must be between 0 and 3", "totemBarSlot");
            }

            IntPtr ptr = StyxWoW.Offsets.Statics.LocalPlayerTotemBars;
            PlayerTotemBar[] bars = StyxWoW.Memory.ReadArray<PlayerTotemBar>(ptr, 4);
            PlayerTotemBar bar = bars[totemBarSlot];

            if (bar.HasTotems == 0)
            {
                return new List<WoWSpell>();
            }

            List<WoWSpell> spells = new List<WoWSpell>();
            for (int i = 0; i < bar.NumTotems; i++)
            {
                int spellId = StyxWoW.Memory.Read<int>(bar.SpellIds + i * 4);
                if (spellId > 0)
                {
                    spells.Add(WoWSpell.FromId(spellId));
                }
            }

            return spells;
        }

        #endregion

        #region Talents & Glyphs

        public bool IsHealer
        {
            get { return SpecType == SpecType.Healer; }
        }

        public SpecType SpecType
        {
            get
            {
                switch (Specialization)
                {
                    case WoWSpec.MageArcane:
                    case WoWSpec.MageFire:
                    case WoWSpec.MageFrost:
                    case WoWSpec.DruidBalance:
                    case WoWSpec.HunterBeastMastery:
                    case WoWSpec.HunterMarksmanship:
                    case WoWSpec.HunterSurvival:
                    case WoWSpec.PriestShadow:
                    case WoWSpec.ShamanElemental:
                    case WoWSpec.WarlockAffliction:
                    case WoWSpec.WarlockDemonology:
                    case WoWSpec.WarlockDestruction:
                        return SpecType.RangedDps;

                    case WoWSpec.PaladinHoly:
                    case WoWSpec.DruidRestoration:
                    case WoWSpec.PriestDiscipline:
                    case WoWSpec.PriestHoly:
                    case WoWSpec.ShamanRestoration:
                    case WoWSpec.MonkMistweaver:
                        return SpecType.Healer;

                    case WoWSpec.WarriorProtection:
                    case WoWSpec.PaladinProtection:
                    case WoWSpec.DruidGuardian:
                    case WoWSpec.DeathKnightBlood:
                    case WoWSpec.MonkBrewmaster:
                        return SpecType.Tank;

                    case WoWSpec.PaladinRetribution:
                    case WoWSpec.WarriorArms:
                    case WoWSpec.WarriorFury:
                    case WoWSpec.DruidFeral:
                    case WoWSpec.DeathKnightFrost:
                    case WoWSpec.DeathKnightUnholy:
                    case WoWSpec.RogueAssassination:
                    case WoWSpec.RogueCombat:
                    case WoWSpec.RogueSubtlety:
                    case WoWSpec.ShamanEnhancement:
                    case WoWSpec.MonkWindwalker:
                        return SpecType.MeleeDps;

                    case (WoWSpec)67:
                    case (WoWSpec)68:
                    case (WoWSpec)69:
                        return SpecType.None;

                    default:
                        return SpecType.None;
                }
            }
        }

        #endregion

        #region Map Information + Corpse Locations

        public override bool IsIndoors
        {
            get
            {
                if (_isIndoorsCache == null)
                {
                    _isIndoorsCache = new PerFrameCachedValue<bool>(() => Lua.GetReturnVal<bool>("return IsIndoors()", 0));
                }

                return _isIndoorsCache;
            }
        }

        public override bool IsOutdoors
        {
            get { return !IsIndoors; }
        }

        public uint MapId
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LocalPlayerCurrentContinentId); }
        }

        public uint AreaId
        {
            get
            {
                if (_areaIdCache == null)
                {
                    _areaIdCache = new PerFrameCachedValue<uint>(() => StyxWoW.CallInjected<uint>(StyxWoW.Offsets.Functions.GetCurrentMapAreaID, CallingConvention.Cdecl));
                }

                return _areaIdCache;
            }
        }

        public Map CurrentMap
        {
            get { return Map.FromId(MapId); }
        }

        public string MapName
        {
            get
            {
                if (CurrentMap == null)
                {
                    return string.Empty;
                }

                return CurrentMap.InternalName;
            }
        }

        public string MinimapZoneText
        {
            get { return StyxWoW.Memory.ReadString(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.MinimapZoneText), Encoding.UTF8); }
        }

        public string RealZoneText
        {
            get { return StyxWoW.Memory.ReadString(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.RealZoneText), Encoding.UTF8); }
        }

        public Stable Stable
        {
            get { return _stable ?? (_stable = new Stable()); }
        }

        public string SubZoneText
        {
            get { return StyxWoW.Memory.ReadString(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.SubZoneText), Encoding.UTF8); }
        }

        public uint ZoneId
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LocalPlayerCurrentZoneId); }
        }

        public uint SubZoneId
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.LocalPlayerCurrentSubZoneId); }
        }

        public AreaTable GetCurrentArea()
        {
            return AreaTable.FromId(SubZoneId) ?? AreaTable.FromId(ZoneId);
        }

        public string ZoneText
        {
            get { return StyxWoW.Memory.ReadString(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.ZoneText), Encoding.UTF8); }
        }

        public int CorpseMapId
        {
            get { return StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.LocalPlayerCorpseMapId); }
        }

        public WoWPoint CorpsePoint
        {
            get { return StyxWoW.Memory.Read<WoWPoint>(StyxWoW.Offsets.Statics.LocalPlayerCorpsePoint); }
        }

        public bool IsInInstance
        {
            get
            {
                Map map = Map.FromId(MapId);
                return map != null && map.MapType != MapType.Common;
            }
        }

        internal static WoWPoint InstanceDeathLocation { get; set; }

        public uint HearthstoneAreaId
        {
            get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.HomeBindAreaId); }
        }

        public WoWPoint InstanceCorpseLocation
        {
            get
            {
                if (InstanceDeathLocation != WoWPoint.Empty)
                {
                    return InstanceDeathLocation;
                }

                return CorpsePoint;
            }
        }

        public int BattlefieldInstanceType
        {
            get { return StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.LocalPlayerBattlefieldInstanceType); }
        }

        public bool IsInArena
        {
            get { return BattlefieldInstanceType == 4; }
        }

        #endregion

        #region Quests

        public QuestLog QuestLog
        {
            get { return QuestLog.Instance; }
        }

        #endregion

        #region Inventory

        public double LowestDurabilityPercent
        {
            get
            {
                double lowestDurability = 100.0;
                foreach (WoWItem item in Inventory.Equipped.PhysicalItems)
                {
                    if (item.DurabilityPercent < lowestDurability)
                    {
                        lowestDurability = item.DurabilityPercent;
                    }
                }

                return lowestDurability / 100.0;
            }
        }

        public uint MaxDurability
        {
            get { return (uint)Inventory.Equipped.PhysicalItems.Sum(item => item.MaxDurability); }
        }

        public bool NormalBagsFull
        {
            get { return FreeNormalBagSlots == 0; }
        }

        public List<ulong> BagItemGuids
        {
            get
            {
                List<ulong> list = Inventory.Backpack.PhysicalItemGuids.ToList();
                for (uint i = 0; i < 4; i++)
                {
                    WoWContainer bag = GetBagAtIndex(i);
                    if (bag != null)
                    {
                        list.AddRange(bag.PhysicalItemGuids);
                    }
                }

                return list;
            }
        }

        public List<WoWItem> BagItems
        {
            get
            {
                return BagItemGuids.Select(ObjectManager.GetObjectByGuid<WoWItem>)
                    .Where(item => item != null)
                    .ToList();
            }
        }

        public IEnumerable<ulong> BankItemGuids
        {
            get
            {
                foreach (ulong itemGuid in Inventory.Bank.ItemGuids.Where(itemGuid => itemGuid != 0UL))
                {
                    yield return itemGuid;
                }

                for (WoWBagSlot slot = WoWBagSlot.Bank1; slot <= WoWBagSlot.Bank7; slot++)
                {
                    WoWContainer bag = GetBag(slot);
                    if (bag == null)
                    {
                        continue;
                    }

                    foreach (ulong itemGuid in bag.ItemGuids.Where(itemGuid => itemGuid != 0UL))
                    {
                        yield return itemGuid;
                    }
                }
            }
        }

        public IEnumerable<WoWItem> BankItems
        {
            get
            {
                return BankItemGuids.Select(ObjectManager.GetObjectByGuid<WoWItem>)
                    .Where(item => item != null);
            }
        }

        public bool BagsFull
        {
            get { return FreeBagSlots == 0; }
        }

        public List<ulong> CarriedItemGuids
        {
            get
            {
                List<ulong> ret = BagItemGuids;
                ret.AddRange(Inventory.Equipped.PhysicalItemGuids);
                return ret;
            }
        }

        public List<WoWItem> CarriedItems
        {
            get
            {
                return (from itemGuid in CarriedItemGuids
                        where itemGuid != 0
                        let item = ObjectManager.GetObjectByGuid<WoWItem>(itemGuid)
                        where item != null
                        select item).ToList();
            }
        }

        public uint Durability
        {
            get { return (uint)Inventory.Equipped.PhysicalItems.Sum(item => item.Durability); }
        }

        public double DurabilityPercent
        {
            get { return Durability / MaxDurability; }
        }

        public uint FreeBagSlots
        {
            get
            {
                uint slotCount = Inventory.Backpack.FreeSlots;
                for (uint i = 0; i < 4; i++)
                {
                    WoWContainer bag = GetBagAtIndex(i);
                    if (bag != null)
                    {
                        slotCount += bag.FreeSlots;
                    }
                }

                return slotCount;
            }
        }

        public uint FreeNormalBagSlots
        {
            get
            {
                uint slotCount = Inventory.Backpack.FreeSlots;
                for (uint i = 0; i < 4; i++)
                {
                    WoWContainer bagAtIndex = GetBagAtIndex(i);
                    if (bagAtIndex != null)
                    {
                        ItemInfo itemInfo = bagAtIndex.ItemInfo;
                        if (itemInfo != null && itemInfo.BagFamily == 0)
                        {
                            slotCount += bagAtIndex.FreeSlots;
                        }
                    }
                }

                return slotCount;
            }
        }

        public WoWPlayerInventory Inventory
        {
            get
            {
                IntPtr invAddr = BaseAddress + (int)StyxWoW.Offsets.Misc.InventoryOffset;
                CGBag bag = StyxWoW.Memory.Read<CGBag>(invAddr);
                return new WoWPlayerInventory(bag);
            }
        }

        public WoWPrice GetEstimatedRepairCost()
        {
            long repairCost = 0;
            foreach (WoWItem item in StyxWoW.Me.CarriedItems)
            {
                if (item != null && item.ItemInfo != null)
                {
                    uint itemLevel = (uint)item.ItemInfo.Level;
                    WoWDb.Row row = StyxWoW.Db[ClientDb.DurabilityCosts].GetRow(itemLevel);
                    if (row != null && row.IsValid)
                    {
                        double multiplier = row.GetStruct<DurabilityCostEntry>().Multiplier[item.ItemInfo.SubClassId];
                        long repairPrice = (long)((item.MaxDurability - item.Durability) * multiplier);
                        repairCost += repairPrice;
                    }
                }
            }

            return new WoWPrice(repairCost);
        }

        public bool CanUseItem(uint itemId, out GameError reason)
        {
            EnsureValid("CanUseItem");
            using (AllocatedMemory allocated = StyxWoW.Memory.CreateAllocatedMemory(4))
            {
                Styx.WoWInternals.WoWCache.WoWCache.InfoBlock infoBlock = StyxWoW.Cache[CacheDb.Item].GetInfoBlockById(itemId);
                if (infoBlock == null)
                {
                    reason = GameError.None;
                    return false;
                }

                byte canUse = StyxWoW.Memory.CallInjected<byte>(StyxWoW.Offsets.Functions.CGPlayer_C__CanUseItem, CallingConvention.ThisCall, (uint)(int)BaseAddress, infoBlock.Address, allocated.Address);

                reason = (GameError)StyxWoW.Memory.Read<uint>(allocated.Address);
                return canUse != 0;
            }
        }

        public WoWContainer GetBag(WoWBagSlot slot)
        {
            return GetBagAtIndex((uint)slot);
        }

        public WoWContainer GetBagAtIndex(uint index)
        {
            if (index > 10)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return ObjectManager.GetObjectByGuid<WoWContainer>(GetBagGuidAtIndex(index));
        }

        public ulong GetBagGuidAtIndex(uint index)
        {
            if (index > 10)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.LocalPlayerBagArray + (int)(8U * index));
        }

        public bool CanEquipItem(WoWItem item)
        {
            return CanEquipItem(item.ItemInfo);
        }

        public bool CanEquipItem(ItemInfo itemInfo)
        {
            GameError trash;
            return CanEquipItem(itemInfo, out trash);
        }

        public bool CanEquipItem(ItemInfo itemInfo, out GameError reason)
        {
            if (itemInfo.InternalInfo.EquipSlot == 0)
            {
                reason = GameError.InvFull;
                return false;
            }

            if (itemInfo.RequiredLevel > Level)
            {
                reason = GameError.InvFull;
                return false;
            }

            return CanUseItem(itemInfo.Id, out reason);
        }

        public bool CanEquipItem(WoWItem item, out GameError reason)
        {
            return CanEquipItem(item.ItemInfo, out reason);
        }

        #endregion

        #region Misc Account Info

        public string RealmName
        {
            get
            {
                if (_realmNameCache == null)
                {
                    _realmNameCache = new PerFrameCachedValue<string>(() =>
                    {
                        try
                        {
                            return Lua.GetReturnVal<string>("return GetRealmName()", 0);
                        }
                        catch
                        {
                            return "Unknown";
                        }
                    });
                }

                return _realmNameCache;
            }
        }

        #endregion

        #region Skills

        public List<uint> ResearchSiteIds
        {
            get
            {
                uint researchingField = (uint)WoWPlayerFields.Researching;
                List<uint> ids = new List<uint>();
                for (uint i = 0; i < 16; i++)
                {
                    ushort id = GetStorageField<ushort>(researchingField + i * 2);
                    if (id > 0)
                    {
                        ids.Add(id);
                    }
                }

                return ids;
            }
        }

        internal Dictionary<int, WoWSkill> Skills
        {
            get
            {
                Dictionary<int, WoWSkill> temp = new Dictionary<int, WoWSkill>();

                for (uint i = 0; i < 128; i++)
                {
                    WoWSkill skill = GetSkill(i);
                    if (skill != null && skill.IsValid && !temp.ContainsKey(skill.Id))
                    {
                        temp.Add(skill.Id, skill);
                    }
                }

                return temp;
            }
        }

        public int CanSkinLevel
        {
            get
            {
                WoWSkill skill = StyxWoW.Me.GetSkill(SkillLine.Skinning);
                int value = skill != null ? skill.CurrentValue : 0;
                if (value == 0)
                {
                    return 0;
                }

                if (value < 100)
                {
                    return value / 10 + 10;
                }

                return value / 5;
            }
        }

        public unsafe WoWSkill GetSkill(uint index)
        {
            if (index >= 128)
            {
                return null;
            }

            IntPtr storage = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjDescriptorFieldsOffset);
            SkillSection skills = StyxWoW.Memory.Read<SkillSection>(storage + (int)WoWPlayerFields.Skill);

            ushort id = skills.Ids[index];
            ushort rank = skills.Rank[index];
            ushort maxRank = skills.MaxRank[index];
            ushort step = skills.Step[index];
            ushort modifier = skills.Modifier[index];
            ushort bonus = skills.Unk6[index];

            if (id == 0)
            {
                return null;
            }

            return storage != (IntPtr)0 ? new WoWSkill(id, step, rank, maxRank, (short)modifier, bonus) : null;
        }

        public WoWSkill GetSkill(int id)
        {
            return Skills.FirstOrDefault(obj => obj.Value.Id == id).Value;
        }

        public WoWSkill GetSkill(string name)
        {
            return Skills.FirstOrDefault(obj => string.Equals(obj.Value.Name, name, StringComparison.InvariantCultureIgnoreCase)).Value;
        }

        public WoWSkill GetSkill(SkillLine skill)
        {
            return GetSkill((int)skill);
        }

        #endregion

        #region Group (Party/Raid) Info

        [Obsolete("This is now obsolete! Please use GroupInfo.IsInParty instead", true)]
        public bool IsInParty
        {
            get { return GroupInfo.IsInParty; }
        }

        [Obsolete("This is now obsolete! Please use GroupInfo.IsInRaid instead", true)]
        public bool IsInRaid
        {
            get { return GroupInfo.IsInRaid; }
        }

        public WoWGroupInfo GroupInfo
        {
            get { return _groupInfo ?? (_groupInfo = new WoWGroupInfo()); }
        }

        public WoWPartyMember.GroupRole Role
        {
            get
            {
                WoWPartyMember myself = GroupInfo.RaidMembers.FirstOrDefault(m => m.Guid == Guid);
                if (myself != null)
                {
                    return myself.Role;
                }

                return WoWPartyMember.GroupRole.None;
            }
        }

        public List<WoWPlayer> RaidMembers
        {
            get { return GroupInfo.RaidMembers.Select(m => m.ToPlayer()).Where(p => p != null).ToList(); }
        }

        public List<WoWPlayer> PartyMembers
        {
            get { return GroupInfo.PartyMembers.Select(m => m.ToPlayer()).Where(p => p != null).ToList(); }
        }

        public ulong GetPartyMemberGuid(int index)
        {
            if (index < 0 || index > 3)
            {
                return 0;
            }

            ulong[] guids = GroupInfo.PartyMemberGuids;
            if (index >= guids.Length)
            {
                return 0;
            }

            return guids[index];
        }

        public WoWPlayer GetRaidMember(int index)
        {
            return ObjectManager.GetObjectByGuid<WoWPlayer>(GetRaidMemberGuid(index));
        }

        public ulong GetRaidMemberGuid(int index)
        {
            if (index < 0 || index > 39)
            {
                return 0;
            }

            ulong[] guids = GroupInfo.RaidMemberGuids;
            if (index >= guids.Length)
            {
                return 0;
            }

            return guids[index];
        }

        #endregion

        #region Combat + Targets

        public override ulong CurrentTargetGuid
        {
            get { return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.LocalPlayerCurrentTargetGuid); }
        }

        public bool IsActuallyInCombat
        {
            get
            {
                if (_isActuallyInCombatCache == null)
                {
                    _isActuallyInCombatCache = new PerFrameCachedValue<bool>(() => HasThreatStatus(ThreatStatus.NotTanking));
                }

                return _isActuallyInCombatCache;
            }
        }

        public bool IsBeingAttacked
        {
            get
            {
                if (_isBeingAttackedCache == null)
                {
                    _isBeingAttackedCache = new PerFrameCachedValue<bool>(() => HasThreatStatus(ThreatStatus.NoobishTank));
                }

                return _isBeingAttackedCache;
            }
        }

        private bool HasThreatStatus(ThreatStatus threshold)
        {
            List<WoWUnit> minions = StyxWoW.Me.Minions;
            if (!StyxWoW.Me.Combat)
            {
                if (!minions.Any(u => u.Combat))
                {
                    return false;
                }
            }

            ulong[] guids = new ulong[] { StyxWoW.Me.Guid }.Concat(minions.Select(u => u.Guid)).ToArray();

            return ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Any(u =>
                u.IsAlive
                && !Styx.CommonBot.Blacklist.Contains(u, Styx.CommonBot.BlacklistFlags.Combat)
                && ((u.CurrentTargetGuid != 0 && u.Combat && guids.Contains(u.CurrentTargetGuid))
                    || u.ThreatInfo.ThreatStatus >= threshold
                    || minions.Any(minion => u.GetThreatInfoFor(minion).ThreatStatus >= threshold)));
        }

        public ulong FocusedUnitGuid
        {
            get { return StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.LocalPlayerFocusGuid); }
        }

        public WoWUnit FocusedUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(FocusedUnitGuid); }
        }

        public bool SetFocus(WoWObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return SetFocus(obj.Guid);
        }

        public bool SetFocus(ulong guid)
        {
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(8))
            {
                allocatedMemory.Write<ulong>(0, guid);
                StyxWoW.CallInjected(StyxWoW.Offsets.Functions.CGGameUI__SetFocus, CallingConvention.Cdecl, allocatedMemory.Address);
            }

            return true;
        }

        internal void FocusInternal(ulong guid)
        {
            StyxWoW.Memory.Write<ulong>(StyxWoW.Offsets.Statics.LocalPlayerFocusGuid, guid);
        }

        public void TargetLastTarget()
        {
            Lua.DoString("TargetLastTarget()", "WoW.lua");
        }

        public void ToggleAttack()
        {
            Lua.DoString("AttackTarget()", "WoW.lua");
        }

        public void ClearTarget()
        {
            TargetInternal(0);
        }

        #endregion

        #region Faction Standing

        public WoWUnitReaction GetReputationLevelWith(uint factionId)
        {
            int reputationWith = GetReputationWith(factionId);
            if (reputationWith >= 42000)
            {
                return WoWUnitReaction.Exalted;
            }

            if (reputationWith >= 21000)
            {
                return WoWUnitReaction.Revered;
            }

            if (reputationWith >= 9000)
            {
                return WoWUnitReaction.Honored;
            }

            if (reputationWith >= 3000)
            {
                return WoWUnitReaction.Friendly;
            }

            if (reputationWith >= 0)
            {
                return WoWUnitReaction.Neutral;
            }

            if (reputationWith >= -3000)
            {
                return WoWUnitReaction.Unfriendly;
            }

            if (reputationWith >= -6000)
            {
                return WoWUnitReaction.Hostile;
            }

            return WoWUnitReaction.Hated;
        }

        public int GetReputationWith(uint toFactionId)
        {
            FactionStanding standing;
            if (GetFactionStanding(toFactionId, out standing))
            {
                return standing.TotalReputation;
            }

            return 0;
        }

        public bool GetFactionStanding(FactionId factionId, out FactionStanding standing)
        {
            return GetFactionStanding((uint)factionId, out standing);
        }

        public bool GetFactionStanding(uint factionId, out FactionStanding standing)
        {
            Faction faction = Faction.FromId(factionId);
            if (faction != null)
            {
                return GetFactionStanding(faction, out standing);
            }

            standing = default(FactionStanding);
            return false;
        }

        public bool GetFactionStanding(Faction faction, out FactionStanding standing)
        {
            if (faction != null)
            {
                int repGainId = faction.Record.RepGainId;
                if (repGainId >= 0 && repGainId <= 256)
                {
                    standing = StyxWoW.Memory.Read<FactionStanding>(StyxWoW.Offsets.Statics.FactionStandings + (int)(MarshalCache<FactionStanding>.Size * (uint)repGainId));
                    return true;
                }
            }

            standing = default(FactionStanding);
            return false;
        }

        #endregion

        #region Vehicle

        public bool InVehicle
        {
            get
            {
                if (_inVehicleCache == null)
                {
                    _inVehicleCache = new PerFrameCachedValue<bool>(() => Lua.GetReturnVal<bool>("return UnitUsingVehicle(\"player\")", 0));
                }

                return _inVehicleCache;
            }
        }

        #endregion

        #region Mirror Timer Info

        public MirrorTimerInfo GetMirrorTimerInfo(MirrorTimerType type)
        {
            return StyxWoW.Memory.Read<MirrorTimerInfo>(StyxWoW.Offsets.Statics.MirrorTimers + (int)((uint)type * MarshalCache<MirrorTimerInfo>.Size));
        }

        #endregion

        #region Positional Methods (IsBehind/SetFacing/etc)

        public bool IsBehind(WoWUnit unit)
        {
            return WoWMathHelper.IsBehind(Location, unit.Location, unit.Rotation, 3.14159274f);
        }

        public void SetFacing(WoWGameObject obj)
        {
            SetFacing(obj.Location);
        }

        public void SetFacing(WoWUnit obj)
        {
            SetFacing(obj.Location);
        }

        public void SetFacing(WoWPoint otherLoc)
        {
            SetFacing(WoWMathHelper.CalculateNeededFacing(Location, otherLoc));
        }

        public void SetFacing(float facingInRadians)
        {
            WoWMovement.SetFacing(facingInRadians);
        }

        #endregion

        public uint HonorPoints
        {
            get
            {
                uint amount;
                try
                {
                    amount = WoWCurrency.GetCurrencyByType(WoWCurrencyType.HonorPoints).Amount;
                }
                catch (NullReferenceException)
                {
                    amount = 0;
                }

                return amount;
            }
        }

        public LfgState LfgMode(LfgCategory category, int? lfgId = null)
        {
            string command = lfgId != null
                ? string.Format("return GetLFGMode({0}, {1})", (int)category, lfgId.Value)
                : string.Format("return GetLFGMode({0})", (int)category);

            List<string> returnValues = Lua.GetReturnValues(command);
            LfgState state = LfgState.NotInLfg;
            if (returnValues != null)
            {
                string value = returnValues.Count > 0 ? returnValues[0].ToLowerInvariant() : "";
                switch (value)
                {
                    case "queued":
                        return LfgState.InQueue;
                    case "abandonedindungeon":
                        return LfgState.AbandonedInDungeon;
                    case "proposal":
                        return LfgState.Proposal;
                    case "lfgparty":
                        return LfgState.InDungeon;
                    case "suspended":
                        return LfgState.Suspended;
                    case "rolecheck":
                        return LfgState.RoleCheck;
                }

                state = LfgState.NotInLfg;
            }

            return state;
        }

        private static Stable _stable;

        private PerFrameCachedValue<bool> _canDualWieldCache;

        private PerFrameCachedValue<bool> _isIndoorsCache;

        private PerFrameCachedValue<uint> _areaIdCache;

        private PerFrameCachedValue<string> _realmNameCache;

        private WoWGroupInfo _groupInfo;

        private PerFrameCachedValue<bool> _isActuallyInCombatCache;

        private PerFrameCachedValue<bool> _isBeingAttackedCache;

        private PerFrameCachedValue<bool> _inVehicleCache;

        #region Structs

        #region Nested type: KnownSpell

        [StructLayout(LayoutKind.Sequential)]
        internal struct KnownSpell
        {
            public uint KnownType;
            public int SpellId;
            public uint Level;
            public uint Unk_TabType;
            public byte Unk1;
            public byte Unk2;

            public override string ToString()
            {
                return string.Format("KnownType: {0}, SpellId: {1}, Level: {2}, UnkTabType: {3}, Unk1: {4}, Unk2: {5}",
                    KnownType, SpellId, Level, Unk_TabType, Unk1, Unk2);
            }
        }

        #endregion

        #region Nested type: PlayerTotemBar

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PlayerTotemBar
        {
            public readonly uint HasTotems;
            public readonly uint NumTotems;
            public readonly IntPtr SpellIds;
            public readonly uint DwordC;
        }

        #endregion

        #region Nested type: SkillSection

        [StructLayout(LayoutKind.Sequential, Size = 448 * 4)]
        private unsafe struct SkillSection
        {
            public fixed ushort Ids[128];
            public fixed ushort Unk1[128];
            public fixed ushort Rank[128];
            public fixed ushort Step[128];
            public fixed ushort MaxRank[128];
            public fixed ushort Modifier[128];
            public fixed ushort Unk6[128];
        }

        #endregion

        #region Nested type: SpellOverrideInfo

        [StructLayout(LayoutKind.Sequential)]
        internal struct SpellOverrideInfo
        {
            public readonly int OriginalSpellId;
            public readonly int NextPtr;
            public readonly int dword8;
            public readonly int dwordC;
            public readonly int dword10;
            public readonly int dword14;
            public readonly int NewSpellId;

            public override string ToString()
            {
                return string.Format("OriginalSpellId: {0}, Dword4: {1:X}, Dword8: {2:X}, DwordC: {3:X}, Dword10: {4:X}, Dword14: {5:X}, NewSpellId: {6}", OriginalSpellId, NextPtr, dword8, dwordC, dword10, dword14, NewSpellId);
            }
        }

        #endregion

        #endregion
    }
}
