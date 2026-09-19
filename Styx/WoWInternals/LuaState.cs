using System;

namespace Styx.WoWInternals
{
    public class LuaState
    {
        internal LuaState(IntPtr address)
        {
            Address = address;
        }

        public IntPtr Address { get; private set; }
    }
}
