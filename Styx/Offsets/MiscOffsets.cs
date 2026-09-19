using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct MiscOffsets
    {
        public IntPtr WoWObjType;
        public IntPtr WoWObjGUID;
        public IntPtr WoWObjNextObject;
        public IntPtr FirstObject;
        public IntPtr LocalGuidOffset;
        public IntPtr WoWUnitMovementDataDirect;
        public IntPtr ClientConnectionOffset;
    }
}
