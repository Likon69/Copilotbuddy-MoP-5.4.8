using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.CharacterManagement
{
    public class AutoEquipper
    {
        #region Properties

        private const int MinimumRollDelay = 4000;
        private const int MaximumRollDelay = 9000;
        private const int MinimumDecisionDelay = 10000;
        private const int MaximumDecisionDelay = 20000;
        private const int MinimumDelayedRollDelay = 2000;
        private const int MaximumDelayedRollDelay = 4000;

        private StyxLog _log;
        private Coroutine _equipmentCoroutine;
        private readonly Dictionary<ulong, DisregardEntry> _disregardedItems = new Dictionary<ulong, DisregardEntry>();
        private readonly List<Coroutine> _lootRolls = new List<Coroutine>();
        private readonly Random _random = new Random();
        private Coroutine _lootCoroutine;
        private PerFrameCachedValue<bool> _canDualWieldTwoHanders;

        public int TargetCheckTime { get; set; }
        public int DisregardTime { get; set; }

        #endregion

        #region Methods

        internal AutoEquipper()
        {
            TargetCheckTime = 10000;
            DisregardTime = 300000;
        }

        [Conditional("NIGHTLY_BUILD")]
        [Conditional("DEBUG")]
        [Conditional("BETA_BUILD")]
        private void LogDebug(string format, params object[] args)
        {
            if (_log != null)
            {
                if (args != null && args.Length > 0)
                {
                    _log.Write(format, args);
                }
                else
                {
                    _log.Write(format);
                }
            }
        }

        private void Log(Color? color, string format, params object[] args)
        {
            if (format.StartsWith("  "))
            {
                format = format.Substring(2);
            }
            format = "[AutoEquip] " + format;
            if (args != null && args.Length > 0)
            {
                if (color.HasValue)
                {
                    Logging.Write(color.Value, format, args);
                }
                else
                {
                    Logging.Write(format, args);
                }
            }
            else if (color.HasValue)
            {
                Logging.Write(color.Value, format);
            }
            else
            {
                Logging.Write(format);
            }
        }

        private void Log(string format, params object[] args)
        {
            Log(null, format, args);
        }

        private void PulseEquipment()
        {
            if (!CharacterSettings.Instance.AutoEquip)
            {
                DisposeCoroutine(ref _equipmentCoroutine);
            }
            else
            {
                if (StyxWoW.Me.Combat || !StyxWoW.Me.IsAlive || StyxWoW.Me.IsCasting || StyxWoW.Me.IsInArena)
                {
                    return;
                }
                if (_equipmentCoroutine == null || _equipmentCoroutine.IsFinished)
                {
                    _equipmentCoroutine = new Coroutine(() => CheckInventory());
                }
                try
                {
                    _equipmentCoroutine.Resume();
                }
                catch (CoroutineException ex)
                {
                    Log(Colors.Red, "Exception was thrown in AE coroutine!\r\n{0}", ex.ToString());
                }
            }
        }

        internal void Pulse()
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                PulseLootRolls();
                PulseEquipment();
            }
        }

        private void DisregardItem(ulong guid)
        {
            if (_disregardedItems.TryGetValue(guid, out var entry))
            {
                entry.CheckTimer.Restart();
                return;
            }
            entry = new DisregardEntry(guid);
            entry.CheckTimer.Restart();
            _disregardedItems[guid] = entry;
        }

        private void GetEquippedWeapons(out WoWItem mainHand, out WoWItem offHand)
        {
            WoWPaperDoll equipped = StyxWoW.Me.Inventory.Equipped;
            mainHand = equipped.GetEquippedItem(WoWInventorySlot.MainHand);
            offHand = equipped.GetEquippedItem(WoWInventorySlot.OffHand);
        }

        private bool CanEquipItem(ItemInfo itemInfo)
        {
            if (StyxWoW.Me.CanEquipItem(itemInfo))
            {
                return true;
            }
            if (itemInfo.Quality == WoWItemQuality.Heirloom && itemInfo.GetShownArmorClass() <= WeightSet.GetArmorClassForCurrentClass())
            {
                return true;
            }
            return false;
        }

        private bool IsInBags(WoWItem candidateItem)
        {
            int bagIndex = candidateItem.BagIndex;
            int bagSlot = candidateItem.BagSlot;
            if (bagIndex == -1 && bagSlot >= 16)
            {
                return false;
            }
            return bagSlot >= 0;
        }

        private bool IsScalingHeirloom(WoWItem candidateItem)
        {
            if (candidateItem.ItemInfo.Quality != WoWItemQuality.Heirloom)
            {
                return false;
            }
            ScalingStatDistribution scalingStatDistribution = candidateItem.ItemInfo.ScalingStatDistribution;
            if (scalingStatDistribution == null)
            {
                return false;
            }
            int level = StyxWoW.Me.Level;
            if (level >= scalingStatDistribution.MinLevel)
            {
                return level < scalingStatDistribution.MaxLevel;
            }
            return false;
        }

        private bool IsSpecializedBag(WoWItem candidateItem)
        {
            if (candidateItem.ItemInfo.EquipSlot == InventoryType.Bag)
            {
                return candidateItem.ItemInfo.BagFamily != 0;
            }
            return false;
        }

        private bool WouldUnequipHeirloom(ItemInfo itemInfo, WoWInventorySlot slot)
        {
            if (slot != WoWInventorySlot.MainHand && slot != WoWInventorySlot.OffHand)
            {
                return false;
            }
            if (CanDualWieldTwoHanders())
            {
                return false;
            }
            GetEquippedWeapons(out var mainHand, out var offHand);
            WoWItem otherWeapon = ((slot == WoWInventorySlot.MainHand) ? offHand : mainHand);
            if (!(otherWeapon == null) && otherWeapon.ItemInfo != null && IsScalingHeirloom(otherWeapon))
            {
                if (IsTwoHanded(otherWeapon.ItemInfo))
                {
                    return true;
                }
                if (slot == WoWInventorySlot.OffHand)
                {
                    return false;
                }
                return IsTwoHanded(itemInfo);
            }
            return false;
        }

        private bool ShouldPreserveSlot(ItemInfo itemInfo, WoWInventorySlot slot, out string reason)
        {
            if (WouldUnequipHeirloom(itemInfo, slot))
            {
                reason = "would unequip heirloom";
                return true;
            }
            WoWItem equippedItem = StyxWoW.Me.Inventory.Equipped.GetEquippedItem(slot);
            if (!(equippedItem == null) && equippedItem.ItemInfo != null)
            {
                if (IsScalingHeirloom(equippedItem))
                {
                    reason = "heirloom equipped";
                    return true;
                }
                if (IsSpecializedBag(equippedItem))
                {
                    reason = "non generic bag equipped";
                    return true;
                }
                reason = null;
                return false;
            }
            reason = null;
            return false;
        }

        private bool WouldBindProtectedItem(WoWItem candidateItem, ItemInfo itemInfo)
        {
            if (!candidateItem.IsSoulbound && itemInfo.Bond == WoWItemBondType.OnEquip)
            {
                if (itemInfo.EquipSlot != InventoryType.Bag)
                {
                    if (!CharacterSettings.Instance.AutoEquipBoEEpics)
                    {
                        return itemInfo.Quality == WoWItemQuality.Epic;
                    }
                    return false;
                }
                return itemInfo.BagSlots > 20;
            }
            return false;
        }

        private bool CanConsiderItem(ItemInfo itemInfo, bool log, out string reason, out List<WoWInventorySlot> slots)
        {
            slots = null;
            if (itemInfo.EquipSlot == InventoryType.None)
            {
                reason = "Not equippable";
                return false;
            }
            if (!CanEquipItem(itemInfo))
            {
                reason = "Cannot equip";
                return false;
            }
            if (itemInfo.EquipSlot == InventoryType.Trinket && StyxWoW.Me.Level >= 90)
            {
                reason = "Is a trinket and we're max level";
                return false;
            }
            slots = DecideEquipmentSlots(itemInfo).ToList();
            if (IsUniqueItemEquipped((int)itemInfo.Id, slots))
            {
                reason = "Unique-equipped and we already have one equipped";
                return false;
            }
            RemoveProtectedSlots(itemInfo, slots, log);
            if (slots.Count <= 0)
            {
                reason = "No inventory slots to equip this in";
                return false;
            }
            reason = null;
            return true;
        }

        private bool ShouldDisregardItem(WoWItem candidateItem, ItemInfo itemInfo, out string reason)
        {
            if (!IsInBags(candidateItem))
            {
                reason = "Not in bags anymore";
                return true;
            }
            if (WouldBindProtectedItem(candidateItem, itemInfo))
            {
                reason = "Would bind item";
                return true;
            }
            reason = null;
            return false;
        }

        private async Task CheckItem(ulong guid)
        {
            DisregardItem(guid);
            WoWItem item = ObjectManager.GetObjectByGuid<WoWItem>(guid);
            if (item == null)
                return;

            _ = item.Name;
            ItemInfo itemInfo = item.ItemInfo;
            if (itemInfo == null)
            {
                await Coroutine.Sleep(2000);
                if (!item.IsValid)
                    return;
                itemInfo = item.ItemInfo;
                if (itemInfo == null)
                    return;
            }

            if (!CanConsiderItem(itemInfo, true, out string reason, out List<WoWInventorySlot> slots) ||
                ShouldDisregardItem(item, itemInfo, out reason))
                return;

            switch (GetEquipmentClass(itemInfo.EquipSlot))
            {
                case WoWItemClass.Container:
                    await CheckForBag(item, itemInfo, slots);
                    break;
                case WoWItemClass.Weapon:
                    await CheckForWeapons(item, itemInfo, slots);
                    break;
                case WoWItemClass.Armor:
                    await CheckForItems(item, itemInfo, slots);
                    break;
            }
        }

        private void RemoveProtectedSlots(ItemInfo itemInfo, List<WoWInventorySlot> slots, bool log)
        {
            if (slots.Count <= 0)
            {
                return;
            }
            List<string> slotDescriptions = new List<string>();
            for (int index = slots.Count - 1; index >= 0; index--)
            {
                if (ShouldPreserveSlot(itemInfo, slots[index], out var reason))
                {
                    slotDescriptions.Add($"{slots[index]} (\u2716 {reason})");
                    slots.RemoveAt(index);
                }
                else
                {
                    slotDescriptions.Add($"{slots[index]} (\u2713)");
                }
            }
        }

        private IEnumerable<EquipCandidate> GetArmorCandidates(WoWItem candidateItem, ItemInfo itemInfo, ItemStats itemStats, IEnumerable<WoWInventorySlot> slots, bool factorArmorClass = true)
        {
            WeightSet weightSet = CharacterManager.CurrentClassProfile.WeightSet;
            float candidateScore = weightSet.EvaluateItem(itemInfo, itemStats, factorArmorClass);
            foreach (WoWInventorySlot slot in slots)
            {
                WoWItem equippedItem = StyxWoW.Me.Inventory.Equipped.GetEquippedItem(slot);
                float currentScore = ((equippedItem != null) ? weightSet.EvaluateItem(equippedItem.ItemInfo, equippedItem.ItemStats, factorArmorClass) : 0f);
                yield return new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, equippedItem, currentScore, slot);
            }
        }

        private IEnumerable<EquipCandidate> GetBagCandidates(WoWItem candidateItem, ItemInfo itemInfo, IEnumerable<WoWInventorySlot> slots)
        {
            float candidateScore = itemInfo.BagSlots;
            foreach (WoWInventorySlot slot in slots)
            {
                WoWItem equippedItem = StyxWoW.Me.Inventory.Equipped.GetEquippedItem(slot);
                if (equippedItem == null)
                {
                    yield return new EquipCandidate(candidateItem, itemInfo, null, candidateScore, null, 0f, slot);
                }
                else
                {
                    yield return new EquipCandidate(candidateItem, itemInfo, null, candidateScore, equippedItem, equippedItem.ItemInfo.BagSlots, slot);
                }
            }
        }

        private bool IsUniqueItemEquipped(int itemId, List<WoWInventorySlot> slots)
        {
            if (slots.Count <= 0)
            {
                return false;
            }
            string slotIds = "{" + string.Join(", ", slots.Select((WoWInventorySlot slot) => (int)(slot + 1))) + "}";
            return Lua.GetReturnVal<bool>(string.Format("local uniqueFamily, maxCount = GetItemUniqueness({0})\r\nif not uniqueFamily or not maxCount then\r\n\treturn 0\r\nend\r\n\r\nlocal alreadyEquippedCount = 0\r\nlocal slotIDTable = {1}\r\nfor i=1,#slotIDTable do\r\n\tlocal equippedItemID = GetInventoryItemID('player', slotIDTable[i])\r\n\tif (uniqueFamily == -1 and {0} == equippedItemID) or (uniqueFamily ~= -1 and uniqueFamily == GetItemUniqueness(equippedItemID)) then\r\n\t\talreadyEquippedCount = alreadyEquippedCount + 1\r\n\tend\r\nend\r\n\r\nreturn alreadyEquippedCount >= maxCount", itemId, slotIds), 0u);
        }

        private async Task EquipCandidate(WoWItem item, EquipCandidate candidate)
        {
            float gain = candidate.Gain;
            if (Math.Abs(gain) < 1E-05f)
            {
                if (candidate.CurrentItem != null ||
                    (item.ItemInfo.Bond == WoWItemBondType.OnEquip && !item.IsSoulbound))
                    return;
            }
            else if (gain < 0f)
                return;

            if (candidate.CurrentItem != null && candidate.CurrentItem.IsValid)
                Log(Colors.LawnGreen, "  Equipping item {0} instead of {1} (slot: {2}) - it scored {3:F2} while the old scored {4:F2}",
                    item.Name, candidate.CurrentItem.Name, candidate.Slot, candidate.CandidateScore, candidate.CurrentScore);
            else
                Log(Colors.LawnGreen, "  Equipping item {0} into empty slot {1} - it scored {2:F2}",
                    item.Name, candidate.Slot, candidate.CandidateScore);

            await EquipItemIntoSlot(item, candidate.Slot);
        }

        private EquipCandidate SelectArmor(WoWItem candidateItem, ItemInfo itemInfo, ItemStats itemStats, IEnumerable<WoWInventorySlot> slots)
        {
            if (!CharacterSettings.Instance.AutoEquipArmor)
            {
                return null;
            }
            if (CharacterManager.CurrentClassProfile == null)
            {
                Log(Colors.Red, "  No class profile is loaded; cannot equip/select weapons/armor");
                return null;
            }
            EquipCandidate[] candidates = GetArmorCandidates(candidateItem, itemInfo, itemStats, slots).ToArray();
            EquipCandidate bestCandidate = (from candidate in candidates
                orderby candidate.Gain descending, candidate.CurrentItem == null descending
                select candidate).FirstOrDefault();
            if (bestCandidate == null)
            {
                return null;
            }
            return bestCandidate;
        }

        private async Task CheckForItems(WoWItem item, ItemInfo itemInfo, IEnumerable<WoWInventorySlot> slots)
        {
            EquipCandidate candidate = SelectArmor(item, itemInfo, item.ItemStats, slots);
            if (candidate != null)
                await EquipCandidate(item, candidate);
        }

        private EquipCandidate SelectBag(WoWItem candidateItem, ItemInfo itemInfo, IEnumerable<WoWInventorySlot> slots)
        {
            if (!CharacterSettings.Instance.AutoEquipBags)
            {
                return null;
            }
            if (itemInfo.BagFamily != 0)
            {
                return null;
            }
            EquipCandidate[] candidates = GetBagCandidates(candidateItem, itemInfo, slots).ToArray();
            EquipCandidate bestCandidate = candidates.OrderByDescending((EquipCandidate candidate) => candidate.Gain).FirstOrDefault();
            if (bestCandidate == null)
            {
                return null;
            }
            return bestCandidate;
        }

        private async Task CheckForBag(WoWItem item, ItemInfo itemInfo, IEnumerable<WoWInventorySlot> slots)
        {
            EquipCandidate candidate = SelectBag(item, itemInfo, slots);
            if (candidate != null)
                await EquipCandidate(item, candidate);
        }

        private string GetItemClassDescription(ItemInfo itemInfo)
        {
            WoWItemClass itemClass = itemInfo.ItemClass;
            string subClass;
            switch (itemClass)
            {
            case WoWItemClass.Consumable:
                subClass = itemInfo.ConsumableClass.ToString();
                break;
            case WoWItemClass.Container:
                subClass = itemInfo.ContainerClass.ToString();
                break;
            case WoWItemClass.Weapon:
                subClass = itemInfo.WeaponClass.ToString();
                break;
            case WoWItemClass.Gem:
                subClass = itemInfo.GemClass.ToString();
                break;
            case WoWItemClass.Armor:
            {
                WoWItemArmorClass armorClass = itemInfo.ArmorClass;
                WoWItemArmorClass shownArmorClass = itemInfo.GetShownArmorClass();
                subClass = ((shownArmorClass != armorClass) ? string.Concat(armorClass, " (shown: ", shownArmorClass, ")") : armorClass.ToString());
                break;
            }
            case WoWItemClass.Projectile:
                subClass = itemInfo.ProjectileClass.ToString();
                break;
            case WoWItemClass.TradeGoods:
                subClass = itemInfo.TradeGoodsClass.ToString();
                break;
            case WoWItemClass.Recipe:
                subClass = itemInfo.RecipeClass.ToString();
                break;
            case WoWItemClass.Quiver:
                subClass = itemInfo.QuiverClass.ToString();
                break;
            case WoWItemClass.Reagent:
            case WoWItemClass.Money:
            case WoWItemClass.Quest:
                subClass = itemInfo.SubClassId.ToString();
                break;
            case WoWItemClass.Key:
                subClass = itemInfo.KeyClass.ToString();
                break;
            default:
                return $"Item class: {(int)itemClass}, sub class: {itemInfo.SubClassId}";
            case WoWItemClass.Miscellaneous:
                subClass = itemInfo.MiscClass.ToString();
                break;
            case WoWItemClass.Glyph:
                subClass = itemInfo.GlyphClass.ToString();
                break;
            }
            return $"{itemClass}: {subClass}";
        }

        private bool MatchesWeaponStyle(ItemInfo itemInfo, DetailedWeaponStyle weaponStyle, WoWInventorySlot? slot = null)
        {
            if (weaponStyle == DetailedWeaponStyle.Interchangable && IsRangedWeapon(itemInfo) && CharacterManager.CurrentClassProfile.Class != WoWClass.Hunter)
            {
                return false;
            }
            switch (itemInfo.EquipSlot)
            {
            case InventoryType.Weapon:
                if (weaponStyle != DetailedWeaponStyle.OneHandAndHeld && weaponStyle != DetailedWeaponStyle.OneHandAndShield && weaponStyle != DetailedWeaponStyle.DualWieldOneHanders && weaponStyle != DetailedWeaponStyle.Interchangable && weaponStyle != DetailedWeaponStyle.DualWieldDaggers && weaponStyle != DetailedWeaponStyle.DualWieldWithDaggerInMainHand)
                {
                    return false;
                }
                if (weaponStyle == DetailedWeaponStyle.DualWieldDaggers && itemInfo.WeaponClass != WoWItemWeaponClass.Dagger)
                {
                    return false;
                }
                if (!slot.HasValue)
                {
                    return true;
                }
                if (slot.Value == WoWInventorySlot.MainHand)
                {
                    if (weaponStyle == DetailedWeaponStyle.DualWieldWithDaggerInMainHand)
                    {
                        return itemInfo.WeaponClass == WoWItemWeaponClass.Dagger;
                    }
                    return true;
                }
                if (slot.Value != WoWInventorySlot.OffHand)
                {
                    return false;
                }
                if (weaponStyle != DetailedWeaponStyle.Interchangable && weaponStyle != DetailedWeaponStyle.DualWieldOneHanders && weaponStyle != DetailedWeaponStyle.DualWieldDaggers && weaponStyle != DetailedWeaponStyle.DualWieldWithDaggerInMainHand)
                {
                    return false;
                }
                return CanDualWield();
            case InventoryType.Shield:
                if (weaponStyle != DetailedWeaponStyle.OneHandAndShield && weaponStyle != DetailedWeaponStyle.OneHandAndHeld && weaponStyle != DetailedWeaponStyle.Interchangable)
                {
                    return false;
                }
                if (slot.HasValue)
                {
                    return slot == WoWInventorySlot.OffHand;
                }
                return true;
            case InventoryType.TwoHandWeapon:
                if (weaponStyle != DetailedWeaponStyle.TwoHander && weaponStyle != DetailedWeaponStyle.DualWieldTwoHanders && weaponStyle != DetailedWeaponStyle.Interchangable)
                {
                    return false;
                }
                if (!slot.HasValue)
                {
                    return true;
                }
                if (slot.Value == WoWInventorySlot.MainHand)
                {
                    return true;
                }
                if (slot.Value != WoWInventorySlot.OffHand)
                {
                    return false;
                }
                if (weaponStyle != DetailedWeaponStyle.Interchangable && weaponStyle != DetailedWeaponStyle.DualWieldTwoHanders)
                {
                    return false;
                }
                return CanDualWieldTwoHanders();
            case InventoryType.WeaponMainHand:
                if (weaponStyle != DetailedWeaponStyle.OneHandAndHeld && weaponStyle != DetailedWeaponStyle.OneHandAndShield && weaponStyle != DetailedWeaponStyle.DualWieldOneHanders && weaponStyle != DetailedWeaponStyle.Interchangable && weaponStyle != DetailedWeaponStyle.DualWieldDaggers && weaponStyle != DetailedWeaponStyle.DualWieldWithDaggerInMainHand)
                {
                    return false;
                }
                if (weaponStyle == DetailedWeaponStyle.DualWieldDaggers && itemInfo.WeaponClass != WoWItemWeaponClass.Dagger)
                {
                    return false;
                }
                if (weaponStyle == DetailedWeaponStyle.DualWieldWithDaggerInMainHand && itemInfo.WeaponClass != WoWItemWeaponClass.Dagger)
                {
                    return false;
                }
                if (slot.HasValue)
                {
                    return slot == WoWInventorySlot.MainHand;
                }
                return true;
            case InventoryType.WeaponOffHand:
                if (weaponStyle != DetailedWeaponStyle.DualWieldOneHanders && weaponStyle != DetailedWeaponStyle.Interchangable && weaponStyle != DetailedWeaponStyle.DualWieldDaggers && weaponStyle != DetailedWeaponStyle.DualWieldWithDaggerInMainHand)
                {
                    return false;
                }
                if (weaponStyle == DetailedWeaponStyle.DualWieldDaggers && itemInfo.WeaponClass != WoWItemWeaponClass.Dagger)
                {
                    return false;
                }
                if (slot.HasValue)
                {
                    if (slot == WoWInventorySlot.OffHand)
                    {
                        return CanDualWield();
                    }
                    return false;
                }
                return true;
            case InventoryType.Holdable:
                if (weaponStyle != DetailedWeaponStyle.OneHandAndHeld && weaponStyle != DetailedWeaponStyle.Interchangable)
                {
                    return false;
                }
                if (slot.HasValue)
                {
                    return slot == WoWInventorySlot.OffHand;
                }
                return true;
            default:
                return false;
            case InventoryType.Ranged:
            case InventoryType.RangedRight:
                if (weaponStyle != DetailedWeaponStyle.Ranged && weaponStyle != DetailedWeaponStyle.Interchangable)
                {
                    return false;
                }
                if (slot.HasValue)
                {
                    return slot == WoWInventorySlot.MainHand;
                }
                return true;
            }
        }

        private IEnumerable<EquipCandidate> GetWeaponCandidates(WoWItem candidateItem, ItemInfo itemInfo, ItemStats itemStats, IEnumerable<WoWInventorySlot> slots)
        {
            DetailedWeaponStyle weaponStyle = GetWeaponStyle();
            WeightSet weightSet = CharacterManager.CurrentClassProfile.WeightSet;
            float candidateScore = weightSet.EvaluateItem(itemInfo, itemStats);
            foreach (WoWInventorySlot slot in slots)
            {
                if ((slot == WoWInventorySlot.MainHand || slot == WoWInventorySlot.OffHand) && MatchesWeaponStyle(itemInfo, weaponStyle, slot))
                {
                    WoWItem equippedItem = StyxWoW.Me.Inventory.Equipped.GetEquippedItem(slot);
                    float currentScore = ((equippedItem != null) ? weightSet.EvaluateItem(equippedItem) : 0f);
                    yield return new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, equippedItem, currentScore, slot);
                }
            }
        }

        private IEnumerable<EquipmentSwapSet> GetWeaponSwapSets(WoWItem candidateItem, ItemInfo itemInfo, ItemStats itemStats, WoWInventorySlot slot)
        {
            WeightSet weightSet = CharacterManager.CurrentClassProfile.WeightSet;
            float candidateScore = weightSet.EvaluateItem(itemInfo, itemStats);
            GetEquippedWeapons(out var mainHand, out var offHand);
            if (mainHand == null && offHand == null)
            {
                yield return new EquipmentSwapSet(new EquipCandidate[1]
                {
                    new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, null, 0f, slot)
                });
                yield break;
            }
            if (IsTwoHanded(itemInfo) && mainHand != null && IsTwoHanded(mainHand.ItemInfo))
            {
                float currentScore = weightSet.EvaluateItem(mainHand);
                yield return new EquipmentSwapSet(new EquipCandidate[1]
                {
                    new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, mainHand, currentScore, slot)
                });
                yield break;
            }
            if (IsTwoHanded(itemInfo))
            {
                float mainHandScore = ((mainHand != null) ? weightSet.EvaluateItem(mainHand) : 0f);
                float offHandScore = ((offHand != null) ? weightSet.EvaluateItem(offHand) : 0f);
                yield return new EquipmentSwapSet(new EquipCandidate[2]
                {
                    new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, mainHand, mainHandScore, WoWInventorySlot.MainHand),
                    new EquipCandidate(null, null, null, 0f, offHand, offHandScore, WoWInventorySlot.OffHand)
                });
                yield break;
            }
            if (mainHand != null && IsTwoHanded(mainHand.ItemInfo))
            {
                float mainHandScore = weightSet.EvaluateItem(mainHand);
                WoWInventorySlot otherSlot = ((slot == WoWInventorySlot.OffHand) ? WoWInventorySlot.MainHand : WoWInventorySlot.OffHand);
                Func<WoWItem, EquipmentSwapSet> createSwapSet = delegate(WoWItem otherItem)
                {
                    float otherScore = ((otherItem != null) ? weightSet.EvaluateItem(otherItem) : 0f);
                    ItemInfo candidateItemInfo = ((otherItem != null) ? otherItem.ItemInfo : null);
                    ItemStats candidateItemStats = ((otherItem != null) ? otherItem.ItemStats : null);
                    EquipCandidate mainHandCandidate;
                    EquipCandidate offHandCandidate;
                    if (slot == WoWInventorySlot.MainHand)
                    {
                        mainHandCandidate = new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, mainHand, mainHandScore, WoWInventorySlot.MainHand);
                        offHandCandidate = new EquipCandidate(otherItem, candidateItemInfo, candidateItemStats, otherScore, null, 0f, WoWInventorySlot.OffHand);
                    }
                    else
                    {
                        mainHandCandidate = new EquipCandidate(otherItem, candidateItemInfo, candidateItemStats, otherScore, mainHand, mainHandScore, WoWInventorySlot.MainHand);
                        offHandCandidate = new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, null, 0f, WoWInventorySlot.OffHand);
                    }
                    return new EquipmentSwapSet(new EquipCandidate[2] { mainHandCandidate, offHandCandidate });
                };
                yield return createSwapSet(null);
                foreach (WoWItem bagItem in StyxWoW.Me.BagItems)
                {
                    if (bagItem.ItemInfo != null && CanConsiderItem(bagItem.ItemInfo, log: false, out var reason, out var _) && !ShouldDisregardItem(bagItem, bagItem.ItemInfo, out reason) && MatchesWeaponStyle(bagItem.ItemInfo, DetailedWeaponStyle.Interchangable, otherSlot) && !IsTwoHanded(bagItem.ItemInfo))
                    {
                        yield return createSwapSet(bagItem);
                    }
                }
                yield break;
            }
            WoWItem equippedItem = StyxWoW.Me.Inventory.Equipped.GetEquippedItem(slot);
            float equippedScore = ((equippedItem != null) ? weightSet.EvaluateItem(equippedItem) : 0f);
            yield return new EquipmentSwapSet(new EquipCandidate[1]
            {
                new EquipCandidate(candidateItem, itemInfo, itemStats, candidateScore, equippedItem, equippedScore, slot)
            });
        }

        private bool CanForceWeaponSlot(ItemInfo itemInfo, WoWInventorySlot slot)
        {
            GetEquippedWeapons(out var mainHand, out var _);
            if (mainHand == null)
            {
                return true;
            }
            if (CanDualWieldTwoHanders())
            {
                return true;
            }
            if (slot == WoWInventorySlot.OffHand && !itemInfo.IsWeapon)
            {
                if (!IsTwoHanded(mainHand.ItemInfo))
                {
                    return true;
                }
                switch (CharacterManager.CurrentClassProfile.GetSpec())
                {
                default:
                    return true;
                case WoWSpec.PaladinProtection:
                case WoWSpec.PaladinRetribution:
                case WoWSpec.WarriorArms:
                case WoWSpec.WarriorFury:
                case WoWSpec.WarriorProtection:
                case WoWSpec.ShamanEnhancement:
                    return false;
                }
            }
            return true;
        }

        private bool TryGetForcedWeaponSlot(DetailedWeaponStyle weaponStyle, ItemInfo itemInfo, out WoWInventorySlot slot)
        {
            slot = WoWInventorySlot.Head;
            if (weaponStyle == DetailedWeaponStyle.Interchangable)
            {
                return false;
            }
            WoWPaperDoll equipped = StyxWoW.Me.Inventory.Equipped;
            foreach (WoWInventorySlot candidateSlot in new WoWInventorySlot[2]
            {
                WoWInventorySlot.MainHand,
                WoWInventorySlot.OffHand
            }.OrderByDescending((WoWInventorySlot inventorySlot) => equipped.GetEquippedItem(inventorySlot) == null))
            {
                WoWItem equippedItem = equipped.GetEquippedItem(candidateSlot);
                if ((!(equippedItem != null) || !MatchesWeaponStyle(equippedItem.ItemInfo, weaponStyle, candidateSlot)) && MatchesWeaponStyle(itemInfo, weaponStyle, candidateSlot))
                {
                    slot = candidateSlot;
                    return true;
                }
            }
            return false;
        }

        private EquipResult SelectWeapon(WoWItem candidateItem, ItemInfo itemInfo, ItemStats itemStats, List<WoWInventorySlot> slots)
        {
            if (!CharacterSettings.Instance.AutoEquipWeapons)
            {
                return null;
            }
            if (CharacterManager.CurrentClassProfile == null)
            {
                Log(Colors.Red, "No class profile is loaded; cannot equip weapons/armor");
                return null;
            }
            DetailedWeaponStyle detailedWeaponStyle = GetWeaponStyle();
            if (!MatchesWeaponStyle(itemInfo, detailedWeaponStyle))
            {
                return null;
            }
            if (TryGetForcedWeaponSlot(detailedWeaponStyle, itemInfo, out var forcedSlot))
            {
                if (!CanForceWeaponSlot(itemInfo, forcedSlot))
                {
                    return null;
                }
                return EquipResult.CreateForceSlot(forcedSlot);
            }
            if (detailedWeaponStyle != DetailedWeaponStyle.Interchangable)
            {
                EquipCandidate[] candidates = GetWeaponCandidates(candidateItem, itemInfo, itemStats, slots).ToArray();
                EquipCandidate bestCandidate = (from candidate in candidates
                    orderby candidate.Gain descending, candidate.CurrentItem == null descending
                    select candidate).FirstOrDefault();
                if (bestCandidate == null)
                {
                    return null;
                }
                return EquipResult.CreateSingleSlot(bestCandidate);
            }
            _ = slots.Count;
            EquipmentSwapSet[] swapSets = GetWeaponSwapSets(candidateItem, itemInfo, itemStats, slots[0]).ToArray();
            EquipmentSwapSet bestSwapSet = (from swapSet in swapSets
                orderby swapSet.Gain descending, swapSet.EquipScenarios.Count((EquipCandidate candidate) => candidate.CurrentItem == null) descending
                select swapSet).FirstOrDefault();
            if (bestSwapSet == null)
            {
                return null;
            }
            string.Join(" + ", from candidate in bestSwapSet.EquipScenarios
                where candidate.CandidateItem != null
                select $"{candidate.CandidateItem.Name} ({candidate.CandidateScore:F2})");
            if (bestSwapSet.EquipScenarios.Any((EquipCandidate candidate) => candidate.CurrentItem != null))
            {
                string.Join(" + ", from candidate in bestSwapSet.EquipScenarios
                    where candidate.CurrentItem != null
                    select $"{candidate.CurrentItem.Name} ({candidate.CurrentScore:F2})");
            }
            return EquipResult.CreateInterchangable(bestSwapSet);
        }

        private async Task CheckForWeapons(WoWItem item, ItemInfo itemInfo, List<WoWInventorySlot> slots)
        {
            EquipResult result = SelectWeapon(item, itemInfo, item.ItemStats, slots);
            if (result == null)
                return;

            if (result.Type == EquipResultType.ForceSlot)
            {
                Log(Colors.LawnGreen, "  Equipping {0} into {1} as this weapon is more compatible with our weapon style ({2})",
                    item.Name, result.ForcedSlot, GetWeaponStyle());
                await EquipItemIntoSlot(item, result.ForcedSlot);
                ResetCheckTimers();
                return;
            }

            if (result.Type == EquipResultType.SingleSlot)
            {
                await EquipCandidate(item, result.EquipScenario);
                return;
            }

            EquipmentSwapSet swapSet = result.InterchangableEquipScenario;
            if (!(swapSet.Gain > 0f))
                return;

            Log(Colors.LawnGreen, "Equipping new weapons because we gain {0:F2} score", swapSet.Gain);
            foreach (EquipCandidate candidate in swapSet.EquipScenarios)
            {
                Log(Colors.LawnGreen, "  {0}: {1} ({2:F2}) instead of {3} ({4:F2}) - gain {5:F2}",
                    candidate.Slot, candidate.CandidateItem != null ? candidate.CandidateItem.Name : "nothing",
                    candidate.CandidateScore, candidate.CurrentItem != null ? candidate.CurrentItem.Name : "nothing",
                    candidate.CurrentScore, candidate.Gain);
                if (candidate.CandidateItem != null)
                    await EquipItemIntoSlot(candidate.CandidateItem, candidate.Slot);
            }
        }

        private async Task CheckInventory()
        {
            while (true)
            {
                Stopwatch timer = Stopwatch.StartNew();
                List<ulong> items = new List<ulong>();
                foreach (ulong guid in StyxWoW.Me.BagItemGuids)
                {
                    if (!_disregardedItems.TryGetValue(guid, out DisregardEntry entry) ||
                        !entry.CheckTimer.IsRunning || entry.CheckTimer.ElapsedMilliseconds >= DisregardTime)
                        items.Add(guid);
                }

                foreach (ulong guid in items)
                {
                    await CheckItem(guid);
                    await Coroutine.Yield();
                }

                int delay = TargetCheckTime - (int)timer.ElapsedMilliseconds;
                if (delay > 0)
                    await Coroutine.Sleep(delay);
                await Coroutine.Yield();
            }
        }

        private void ResetCheckTimers()
        {
            foreach (DisregardEntry value in _disregardedItems.Values)
            {
                value.CheckTimer.Reset();
            }
        }

        private void OnBotStarted(EventArgs args)
        {
            if (StyxWoW.IsInGame && !(StyxWoW.Me == null) && CharacterSettings.Instance.AutoEquip)
            {
                _ = StyxWoW.Me.Specialization;
                _ = CharacterManager.CurrentClassProfile;
            }
        }

        private void OnBotStopped(EventArgs args)
        {
            DisposeCoroutine(ref _equipmentCoroutine);
            _disregardedItems.Clear();
            DisposeLootRolls();
        }

        private void OnLevelUp(BotEvents.Player.LevelUpEventArgs args)
        {
            ResetCheckTimers();
        }

        private void DisposeCoroutine(ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                coroutine.Dispose();
                coroutine = null;
            }
        }

        internal void Initialize()
        {
            BotEvents.OnBotStarted += OnBotStarted;
            BotEvents.OnBotStopped += OnBotStopped;
            BotEvents.Player.OnLevelUp += OnLevelUp;
            AttachLootEvents();
        }

        internal void Shutdown()
        {
            DetachLootEvents();
            BotEvents.OnBotStarted -= OnBotStarted;
            BotEvents.OnBotStopped -= OnBotStopped;
            BotEvents.Player.OnLevelUp -= OnLevelUp;
        }

        private void PulseLootRolls()
        {
            if (!CharacterSettings.Instance.RollOnItems)
            {
                DisposeLootRolls();
                return;
            }
            if (_lootCoroutine == null || _lootCoroutine.IsFinished)
            {
                _lootCoroutine = new Coroutine(() => ProcessLootRolls());
            }
            try
            {
                _lootCoroutine.Resume();
            }
            catch (CoroutineException ex)
            {
                Log(Colors.Red, "Exception was thrown in Loot roll coroutine!\r\n{0}", ex.ToString());
            }
        }

        private void DisposeLootRolls()
        {
            DisposeCoroutine(ref _lootCoroutine);
            foreach (Coroutine item in _lootRolls)
            {
                item.Dispose();
            }
            _lootRolls.Clear();
        }

        private void AttachLootEvents()
        {
            Lua.Events.AttachEvent("START_LOOT_ROLL", HandleLootRoll);
        }

        private void DetachLootEvents()
        {
            Lua.Events.DetachEvent("START_LOOT_ROLL", HandleLootRoll);
        }

        private async Task ProcessLootRolls()
        {
            while (true)
            {
                for (int i = _lootRolls.Count - 1; i >= 0; i--)
                {
                    Coroutine coroutine = _lootRolls[i];
                    coroutine.Resume();
                    if (coroutine.IsFinished)
                    {
                        _lootRolls.RemoveAt(i);
                        await Coroutine.Sleep(750);
                    }
                }
                await Coroutine.Yield();
            }
        }

        private LootRollItem GetLootRollItem(int rollId)
        {
            List<string> returnValues = Lua.GetReturnValues(string.Format("return GetLootRollItemLink({0}), GetLootRollItemInfo({0})", rollId));
            try
            {
                if (returnValues.Count <= 8)
                {
                    Log(Colors.Red, "Could not parse loot roll item info!");
                    return null;
                }
                string itemLink = Lua.ParseLuaValue<string>(returnValues[0]);
                ItemInfo itemInfo = ItemInfo.FromLink(itemLink);
                if (itemInfo == null)
                {
                    Log(Colors.Red, "Could not get item info from item link ({0})", itemLink);
                    return null;
                }
                LootRollItem item = new LootRollItem();
                item.ItemLink = itemLink;
                item.ItemInfo = itemInfo;
                item.Name = Lua.ParseLuaValue<string>(returnValues[2]);
                item.CanNeed = Lua.ParseLuaValue<bool>(returnValues[6]);
                item.CanGreed = Lua.ParseLuaValue<bool>(returnValues[7]);
                item.CanDisenchant = Lua.ParseLuaValue<bool>(returnValues[8]);
                return item;
            }
            catch (Exception ex)
            {
                Log(Colors.Red, "Could not parse loot roll item info");
                Logging.WriteDiagnostic(Colors.Red, ex.ToString());
                return null;
            }
        }

        private StatType? GetPrimaryStat()
        {
            WoWSpec spec = CharacterManager.CurrentClassProfile.GetSpec();
            switch (spec)
            {
            case WoWSpec.DruidFeral:
            case WoWSpec.DruidGuardian:
            case WoWSpec.HunterBeastMastery:
            case WoWSpec.HunterMarksmanship:
            case WoWSpec.HunterSurvival:
            case WoWSpec.RogueAssassination:
            case WoWSpec.RogueCombat:
            case WoWSpec.RogueSubtlety:
            case WoWSpec.ShamanEnhancement:
            case WoWSpec.MonkBrewmaster:
            case WoWSpec.MonkWindwalker:
                return StatType.Agility;
            case WoWSpec.MageArcane:
            case WoWSpec.MageFire:
            case WoWSpec.MageFrost:
            case WoWSpec.PaladinHoly:
            case WoWSpec.DruidBalance:
            case WoWSpec.DruidRestoration:
            case WoWSpec.PriestDiscipline:
            case WoWSpec.PriestHoly:
            case WoWSpec.PriestShadow:
            case WoWSpec.ShamanElemental:
            case WoWSpec.ShamanRestoration:
            case WoWSpec.WarlockAffliction:
            case WoWSpec.WarlockDemonology:
            case WoWSpec.WarlockDestruction:
            case WoWSpec.MonkMistweaver:
                return StatType.Intellect;
            default:
                Log(Colors.Red, "Cannot get primary stat; current spec '{0}' is unknown", spec);
                return null;
            case WoWSpec.PaladinProtection:
            case WoWSpec.PaladinRetribution:
            case WoWSpec.WarriorArms:
            case WoWSpec.WarriorFury:
            case WoWSpec.WarriorProtection:
            case WoWSpec.DeathKnightBlood:
            case WoWSpec.DeathKnightFrost:
            case WoWSpec.DeathKnightUnholy:
                return StatType.Strength;
            }
        }

        private IEnumerable<StatType> GetUnwantedSecondaryStats()
        {
            WoWSpec spec = CharacterManager.CurrentClassProfile.GetSpec();
            switch (spec)
            {
            case WoWSpec.PaladinHoly:
            case WoWSpec.DruidRestoration:
            case WoWSpec.PriestDiscipline:
            case WoWSpec.PriestHoly:
            case WoWSpec.ShamanRestoration:
            case WoWSpec.MonkMistweaver:
                yield return StatType.Dodge;
                yield return StatType.Parry;
                yield return StatType.Hit;
                yield return StatType.Expertise;
                break;
            default:
                Log(Colors.Red, "Cannot get unwanted secondary stats; current spec '{0}' is unknown", spec);
                break;
            case WoWSpec.MageArcane:
            case WoWSpec.MageFire:
            case WoWSpec.MageFrost:
            case WoWSpec.PaladinRetribution:
            case WoWSpec.WarriorArms:
            case WoWSpec.WarriorFury:
            case WoWSpec.DruidBalance:
            case WoWSpec.DruidFeral:
            case WoWSpec.DeathKnightFrost:
            case WoWSpec.DeathKnightUnholy:
            case WoWSpec.HunterBeastMastery:
            case WoWSpec.HunterMarksmanship:
            case WoWSpec.HunterSurvival:
            case WoWSpec.PriestShadow:
            case WoWSpec.RogueAssassination:
            case WoWSpec.RogueCombat:
            case WoWSpec.RogueSubtlety:
            case WoWSpec.ShamanElemental:
            case WoWSpec.ShamanEnhancement:
            case WoWSpec.WarlockAffliction:
            case WoWSpec.WarlockDemonology:
            case WoWSpec.WarlockDestruction:
            case WoWSpec.MonkBrewmaster:
            case WoWSpec.MonkWindwalker:
                yield return StatType.Spirit;
                yield return StatType.Dodge;
                yield return StatType.Parry;
                break;
            case WoWSpec.PaladinProtection:
            case WoWSpec.WarriorProtection:
            case WoWSpec.DruidGuardian:
            case WoWSpec.DeathKnightBlood:
                yield return StatType.Spirit;
                break;
            }
        }

        private RollType GetGreedRollType(LootRollItem rollItem)
        {
            if (rollItem.CanGreed && !rollItem.CanDisenchant)
            {
                return RollType.Greed;
            }
            if (rollItem.CanDisenchant && !rollItem.CanGreed)
            {
                return RollType.Disenchant;
            }
            if (!CharacterSettings.Instance.RollDisenchantWhenGreed)
            {
                return RollType.Greed;
            }
            if (rollItem.ItemInfo.Bond != WoWItemBondType.OnEquip)
            {
                return RollType.Disenchant;
            }
            WoWItemQuality quality = rollItem.ItemInfo.Quality;
            if (quality <= CharacterSettings.Instance.MaxBoEDisenchantQuality)
            {
                return RollType.Disenchant;
            }
            return RollType.Greed;
        }

        private bool CanRollForItem(ItemInfo itemInfo, ItemStats itemStats, out string reason, out bool delayNeed)
        {
            reason = null;
            delayNeed = false;
            if (itemInfo.EquipSlot == InventoryType.Trinket)
            {
                reason = "Is trinket";
                return false;
            }
            if (itemInfo.ItemClass == WoWItemClass.Armor)
            {
                WoWItemArmorClass shownArmorClass = itemInfo.GetShownArmorClass();
                if (shownArmorClass >= WoWItemArmorClass.Cloth && shownArmorClass <= WoWItemArmorClass.Plate)
                {
                    WoWItemArmorClass preferredArmorClass = WeightSet.GetArmorClassForCurrentClass();
                    if (shownArmorClass != preferredArmorClass)
                    {
                        reason = $"Is {shownArmorClass} and we want {preferredArmorClass}";
                        return false;
                    }
                }
            }
            if (itemInfo.ItemClass != WoWItemClass.Container)
            {
                StatType? primaryStat = GetPrimaryStat();
                if (!primaryStat.HasValue)
                {
                    reason = "Couldn't get primary stat";
                    return false;
                }
                Func<StatType, bool> hasStat = (StatType stat) => itemStats.Stats.TryGetValue(stat, out var value) && value > 0f;
                if (!hasStat(primaryStat.Value) && new StatType[3]
                {
                    StatType.Agility,
                    StatType.Strength,
                    StatType.Intellect
                }.Any(hasStat))
                {
                    reason = $"Doesn't have our primary stat ({primaryStat.Value})";
                    return false;
                }
                foreach (StatType stat in GetUnwantedSecondaryStats())
                {
                    if (hasStat(stat))
                    {
                        reason = "has unwanted secondary stat " + stat;
                        delayNeed = true;
                        break;
                    }
                }
            }
            return true;
        }

        private LootRollDecision SelectLootRoll(LootRollItem item)
        {
            int availableRolls = 0;
            if (item.CanNeed)
                availableRolls++;
            if (item.CanGreed)
                availableRolls++;
            if (item.CanDisenchant)
                availableRolls++;

            List<string> rollNames = new List<string>();
            if (item.CanNeed)
                rollNames.Add("Need");
            if (item.CanGreed)
                rollNames.Add("Greed");
            if (item.CanDisenchant)
                rollNames.Add("Disenchant");

            if (availableRolls == 0)
                return LootRollDecision.Create(RollType.Pass);
            if (availableRolls == 1)
            {
                if (item.CanNeed)
                    return LootRollDecision.Create(RollType.Need);
                if (item.CanDisenchant)
                    return LootRollDecision.Create(RollType.Disenchant);
                return LootRollDecision.Create(RollType.Greed);
            }

            if (CharacterManager.CurrentClassProfile == null || !CharacterSettings.Instance.AutoEquip)
                return LootRollDecision.Create(GetGreedRollType(item));
            if (!CanConsiderItem(item.ItemInfo, true, out string reason, out List<WoWInventorySlot> slots))
                return LootRollDecision.Create(GetGreedRollType(item));

            ItemStats itemStats = ItemStats.FromLink(item.ItemLink);
            if (itemStats == null)
            {
                Log(Colors.Red, "Got null ItemStats when deciding on loot roll!");
                return LootRollDecision.Create(GetGreedRollType(item));
            }
            if (!CanRollForItem(item.ItemInfo, itemStats, out reason, out bool delayNeed))
                return LootRollDecision.Create(GetGreedRollType(item));

            bool forced;
            float gain;
            WoWItemClass equipmentClass = GetEquipmentClass(item.ItemInfo.EquipSlot);
            switch (equipmentClass)
            {
                case WoWItemClass.Weapon:
                    EquipResult result = SelectWeapon(null, item.ItemInfo, itemStats, slots);
                    if (result == null)
                        return LootRollDecision.Create(GetGreedRollType(item));
                    forced = result.Type == EquipResultType.ForceSlot;
                    gain = forced ? 0f : result.Type == EquipResultType.SingleSlot
                        ? result.EquipScenario.Gain : result.InterchangableEquipScenario.Gain;
                    break;
                case WoWItemClass.Container:
                case WoWItemClass.Armor:
                    EquipCandidate candidate = equipmentClass == WoWItemClass.Container
                        ? SelectBag(null, item.ItemInfo, slots)
                        : SelectArmor(null, item.ItemInfo, itemStats, slots);
                    if (candidate == null)
                        return LootRollDecision.Create(GetGreedRollType(item));
                    gain = candidate.Gain;
                    forced = false;
                    break;
                default:
                    return LootRollDecision.Create(GetGreedRollType(item));
            }

            if (!forced && gain <= 1E-05f)
                return LootRollDecision.Create(GetGreedRollType(item));
            if (delayNeed)
                return LootRollDecision.CreateDelayedNeed(reason, gain, forced);
            return LootRollDecision.CreateNeed(gain, forced);
        }

        private void HandleLootRoll(object sender, LuaEventArgs e)
        {
            if (!CharacterSettings.Instance.RollOnItems || (BotManager.Current != null && (BotManager.Current.PulseFlags & PulseFlags.CharacterManager) == 0))
            {
                return;
            }
            if (e.Args.Length >= 1 && int.TryParse(e.Args[0].ToString(), out var rollId))
            {
                using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
                {
                    LootRollItem rollItem = GetLootRollItem(rollId);
                    LootRollDecision decision = SelectLootRoll(rollItem);
                    Log(Colors.LawnGreen, "  {0}", decision.GetDescription(rollItem, this));
                    Coroutine coroutine = CreateLootRollCoroutine(rollId, rollItem, decision.Type);
                    coroutine.Resume();
                    if (!coroutine.IsFinished)
                    {
                        _lootRolls.Add(coroutine);
                    }
                    return;
                }
            }
            Log(Colors.Red, "Failed to parse loot roll ID in START_LOOT_ROLL handler");
        }

        private Coroutine CreateLootRollCoroutine(int rollId, LootRollItem rollItem, RollType rollType)
        {
            if (rollType == RollType.DelayedNeed)
            {
                return new Coroutine(() => RollDelayedNeed(rollId, rollItem));
            }
            return new Coroutine(() => RollAfterDelay(rollId, rollItem, rollType));
        }

        private bool HaveOtherPlayersDecided(int rollId, out bool someoneNeeded)
        {
            List<string> returnValues = Lua.GetReturnValues($"\r\nlocal numItems = C_LootHistory.GetNumItems()\r\nfor i=1, numItems do\r\n\tlocal rollID, itemLink, numPlayers, isDone, winnerIndex = C_LootHistory.GetItem(i)\r\n\tif rollID == {rollId} then\r\n\t\tfor j=1, numPlayers do\r\n\t\t\tlocal name, class, rollType, roll, isWinner, isMe = C_LootHistory.GetPlayerInfo(i, j)\r\n\t\t\tif not isMe then\r\n\t\t\t\tif rollType == LOOT_ROLL_TYPE_NEED then\r\n\t\t\t\t\treturn 1, 1 -- Decided, someone rolled need\r\n\t\t\t\tend\r\n\r\n\t\t\t\tif rollType ~= LOOT_ROLL_TYPE_PASS and rollType ~= LOOT_ROLL_TYPE_GREED\r\n\t\t\t\t\tand rollType ~= LOOT_ROLL_TYPE_DISENCHANT then\r\n\t\t\t\t\treturn 0, 0 -- Not decided yet\r\n\t\t\t\tend\r\n\t\t\tend\r\n\t\tend\r\n\tend\r\nend\r\n\r\nreturn 1, 0 -- Decided, no one rolled need\r\n");
            someoneNeeded = Lua.ParseLuaValue<bool>(returnValues[1]);
            return Lua.ParseLuaValue<bool>(returnValues[0]);
        }

        private bool ConfirmLootRoll(int rollId, RollType rollType)
        {
            int luaRollType;
            switch (rollType)
            {
                case RollType.Pass:
                    luaRollType = 0;
                    break;
                case RollType.Need:
                case RollType.DelayedNeed:
                    luaRollType = 1;
                    break;
                case RollType.Greed:
                    luaRollType = 2;
                    break;
                case RollType.Disenchant:
                    luaRollType = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            return Lua.GetReturnVal<bool>(string.Format("if GetLootRollItemInfo({0}) then ConfirmLootRoll({0}, {1}) return 1 end return 0",
                rollId, luaRollType), 0);
        }

        private async Task RollDelayedNeed(int rollId, LootRollItem item)
        {
            int delay = _random.Next(MinimumDecisionDelay, MaximumDecisionDelay);
            Log(Colors.LawnGreen, "    waiting {0} seconds for people to decide..", delay / 1000);
            await Coroutine.Yield();

            bool someoneNeeded = false;
            bool decided = await Coroutine.Wait(delay, () => HaveOtherPlayersDecided(rollId, out someoneNeeded));
            if (decided)
            {
                delay = _random.Next(MinimumDelayedRollDelay, MaximumDelayedRollDelay);
                RollType rollType;
                if (!someoneNeeded)
                {
                    rollType = RollType.Need;
                    Log(Colors.LawnGreen, "No one rolled need on {0}. Will roll {1} in {2} seconds.",
                        item.Name, RollType.Need, delay / 1000);
                }
                else
                {
                    rollType = GetGreedRollType(item);
                    Log(Colors.LawnGreen, "Someone rolled need on {0}. Will roll {1} in {2} seconds.",
                        item.Name, rollType, delay / 1000);
                }
                await Coroutine.Sleep(delay);
                ConfirmLootRoll(rollId, rollType);
            }
            else
            {
                RollType rollType = GetGreedRollType(item);
                if (ConfirmLootRoll(rollId, rollType))
                    Log(Colors.LawnGreen, "Rolling {0} on {1} as someone has not decided yet.", rollType, item.Name);
            }
        }

        private async Task RollAfterDelay(int rollId, LootRollItem item, RollType rollType)
        {
            int delay = _random.Next(MinimumRollDelay, MaximumRollDelay);
            Log(Colors.LawnGreen, "    will do roll in {0} seconds..", delay / 1000);
            await Coroutine.Sleep(delay);
            ConfirmLootRoll(rollId, rollType);
        }

        private bool HasCursorItem()
        {
            return Lua.GetReturnVal<bool>("if GetCursorInfo() then return 1 else return 0 end", 0u);
        }

        private bool IsBagItemLocked(int bag, int slot)
        {
            if (bag < -1 || bag > 4)
            {
                throw new ArgumentOutOfRangeException("bag");
            }
            return Lua.GetReturnVal<bool>($"return GetContainerItemInfo({bag + 1}, {slot + 1})", 2u);
        }

        private bool IsInventoryItemLocked(WoWInventorySlot slot)
        {
            return Lua.GetReturnVal<bool>($"return IsInventoryItemLocked({(int)(slot + 1)})", 0u);
        }

        private async Task<bool> MoveBagToBackpack(WoWItem item)
        {
            int bagIndex = item.BagIndex;
            int bagSlot = item.BagSlot;
            Lua.DoString($"PickupContainerItem({bagIndex + 1}, {bagSlot + 1})");
            int backpackSlot = StyxWoW.Me.Inventory.Backpack.IndexOf(0UL);
            if (backpackSlot == -1)
            {
                Log(Colors.LawnGreen, "  Swapping with first item in backpack first");
                backpackSlot = 0;
            }
            else
                Log(Colors.LawnGreen, "  Moving into backpack first", backpackSlot + 1);

            Lua.DoString($"PickupContainerItem(0, {backpackSlot + 1})");
            await Coroutine.Sleep(500);
            bool unlocked = await Coroutine.Wait(5000, () => !IsBagItemLocked(bagIndex, bagSlot));
            await Coroutine.Sleep(500);
            return unlocked && item.IsValid && item.BagIndex == -1 && item.BagSlot == backpackSlot;
        }

        private async Task<bool> UnequipTwoHander()
        {
            int bagIndex = -1;
            int bagSlot = StyxWoW.Me.Inventory.Backpack.IndexOf(0UL);
            if (bagSlot == -1)
            {
                for (WoWBagSlot slot = WoWBagSlot.Bag1; slot <= WoWBagSlot.Bag4; slot++)
                {
                    WoWContainer bag = StyxWoW.Me.GetBag(slot);
                    if (bag != null && bag.ItemInfo != null && bag.ItemInfo.BagFamily == 0)
                    {
                        bagIndex = (int)slot;
                        bagSlot = bag.Bag.IndexOf(0UL);
                        if (bagSlot != -1)
                            break;
                    }
                }
            }

            if (bagSlot == -1)
            {
                Log(Colors.Red, "  Cannot equip because we cannot unequip our two hander as the inventory is full");
                return false;
            }

            Log(Colors.LawnGreen, "  Unequipping two hander into bag {0} slot {1} first..", bagIndex + 1, bagSlot + 1);
            Lua.DoString("PickupInventoryItem(16)");
            Lua.DoString($"PickupContainerItem({bagIndex + 1}, {bagSlot + 1})");
            await Coroutine.Sleep(500);
            bool unlocked = await Coroutine.Wait(5000, () => !IsInventoryItemLocked(WoWInventorySlot.MainHand));
            await Coroutine.Sleep(500);
            return unlocked && StyxWoW.Me.Inventory.Equipped.GetEquippedItem(WoWInventorySlot.MainHand) == null;
        }

        private async Task<bool> ConfirmItemBinding()
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < 1500)
            {
                foreach (string popup in new[] { "EQUIP_BIND", "AUTOEQUIP_BIND" })
                {
                    if (!Lua.GetReturnVal<bool>($"return StaticPopup_Visible('{popup}')", 0))
                        continue;
                    await Coroutine.Sleep(750);
                    Lua.DoString($"local name, frame = StaticPopup_Visible('{popup}') if frame then StaticPopup_OnClick(frame, 1) end");
                    return true;
                }
                await Coroutine.Yield();
            }
            return false;
        }

        private async Task EquipItemIntoSlot(WoWItem item, WoWInventorySlot slot)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (!item.IsValid)
                {
                    Log(Colors.Red, "  Item became invalid");
                    return;
                }

                if (HasCursorItem())
                {
                    Lua.DoString("ClearCursor()");
                    if (!await Coroutine.Wait(2000, () => !HasCursorItem()))
                    {
                        Log(Colors.Red, "  Cursor was not successfully cleared before trying to equip! Trying again..");
                        await Coroutine.Sleep(1000);
                        continue;
                    }
                    if (!item.IsValid)
                    {
                        Log(Colors.Red, "  Item became invalid");
                        return;
                    }
                }

                int bagIndex = item.BagIndex;
                int bagSlot = item.BagSlot;
                if (bagIndex == -1 && item.ContainerGuid != StyxWoW.Me.Guid)
                {
                    Log(Colors.Red, "  Cannot equip - bag slot is -1 but the container GUID for this item is not the local player");
                    return;
                }

                if (slot >= WoWInventorySlot.Bag1 && slot <= WoWInventorySlot.Bag4)
                {
                    WoWInventorySlot containingBagSlot = (WoWInventorySlot)(19 + bagIndex);
                    if (slot == containingBagSlot)
                    {
                        if (!await MoveBagToBackpack(item))
                        {
                            Log(Colors.Red, "  Failed when moving bag to backpack, trying again");
                            await Coroutine.Sleep(1000);
                            continue;
                        }
                        bagIndex = item.BagIndex;
                        bagSlot = item.BagSlot;
                    }
                }

                if (slot == WoWInventorySlot.OffHand)
                {
                    GetEquippedWeapons(out WoWItem mainHand, out WoWItem offHand);
                    if (mainHand != null && IsTwoHanded(mainHand.ItemInfo) && offHand == null && !CanDualWieldTwoHanders())
                    {
                        if (!await UnequipTwoHander())
                        {
                            Log(Colors.Red, "  Failed while unequipping two-hander, trying again in 1 second");
                            await Coroutine.Sleep(1000);
                            continue;
                        }
                    }
                }

                Lua.DoString($"PickupContainerItem({bagIndex + 1}, {bagSlot + 1})");
                Lua.DoString($"PickupInventoryItem({(int)slot + 1})");
                if (!item.IsSoulbound && item.ItemInfo.Bond == WoWItemBondType.OnEquip)
                {
                    await ConfirmItemBinding();
                    await Coroutine.Sleep(500);
                }
                await Coroutine.Wait(5000, () => !IsBagItemLocked(bagIndex, bagSlot));
                await Coroutine.Sleep(750);
                return;
            }
        }

        internal QuestRewardItem GetBestQuestReward(QuestRewardItem[] rewards)
        {
            QuestRewardItem bestReward = null;
            float bestScore = float.MinValue;
            bool forced = false;
            foreach (QuestRewardItem reward in rewards)
            {
                ItemInfo itemInfo = reward.ItemInfo;
                if (!CanConsiderItem(itemInfo, log: true, out var _, out var slots))
                {
                    continue;
                }
                WoWItemClass woWItemClass = GetEquipmentClass(itemInfo.EquipSlot);
                ItemStats itemStats = null;
                if (woWItemClass != WoWItemClass.Container)
                {
                    itemStats = ItemStats.FromLink(reward.ItemLink);
                    if (itemStats == null)
                    {
                        continue;
                    }
                }
                switch (woWItemClass)
                {
                case WoWItemClass.Weapon:
                {
                    EquipResult result = SelectWeapon(null, itemInfo, itemStats, slots);
                    if (result == null)
                    {
                        break;
                    }
                    float score;
                    if (result.Type == EquipResultType.ForceSlot)
                    {
                        if (!forced)
                        {
                            forced = true;
                            bestScore = float.MinValue;
                        }
                        score = CharacterManager.CurrentClassProfile.WeightSet.EvaluateItem(itemInfo, itemStats);
                    }
                    else
                    {
                        if (forced)
                        {
                            break;
                        }
                        score = ((result.Type != EquipResultType.SingleSlot) ? result.InterchangableEquipScenario.Gain : result.EquipScenario.Gain);
                    }
                    if (score > bestScore)
                    {
                        bestReward = reward;
                        bestScore = score;
                    }
                    break;
                }
                case WoWItemClass.Container:
                case WoWItemClass.Armor:
                    if (!forced)
                    {
                        EquipCandidate candidate = ((woWItemClass == WoWItemClass.Armor) ? SelectArmor(null, itemInfo, itemStats, slots) : SelectBag(null, itemInfo, slots));
                        if (candidate != null && candidate.Gain > bestScore)
                        {
                            bestReward = reward;
                            bestScore = candidate.Gain;
                        }
                    }
                    break;
                }
            }
            if (!(bestScore <= 0f) && bestReward != null)
            {
                if (forced)
                {
                    Log(Colors.LawnGreen, "Selecting {0} as the quest reward because it is more compatible with our weapon style ({1}), and it scores the highest ({2:F2})", bestReward.ItemInfo.Name, GetWeaponStyle(), bestScore);
                }
                else
                {
                    Log(Colors.LawnGreen, "Selecting {0} as the quest reward, as it yields the highest gain ({1:F2})", bestReward.ItemInfo.Name, bestScore);
                }
                return bestReward;
            }
            return null;
        }

        private bool IsTwoHanded(ItemInfo itemInfo)
        {
            return itemInfo.InventoryType == InventoryType.TwoHandWeapon;
        }

        private bool IsRangedWeapon(ItemInfo itemInfo)
        {
            switch (itemInfo.WeaponClass)
            {
            default:
                return false;
            case WoWItemWeaponClass.Bow:
            case WoWItemWeaponClass.Gun:
            case WoWItemWeaponClass.Crossbow:
                return true;
            }
        }

        private DetailedWeaponStyle GetEquippedWeaponStyle(DetailedWeaponStyle fallbackStyle)
        {
            GetEquippedWeapons(out var mainHand, out var offHand);
            if (mainHand == null && offHand == null)
            {
                return fallbackStyle;
            }
            if (offHand != null)
            {
                if (offHand.ItemInfo.ItemClass != WoWItemClass.Armor)
                {
                    if (!IsTwoHanded(offHand.ItemInfo))
                    {
                        return DetailedWeaponStyle.DualWieldOneHanders;
                    }
                    return DetailedWeaponStyle.DualWieldTwoHanders;
                }
                if (offHand.ItemInfo.ArmorClass == WoWItemArmorClass.Shield)
                {
                    return DetailedWeaponStyle.OneHandAndShield;
                }
                return DetailedWeaponStyle.OneHandAndHeld;
            }
            if (IsRangedWeapon(mainHand.ItemInfo))
            {
                return DetailedWeaponStyle.Ranged;
            }
            if (IsTwoHanded(mainHand.ItemInfo))
            {
                return DetailedWeaponStyle.TwoHander;
            }
            return fallbackStyle;
        }

        private DetailedWeaponStyle GetSubtletyWeaponStyle()
        {
            if (!StyxWoW.Me.KnowsSpell(53))
            {
                return DetailedWeaponStyle.DualWieldOneHanders;
            }
            GetEquippedWeapons(out var mainHand, out var _);
            if (!(mainHand == null) && mainHand.ItemInfo != null)
            {
                if (mainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.Dagger)
                {
                    return DetailedWeaponStyle.DualWieldOneHanders;
                }
                return DetailedWeaponStyle.DualWieldWithDaggerInMainHand;
            }
            return DetailedWeaponStyle.DualWieldOneHanders;
        }

        private DetailedWeaponStyle GetAutomaticWeaponStyle()
        {
            WoWSpec spec = CharacterManager.CurrentClassProfile.GetSpec();
            switch (spec)
            {
            case WoWSpec.WarriorFury:
            {
                if (!CanDualWieldTwoHanders())
                {
                    if (!CanDualWield())
                    {
                        return DetailedWeaponStyle.Interchangable;
                    }
                    return DetailedWeaponStyle.DualWieldOneHanders;
                }
                GetEquippedWeapons(out var mainHand, out var offHand);
                if (mainHand == null && offHand == null)
                {
                    return DetailedWeaponStyle.DualWieldTwoHanders;
                }
                if ((mainHand != null && IsTwoHanded(mainHand.ItemInfo)) || (offHand != null && IsTwoHanded(offHand.ItemInfo)))
                {
                    return DetailedWeaponStyle.DualWieldTwoHanders;
                }
                return DetailedWeaponStyle.DualWieldOneHanders;
            }
            case WoWSpec.PaladinProtection:
            case WoWSpec.WarriorProtection:
                return DetailedWeaponStyle.OneHandAndShield;
            case WoWSpec.None:
                Log(Colors.Red, "Current class profile has an invalid specialization or class");
                return GetEquippedWeaponStyle(DetailedWeaponStyle.Interchangable);
            default:
                Log(Colors.Red, "Current class profile has an invalid spec: {0}", spec);
                return GetEquippedWeaponStyle(DetailedWeaponStyle.Interchangable);
            case WoWSpec.HunterBeastMastery:
            case WoWSpec.HunterMarksmanship:
            case WoWSpec.HunterSurvival:
                return DetailedWeaponStyle.Ranged;
            case WoWSpec.RogueAssassination:
                return DetailedWeaponStyle.DualWieldDaggers;
            case WoWSpec.RogueSubtlety:
                return GetSubtletyWeaponStyle();
            case WoWSpec.RogueCombat:
            case WoWSpec.ShamanEnhancement:
                if (!CanDualWield())
                {
                    return DetailedWeaponStyle.Interchangable;
                }
                return DetailedWeaponStyle.DualWieldOneHanders;
            case WoWSpec.DeathKnightFrost:
            case WoWSpec.MonkBrewmaster:
            case WoWSpec.MonkWindwalker:
                return GetEquippedWeaponStyle(DetailedWeaponStyle.TwoHander);
            case WoWSpec.PaladinRetribution:
            case WoWSpec.WarriorArms:
            case WoWSpec.DruidFeral:
            case WoWSpec.DruidGuardian:
            case WoWSpec.DeathKnightBlood:
            case WoWSpec.DeathKnightUnholy:
                return DetailedWeaponStyle.TwoHander;
            case WoWSpec.MageArcane:
            case WoWSpec.MageFire:
            case WoWSpec.MageFrost:
            case WoWSpec.PaladinHoly:
            case WoWSpec.DruidBalance:
            case WoWSpec.DruidRestoration:
            case WoWSpec.PriestDiscipline:
            case WoWSpec.PriestHoly:
            case WoWSpec.PriestShadow:
            case WoWSpec.ShamanElemental:
            case WoWSpec.ShamanRestoration:
            case WoWSpec.WarlockAffliction:
            case WoWSpec.WarlockDemonology:
            case WoWSpec.WarlockDestruction:
            case WoWSpec.MonkMistweaver:
                return DetailedWeaponStyle.Interchangable;
            }
        }

        private DetailedWeaponStyle GetMainHandAndOffHandStyle()
        {
            WoWSpec spec = CharacterManager.CurrentClassProfile.GetSpec();
            switch (spec)
            {
            case WoWSpec.MageArcane:
            case WoWSpec.MageFire:
            case WoWSpec.MageFrost:
            case WoWSpec.PaladinHoly:
            case WoWSpec.DruidBalance:
            case WoWSpec.DruidRestoration:
            case WoWSpec.PriestDiscipline:
            case WoWSpec.PriestHoly:
            case WoWSpec.PriestShadow:
            case WoWSpec.ShamanElemental:
            case WoWSpec.ShamanRestoration:
            case WoWSpec.WarlockAffliction:
            case WoWSpec.WarlockDemonology:
            case WoWSpec.WarlockDestruction:
            case WoWSpec.MonkMistweaver:
                return DetailedWeaponStyle.OneHandAndHeld;
            default:
                Log(Colors.Red, "Current class profile has an invalid spec: {0}", spec);
                return GetEquippedWeaponStyle(DetailedWeaponStyle.Interchangable);
            case WoWSpec.PaladinRetribution:
            case WoWSpec.WarriorArms:
            case WoWSpec.DruidFeral:
            case WoWSpec.DruidGuardian:
            case WoWSpec.HunterBeastMastery:
            case WoWSpec.HunterMarksmanship:
            case WoWSpec.HunterSurvival:
            case WoWSpec.ShamanEnhancement:
                Log(Colors.Red, "This class spec cannot use the MainHandAndOffHand style - changed to Automatic");
                CharacterSettings.Instance.WeaponStyle = WeaponStyle.Automatic;
                return GetAutomaticWeaponStyle();
            case WoWSpec.WarriorFury:
            case WoWSpec.DeathKnightFrost:
            case WoWSpec.DeathKnightUnholy:
            case WoWSpec.RogueAssassination:
            case WoWSpec.RogueCombat:
            case WoWSpec.RogueSubtlety:
            case WoWSpec.MonkBrewmaster:
            case WoWSpec.MonkWindwalker:
                return DetailedWeaponStyle.DualWieldOneHanders;
            case WoWSpec.PaladinProtection:
            case WoWSpec.WarriorProtection:
                return DetailedWeaponStyle.OneHandAndShield;
            }
        }

        private DetailedWeaponStyle GetWeaponStyle()
        {
            WeaponStyle weaponStyle = CharacterSettings.Instance.WeaponStyle;
            switch (weaponStyle)
            {
            default:
                Log(Colors.Red, "Weapon style was set to {0} - setting to automatic", weaponStyle);
                CharacterSettings.Instance.WeaponStyle = WeaponStyle.Automatic;
                return GetAutomaticWeaponStyle();
            case WeaponStyle.Automatic:
                return GetAutomaticWeaponStyle();
            case WeaponStyle.MainHandAndOffHand:
                return GetMainHandAndOffHandStyle();
            case WeaponStyle.TwoHander:
                return DetailedWeaponStyle.TwoHander;
            case WeaponStyle.DualWieldTwoHanders:
                return DetailedWeaponStyle.DualWieldTwoHanders;
            }
        }

        private bool CanDualWield()
        {
            return StyxWoW.Me.CanDualWield();
        }

        private bool CanDualWieldTwoHanders()
        {
            return _canDualWieldTwoHanders ?? (_canDualWieldTwoHanders = new PerFrameCachedValue<bool>(() => StyxWoW.Me.KnowsSpell(46917)));
        }

        private IEnumerable<WoWInventorySlot> DecideEquipmentSlots(ItemInfo itemInfo)
        {
            InventoryType inventoryType = itemInfo.InventoryType;
            switch (inventoryType)
            {
            case InventoryType.Head:
                yield return WoWInventorySlot.Head;
                break;
            case InventoryType.Neck:
                yield return WoWInventorySlot.Neck;
                break;
            case InventoryType.Shoulder:
                yield return WoWInventorySlot.Shoulder;
                break;
            case InventoryType.Body:
                yield return WoWInventorySlot.Shirt;
                break;
            case InventoryType.Waist:
                yield return WoWInventorySlot.Waist;
                break;
            case InventoryType.Legs:
                yield return WoWInventorySlot.Legs;
                break;
            case InventoryType.Feet:
                yield return WoWInventorySlot.Feet;
                break;
            case InventoryType.Wrist:
                yield return WoWInventorySlot.Wrist;
                break;
            case InventoryType.Hand:
                yield return WoWInventorySlot.Hands;
                break;
            case InventoryType.Finger:
                yield return WoWInventorySlot.Finger1;
                yield return WoWInventorySlot.Finger2;
                break;
            case InventoryType.Trinket:
                yield return WoWInventorySlot.Trinket1;
                yield return WoWInventorySlot.Trinket2;
                break;
            case InventoryType.Weapon:
                yield return WoWInventorySlot.MainHand;
                if (CanDualWield())
                {
                    yield return WoWInventorySlot.OffHand;
                }
                break;
            case InventoryType.Shield:
                yield return WoWInventorySlot.OffHand;
                break;
            case InventoryType.Cloak:
                yield return WoWInventorySlot.Back;
                break;
            case InventoryType.TwoHandWeapon:
                yield return WoWInventorySlot.MainHand;
                if (CanDualWieldTwoHanders())
                {
                    yield return WoWInventorySlot.OffHand;
                }
                break;
            case InventoryType.Bag:
                yield return WoWInventorySlot.Bag1;
                yield return WoWInventorySlot.Bag2;
                yield return WoWInventorySlot.Bag3;
                yield return WoWInventorySlot.Bag4;
                break;
            case InventoryType.Tabard:
                yield return WoWInventorySlot.Tabard;
                break;
            case InventoryType.Chest:
            case InventoryType.Robe:
                yield return WoWInventorySlot.Chest;
                break;
            case InventoryType.WeaponMainHand:
                yield return WoWInventorySlot.MainHand;
                break;
            case InventoryType.WeaponOffHand:
                if (CanDualWield())
                {
                    yield return WoWInventorySlot.OffHand;
                }
                break;
            case InventoryType.Holdable:
                yield return WoWInventorySlot.OffHand;
                break;
            default:
                Log("Got unexpected inventory type '{0}'", inventoryType);
                break;
            case InventoryType.Ranged:
            case InventoryType.RangedRight:
                yield return WoWInventorySlot.MainHand;
                break;
            }
        }

        private WoWItemClass GetEquipmentClass(InventoryType inventoryType)
        {
            switch (inventoryType)
            {
            case InventoryType.Bag:
                return WoWItemClass.Container;
            case InventoryType.Head:
            case InventoryType.Neck:
            case InventoryType.Shoulder:
            case InventoryType.Body:
            case InventoryType.Chest:
            case InventoryType.Waist:
            case InventoryType.Legs:
            case InventoryType.Feet:
            case InventoryType.Wrist:
            case InventoryType.Hand:
            case InventoryType.Finger:
            case InventoryType.Trinket:
            case InventoryType.Cloak:
            case InventoryType.Tabard:
            case InventoryType.Robe:
                return WoWItemClass.Armor;
            default:
                Log("Got unexpected inventory type '{0}'", inventoryType);
                return WoWItemClass.Miscellaneous;
            case InventoryType.Weapon:
            case InventoryType.Shield:
            case InventoryType.Ranged:
            case InventoryType.TwoHandWeapon:
            case InventoryType.WeaponMainHand:
            case InventoryType.WeaponOffHand:
            case InventoryType.Holdable:
            case InventoryType.RangedRight:
                return WoWItemClass.Weapon;
            }
        }

        #endregion

        #region Nested Types

        private class LootRollItem
        {
            public string ItemLink { get; set; }
            public string Name { get; set; }
            public ItemInfo ItemInfo { get; set; }
            public bool CanNeed { get; set; }
            public bool CanGreed { get; set; }
            public bool CanDisenchant { get; set; }
        }

        private class LootRollDecision
        {
            private bool _simple;

            public RollType Type { get; private set; }
            public string DelayReason { get; private set; }
            public float Gain { get; private set; }
            public bool Forced { get; private set; }

            public string GetDescription(LootRollItem item, AutoEquipper autoEquipper)
            {
                if (_simple)
                    return string.Format("Rolling {0} on {1}", Type, item.Name);

                if (Type == RollType.DelayedNeed)
                {
                    if (Forced)
                        return string.Format("Rolling {0} on {1} if no one else does; it {2}, but is more compatible with our weapon style ({3})",
                            RollType.Need, item.Name, DelayReason, autoEquipper.GetWeaponStyle());
                    return string.Format("Rolling {0} on {1} if no one else does; it {2}, but yields a gain of {3:F2}",
                        RollType.Need, item.Name, DelayReason, Gain);
                }
                if (Forced)
                    return string.Format("Rolling {0} on {1} as it is more compatible with our weapon style ({2})",
                        RollType.Need, item.Name, autoEquipper.GetWeaponStyle());
                return string.Format("Rolling {0} on {1}; it yields a gain of {2:F2} score", RollType.Need, item.Name, Gain);
            }

            public static LootRollDecision Create(RollType type)
            {
                return new LootRollDecision { Type = type, _simple = true };
            }

            public static LootRollDecision CreateNeed(float gain, bool forced)
            {
                return new LootRollDecision { Type = RollType.Need, Gain = gain, Forced = forced };
            }

            public static LootRollDecision CreateDelayedNeed(string reason, float gain, bool forced)
            {
                return new LootRollDecision { Type = RollType.DelayedNeed, DelayReason = reason, Gain = gain, Forced = forced };
            }
        }

        #endregion
    }
}
