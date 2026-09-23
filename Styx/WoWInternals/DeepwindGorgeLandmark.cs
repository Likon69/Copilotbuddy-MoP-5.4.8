using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class DeepwindGorgeLandmark : AreaPoiLandmark
    {
        public DeepwindGorgeLandmark(int index, LandMarkEntry data)
            : base(index, data)
        {
        }

        public DeepwindGorgeLandmarkType LandmarkType
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

        private static DeepwindGorgeLandmarkType GetMarkType(WoWLandMark landmark)
        {
            switch (landmark.Entry)
            {
                case 2960U:
                case 2961U:
                case 2962U:
                case 2963U:
                case 2965U:
                    return DeepwindGorgeLandmarkType.PandarenMine;
                default:
                    return DeepwindGorgeLandmarkType.Unknown;
                case 2966U:
                case 2972U:
                case 2973U:
                case 2974U:
                case 2975U:
                    return DeepwindGorgeLandmarkType.CenterMine;
                case 2967U:
                case 2968U:
                case 2969U:
                case 2970U:
                case 2971U:
                    return DeepwindGorgeLandmarkType.GoblinMine;
            }
        }
    }
}
