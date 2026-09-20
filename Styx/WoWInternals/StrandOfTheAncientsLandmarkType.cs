using System;

namespace Styx.WoWInternals
{
    [Flags]
    public enum StrandOfTheAncientsLandmarkType
    {
        Unknown = 0,
        ChamberOfAncientRelics = 1,
        EastGraveyard = 2,
        GateOfTheBlueSapphire = 4,
        GateOfTheGreenEmerald = 8,
        GateOfThePurpleAmethyst = 16,
        GateOfTheRedSun = 32,
        GateOfTheYellowMoon = 64,
        SouthGraveyard = 128,
        WestGraveyard = 256,
        HordeDefense = 512,
        AllianceDefense = 1024,
        Gates = 124
    }
}
