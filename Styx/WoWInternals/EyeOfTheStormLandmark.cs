using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class EyeOfTheStormLandmark : AreaPoiLandmark
    {
        public EyeOfTheStormLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public EyeOfTheStormLandmarkType LandmarkType
        {
            get { return GetMarkType(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                switch (NormalIcon)
                {
                    case 6:
                        return LandmarkControlType.Uncontrolled;
                    case 9:
                    case 12:
                        return LandmarkControlType.InConflict;
                    case 10:
                        return LandmarkControlType.HordeControlled;
                    case 11:
                        return LandmarkControlType.AllianceControlled;
                }

                return LandmarkControlType.Unknown;
            }
        }

        private static EyeOfTheStormLandmarkType GetMarkType(WoWLandMark landmark)
        {
            uint entry = landmark.Entry;
            switch (entry)
            {
                case 1941U:
                case 1942U:
                case 1943U:
                case 1959U:
                case 1960U:
                    return EyeOfTheStormLandmarkType.BloodElfTower;
                case 1944U:
                case 1945U:
                case 1946U:
                case 1957U:
                    break;
                case 1947U:
                case 1948U:
                case 1949U:
                case 1955U:
                case 1956U:
                    return EyeOfTheStormLandmarkType.MageTower;
                case 1950U:
                case 1951U:
                case 1952U:
                case 1953U:
                case 1954U:
                    return EyeOfTheStormLandmarkType.DraeneiRuins;
                case 1958U:
                    return EyeOfTheStormLandmarkType.Unknown;
                default:
                    if (entry != 9158U)
                    {
                        return EyeOfTheStormLandmarkType.Unknown;
                    }
                    break;
            }

            return EyeOfTheStormLandmarkType.FelReaverRuins;
        }
    }
}
