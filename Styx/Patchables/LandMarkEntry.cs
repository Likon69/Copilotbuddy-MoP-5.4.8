using System;
using System.Runtime.InteropServices;
using Styx.WoWInternals;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct LandMarkEntry
    {
        public LandmarkType Type
        {
            get { return (LandmarkType)_type; }
        }

        public uint Entry;

        public float MapX;

        public float MapY;

        private int _type;
    }
}
