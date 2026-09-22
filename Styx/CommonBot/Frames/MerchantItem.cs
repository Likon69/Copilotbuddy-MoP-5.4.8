using System;
using System.Runtime.InteropServices;
using Styx.WoWInternals.DB;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Frames
{
    public class MerchantItem
    {
        internal MerchantItem(NativeMerchantItem item, int index)
        {
            NativeItem = item;
            Index = index;
        }

        public int Index { get; private set; }
        public bool IsCurrency => NativeItem.ItemType == VendorItemType.Currency;
        public uint ItemId => NativeItem.ItemId;
        public uint TextureId => NativeItem.TextureId;
        public int NumAvailable => NativeItem.NumAvailable;
        public ulong BuyPrice => (ulong)NativeItem.Price;
        public int Quantity => NativeItem.Quantity;
        public uint ExtendedCostId => NativeItem.ItemExtendedCostId;

        private ItemInfo _cachedItemInfo;
        private CurrencyType _cachedCurrencyInfo;

        public ItemInfo ItemInfo => IsCurrency ? null : _cachedItemInfo ?? (_cachedItemInfo = ItemInfo.FromId(ItemId));
        public CurrencyType CurrencyInfo => !IsCurrency ? null : _cachedCurrencyInfo ?? (_cachedCurrencyInfo = CurrencyType.FromId(ItemId));
        public string Name => IsCurrency ? CurrencyInfo?.Name ?? "(null)" : ItemInfo?.Name ?? "(null)";

        public override string ToString()
        {
            return string.Format("[{0}] ItemId:{1} TextureId:{2} NumAvaible:{3} BuyPrice:{4} Quantity:{5} ExtendedCostId:{6}",
                Name, ItemId, TextureId, NumAvailable, BuyPrice, Quantity, ExtendedCostId);
        }

        internal readonly NativeMerchantItem NativeItem;

        internal enum VendorItemType
        {
            None,
            Item,
            Currency
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeMerchantItem
        {
            public uint Id;
            public VendorItemType ItemType;
            public uint ItemId;
            public uint TextureId;
            public int NumAvailable;
            public int Price;
            public int Unknown18;
            public int Quantity;
            public uint Unknown20;
            public uint ItemExtendedCostId;
            public uint Unknown28;
            public uint Unknown2C;

            public string Name => ItemType == VendorItemType.Currency
                ? CurrencyType.FromId(ItemId).Name
                : ItemInfo.FromId(ItemId).Name;

            public override string ToString()
            {
                return string.Format("Name: {12}, Id: {0}, ItemType: {1}, ItemId: {2}, TextureId: {3}, NumAvailable: {4}, Price: {5}, Dword18: {6}, Quantity: {7}, Dword20: {8}, ItemExtendedCostId: {9}, Dword28: {10}, Dword2C: {11}",
                    Id, ItemType, ItemId, TextureId, NumAvailable, Price, Unknown18, Quantity, Unknown20,
                    ItemExtendedCostId, Unknown28, Unknown2C, Name);
            }
        }
    }
}
