namespace Styx.CommonBot.Frames
{
    public struct GossipEntry
    {
        public string Text;
        public GossipEntryType Type;
        public int Index;

        public enum GossipEntryType
        {
            Unknown,
            Banker,
            BattleMaster,
            Binder,
            Gossip,
            Healer,
            Petition,
            Tabard,
            Taxi,
            Trainer,
            Unlearn,
            Vendor
        }
    }
}
