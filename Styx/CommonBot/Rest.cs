using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Inventory;
using Styx.Helpers;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public static class Rest
    {
        private static readonly WaitTimer FeedTimer = WaitTimer.FiveSeconds;
        private static readonly WaitTimer DrinkTimer = WaitTimer.FiveSeconds;

        public static double RestPercentageHealth { get; set; }
        public static double RestPercentageMana { get; set; }
        public static bool NoFood { get; private set; }
        public static bool NoDrink { get; private set; }

        public static void FeedImmediate()
        {
            if (!FeedTimer.IsFinished)
                return;
            FeedTimer.Reset();
            WoWItem food = Consumable.GetBestFood(false);
            if (food != null)
            {
                Logging.Write(string.Format(StyxResources.Eating__0_, food.Name));
                food.Use();
                return;
            }
            Logging.Write(StyxResources.Could_not_find_any_food_to_eat_);
        }

        public static void DrinkImmediate()
        {
            if (!DrinkTimer.IsFinished)
                return;
            DrinkTimer.Reset();
            WoWItem drink = Consumable.GetBestDrink(false);
            if (drink != null)
            {
                Logging.Write(string.Format(StyxResources.Drinking__0_, drink.Name));
                drink.Use();
                return;
            }
            Logging.Write(StyxResources.Could_not_find_any_water_to_drink_);
        }

        public static void Feed()
        {
            LocalPlayer me = StyxWoW.Me;
            if (me.CurrentTarget != null)
            {
                Logging.Write(StyxResources.Resting_);
                me.ClearTarget();
            }
            if (me.IsMoving)
                WoWMovement.MoveStop();

            string foodName = CharacterSettings.Instance.FoodName;
            string drinkName = CharacterSettings.Instance.DrinkName;
            if (!string.IsNullOrEmpty(foodName) && me.HealthPercent <= 55.0 && !me.Auras.ContainsKey("Food"))
            {
                int count = foodName.All(char.IsDigit)
                    ? Lua.GetReturnVal<int>(string.Format("return GetItemCount({0})", foodName), 0U)
                    : Lua.GetReturnVal<int>("return GetItemCount(\"" + Lua.Escape(foodName) + "\")", 0U);
                if (count > 0)
                {
                    Lua.DoString(string.Format("UseItemByName(\"{0}\")", Lua.Escape(foodName)), "WoW.lua");
                    NoFood = false;
                }
                else
                {
                    NoFood = true;
                    Logging.Write(StyxResources.No__0__in_bags_, foodName);
                }
            }

            var nonManaUsers = new List<WoWClass> { WoWClass.DeathKnight, WoWClass.Hunter, WoWClass.Rogue };
            if (!SpellManager.HasSpell("Soothing Mist"))
                nonManaUsers.Add(WoWClass.Monk);

            if (!string.IsNullOrEmpty(drinkName) && !nonManaUsers.Contains(me.Class) &&
                me.ManaPercent <= 55.0 && !me.Auras.ContainsKey("Drink"))
            {
                int count = drinkName.All(char.IsDigit)
                    ? Lua.GetReturnVal<int>(string.Format("return GetItemCount({0})", drinkName), 0U)
                    : Lua.GetReturnVal<int>("return GetItemCount(\"" + Lua.Escape(drinkName) + "\")", 0U);
                if (count > 0)
                {
                    NoDrink = false;
                    Lua.DoString(string.Format("UseItemByName(\"{0}\")", Lua.Escape(drinkName)), "WoW.lua");
                }
                else
                {
                    NoDrink = true;
                    Logging.Write(StyxResources.No__0__in_bags_, drinkName);
                }
            }

            if (drinkName == string.Empty && foodName == string.Empty)
                return;

            StyxWoW.Sleep(1000);
            using (StyxWoW.Memory.ReleaseFrame(true))
            {
                while (me.Auras.ContainsKey("Food") || me.Auras.ContainsKey("Drink"))
                {
                    StyxWoW.Sleep(100);
                    if ((me.HealthPercent != 100.0 || me.ManaPercent != 100.0) && !me.Combat &&
                        (me.HealthPercent != 100.0 || me.Auras.ContainsKey("Drink")))
                    {
                        if (me.ManaPercent != 100.0)
                            continue;
                        if (me.Auras.ContainsKey("Food"))
                            continue;
                    }
                    break;
                }
            }
        }
    }
}
