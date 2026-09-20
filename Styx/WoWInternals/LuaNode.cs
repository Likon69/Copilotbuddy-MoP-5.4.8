using System;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaNode
    {
        internal LuaNode(IntPtr address)
        {
            Address = address;
            _node = StyxWoW.Memory.Read<NativeLuaNode>(address);
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaNode>.Size; }
        }

        public IntPtr Address { get; private set; }

        public LuaTValue Value
        {
            get { return _value ?? (_value = new LuaTValue(Address)); }
        }

        public LuaTKey Key
        {
            get { return _key ?? (_key = new LuaTKey(Address + LuaTValue.NativeSize)); }
        }

        private LuaTKey _key;

        private NativeLuaNode _node;

        private LuaTValue _value;
    }
}
