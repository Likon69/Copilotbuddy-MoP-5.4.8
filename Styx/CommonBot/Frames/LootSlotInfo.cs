using System;
using System.Collections.Generic;
using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class LootSlotInfo
    {
        private readonly List<string> _info;

        internal LootSlotInfo(int index)
        {
            index++;
            _info = Lua.GetReturnValues("return GetLootSlotInfo(" + index + ")", "WoW.lua");
        }

        public string LootIcon
        {
            get { return _info[0] != "" ? _info[0] : ""; }
        }

        public string LootName
        {
            get { return _info[1] != "" ? _info[1] : ""; }
        }

        public int LootQuantity
        {
            get { return _info[2] != "" ? int.Parse(_info[2]) : 0; }
        }

        public LootRarity LootRarity
        {
            get { return _info[3] != "" ? (LootRarity)Enum.Parse(typeof(LootRarity), _info[3]) : LootRarity.Unknown; }
        }

        public bool Locked
        {
            get { return _info[4] != "" && _info[4] == "1"; }
        }
    }
}
