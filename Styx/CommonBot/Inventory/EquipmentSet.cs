using System.Collections.Generic;
using System.Linq;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Inventory
{
    public class EquipmentSet
    {
        public EquipmentSet(int index) { Name = Lua.GetReturnVal<string>(string.Format("return GetEquipmentSetInfo({0})", index), 0U); }
        public string Name { get; private set; }

        public IEnumerable<WoWItem> Items
        {
            get
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    var ids = Lua.GetReturnValues(string.Format("return unpack(GetEquipmentSetItemIDs(\"{0}\"))", Name));
                    if (ids == null) yield break;
                    foreach (var value in ids)
                    {
                        uint id;
                        if (!uint.TryParse(value, out id) || id == 0) continue;
                        var item = StyxWoW.Me.CarriedItems.FirstOrDefault(candidate => candidate.Entry == id);
                        if (item != null) yield return item;
                    }
                }
            }
        }

        public bool IsEquipped => Lua.GetReturnVal<bool>(InfoLua, 2U);
        public int NumItems => Lua.GetReturnVal<int>(InfoLua, 3U);
        public int NumEquipped => Lua.GetReturnVal<int>(InfoLua, 4U);
        public int NumInInventory => Lua.GetReturnVal<int>(InfoLua, 5U);
        public int NumMissing => Lua.GetReturnVal<int>(InfoLua, 6U);
        public int NumIgnored => Lua.GetReturnVal<int>(InfoLua, 7U);
        private string InfoLua => string.Format("return GetEquipmentSetInfoByName(\"{0}\")", Name);

        public void UseEquipmentSet() => Lua.DoString("UseEquipmentSet(\"{0}\")", Name);
        public void DeleteEquipmentSet() => Lua.DoString("DeleteEquipmentSet(\"{0}\")", Name);
        public void SaveEquipmentSet() => Lua.DoString("SaveEquipmentSet(\"{0}\")", Name);

        public override string ToString()
        {
            using (StyxWoW.Memory.AcquireFrame())
                return string.Format("Name: {0} IsEquipped: {1} NumItems: {2} NumEquipped: {3} NumInInventory: {4} NumMissing: {5} NumIgnored: {6}", Name, IsEquipped, NumItems, NumEquipped, NumInInventory, NumMissing, NumIgnored);
        }
    }
}
