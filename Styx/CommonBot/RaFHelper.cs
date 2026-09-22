using System;
using System.Linq;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public static class RaFHelper
    {
        private static WoWPlayer _leader;

        public static WoWPlayer Leader
        {
            get { return _leader; }
        }

        public static void ClearLeader()
        {
            _leader = null;
        }

        public static void SetLeader(uint ptr)
        {
            _leader = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false)
                .FirstOrDefault(player => player.BaseAddress == (IntPtr)(long)(ulong)ptr);
        }

        public static void SetLeader(WoWPlayer unit)
        {
            _leader = unit;
        }

        public static void SetLeader(ulong guid)
        {
            SetLeader(ObjectManager.GetObjectByGuid<WoWPlayer>(guid));
        }

        public static void SetLeader(string keyword)
        {
            SetLeader(ObjectManager.GetUnitByKeyword<WoWPlayer>(keyword));
        }
    }
}
