using System;
using Styx.Helpers;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    public class WoWSimpleMovementInfo
    {
        internal WoWSimpleMovementInfo(IntPtr ptr)
        {
            BaseAddress = ptr;
        }

        private CSimpleMovementData Struct
        {
            get
            {
                if (_structCache == null)
                {
                    _structCache = new PerFrameCachedValue<CSimpleMovementData>(() => StyxWoW.Memory.Read<CSimpleMovementData>(BaseAddress));
                }

                return _structCache;
            }
        }

        internal IntPtr BaseAddress { get; private set; }

        public bool IsValid
        {
            get { return BaseAddress != IntPtr.Zero; }
        }

        public WoWPoint Position
        {
            get { return Struct.uint10Vec3; }
        }

        public float Heading
        {
            get { return Struct.Facing; }
        }

        public ulong TransportGuid
        {
            get { return Struct.TransportGuid; }
        }

        private PerFrameCachedValue<CSimpleMovementData> _structCache;

        internal struct CSimpleMovementData
        {
            public uint uint0;

            public uint uint4;

            public ulong TransportGuid;

            public Vector3 uint10Vec3;

            public uint uint1C;

            public float Facing;

            public float Pitch;

            public uint uint28;

            public uint uint2C;
        }
    }
}
