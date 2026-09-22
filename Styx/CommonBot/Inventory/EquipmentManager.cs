using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals;

namespace Styx.CommonBot.Inventory
{
    public static class EquipmentManager
    {
        public static readonly List<EquipmentSet> EquipmentSets = new List<EquipmentSet>();

        internal static void Initialize()
        {
            Lua.Events.AttachEvent("EQUIPMENT_SETS_CHANGED", EquipmentSetsChanged);
            EquipmentSets.Clear();
            using (StyxWoW.Memory.AcquireFrame())
                for (var i = 1; i <= Lua.GetReturnVal<int>("return GetNumEquipmentSets()", 0U); i++) EquipmentSets.Add(new EquipmentSet(i));
        }

        private static void EquipmentSetsChanged(object sender, LuaEventArgs args)
        {
            Lua.Events.DetachEvent("EQUIPMENT_SETS_CHANGED", EquipmentSetsChanged);
            Initialize();
        }

        public static bool IsEnabled => Lua.GetReturnVal<bool>("return CanUseEquipmentSets()", 0U);
        public static EquipmentSet ActiveSet
        {
            get { using (StyxWoW.Memory.AcquireFrame()) return EquipmentSets.FirstOrDefault(set => set.IsEquipped); }
        }

        public static bool EquipSet(string name)
        {
            using (StyxWoW.Memory.AcquireFrame())
            {
                var set = EquipmentSets.FirstOrDefault(candidate => candidate.Name.Equals(name));
                if (set == null) return false;
                set.UseEquipmentSet();
                return true;
            }
        }
    }
}
