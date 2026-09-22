using System;

namespace Styx.CommonBot.CharacterManagement
{
    internal class EquipResult
    {
        public EquipResultType Type { get; private set; }
        public EquipCandidate EquipScenario { get; private set; }
        public EquipmentSwapSet InterchangableEquipScenario { get; private set; }
        public WoWInventorySlot ForcedSlot { get; private set; }

        private EquipResult(EquipResultType type)
        {
            this.Type = type;
        }

        public static EquipResult CreateForceSlot(WoWInventorySlot slot)
        {
            return new EquipResult(EquipResultType.ForceSlot)
            {
                ForcedSlot = slot
            };
        }

        public static EquipResult CreateSingleSlot(EquipCandidate candidate)
        {
            return new EquipResult(EquipResultType.SingleSlot)
            {
                EquipScenario = candidate
            };
        }

        public static EquipResult CreateInterchangable(EquipmentSwapSet swapSet)
        {
            return new EquipResult(EquipResultType.Interchangable)
            {
                InterchangableEquipScenario = swapSet
            };
        }
    }
}
