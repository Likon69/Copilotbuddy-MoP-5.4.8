using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWChair : WoWSubObject
    {
        internal WoWChair(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public WoWPoint[] SlotPositions
        {
            get { return StyxWoW.Memory.ReadArray<WoWPoint>(BaseAddress + 16, ChairSlots); }
        }

        public int ChairSlots
        {
            get
            {
                WoWGameObject ownerObject = OwnerObject;
                int num;
                if (ownerObject != null && ownerObject.GetDataSlot(GameObjectDataSlot.NumChairSlots, out num))
                {
                    return num;
                }
                return 0;
            }
        }

        #region Nested Types

        private enum ChairOffsets
        {
            SlotPositions = 16
        }

        #endregion
    }
}
