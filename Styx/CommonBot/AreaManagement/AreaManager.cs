using System.Collections.Generic;
using Styx.CommonBot.Profiles;

namespace Styx.CommonBot.AreaManagement
{
    public class AreaManager
    {
        private readonly List<Area> _areaList = new List<Area>();

        public void SetArea(GrindArea area)
        {
            _grindArea = area;
        }

        public void Add(Area value)
        {
            _areaList.Add(value);
        }

        private GrindArea _grindArea;

        public GrindArea CurrentGrindArea
        {
            get
            {
                return _grindArea ?? (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.GrindArea : null);
            }
        }
    }
}
