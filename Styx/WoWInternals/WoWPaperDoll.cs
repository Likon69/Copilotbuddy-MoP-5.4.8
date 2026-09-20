using System;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWPaperDoll : WoWBag
    {
        internal WoWPaperDoll(CGBag bag)
            : base(bag)
        {
        }

        internal WoWPaperDoll(CGBag bag, uint firstSlotIndex, uint slots)
            : base(bag, firstSlotIndex, slots)
        {
        }

        public WoWItem GetEquippedItem(WoWInventorySlot slot)
        {
            return GetItemBySlot((uint)slot);
        }

        public WoWItem Head
        {
            get { return GetEquippedItem(WoWInventorySlot.Head); }
        }

        public WoWItem Neck
        {
            get { return GetEquippedItem(WoWInventorySlot.Neck); }
        }

        public WoWItem Shoulder
        {
            get { return GetEquippedItem(WoWInventorySlot.Shoulder); }
        }

        public WoWItem Shirt
        {
            get { return GetEquippedItem(WoWInventorySlot.Shirt); }
        }

        public WoWItem Chest
        {
            get { return GetEquippedItem(WoWInventorySlot.Chest); }
        }

        public WoWItem Waist
        {
            get { return GetEquippedItem(WoWInventorySlot.Waist); }
        }

        public WoWItem Legs
        {
            get { return GetEquippedItem(WoWInventorySlot.Legs); }
        }

        public WoWItem Feet
        {
            get { return GetEquippedItem(WoWInventorySlot.Feet); }
        }

        public WoWItem Wrist
        {
            get { return GetEquippedItem(WoWInventorySlot.Wrist); }
        }

        public WoWItem Hands
        {
            get { return GetEquippedItem(WoWInventorySlot.Hands); }
        }

        public WoWItem Finger1
        {
            get { return GetEquippedItem(WoWInventorySlot.Finger1); }
        }

        public WoWItem Finger2
        {
            get { return GetEquippedItem(WoWInventorySlot.Finger2); }
        }

        public WoWItem Trinket1
        {
            get { return GetEquippedItem(WoWInventorySlot.Trinket1); }
        }

        public WoWItem Trinket2
        {
            get { return GetEquippedItem(WoWInventorySlot.Trinket2); }
        }

        public WoWItem Back
        {
            get { return GetEquippedItem(WoWInventorySlot.Back); }
        }

        public WoWItem MainHand
        {
            get { return GetEquippedItem(WoWInventorySlot.MainHand); }
        }

        public WoWItem OffHand
        {
            get { return GetEquippedItem(WoWInventorySlot.OffHand); }
        }

        public WoWItem Ranged
        {
            get { return GetEquippedItem(WoWInventorySlot.Ranged); }
        }

        public WoWItem Tabard
        {
            get { return GetEquippedItem(WoWInventorySlot.Tabard); }
        }

        public WoWItem Bag1
        {
            get { return GetEquippedItem(WoWInventorySlot.Bag1); }
        }

        public WoWItem Bag2
        {
            get { return GetEquippedItem(WoWInventorySlot.Bag2); }
        }

        public WoWItem Bag3
        {
            get { return GetEquippedItem(WoWInventorySlot.Bag3); }
        }

        public WoWItem Bag4
        {
            get { return GetEquippedItem(WoWInventorySlot.Bag4); }
        }
    }
}
