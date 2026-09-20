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

        public LuaTable Globals
        {
            get { return _globals ?? (_globals = new LuaTValue(Address + (int)StyxWoW.Offsets.Misc.LuaStateGlobals).Value.Table); }
        }

        private LuaTable _globals;
    }
}
