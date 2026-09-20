using System;
using System.Runtime.InteropServices;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct NativeWowTable
    {
        public IntPtr _vtable;
        public int Dword4;
        public int Dword8;
        public int DwordC;
        public int Dword10;
        public int Dword14;
        public IntPtr Buckets;
        public int Dword1C;
        public uint _unk2;
        public uint BucketMask;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct NativeWowTableBucket
    {
        public IntPtr NextOffset;
        private uint _unk;
        public IntPtr First;
    }
}
