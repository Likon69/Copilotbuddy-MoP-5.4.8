using System;

namespace Styx.WoWInternals
{
    public static class WoWTotemExtensions
    {
        public static int GetTotemSpellId(this WoWTotem totem)
        {
            return (int)totem & 65535;
        }

        public static WoWTotemType GetTotemType(this WoWTotem totem)
        {
            return (WoWTotemType)((uint)totem >> 16);
        }
    }
}
