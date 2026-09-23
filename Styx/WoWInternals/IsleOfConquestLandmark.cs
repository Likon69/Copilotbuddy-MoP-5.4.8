using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class IsleOfConquestLandmark : AreaPoiLandmark
    {
        public IsleOfConquestLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public IsleOfConquestLandmarkType LandmarkType
        {
            get { return GetMarkType(this); }
        }

        public LandmarkControlType ControlType
        {
            get
            {
                int normalIcon = NormalIcon;
                switch (normalIcon)
                {
                    case 6:
                    case 16:
                        return LandmarkControlType.Uncontrolled;
                    case 7:
                    case 8:
                    case 13:
                    case 14:
                    case 15:
                        break;
                    case 9:
                    case 12:
                    case 17:
                    case 19:
                        return LandmarkControlType.InConflict;
                    case 10:
                    case 20:
                        return LandmarkControlType.HordeControlled;
                    case 11:
                    case 18:
                        return LandmarkControlType.AllianceControlled;
                    default:
                        switch (normalIcon)
                        {
                            case 77:
                                return LandmarkControlType.HordeControlled;
                            case 78:
                            case 81:
                                break;
                            case 79:
                            case 82:
                                return LandmarkControlType.Destroyed;
                            case 80:
                                return LandmarkControlType.AllianceControlled;
                            default:
                                switch (normalIcon)
                                {
                                    case 135:
                                    case 140:
                                    case 145:
                                    case 150:
                                        return LandmarkControlType.Uncontrolled;
                                    case 136:
                                    case 141:
                                    case 146:
                                    case 151:
                                        return LandmarkControlType.AllianceControlled;
                                    case 137:
                                    case 139:
                                    case 142:
                                    case 144:
                                    case 147:
                                    case 149:
                                    case 152:
                                    case 154:
                                        return LandmarkControlType.InConflict;
                                    case 138:
                                    case 143:
                                    case 148:
                                    case 153:
                                        return LandmarkControlType.HordeControlled;
                                }
                                break;
                        }
                        break;
                }

                return LandmarkControlType.Unknown;
            }
        }

        private static IsleOfConquestLandmarkType GetMarkType(WoWLandMark landmark)
        {
            switch (landmark.Entry)
            {
                case 2345U:
                case 2346U:
                case 2347U:
                case 2348U:
                case 2349U:
                    return IsleOfConquestLandmarkType.Workshop;
                case 2350U:
                case 2352U:
                case 2353U:
                case 2354U:
                case 2355U:
                    return IsleOfConquestLandmarkType.Hangar;
                case 2356U:
                case 2357U:
                case 2358U:
                case 2359U:
                case 2360U:
                    return IsleOfConquestLandmarkType.Docks;
                case 2361U:
                case 2362U:
                case 2363U:
                case 2364U:
                case 2365U:
                    return IsleOfConquestLandmarkType.Quarry;
                case 2366U:
                case 2367U:
                case 2368U:
                case 2369U:
                case 2370U:
                    return IsleOfConquestLandmarkType.Refinery;
                case 2371U:
                case 2372U:
                    return IsleOfConquestLandmarkType.HordeGateFront;
                case 2373U:
                case 2374U:
                    return IsleOfConquestLandmarkType.HordeGateEast;
                case 2375U:
                case 2376U:
                    return IsleOfConquestLandmarkType.HordeGateWest;
                case 2377U:
                case 2379U:
                    return IsleOfConquestLandmarkType.AllianceGateEast;
                case 2378U:
                case 2382U:
                    return IsleOfConquestLandmarkType.AllianceGateFront;
                case 2380U:
                case 2381U:
                    return IsleOfConquestLandmarkType.AllianceGateWest;
                case 2383U:
                case 2384U:
                case 2385U:
                case 2386U:
                case 2387U:
                    return IsleOfConquestLandmarkType.AllianceKeep;
                case 2388U:
                case 2389U:
                case 2390U:
                case 2391U:
                case 2392U:
                    return IsleOfConquestLandmarkType.HordeKeep;
            }

            return IsleOfConquestLandmarkType.Unknown;
        }
    }
}
