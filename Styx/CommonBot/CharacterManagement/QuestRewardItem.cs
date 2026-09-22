using System;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.CharacterManagement
{
    internal class QuestRewardItem
    {
        public int Index { get; set; }
        public ItemInfo ItemInfo { get; set; }
        public string ItemLink { get; set; }
        public int Count { get; set; }
    }
}
