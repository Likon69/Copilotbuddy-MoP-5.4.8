using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.Patchables;
using Styx.WoWInternals;

namespace Styx.CommonBot
{
    public class Landmarks
    {
        public Landmarks()
        {
            Refresh();
        }

        public WoWLandMark GetLandmarkById(uint id)
        {
            Refresh();
            return LandmarkList.FirstOrDefault(landMark => landMark.Entry == id);
        }

        public int NumMapLandmarks
        {
            get { return StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.NumMapLandmarks); }
        }

        public void Refresh()
        {
            LandmarkList.Clear();
            int numMapLandmarks = NumMapLandmarks;
            LandMarkEntry[] array = StyxWoW.Memory.ReadArray<LandMarkEntry>(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LandMarksArray), numMapLandmarks);

            for (uint i = 0; i < numMapLandmarks; i++)
            {
                LandMarkEntry landMarkEntry = array[(int)i];
                WoWLandMark woWLandMark;

                switch (landMarkEntry.Type)
                {
                    case LandmarkType.Other:
                    case LandmarkType.TaxiNodes:
                    case LandmarkType.Unknown:
                        woWLandMark = null;
                        break;
                    case LandmarkType.ResearchSite:
                        woWLandMark = new ResearchSiteLandmark((int)i, array[(int)i]);
                        break;
                    case LandmarkType.BattlePetTamer:
                    default:
                        woWLandMark = new AreaPoiLandmark((int)i, array[(int)i]);
                        break;
                }

                if (woWLandMark != null)
                {
                    LandmarkList.Add(woWLandMark);
                }
            }
        }

        public void Display()
        {
            foreach (WoWLandMark woWLandMark in LandmarkList)
            {
                Logging.Write(woWLandMark.ToString());
            }
        }

        public readonly List<WoWLandMark> LandmarkList = new List<WoWLandMark>();
    }
}
