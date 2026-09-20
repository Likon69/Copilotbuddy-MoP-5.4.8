using System;

namespace Styx.WoWInternals
{
    [Flags]
    public enum AlteracValleyLandmarkType
    {
        Unknown = 0,
        DunBaldar = 1,
        StormpikeAidStation = 2,
        DunBaldarNorthBunker = 4,
        DunBaldarSouthBunker = 8,
        StormpikeGraveyard = 16,
        IrondeepMine = 32,
        IcewingBunker = 64,
        StonehearthGraveyard = 128,
        StonehearthOutpost = 256,
        StonehearthBunker = 512,
        FrostwolfKeep = 1024,
        FrostwolfReliefHut = 2048,
        WestFrostwolfTower = 4096,
        EastFrostwolfTower = 8192,
        FrostwolfGraveyard = 16384,
        ColdtoothMine = 32768,
        TowerPoint = 65536,
        IcebloodTower = 131072,
        IcebloodGraveyard = 262144,
        IcebloodGarrison = 524288,
        IcewingCavern = 1048576,
        SnowfallGraveyard = 2097152,
        WildpawCavern = 4194304,
        AllianceTowers = 588,
        HordeTowers = 208896
    }
}
