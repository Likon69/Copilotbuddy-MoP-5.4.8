using System;
using System.Text;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaTString
    {
        internal LuaTString(IntPtr address)
        {
            Address = address;
            _string = StyxWoW.Memory.Read<NativeLuaTString>(address);
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaTString>.Size; }
        }

        public IntPtr Address { get; private set; }

        public uint Hash
        {
            get { return _string.Hash; }
        }

        public string Value
        {
            get { return _val ?? (_val = StyxWoW.Memory.ReadString(Address + MarshalCache<NativeLuaTString>.Size, Encoding.UTF8, (int)_string.Length)); }
        }

        private NativeLuaTString _string;

        private string _val;
    }
}
