using System;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaTValue
    {
        internal LuaTValue(IntPtr address)
            : this(address, StyxWoW.Memory.Read<NativeLuaTValue>(address))
        {
        }

        internal LuaTValue(IntPtr address, NativeLuaTValue val)
        {
            Address = address;
            _value = val;
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaTValue>.Size; }
        }

        public IntPtr Address { get; private set; }

        public LuaType Type
        {
            get { return _value.Type; }
        }

        public LuaValue Value
        {
            get { return _val ?? (_val = new LuaValue(Address)); }
        }

        public override string ToString()
        {
            string value = null;
            switch (Type)
            {
                case LuaType.Number:
                    value = "Double: " + Value.Double;
                    break;
                case LuaType.Boolean:
                    value = "Bool: " + Value.Bool;
                    break;
                case LuaType.String:
                    value = "String: " + Value.String.Value;
                    break;
            }
            if (value != null)
            {
                return string.Format("[LuaTValue Type: {0}, {1}]", Type, value);
            }
            return string.Format("[LuaTValue Type: {0}]", Type);
        }

        private LuaValue _val;

        private NativeLuaTValue _value;
    }
}
