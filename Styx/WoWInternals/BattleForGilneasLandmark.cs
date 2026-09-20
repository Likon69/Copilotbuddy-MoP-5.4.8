using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class BattleForGilneasLandmark : AreaPoiLandmark
    {
        public BattleForGilneasLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public BattleForGilneasLandmarkType LandmarkType
        {
            get { return smethod_1(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                switch (NormalIcon)
                {
                    case 6:
                    case 16:
                    case 26:
                        return LandmarkControlType.Uncontrolled;
                    default:
                        return LandmarkControlType.Unknown;
                    case 9:
                    case 12:
                    case 17:
                    case 19:
                    case 27:
                    case 29:
                        return LandmarkControlType.InConflict;
                    case 10:
                    case 20:
                    case 30:
                        return LandmarkControlType.HordeControlled;
                    case 11:
                    case 18:
                    case 28:
                        return LandmarkControlType.AllianceControlled;
                }
            }
        }

        private static BattleForGilneasLandmarkType smethod_1(WoWLandMark woWLandMark_0)
        {
            switch (woWLandMark_0.Entry)
            {
                case 2400U:
                case 2401U:
                case 2402U:
                case 2403U:
                case 2404U:
                    return BattleForGilneasLandmarkType.Mines;
                case 2405U:
                case 2406U:
                case 2407U:
                case 2408U:
                case 2409U:
                    return BattleForGilneasLandmarkType.Waterworks;
                case 2410U:
                case 2411U:
                case 2412U:
                case 2413U:
                case 2414U:
                    return BattleForGilneasLandmarkType.Lighthouse;
                default:
                    return BattleForGilneasLandmarkType.Unknown;
            }
        }
    }
}
