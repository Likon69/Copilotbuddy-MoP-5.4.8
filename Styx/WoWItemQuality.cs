using System;
using Styx.Localization;

namespace Styx
{
    public enum WoWItemQuality : uint
    {
        [LocalizedDescription("PoorItemQuality")]
        Poor,

        [LocalizedDescription("CommonItemQuality")]
        Common,

        [LocalizedDescription("UncommonItemQuality")]
        Uncommon,

        [LocalizedDescription("RareItemQuality")]
        Rare,

        [LocalizedDescription("EpicItemQuality")]
        Epic,

        [LocalizedDescription("LegendaryItemQuality")]
        Legendary,

        [LocalizedDescription("ArtifactItemQuality")]
        Artifact,

        [LocalizedDescription("HeirloomItemQuality")]
        Heirloom
    }
}
