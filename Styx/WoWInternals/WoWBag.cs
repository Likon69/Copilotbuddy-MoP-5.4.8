using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Resources;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWBag
    {
        internal WoWBag(CGBag bag)
            : this(bag, 0U, bag.SlotCount)
        {
        }

        internal WoWBag(CGBag bag, uint firstSlotIndex, uint slots)
        {
            _bag = bag;
            if (firstSlotIndex + slots > bag.SlotCount)
            {
                throw new ArgumentOutOfRangeException("slots");
            }

            FirstSlotIndex = firstSlotIndex;
            Slots = slots;
        }

        public uint FirstSlotIndex { get; private set; }

        public uint Slots { get; private set; }

        public string Name
        {
            get
            {
                if (!IsInventory)
                {
                    if (Guid != ObjectManager.LocalGuid)
                    {
                        WoWContainer objectByGuid = ObjectManager.GetObjectByGuid<WoWContainer>(Guid);
                        if (!(objectByGuid == null))
                        {
                            return objectByGuid.Name;
                        }

                        return StyxResources.Unknown;
                    }
                }

                return StyxResources.Inventory;
            }
        }

        public bool IsInventory
        {
            get { return _bag.IsInventory != 0; }
        }

        public ulong Guid
        {
            get { return _bag.Guid; }
        }

        public ulong[] ItemGuids
        {
            get { return StyxWoW.Memory.ReadArray<ulong>(_bag.ItemGuidArray + (int)(FirstSlotIndex * 8U), (int)Slots); }
        }

        public WoWItem[] Items
        {
            get
            {
                IEnumerable<ulong> itemGuids = ItemGuids;
                return itemGuids.Select(guid => ObjectManager.GetObjectByGuid<WoWItem>(guid)).ToArray();
            }
        }

        public ulong[] PhysicalItemGuids
        {
            get
            {
                IEnumerable<ulong> itemGuids = ItemGuids;
                return itemGuids.Where(guid => guid != 0UL).ToArray();
            }
        }

        public WoWItem[] PhysicalItems
        {
            get { return PhysicalItemGuids.Select(new Func<ulong, WoWItem>(ObjectManager.GetObjectByGuid<WoWItem>)).ToArray(); }
        }

        public uint FreeSlots
        {
            get { return Slots - UsedSlots; }
        }

        public uint UsedSlots
        {
            get
            {
                ulong[] itemGuids = ItemGuids;
                uint num = 0U;
                for (int i = 0; i < itemGuids.Length; i++)
                {
                    if (itemGuids[i] != 0UL)
                    {
                        num += 1U;
                    }
                }

                return num;
            }
        }

        public ulong GetItemGuidBySlot(uint slot)
        {
            if (slot >= Slots)
            {
                throw new ArgumentOutOfRangeException("slot");
            }

            return StyxWoW.Memory.Read<ulong>(_bag.ItemGuidArray + (int)((FirstSlotIndex + slot) * 8U));
        }

        public WoWItem GetItemBySlot(uint slot)
        {
            if (slot >= Slots)
            {
                throw new ArgumentOutOfRangeException("slot");
            }

            return ObjectManager.GetObjectByGuid<WoWItem>(StyxWoW.Memory.Read<ulong>(_bag.ItemGuidArray + (int)((FirstSlotIndex + slot) * 8U)));
        }

        public bool Contains(WoWItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return Contains(item.Guid);
        }

        public bool Contains(ulong itemGuid)
        {
            return IndexOf(itemGuid) != -1;
        }

        public int IndexOf(WoWItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return IndexOf(item.Guid);
        }

        public int IndexOf(ulong guid)
        {
            ulong[] itemGuids = ItemGuids;
            for (int i = 0; i < itemGuids.Length; i++)
            {
                if (itemGuids[i] == guid)
                {
                    return i;
                }
            }

            return -1;
        }

        private readonly CGBag _bag;
    }
}
