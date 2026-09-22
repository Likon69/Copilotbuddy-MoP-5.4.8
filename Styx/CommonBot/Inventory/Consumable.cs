using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Inventory
{
    public static class Consumable
    {
        private static readonly string[] FoodSpellNames = { "Food", "Refreshment" };
        private static readonly string[] DrinkSpellNames = { "Drink", "Starfire Espresso", "Refreshment" };

        public static List<WoWItem> GetFood() => StyxWoW.Me.BagItems.Where(item => item != null && IsFood(item.ItemInfo)).ToList();
        public static List<WoWItem> GetDrinks() => StyxWoW.Me.BagItems.Where(item => item != null && IsDrink(item.ItemInfo)).ToList();
        public static WoWItem GetBestFood(bool includeSpecialtyItems) => GetBest(GetFood(), FoodSpellNames, includeSpecialtyItems);
        public static WoWItem GetBestDrink(bool includeSpecialtyItems) => GetBest(GetDrinks(), DrinkSpellNames, includeSpecialtyItems);

        public static bool IsFood(ItemInfo itemInfo) => HasConsumableSpell(itemInfo, FoodSpellNames);
        public static bool IsDrink(ItemInfo itemInfo) => HasConsumableSpell(itemInfo, DrinkSpellNames);

        private static WoWItem GetBest(IEnumerable<WoWItem> items, string[] spellNames, bool includeSpecialtyItems)
        {
            uint largestStack = 0;
            var highestRequiredLevel = -1;
            WoWItem best = null;
            foreach (var item in items)
            {
                if (item.StackCount <= largestStack || item.ItemInfo.RequiredLevel <= highestRequiredLevel || item.ItemInfo.RequiredLevel > StyxWoW.Me.Level) continue;
                if (!includeSpecialtyItems && !item.ItemSpells.All(spell => spell.ActualSpell != null && spellNames.Contains(spell.ActualSpell.Name))) continue;
                best = item;
                largestStack = item.StackCount;
                highestRequiredLevel = item.ItemInfo.RequiredLevel;
            }
            return best;
        }

        private static bool HasConsumableSpell(ItemInfo itemInfo, string[] spellNames)
        {
            return itemInfo != null && itemInfo.ItemClass == WoWItemClass.Consumable && itemInfo.SpellId != null &&
                   itemInfo.SpellId.Where(id => id != 0).Select(WoWSpell.FromId)
                       .Any(spell => spell != null && spell.IsValid && spellNames.Contains(spell.Name));
        }
    }
}
