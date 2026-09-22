using System;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.CharacterManagement
{
    internal class EquipCandidate
    {
        public float CurrentScore { get; private set; }
        public WoWItem CurrentItem { get; private set; }
        public float CandidateScore { get; private set; }
        public WoWItem CandidateItem { get; private set; }
        public ItemInfo CandidateItemInfo { get; private set; }
        public ItemStats CandidateItemStats { get; private set; }
        public WoWInventorySlot Slot { get; private set; }

        public float Gain
        {
            get { return this.CandidateScore - this.CurrentScore; }
        }

        public EquipCandidate(WoWItem candidateItem, ItemInfo candidateItemInfo, ItemStats candidateItemStats, float candidateScore, WoWItem currentItem, float currentScore, WoWInventorySlot slot)
        {
            this.CandidateItem = candidateItem;
            this.CandidateItemInfo = candidateItemInfo;
            this.CandidateItemStats = candidateItemStats;
            this.CandidateScore = candidateScore;
            this.CurrentItem = currentItem;
            this.CurrentScore = currentScore;
            this.Slot = slot;
        }
    }
}
