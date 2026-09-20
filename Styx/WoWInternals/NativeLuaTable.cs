using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeLuaTable
    {
        public NativeLuaCommonHeader Header;

        public byte Flags;

        private byte _lSizeNode;

        public IntPtr MetaTablePtr;

        public IntPtr ValuesPtr;

        public IntPtr NodePtr;

        private uint _lastNodeFree;

        private uint _gclist;

        public uint ValuesCount;

        public uint NodesCount
        {
            get { return 1u << _lSizeNode; }
        }
    }
}
