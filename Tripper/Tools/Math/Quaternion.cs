using System;
using System.Runtime.InteropServices;

namespace Tripper.Tools.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Quaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
    }
}
