using System;

namespace Styx.WoWInternals
{
    [Flags]
    public enum IsleOfConquestLandmarkType
    {
        Unknown = 0,
        AllianceGateFront = 1,
        AllianceGateWest = 2,
        AllianceGateEast = 4,
        AllianceKeep = 8,
        HordeGateFront = 16,
        HordeGateWest = 32,
        HordeGateEast = 64,
        HordeKeep = 128,
        Docks = 256,
        Hangar = 512,
        Quarry = 1024,
        Refinery = 2048,
        Workshop = 4096,
        HordeGates = 112,
        AllianceGates = 7,
        Gates = 119,
        Bases = 7936
    }
}
