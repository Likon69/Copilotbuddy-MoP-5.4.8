using System;
using System.Linq;

namespace Styx.CommonBot.CharacterManagement
{
    internal class EquipmentSwapSet
    {
        public EquipCandidate[] EquipScenarios { get; private set; }

        public float Gain
        {
            get { return this.EquipScenarios.Sum(s => s.Gain); }
        }

        public EquipmentSwapSet(EquipCandidate[] equipScenarios)
        {
            this.EquipScenarios = equipScenarios;
        }
    }
}
