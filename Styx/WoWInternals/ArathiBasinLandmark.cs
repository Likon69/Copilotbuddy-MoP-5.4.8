using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class ArathiBasinLandmark : AreaPoiLandmark
    {
        public ArathiBasinLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public ArathiBasinLandmarkType LandmarkType
        {
            get { return smethod_1(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                switch (NormalIcon)
                {
                    case 16:
                    case 21:
                    case 26:
                    case 31:
                    case 36:
                        return LandmarkControlType.Uncontrolled;
                    case 17:
                    case 19:
                    case 22:
                    case 24:
                    case 27:
                    case 29:
                    case 32:
                    case 34:
                    case 37:
                    case 39:
                        return LandmarkControlType.InConflict;
                    case 18:
                    case 23:
                    case 28:
                    case 33:
                    case 38:
                        return LandmarkControlType.AllianceControlled;
                    case 20:
                    case 25:
                    case 30:
                    case 35:
                    case 40:
                        return LandmarkControlType.HordeControlled;
                    default:
                        return LandmarkControlType.Unknown;
                }
            }
        }

        private static ArathiBasinLandmarkType smethod_1(WoWLandMark woWLandMark_0)
        {
            uint entry = woWLandMark_0.Entry;
            if (entry >= 1609U && entry <= 1613U)
            {
                return ArathiBasinLandmarkType.Stables;
            }

            if (entry >= 1614U && entry <= 1618U)
            {
                return ArathiBasinLandmarkType.Blacksmith;
            }

            if (entry >= 1619U && entry <= 1623U)
            {
                return ArathiBasinLandmarkType.LumberMill;
            }

            if (entry >= 1624U && entry <= 1628U)
            {
                return ArathiBasinLandmarkType.GoldMine;
            }

            if (entry >= 1629U && entry <= 1633U)
            {
                return ArathiBasinLandmarkType.Farm;
            }

            return ArathiBasinLandmarkType.Unknown;
        }
    }
}
