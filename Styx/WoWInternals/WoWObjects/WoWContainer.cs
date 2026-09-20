using System;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWContainer : WoWItem
    {
        internal WoWContainer(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public WoWBag Bag
        {
            get { return new WoWBag(StyxWoW.Memory.Read<CGBag>(BaseAddress + (int)StyxWoW.Offsets.Misc.ContainerBagOffset)); }
        }

        public new int BagIndex
        {
            get
            {
                for (uint num = 0U; num <= 10U; num += 1U)
                {
                    if (StyxWoW.Me.GetBagGuidAtIndex(num) == Guid)
                    {
                        return (int)num;
                    }
                }
                return -1;
            }
        }

        public BagType BagType
        {
            get { return (BagType)ItemInfo.SubClassId; }
        }

        public uint Slots
        {
            get { return Bag.Slots; }
        }

        public uint UsedSlots
        {
            get { return Bag.UsedSlots; }
        }

        public ulong[] ItemGuids
        {
            get { return Bag.ItemGuids; }
        }

        public WoWItem[] Items
        {
            get { return Bag.Items; }
        }

        public ulong[] PhysicalItemGuids
        {
            get { return Bag.PhysicalItemGuids; }
        }

        public WoWItem[] PhysicalItems
        {
            get { return Bag.PhysicalItems; }
        }

        public uint FreeSlots
        {
            get { return Bag.FreeSlots; }
        }

        public ulong GetItemGuidBySlot(uint slot)
        {
            return Bag.GetItemGuidBySlot(slot);
        }

        public WoWItem GetItemBySlot(uint slot)
        {
            return Bag.GetItemBySlot(slot);
        }

        public uint SlotsFromDescriptor
        {
            get { return GetStorageField<uint>((uint)WoWContainerFields.NumSlots); }
        }

        public static implicit operator WoWBag(WoWContainer container)
        {
            if (!(container != null))
            {
                return null;
            }
            return container.Bag;
        }
    }
}
