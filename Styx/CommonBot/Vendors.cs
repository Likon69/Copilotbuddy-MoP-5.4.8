using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Styx.Common;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public delegate void BuyItemsEventHandler(BuyItemsEventArgs e);

    public delegate void VendorItemsEventHandler(SellItemsEventArgs e);

    public delegate void MailItemsEventHandler(MailItemsEventArgs e);

    public class BuyItemsEventArgs
    {
        public BuyItemsEventArgs()
        {
            BuyItemsIds = new Dictionary<uint, int>();
        }

        public Dictionary<uint, int> BuyItemsIds { get; private set; }
    }

    public class SellItemsEventArgs
    {
        public SellItemsEventArgs()
        {
            NameExceptions = new List<string>();
            IdExceptions = new List<uint>();
        }

        public List<string> NameExceptions { get; private set; }
        public List<uint> IdExceptions { get; private set; }
    }

    public class MailItemsEventArgs
    {
        public MailItemsEventArgs()
        {
            AdditionalItems = new List<WoWItem>();
        }

        public List<WoWItem> AdditionalItems { get; private set; }
    }

    public static class Vendors
    {
        private static readonly TrainerFrame TrainerFrame = new TrainerFrame();
        private static readonly MerchantFrame Merchant = new MerchantFrame();
        private static readonly MailFrame Mail = new MailFrame();
        private static readonly GossipFrame Gossip = new GossipFrame();

        public static VendorItemsEventHandler OnVendorItems;
        public static BuyItemsEventHandler OnBuyItems;
        public static EventHandler OnRepairItems;
        public static MailItemsEventHandler OnMailItems;

        public static bool ForceTrainer { get; set; }
        public static bool ForceSell { get; set; }
        public static bool ForceRepair { get; set; }
        public static bool ForceMail { get; set; }
        public static bool ForceBuy { get; set; }
        public static NavType? NavType { get; set; }

        public static WoWUnit NearestFlightMerchant
        {
            get
            {
                WoWUnit woWUnit = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                    .Where(u => u.IsFlightMaster && u.InteractType == WoWInteractType.TaxiPathAvailable && !Blacklist.Contains(u, BlacklistFlags.Interact))
                    .OrderBy(u => u.Distance)
                    .FirstOrDefault();

                if (woWUnit != null && !Navigator.CanNavigateFully(StyxWoW.Me.Location, woWUnit.Location))
                {
                    Logging.Write(string.Format(StyxResources.Blacklisting__0__for_5_minutes_because_we_can_t_navigate_to_it_, woWUnit.Name));
                    Blacklist.Add(woWUnit, BlacklistFlags.Interact, new TimeSpan(0, 5, 0), "Can't navigate to the target!");
                    return null;
                }
                return woWUnit;
            }
        }

        public static bool RepairDisabled { get; set; }

        public static void TrainSkills()
        {
            TrainerFrame.BuyAll();
            ForceTrainer = false;
        }

        [DebuggerStepThrough]
        public static async Task MailAllItemsCoroutine()
        {
            List<WoWItem> items = new List<WoWItem>();
            items.AddRange(InventoryManager.GetItemsToMail());
            if (OnMailItems != null)
            {
                MailItemsEventArgs eventArgs = new MailItemsEventArgs();
                foreach (MailItemsEventHandler handler in OnMailItems.GetInvocationList())
                {
                    try
                    {
                        handler(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                        eventArgs.AdditionalItems.Clear();
                        continue;
                    }
                    foreach (WoWItem item in eventArgs.AdditionalItems)
                    {
                        if (!items.Contains(item))
                        {
                            items.Add(item);
                        }
                    }
                    eventArgs.AdditionalItems.Clear();
                }
            }
            await Mail.SendMailWithManyAttachmentsCoroutine(CharacterSettings.Instance.MailRecipient, items.ToArray());
            ForceMail = false;
        }

        [Obsolete("Use MailAllItemsCoroutine instead")]
        public static void MailAllItems()
        {
            List<WoWItem> list = new List<WoWItem>();
            list.AddRange(InventoryManager.GetItemsToMail());
            if (OnMailItems != null)
            {
                MailItemsEventArgs mailItemsEventArgs = new MailItemsEventArgs();
                Delegate[] invocationList = OnMailItems.GetInvocationList();
                int i = 0;
                while (i < invocationList.Length)
                {
                    MailItemsEventHandler mailItemsEventHandler = (MailItemsEventHandler)invocationList[i];
                    try
                    {
                        mailItemsEventHandler(mailItemsEventArgs);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                        mailItemsEventArgs.AdditionalItems.Clear();
                        goto IL_00A9;
                    }
                    goto IL_005A;
                    IL_00A9:
                    i++;
                    continue;
                    IL_005A:
                    foreach (WoWItem woWItem in mailItemsEventArgs.AdditionalItems)
                    {
                        if (!list.Contains(woWItem))
                        {
                            list.Add(woWItem);
                        }
                    }
                    mailItemsEventArgs.AdditionalItems.Clear();
                    goto IL_00A9;
                }
            }
            Mail.SendMailWithManyAttachments(CharacterSettings.Instance.MailRecipient, 0, list.Where(i => !i.IsSoulbound).ToArray());
            ForceMail = false;
        }

        public static void RepairAllItems()
        {
            if (OnRepairItems != null)
            {
                OnRepairItems(null, EventArgs.Empty);
            }
            Merchant.RepairAllItems();
            ForceRepair = false;
        }

        public static void SellAllItems()
        {
            ItemQuality itemQuality = ItemQuality.None;
            Profile currentProfile = ProfileManager.CurrentProfile;
            if (currentProfile != null)
            {
                if (currentProfile.SellGrey)
                {
                    itemQuality |= ItemQuality.Poor;
                }
                if (currentProfile.SellWhite)
                {
                    itemQuality |= ItemQuality.Common;
                }
                if (currentProfile.SellGreen)
                {
                    itemQuality |= ItemQuality.Uncommon;
                }
                if (currentProfile.SellBlue)
                {
                    itemQuality |= ItemQuality.Rare;
                }
                if (currentProfile.SellPurple)
                {
                    itemQuality |= ItemQuality.Epic;
                }
            }
            HashSet<string> hashSet = new HashSet<string>();
            HashSet<uint> hashSet2 = new HashSet<uint>();
            foreach (string text in ProtectedItemsManager.GetAllItemNames())
            {
                hashSet.Add(text.ToLowerInvariant());
            }
            foreach (uint num in ProtectedItemsManager.GetAllItemIds())
            {
                hashSet2.Add(num);
            }
            foreach (WoWItem woWItem in EquipmentManager.EquipmentSets.SelectMany(s => s.Items))
            {
                hashSet2.Add(woWItem.Entry);
            }
            foreach (WoWInternals.Quest.QuestObjective questObjective in StyxWoW.Me.QuestLog.GetAllQuests().SelectMany(q => q.GetObjectives()))
            {
                if (questObjective.Type == WoWInternals.Quest.QuestObjectiveType.CollectIntermediateItem || questObjective.Type == WoWInternals.Quest.QuestObjectiveType.CollectItem)
                {
                    hashSet2.Add(questObjective.ID);
                }
            }
            uint num2;
            if (uint.TryParse(CharacterSettings.Instance.FoodName, out num2))
            {
                hashSet2.Add(num2);
            }
            else
            {
                hashSet.Add(CharacterSettings.Instance.FoodName.ToLowerInvariant());
            }
            uint num3;
            if (uint.TryParse(CharacterSettings.Instance.DrinkName, out num3))
            {
                hashSet2.Add(num3);
            }
            else
            {
                hashSet.Add(CharacterSettings.Instance.DrinkName.ToLowerInvariant());
            }
            foreach (WoWItem woWItem2 in StyxWoW.Me.BagItems)
            {
                if (!hashSet2.Contains(woWItem2.Entry) && !hashSet.Contains(woWItem2.Name) && (woWItem2.ItemInfo.Bond == WoWItemBondType.Quest || woWItem2.ItemInfo.ItemClass == WoWItemClass.Key || woWItem2.IsConjured || woWItem2.ItemInfo.BeginQuestId != 0 || woWItem2.ItemInfo.SellPrice == 0 || woWItem2.ItemInfo.IsAccountBound || woWItem2.ItemInfo.IsBattleNetAccountBound))
                {
                    hashSet2.Add(woWItem2.Entry);
                }
            }
            if (OnVendorItems != null)
            {
                SellItemsEventArgs sellItemsEventArgs = new SellItemsEventArgs();
                Delegate[] invocationList = OnVendorItems.GetInvocationList();
                int i = 0;
                while (i < invocationList.Length)
                {
                    VendorItemsEventHandler vendorItemsEventHandler = (VendorItemsEventHandler)invocationList[i];
                    try
                    {
                        vendorItemsEventHandler(sellItemsEventArgs);
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                        sellItemsEventArgs.NameExceptions.Clear();
                        sellItemsEventArgs.IdExceptions.Clear();
                        goto IL_03D6;
                    }
                    goto IL_0341;
                    IL_03D6:
                    i++;
                    continue;
                    IL_0341:
                    foreach (string text2 in sellItemsEventArgs.NameExceptions)
                    {
                        hashSet.Add(text2.ToLowerInvariant());
                    }
                    foreach (uint num4 in sellItemsEventArgs.IdExceptions)
                    {
                        hashSet2.Add(num4);
                    }
                    sellItemsEventArgs.NameExceptions.Clear();
                    sellItemsEventArgs.IdExceptions.Clear();
                    goto IL_03D6;
                }
            }
            Merchant.SellItemQualities(itemQuality, hashSet, hashSet2);
            ForceSell = false;
        }

        public static void BuyItems()
        {
            Dictionary<uint, int> dictionary = new Dictionary<uint, int>();
            if (OnBuyItems != null)
            {
                BuyItemsEventArgs buyItemsEventArgs = new BuyItemsEventArgs();
                foreach (BuyItemsEventHandler buyItemsEventHandler in OnBuyItems.GetInvocationList())
                {
                    try
                    {
                        buyItemsEventHandler(buyItemsEventArgs);
                        foreach (KeyValuePair<uint, int> keyValuePair in buyItemsEventArgs.BuyItemsIds)
                        {
                            dictionary.Add(keyValuePair.Key, keyValuePair.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                        buyItemsEventArgs.BuyItemsIds.Clear();
                    }
                }
                if (dictionary.Count > 0)
                {
                    foreach (MerchantItem merchantItem in Merchant.GetAllMerchantItems())
                    {
                        if (dictionary.ContainsKey(merchantItem.ItemId))
                        {
                            Merchant.BuyItem(merchantItem.Index, dictionary[merchantItem.ItemId]);
                        }
                    }
                }
            }
            Vendor asVendor = BotPoi.Current.AsVendor;
            if (asVendor.Type != Vendor.VendorType.Food && asVendor.Type != Vendor.VendorType.Restock)
            {
                ForceBuy = false;
                return;
            }
            bool flag = StyxWoW.Me.PowerType == WoWPowerType.Mana || StyxWoW.Me.Class == WoWClass.Druid;
            int bestDrinkFromVendor = Merchant.GetBestDrinkFromVendor();
            int bestFoodFromVendor = Merchant.GetBestFoodFromVendor();
            if (bestDrinkFromVendor == -1 && bestFoodFromVendor == -1)
            {
                Logging.Write(StyxResources.Vendor_does_not_sell_food_or_water___Or_none_could_be_found__Blacklisting_it_);
                ProfileManager.CurrentProfile.VendorManager.Blacklist.Add(BotPoi.Current.AsVendor);
                BotPoi.Clear(StyxResources.Blacklisted_Vendor);
                ForceBuy = false;
                return;
            }
            if (Consumable.GetBestDrink(false) == null && bestDrinkFromVendor != -1 && flag)
            {
                MerchantItem merchantItemByIndex = Merchant.GetMerchantItemByIndex(bestDrinkFromVendor);
                Logging.Write(StyxResources.Buying___0____1__, new object[]
                {
                    CharacterSettings.Instance.DrinkAmount,
                    merchantItemByIndex.Name
                });
                if (!Merchant.BuyItem(bestDrinkFromVendor, CharacterSettings.Instance.DrinkAmount))
                {
                    ProfileManager.CurrentProfile.VendorManager.Blacklist.Add(BotPoi.Current.AsVendor);
                    BotPoi.Clear(StyxResources.Blacklisted_Vendor);
                    ForceBuy = false;
                    return;
                }
            }
            if (Consumable.GetBestFood(false) == null && bestFoodFromVendor != -1)
            {
                MerchantItem merchantItemByIndex2 = Merchant.GetMerchantItemByIndex(bestFoodFromVendor);
                Logging.Write(StyxResources.Buying___0____1__, new object[]
                {
                    CharacterSettings.Instance.FoodAmount,
                    merchantItemByIndex2.Name
                });
                if (!Merchant.BuyItem(bestFoodFromVendor, CharacterSettings.Instance.FoodAmount))
                {
                    ProfileManager.CurrentProfile.VendorManager.Blacklist.Add(BotPoi.Current.AsVendor);
                    BotPoi.Clear(StyxResources.Blacklisted_Vendor);
                    ForceBuy = false;
                    return;
                }
            }
            if ((bestDrinkFromVendor == -1 && Consumable.GetBestFood(false) != null) || (bestFoodFromVendor == -1 && Consumable.GetBestDrink(false) != null))
            {
                Logging.Write(StyxResources.Vendor_does_not_sell_food_or_water___Or_none_could_be_found__Blacklisting_it_);
                ProfileManager.CurrentProfile.VendorManager.Blacklist.Add(BotPoi.Current.AsVendor);
                BotPoi.Clear(StyxResources.Blacklisted_Vendor);
                return;
            }
            ForceBuy = false;
        }
    }
}
