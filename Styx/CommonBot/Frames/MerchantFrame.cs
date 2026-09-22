using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Styx.Common;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Frames
{
    public class MerchantFrame : Frame
    {
        public static readonly MerchantFrame Instance = new MerchantFrame();
        private readonly HashSet<uint> _manaFood = new HashSet<uint> { 81406U, 81414U };

        public MerchantFrame() : base("MerchantFrame") { }

        public WoWUnit Merchant => ObjectManager.GetObjectByGuid<WoWUnit>(StyxWoW.Memory.Read<ulong>(StyxWoW.Offsets.Statics.MerchantGuid));
        public int NumBuybackItems => StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumBuybackItems);
        public int MerchantNumItems => StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.MerchantNumItems);

        public void SellItemQualities(ItemQuality qualities, IEnumerable<string> nameExceptions, IEnumerable<uint> idExceptions)
        {
            if (!IsVisible)
                throw new InvalidOperationException("Cannot sell items while the vendor window is not open.");
            if (qualities == ItemQuality.None)
                return;

            var tests = new List<string>();
            if ((qualities & ItemQuality.Poor) != 0) tests.Add("quality == 0");
            if ((qualities & ItemQuality.Common) != 0) tests.Add("quality == 1");
            if ((qualities & ItemQuality.Uncommon) != 0) tests.Add("quality == 2");
            if ((qualities & ItemQuality.Rare) != 0) tests.Add("quality == 3");
            if ((qualities & ItemQuality.Epic) != 0) tests.Add("quality == 4");

            var qualityTest = new StringBuilder("(" + tests[0]);
            for (var i = 1; i < tests.Count; i++) qualityTest.Append(" or " + tests[i]);
            qualityTest.Append(")");

            var exceptions = new StringBuilder();
            if (nameExceptions != null || idExceptions != null)
            {
                var names = new HashSet<string>();
                var ids = new HashSet<uint>();
                exceptions.Append("local itemExceptions = {");
                var anyAdded = false;
                if (nameExceptions != null)
                {
                    foreach (var value in nameExceptions)
                    {
                        var name = value.ToLower();
                        if (string.IsNullOrEmpty(name) || !names.Add(name)) continue;
                        exceptions.Append("{n=\"" + Lua.Escape(name) + "\"},");
                        anyAdded = true;
                    }
                }
                if (idExceptions != null)
                {
                    foreach (var id in idExceptions)
                    {
                        if (!ids.Add(id)) continue;
                        exceptions.Append("{i=" + id + "},");
                        anyAdded = true;
                    }
                }
                if (anyAdded) exceptions.Remove(exceptions.Length - 1, 1);
                exceptions.Append("}");
            }

            Lua.DoString(string.Format("{0}for b=0,4 do for s=1,GetContainerNumSlots(b) do local itemLink = GetContainerItemLink(b, s) if itemLink then local _,_,_,_,id,_,_,_,_,_,_,_,_,_,name = string.find(itemLink, \"|?c?f?f?(%x*)|?H?([^:]*):?(%d+):?(%d*):?(%d*):?(%d*):?(%d*):?(%d*):?(%-?%d*):?(%-?%d*):?(%d*):?(%d*)|?h?%[?([^%[%]]*)%]?|?h?|?r?\") id = tonumber(id) name = string.lower(name) local _, _, quality = GetItemInfo(itemLink) if {1} then local skip = false if itemExceptions then for i=1, #itemExceptions do if (itemExceptions[i].i and id == itemExceptions[i].i) or (itemExceptions[i].n and name == itemExceptions[i].n) then skip = true break end end end if not skip then UseContainerItem(b, s) end end end end end", exceptions + " ", qualityTest), "WoW.lua");
        }

        public void Close() => Hide();
        public new void Hide() => Lua.DoString("CloseMerchant()", "WoW.lua");

        public bool BuyItem(int index, int amount)
        {
            var item = GetMerchantItemByIndex(index);
            var unitPrice = Math.Ceiling(item.BuyPrice / (float)item.Quantity);
            if (amount * unitPrice <= StyxWoW.Me.Copper)
            {
                var purchaseQuantity = item.IsCurrency ? item.Quantity : 1;
                var lua = string.Format("BuyMerchantItem({0}, {1})", item.Index + 1, purchaseQuantity);
                using (StyxWoW.Memory.AcquireFrame())
                    for (var i = 0; i < amount; i++) Lua.DoString(lua, "WoW.lua");
                return true;
            }
            Logging.Write(string.Format(StyxResources.Not_enough_money_to_buy__0_, amount));
            return false;
        }

        public bool BuyItem(string name, int amount)
        {
            if (!IsVisible) throw new Exception("Trying to buy items while merchant frame is closed.");
            if (amount == 0) return false;
            for (var i = 0; i < MerchantNumItems; i++)
            {
                var item = GetMerchantItemByIndex(i);
                if (item.Name == name && amount * Math.Ceiling(item.BuyPrice / (float)item.Quantity) <= StyxWoW.Me.Copper)
                    BuyItem(item.Index, amount);
            }
            return false;
        }

        public void SellItem(WoWItem item)
        {
            Lua.DoString("for b=0,4 do if GetBagName(b) then for s=1, GetContainerNumSlots(b) do local id = GetContainerItemID(b, s) if id then local _, stackCount = GetContainerItemInfo(b, s) if id == {0} and stackCount == {1} then UseContainerItem(b, s) end end end end end", item.Entry, item.StackCount);
        }

        public void RepairAllItems() => RepairAllItems(false);
        public void RepairAllItems(bool useGuildBankFunds) => Lua.DoString("RepairAllItems(" + (useGuildBankFunds ? "1" : "nil") + ")", "WoW.lua");

        public MerchantItem[] GetAllMerchantItems()
        {
            if (MerchantNumItems <= 0) return new MerchantItem[0];
            var items = new List<MerchantItem>();
            using (StyxWoW.Memory.AcquireFrame())
                for (var i = 0; i < MerchantNumItems; i++)
                {
                    var item = GetMerchantItemByIndex(i);
                    if (item != null) items.Add(item);
                }
            return items.ToArray();
        }

        public MerchantItem GetMerchantItemByIndex(int index)
        {
            if (index < 0 || index >= MerchantNumItems) return null;
            var executor = StyxWoW.Memory.Executor;
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                foreach (var line in StyxWoW.Memory.GenerateAsm(StyxWoW.Offsets.Functions.MerchantFrame__GetItemAtIndex, CallingConvention.Cdecl, index))
                    executor.AddLine(line);
                executor.Execute();
                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    var pointer = StyxWoW.Memory.Read<IntPtr>(executor.ReturnPointer);
                    return pointer == IntPtr.Zero ? null : new MerchantItem(StyxWoW.Memory.Read<MerchantItem.NativeMerchantItem>(pointer), index);
                }
            }
        }

        public WoWItem GetBuybackItem(int index)
        {
            if (index < 0 || index >= 12) throw new ArgumentOutOfRangeException("index");
            return StyxWoW.Me.Inventory.GetItemBySlot(StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.BuybackItemSlotIndexes + index * 4));
        }

        public int GetBestDrinkFromVendor()
        {
            try { return GetBestFromVendor("Drink", _manaFood); }
            catch (Exception) { return -1; }
        }

        public int GetBestFoodFromVendor()
        {
            try
            {
                var food = GetBestFromVendor("Food");
                return food == -1 ? GetBestFromVendor("Refreshment") : food;
            }
            catch (Exception) { return -1; }
        }

        private int GetBestFromVendor(string spellName, HashSet<uint> itemIds = null)
        {
            var index = -1;
            var bestLevel = -1;
            foreach (var item in GetAllMerchantItems())
            {
                if (item.ItemInfo == null) continue;
                var spell = WoWSpell.FromId(item.ItemInfo.SpellId[0]);
                if ((spell == null || spell.Name != spellName) && (itemIds == null || !itemIds.Contains(item.ItemId))) continue;
                GameError reason;
                if (StyxWoW.Me.CanUseItem(item.ItemId, out reason))
                {
                    if (item.ItemInfo.RequiredLevel <= bestLevel) continue;
                    bestLevel = item.ItemInfo.RequiredLevel;
                    index = item.Index;
                }
                else Logging.WriteDiagnostic("Can't use item with id:{0} Reason:{1}", item.ItemId, reason);
            }
            return index;
        }
    }
}
