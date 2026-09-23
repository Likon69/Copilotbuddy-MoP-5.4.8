using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Bots.Grind;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Database;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.ObjectDatabase;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Bots.DungeonBuddy
{
    partial class DungeonBot
    {
        private static bool ShouldSellPreemptively
        {
            get
            {
                if (!IsFarming && !IsInInstance && Me.FreeBagSlots <= DungeonBuddySettings.Instance.OutsideInstanceMinFreeBagSlots)
                    return ItemsToSellCount > 0;
                return false;
            }
        }

        private static bool ShouldMailPreemptively
        {
            get
            {
                if (!IsFarming && !IsInInstance && Me.FreeBagSlots <= DungeonBuddySettings.Instance.OutsideInstanceMinFreeBagSlots)
                    return ItemsToMailCount > 0;
                return false;
            }
        }

        private static bool ShouldRepairPreemptively
        {
            get
            {
                if (!IsFarming && !IsInInstance)
                    return Me.LowestDurabilityPercent < DungeonBuddySettings.Instance.OutsideInstanceRepairPercent / 100;
                return false;
            }
        }

        private static bool ShouldBuyConsumablesPreemptively
        {
            get
            {
                if (!IsFarming && !IsInInstance)
                {
                    if (CharacterSettings.Instance.DrinkAmount > 0 && CharacterSettings.Instance.DrinkAmount * 0.5 >= CarriedDrinkCount)
                        return true;

                    if (CharacterSettings.Instance.FoodAmount > 0)
                        return CharacterSettings.Instance.FoodAmount * 0.5 >= CarriedFoodCount;

                    return false;
                }

                return false;
            }
        }

        private Composite CreateSellBuyRepairMailBehavior()
        {
            WoWUnit vendor = null;

            return new PrioritySelector(
                new DecoratorIsPoiType(PoiType.Mail, CreateMailBehavior()),
                new DecoratorIsPoiType(
                    new[] { PoiType.Buy, PoiType.Repair, PoiType.Sell },
                    new PrioritySelector(
                        ctx => vendor = BotPoi.Current.AsObject as WoWUnit,
                        new Decorator(ctx => BotPoi.Current.Type != PoiType.Buy, new ActionRunCoroutine(ctx => LevelBot.MountVendor(BotPoi.Current.Type))),
                        new Decorator(ctx => BotPoi.Current.Location.DistanceSqr(Me.Location) > 4 * 4 || (vendor != null && !HasLineOfSightTo(vendor)), CreateMovetoPoiBehavior()),
                        new Decorator(
                            ctx => vendor == null || !vendor.IsValid,
                            new Sequence(
                                new Action(ctx => Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), "No NPC found at location")),
                                new ActionClearPoi("Vendor Blacklisted"))),
                        new Decorator(
                            ctx => !vendor.IsVendor,
                            new Sequence(
                                new Action(ctx => Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), "NPC is not a vendor")),
                                new ActionClearPoi("Vendor Blacklisted"))),
                        new Decorator(ctx => Me.CurrentTarget == null || Me.CurrentTarget != vendor, new Action(ctx => vendor.Target())),
                        new DecoratorIsPoiType(new[] { PoiType.Buy, PoiType.Sell, PoiType.Repair }, CreateVendorBehavior()))));
        }

        private static Composite CreateVendorBehavior()
        {
            WoWUnit vendor = null;
            return new PrioritySelector(
                ctx => vendor = BotPoi.Current.AsObject.ToUnit(),
                new Decorator(ctx => !MerchantFrame.Instance.IsVisible, new ActionRunCoroutine(ctx => InteractWithVendor(vendor))),
                new Decorator(
                    ctx => MerchantFrame.Instance.IsVisible,
                    new PrioritySelector(
                        new DecoratorIsPoiType(PoiType.Buy, new ActionRunCoroutine(ctx => BuyConsumables(vendor))),
                        new DecoratorIsPoiType(new[] { PoiType.Repair, PoiType.Sell }, new ActionRunCoroutine(ctx => RepairAndSellItems(vendor))))));
        }

        private static async Task<bool> InteractWithVendor(WoWUnit vendor)
        {
            if (MerchantFrame.Instance.IsVisible)
                return false;

            await ScriptHelpers.StopMovingIfMoving();
            vendor.Interact();

            if (await Coroutine.Wait(4000, () => MerchantFrame.Instance.IsVisible || GossipFrame.Instance.IsVisible))
            {
                if (MerchantFrame.Instance.IsVisible)
                    return true;

                if (await SelectGossipOption(GossipEntry.GossipEntryType.Vendor))
                    return true;
            }

            string vendorLabel = $"{BotPoi.Current.Name} [{BotPoi.Current.Entry}]";
            Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), $"Vendor {vendorLabel} doesn't offer a merchant frame");
            BotPoi.Clear("Vendor blacklisted");
            return true;
        }

        private static async Task<bool> SelectGossipOption(GossipEntry.GossipEntryType gossipEntryType)
        {
            if (!GossipFrame.Instance.IsVisible)
                return false;

            List<GossipEntry> gossipOptionEntries = GossipFrame.Instance.GossipOptionEntries;
            if (gossipOptionEntries == null)
                return false;

            foreach (GossipEntry gossipEntry in gossipOptionEntries)
            {
                if (gossipEntry.Type != gossipEntryType)
                    continue;

                GossipFrame.Instance.SelectGossipOption(gossipEntry.Index);
                return await Coroutine.Wait(2000, () => !GossipFrame.Instance.IsVisible);
            }

            return false;
        }

        private static bool HasLineOfSightTo(WoWObject obj)
        {
            return !GameWorld.TraceLine(StyxWoW.Me.GetTraceLinePos(), obj.Location.Add(0, 0, 2.132f), TraceLineHitFlags.DoodadCollision | TraceLineHitFlags.WmoCollision | TraceLineHitFlags.Terrain);
        }

        private static Mailbox FindNearestMailbox()
        {
            MailboxResult nearestMailbox = Query.GetNearestMailbox(Me.MapId, Me.Location);
            return nearestMailbox != null ? new Mailbox(nearestMailbox.Location) : null;
        }

        private static Vendor FindNearestVendor(Vendor.VendorType vendorType)
        {
            switch (vendorType)
            {
                case Vendor.VendorType.Repair:
                case Vendor.VendorType.Sell:
                    {
                        NpcResult nearestNpc = Query.GetNearestNpc(Me.MapId, Me.Location, UnitNPCFlags.Repair);
                        if (nearestNpc != null)
                            return new Vendor(nearestNpc.Entry, nearestNpc.Name, vendorType, nearestNpc.Location);
                        break;
                    }
                case Vendor.VendorType.Food:
                case Vendor.VendorType.Restock:
                    {
                        NpcResult nearestNpc = Query.GetNearestNpc(Me.MapId, Me.Location, UnitNPCFlags.FoodVendor);
                        if (nearestNpc != null)
                            return new Vendor(nearestNpc.Entry, nearestNpc.Name, vendorType, nearestNpc.Location);
                        break;
                    }
            }

            return null;
        }

        private static Composite CreateMovetoPoiBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ctx => BotPoi.Current.Location != WoWPoint.Zero,
                    new Action(
                        ctx =>
                        {
                            if (Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(BotPoi.Current.Location)) == RunStatus.Failure &&
                                (BotPoi.Current.Type == PoiType.Sell || BotPoi.Current.Type == PoiType.Buy || BotPoi.Current.Type == PoiType.Repair))
                            {
                                Vendor asVendor = BotPoi.Current.AsVendor;
                                if (asVendor != null)
                                {
                                    Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), $"Vendor: {asVendor.Name} can't be navigated to");
                                    BotPoi.Clear("Vendor Blacklisted");
                                }
                            }

                            return RunStatus.Success;
                        })));
        }

        #region Mail

        private static int SendMailCount { get { return Lua.GetReturnVal<int>("local cnt = 0 for i=1, ATTACHMENTS_MAX_SEND do if GetSendMailItem(i) then cnt=cnt+1 end end return cnt", 0); } }

        private static int ItemsToMailCount
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CharacterSettings.Instance.MailRecipient))
                    return 0;
                return GetItemsToMailList().Length;
            }
        }

        private static Composite CreateMailBehavior()
        {
            WoWGameObject mailbox = null;
            WoWItem[] itemsToMail = null;

            return new PrioritySelector(
                ctx => mailbox = BotPoi.Current.AsObject as WoWGameObject,
                new Decorator(ctx => BotPoi.Current.Location.DistanceSqr(Me.Location) > 4 * 4 || (mailbox != null && !HasLineOfSightTo(mailbox)), CreateMovetoPoiBehavior()),
                new Decorator(
                    ctx => mailbox == null,
                    new Sequence(
                        new Action(ctx => Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromDays(7), $"Mailbox not found at location: {BotPoi.Current.Location}")),
                        new ActionClearPoi("Mailbox blacklisted"))),
                new Decorator(ctx => !MailFrame.Instance.IsVisible, new Action(ctx => mailbox.Interact())),
                new Decorator(
                    ctx => MailFrame.Instance != null && MailFrame.Instance.IsVisible,
                    new PrioritySelector(
                        ctx => itemsToMail = GetItemsToMailList(),
                        new Decorator(
                            ctx => itemsToMail.Length > 0,
                            new Sequence(
                                new Action(
                                    ctx =>
                                    {
                                        using (StyxWoW.Memory.AcquireFrame())
                                        {
                                            MailFrame.Instance.SwitchToSendMailTab();
                                            foreach (var item in itemsToMail.Take(12))
                                            {
                                                item.UseContainerItem();
                                            }
                                        }
                                    }),
                                new WaitContinue(2, ctx => false, new ActionAlwaysSucceed()),
                                new Action(ctx => SendMail()))),
                        new Decorator(
                            ctx => itemsToMail.Length == 0,
                            new Sequence(
                                new Action(ctx => SendMail()),
                                new Action(ctx => BotPoi.Clear()),
                                new DecoratorContinue(
                                    ctx => ItemsToSellCount > 0,
                                    new Sequence(
                                        ctx => FindNearestVendor(Vendor.VendorType.Repair),
                                        new DecoratorContinue(ctx => ctx is Vendor, new ActionSetPoi(true, ctx => new BotPoi((Vendor)ctx, PoiType.Sell))))))))));
        }

        private static void SendMail()
        {
            var subject = Lua.GetReturnVal<string>("GetSendMailItem(1)", 0);

            Lua.DoString(
                "for i=1,ATTACHMENTS_MAX_SEND do if GetSendMailItem(i) ~= nil then SendMail (\"{0}\",\"{1}\",'') end end ",
                CharacterSettings.Instance.MailRecipient.ToFormatedUTF8(),
                subject != null ? subject.ToFormatedUTF8() : " ");
        }

        private static WoWItem[] GetItemsToMailList()
        {
            return Me.BagItems.Where(item => ShouldMailItem(item) && !item.IsDisabled).ToArray();
        }

        private static bool ShouldMailItem(WoWItem item)
        {
            if (item == null || !item.IsValid)
                return false;

            if (item.IsSoulbound)
                return false;

            if (DungeonBuddySettings.Instance.MailBoeItems && item.ItemInfo.Bond == WoWItemBondType.OnEquip && !ProtectedItemsManager.Contains(item.Entry))
            {
                try
                {
                    return item.Quality >= DungeonBuddySettings.Instance.MailItemQuality;
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }

            return ForceMailManager.Contains(item.Entry);
        }

        #endregion

        #region Sell

        private static int ItemsToSellCount { get { return GetItemsToSellList().Length; } }

        private static WoWItem[] GetItemsToSellList()
        {
            var items = Me.BagItems.Where(ShouldSellItem).ToList();
            items.AddRange(GetExcessConsumablesToSell());
            return items.ToArray();
        }

        private static IEnumerable<WoWItem> GetExcessConsumablesToSell()
        {
            uint carriedFood = 0;
            uint carriedDrink = 0;

            var candidates =
                from item in Me.BagItems
                where item != null && item.ItemInfo != null
                let isFood = Consumable.IsFood(item.ItemInfo)
                let isDrink = Consumable.IsDrink(item.ItemInfo)
                where (isFood || isDrink) && item.ItemInfo.SellPrice > 0 && item.ItemSpells.Count == 1 && item.ItemInfo.RequiredLevel <= Me.Level
                orderby item.ItemInfo.RequiredLevel descending, item.StackCount descending
                select new { Item = item, IsFood = isFood, IsDrink = isDrink };

            var itemsToSell = new List<WoWItem>();
            foreach (var candidate in candidates)
            {
                bool isBoth = candidate.IsFood && candidate.IsDrink;
                int desiredAmount = isBoth
                    ? Math.Max(CharacterSettings.Instance.FoodAmount, CharacterSettings.Instance.DrinkAmount)
                    : candidate.IsFood ? CharacterSettings.Instance.FoodAmount : CharacterSettings.Instance.DrinkAmount;
                uint carriedAmount = isBoth ? Math.Min(carriedFood, carriedDrink) : candidate.IsFood ? carriedFood : carriedDrink;

                if (carriedAmount > desiredAmount)
                    itemsToSell.Add(candidate.Item);

                if (isBoth)
                {
                    carriedFood += candidate.Item.StackCount;
                    carriedDrink += candidate.Item.StackCount;
                }
                else if (candidate.IsFood)
                {
                    carriedFood += candidate.Item.StackCount;
                }
                else
                {
                    carriedDrink += candidate.Item.StackCount;
                }
            }

            IEnumerable<WoWItem> excessPotions = Me.BagItems.Where(item =>
                item != null && item.IsValid && item.ItemInfo != null && item.ItemInfo.SellPrice > 0 && item.ItemSpells.Count == 1 &&
                (item.ItemInfo.ConsumableClass == WoWItemConsumableClass.Potion || item.ItemInfo.ConsumableClass == WoWItemConsumableClass.Elixir || item.ItemInfo.ConsumableClass == WoWItemConsumableClass.Flask) &&
                item.ItemInfo.RequiredLevel < Me.Level - 20);

            itemsToSell.AddRange(excessPotions);
            return itemsToSell;
        }

        private static bool ShouldSellItem(WoWItem item)
        {
            if (item == null)
                return false;

            try
            {
                if (item.Quality > DungeonBuddySettings.Instance.SellItemQuality)
                    return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(CharacterSettings.Instance.MailRecipient) && ShouldMailItem(item))
                return false;

            if (item.ItemInfo.Bond != WoWItemBondType.Quest && item.ItemInfo.ItemClass != WoWItemClass.Key && !item.IsConjured && item.ItemInfo.BeginQuestId == 0 && item.ItemInfo.SellPrice > 0)
                return !ProtectedItemsManager.Contains(item.Entry);

            return false;
        }

        private static void SellItems(IEnumerable<WoWItem> items)
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                foreach (WoWItem woWItem in items.Where(i => i != null))
                {
                    woWItem.UseContainerItem();
                }
            }
        }

        #endregion

        #region Buy

        private static int CarriedFoodCount { get { return (int)Me.CarriedItems.Sum(i => i != null && i.IsValid && Consumable.IsFood(i.ItemInfo) ? i.StackCount : 0); } }

        private static int CarriedDrinkCount { get { return (int)Me.CarriedItems.Sum(i => i != null && i.IsValid && Consumable.IsDrink(i.ItemInfo) ? i.StackCount : 0); } }

        private static async Task<bool> BuyConsumables(WoWUnit vendor)
        {
            bool needsFood = CharacterSettings.Instance.FoodAmount > 0 && CharacterSettings.Instance.FoodAmount * 0.5 >= CarriedFoodCount;

            List<ItemInfo> merchantItems = await GetListOfMerchantItemInfos();
            ItemInfo itemInfo = needsFood ? GetBestFood(merchantItems) : GetBestDrink(merchantItems);
            if (itemInfo == null)
            {
                Blacklist.Add(vendor.Entry, vendor.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), "No suitable consumable found on merchant");
                BotPoi.Clear("Vendor Blacklisted");
                return true;
            }

            int quantity = needsFood ? CharacterSettings.Instance.FoodAmount - CarriedFoodCount : CharacterSettings.Instance.DrinkAmount - CarriedDrinkCount;
            ScriptHelpers.BuyItem(itemInfo.Id, quantity);
            await Coroutine.Sleep(2000);

            BotPoi.Clear("Bought consumables");
            return true;
        }

        private static async Task<bool> RepairAndSellItems(WoWUnit vendor)
        {
            if (BotPoi.Current.Type == PoiType.Repair && !vendor.IsRepairMerchant)
            {
                Blacklist.Add(BotPoi.Current.Entry, BotPoi.Current.Location, BlacklistFlags.NpcDatabase, TimeSpan.FromMinutes(15), "Npc cannot repair");
                BotPoi.Clear("Vendor Blacklisted");
                return true;
            }

            MerchantFrame.Instance.RepairAllItems();

            WoWItem[] itemsToSell = GetItemsToSellList();
            if (itemsToSell != null && itemsToSell.Any())
            {
                uint freeBagSlots = StyxWoW.Me.FreeBagSlots;
                SellItems(itemsToSell);
                await Coroutine.Wait(5000, () => StyxWoW.Me.FreeBagSlots < freeBagSlots);
            }

            if (ItemsToMailCount > 0)
            {
                Mailbox mailbox = FindNearestMailbox();
                if (mailbox != null)
                {
                    BotPoi.Current = new BotPoi(mailbox);
                    return true;
                }
            }

            BotPoi.Clear("Done repairing + selling");
            return true;
        }

        private static async Task<List<ItemInfo>> GetListOfMerchantItemInfos()
        {
            var itemInfos = new List<ItemInfo>();
            var merchantItems = MerchantFrame.Instance.GetAllMerchantItems().ToList();

            while (merchantItems.Any())
            {
                for (int i = merchantItems.Count - 1; i >= 0; i--)
                {
                    MerchantItem merchantItem = merchantItems[i];
                    if (merchantItem.ItemInfo != null)
                    {
                        itemInfos.Add(merchantItem.ItemInfo);
                        merchantItems.RemoveAt(i);
                    }
                }

                await Coroutine.Yield();
            }

            return itemInfos;
        }

        private static ItemInfo GetBestFood(IEnumerable<ItemInfo> itemInfos) { return itemInfos.Where(i => Consumable.IsFood(i) && i.RequiredLevel <= Me.Level && Me.Level - i.RequiredLevel <= 20).OrderByDescending(i => i.RequiredLevel).FirstOrDefault(); }

        private static ItemInfo GetBestDrink(IEnumerable<ItemInfo> itemInfos) { return itemInfos.Where(i => Consumable.IsDrink(i) && i.RequiredLevel <= Me.Level && Me.Level - i.RequiredLevel <= 20).OrderByDescending(i => i.RequiredLevel).FirstOrDefault(); }

        #endregion
    }
}
