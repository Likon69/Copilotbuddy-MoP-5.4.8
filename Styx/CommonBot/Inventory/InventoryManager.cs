using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Inventory
{
    public static class InventoryManager
    {
        private static readonly HashSet<uint> AccountBoundExceptions = new HashSet<uint>
        {
            102287,102280,102273,102266,102289,102282,102275,102268,102284,102277,102270,102263,
            102321,102322,102323,102320,102286,102279,102272,102265,102290,102283,102276,102269,
            102288,102281,102274,102267,102285,102278,102271,102264,102318,104347,104345,102291,
            104013,104012,104010,104009
        };

        public static List<InventorySlot> GetInventorySlotsByEquipSlot(InventoryType type)
        {
            var slots = new List<InventorySlot>();
            switch (type)
            {
                case InventoryType.Head: slots.Add(InventorySlot.HeadSlot); break;
                case InventoryType.Neck: slots.Add(InventorySlot.NeckSlot); break;
                case InventoryType.Shoulder: slots.Add(InventorySlot.ShoulderSlot); break;
                case InventoryType.Body: slots.Add(InventorySlot.ShirtSlot); break;
                case InventoryType.Chest:
                case InventoryType.Robe: slots.Add(InventorySlot.ChestSlot); break;
                case InventoryType.Waist: slots.Add(InventorySlot.WaistSlot); break;
                case InventoryType.Legs: slots.Add(InventorySlot.LegsSlot); break;
                case InventoryType.Feet: slots.Add(InventorySlot.FeetSlot); break;
                case InventoryType.Wrist: slots.Add(InventorySlot.WristSlot); break;
                case InventoryType.Hand: slots.Add(InventorySlot.HandsSlot); break;
                case InventoryType.Finger: slots.Add(InventorySlot.Finger0Slot); slots.Add(InventorySlot.Finger1Slot); break;
                case InventoryType.Trinket: slots.Add(InventorySlot.Trinket0Slot); slots.Add(InventorySlot.Trinket1Slot); break;
                case InventoryType.Weapon: slots.Add(InventorySlot.MainHandSlot); slots.Add(InventorySlot.SecondaryHandSlot); break;
                case InventoryType.Shield:
                case InventoryType.WeaponOffHand:
                case InventoryType.Holdable: slots.Add(InventorySlot.SecondaryHandSlot); break;
                case InventoryType.Ranged:
                case InventoryType.WeaponMainHand:
                case InventoryType.RangedRight: slots.Add(InventorySlot.MainHandSlot); break;
                case InventoryType.Cloak: slots.Add(InventorySlot.BackSlot); break;
                case InventoryType.TwoHandWeapon:
                    slots.Add(InventorySlot.MainHandSlot);
                    var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;
                    if (StyxWoW.Me.Specialization == WoWSpec.WarriorFury && StyxWoW.Me.Level >= 38 && mainHand != null && mainHand.ItemInfo.InventoryType == InventoryType.TwoHandWeapon) slots.Add(InventorySlot.SecondaryHandSlot);
                    break;
                case InventoryType.Bag:
                case InventoryType.Quiver: slots.AddRange(new[] { InventorySlot.Bag0Slot, InventorySlot.Bag1Slot, InventorySlot.Bag2Slot, InventorySlot.Bag3Slot }); break;
                case InventoryType.Tabard: slots.Add(InventorySlot.TabardSlot); break;
                case InventoryType.Ammo: slots.Add(InventorySlot.AmmoSlot); break;
            }
            return slots;
        }

        public static bool HaveItemsToMail => GetItemsToMail().Length != 0;
        public static WoWItem[] GetItemsToMail() => StyxWoW.Me.BagItems.Where(CanMail).ToArray();

        private static bool CanMail(WoWItem item)
        {
            var profile = ProfileManager.CurrentProfile;
            var name = item.Name.ToLower();
            var entry = item.Entry.ToString();
            var food = CharacterSettings.Instance.FoodName.ToLower();
            var drink = CharacterSettings.Instance.DrinkName.ToLower();
            if (item.IsSoulbound && !AccountBoundExceptions.Contains(item.Entry)) { Logging.WriteDiagnostic("Can't mail item: {0}. Item is soulbound", item.Name); return false; }
            if (item.IsConjured) { Logging.WriteDiagnostic("Can't mail item: {0}. Item is conjured", item.Name); return false; }
            if (item.ItemInfo.Bond == WoWItemBondType.Quest) { Logging.WriteDiagnostic("Can't mail item: {0}. Item is a quest item", item.Name); return false; }
            if ((profile != null && profile.ForceMail.Contains(item.Entry)) || ForceMailManager.Contains(item.Entry)) return true;
            if (ProtectedItemsManager.Contains(item.Entry) || ProtectedItemsManager.Contains(name)) { Logging.WriteDiagnostic("Can't mail item: {0}. Item is a protected item", item.Name); return false; }
            if (profile != null && !profile.MailQualities.Contains(item.Quality)) { Logging.WriteDiagnostic("Can't mail item: {0}. Item doesn't meet the item qualities specified in the profile", item.Name); return false; }
            var inBag = Lua.GetReturnVal<string>(string.Format("for bag=0,4 do if GetBagName(bag) then for slot=1,GetContainerNumSlots(bag) do if GetContainerItemID(bag, slot) == {0} then local _, count = GetContainerItemInfo(bag, slot) if count == {1} then return \"true\" end end end end end return \"false\"", item.Entry, item.StackCount), 0U);
            if (inBag == "false") { Logging.WriteDiagnostic("Can't mail item: {0}. Item is equipped", item.Name); return false; }
            return name != food && name != drink && entry != food && entry != drink;
        }
    }
}
