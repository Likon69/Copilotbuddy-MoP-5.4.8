using System;
using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.CharacterManagement
{
    public class WeightSet
    {
        public WeightSet(Dictionary<WeighableStatType, float> weights)
        {
            if (weights == null)
            {
                throw new ArgumentNullException("weights");
            }
            this.Weights = weights;
        }

        public Dictionary<WeighableStatType, float> Weights { get; private set; }

        private float GetWeightedValue(WeighableStatType statType, float value)
        {
            float num;
            if (!this.Weights.TryGetValue(statType, out num))
            {
                return 0f;
            }
            return num * value;
        }

        internal static WoWItemArmorClass GetArmorClassForCurrentClass()
        {
            switch (StyxWoW.Me.Class)
            {
            case WoWClass.Warrior:
            case WoWClass.Paladin:
                if (StyxWoW.Me.Level >= 40)
                {
                    return WoWItemArmorClass.Plate;
                }
                return WoWItemArmorClass.Mail;
            case WoWClass.Hunter:
            case WoWClass.Shaman:
                if (StyxWoW.Me.Level >= 40)
                {
                    return WoWItemArmorClass.Mail;
                }
                return WoWItemArmorClass.Leather;
            case WoWClass.Rogue:
            case WoWClass.Monk:
            case WoWClass.Druid:
                return WoWItemArmorClass.Leather;
            case WoWClass.Priest:
            case WoWClass.Mage:
            case WoWClass.Warlock:
                return WoWItemArmorClass.Cloth;
            case WoWClass.DeathKnight:
                return WoWItemArmorClass.Plate;
            default:
                return WoWItemArmorClass.None;
            }
        }

        public float EvaluateItem(ItemInfo itemInfo, ItemStats itemStats, bool factorArmorClass = false)
        {
            if (itemInfo == null)
            {
                throw new ArgumentNullException("itemInfo");
            }
            if (itemStats == null)
            {
                throw new ArgumentNullException("itemStats");
            }
            float num = itemStats.Stats.Sum(kvp => GetWeightedValue((WeighableStatType)kvp.Key, kvp.Value));
            num += this.GetWeightedValue(WeighableStatType.ItemLevel, (float)itemInfo.Level);
            if (factorArmorClass && itemInfo.ItemClass == WoWItemClass.Armor)
            {
                WoWItemArmorClass shownArmorClass = itemInfo.GetShownArmorClass();
                if (shownArmorClass >= WoWItemArmorClass.Cloth && shownArmorClass <= WoWItemArmorClass.Plate)
                {
                    WoWItemArmorClass woWItemArmorClass = WeightSet.GetArmorClassForCurrentClass();
                    int num2 = Math.Abs(woWItemArmorClass - shownArmorClass);
                    num /= (float)(num2 + 1);
                }
            }
            return num;
        }

        public float EvaluateItem(WoWItem item, bool factorArmorClass = false)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return this.EvaluateItem(item.ItemInfo, item.ItemStats, factorArmorClass);
        }
    }
}
