using System.Linq;
using Styx.CommonBot.Frames;

namespace Styx.CommonBot.POI
{
    public static class PoiTypeExtensions
    {
        public static GossipEntry.GossipEntryType GetGossipType(this PoiType type)
        {
            switch (type)
            {
                case PoiType.Buy:
                case PoiType.Sell:
                case PoiType.Repair:
                    return GossipEntry.GossipEntryType.Vendor;
                case PoiType.Train:
                    return GossipEntry.GossipEntryType.Trainer;
                case PoiType.Mail:
                    return GossipEntry.GossipEntryType.Gossip;
                case PoiType.Fly:
                    var gf = GossipFrame.Instance;
                    if (gf.IsVisible)
                    {
                        var entries = gf.GossipOptionEntries.Any(g => g.Type == GossipEntry.GossipEntryType.Taxi);
                        if (!entries)
                            return GossipEntry.GossipEntryType.Gossip;
                    }
                    return GossipEntry.GossipEntryType.Taxi;
                default:
                    return GossipEntry.GossipEntryType.Unknown;
            }
        }

        public static bool IsOneOf(this PoiType type, params PoiType[] types)
        {
            return types.Contains(type);
        }
    }
}
