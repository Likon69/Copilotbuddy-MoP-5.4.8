using System;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaValue
    {
        internal LuaValue(IntPtr address)
        {
            Address = address;
            _value = StyxWoW.Memory.Read<NativeLuaValue>(address);
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaValue>.Size; }
        }

        public IntPtr Address { get; private set; }

        public double Double
        {
            get { return _value.Number; }
        }

        public bool Bool
        {
            get { return _value.Bool; }
        }

        public IntPtr Pointer
        {
            get { return _value.Pointer; }
        }

        public LuaTString String
        {
            get { return _string ?? (_string = new LuaTString(_value.Pointer)); }
        }

        public LuaTable Table
        {
            get { return _table ?? (_table = new LuaTable(_value.Pointer)); }
        }

        private LuaTString _string;

        private LuaTable _table;

        private NativeLuaValue _value;
    }
}
