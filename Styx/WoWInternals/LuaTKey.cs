using System;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaTKey
    {
        internal LuaTKey(IntPtr address)
        {
            Address = address;
            _key = StyxWoW.Memory.Read<NativeLuaTKey>(address);
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaTKey>.Size; }
        }

        public IntPtr Address { get; private set; }

        public LuaType Type
        {
            get { return _key.Type; }
        }

        public LuaValue Value
        {
            get { return _val ?? (_val = new LuaValue(Address)); }
        }

        public LuaNode Next
        {
            get
            {
                if (!_nextConstructed)
                {
                    _next = _key.NextNodePtr != IntPtr.Zero ? new LuaNode(_key.NextNodePtr) : null;
                    _nextConstructed = true;
                }
                return _next;
            }
        }

        private NativeLuaTKey _key;

        private LuaNode _next;

        private bool _nextConstructed;

        private LuaValue _val;
    }
}
