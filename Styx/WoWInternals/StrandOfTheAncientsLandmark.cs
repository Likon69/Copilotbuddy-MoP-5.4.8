using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class StrandOfTheAncientsLandmark : AreaPoiLandmark
    {
        public StrandOfTheAncientsLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public StrandOfTheAncientsLandmarkType LandmarkType
        {
            get { return smethod_1(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                switch (WorldState)
                {
                    case 2U:
                        return LandmarkControlType.InConflict;
                    case 3U:
                    case 6U:
                        return LandmarkControlType.Destroyed;
                }

                switch (NormalIcon)
                {
                    case 13:
                        return LandmarkControlType.HordeControlled;
                    case 15:
                        return LandmarkControlType.AllianceControlled;
                }

                return LandmarkControlType.Unknown;
            }
        }

        private static StrandOfTheAncientsLandmarkType smethod_1(WoWLandMark woWLandMark_0)
        {
            uint entry = woWLandMark_0.Entry;
            if (entry > 2114U)
            {
                switch (entry)
                {
                    case 2117U:
                        return StrandOfTheAncientsLandmarkType.GateOfTheBlueSapphire;
                    case 2118U:
                    case 2119U:
                    case 2121U:
                    case 2122U:
                    case 2123U:
                    case 2124U:
                    case 2125U:
                    case 2126U:
                    case 2131U:
                        return StrandOfTheAncientsLandmarkType.Unknown;
                    case 2120U:
                        return StrandOfTheAncientsLandmarkType.GateOfTheGreenEmerald;
                    case 2127U:
                        return StrandOfTheAncientsLandmarkType.HordeDefense;
                    case 2128U:
                        return StrandOfTheAncientsLandmarkType.AllianceDefense;
                    case 2129U:
                    case 2130U:
                        return StrandOfTheAncientsLandmarkType.EastGraveyard;
                    case 2132U:
                        break;
                    case 2133U:
                    case 2134U:
                        return StrandOfTheAncientsLandmarkType.SouthGraveyard;
                    case 2135U:
                        return StrandOfTheAncientsLandmarkType.GateOfTheYellowMoon;
                    default:
                        if (entry == 2292U)
                        {
                            return StrandOfTheAncientsLandmarkType.ChamberOfAncientRelics;
                        }
                        if (entry != 2324U)
                        {
                            return StrandOfTheAncientsLandmarkType.Unknown;
                        }
                        break;
                }

                return StrandOfTheAncientsLandmarkType.WestGraveyard;
            }

            if (entry == 2111U)
            {
                return StrandOfTheAncientsLandmarkType.GateOfThePurpleAmethyst;
            }

            if (entry == 2114U)
            {
                return StrandOfTheAncientsLandmarkType.GateOfTheRedSun;
            }

            return StrandOfTheAncientsLandmarkType.Unknown;
        }
    }
}
