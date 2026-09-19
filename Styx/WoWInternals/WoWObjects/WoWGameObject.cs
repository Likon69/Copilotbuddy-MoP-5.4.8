using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWGameObject : WoWObject
    {
        public WoWGameObject(IntPtr baseAddress)
            : base(baseAddress)
        {
        }
    }
}
