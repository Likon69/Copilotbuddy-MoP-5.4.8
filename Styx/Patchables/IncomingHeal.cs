using System;
using System.Runtime.InteropServices;

namespace Styx.Patchables
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IncomingHeal
    {
        public bool IsHealOverTime
        {
            get
            {
                return _isHealOverTime == 1;
            }
        }

        public ulong OwnerGuid;

        public int spellId;

        private int _dword_C;

        public uint HealAmount;

        private byte _isHealOverTime;

        private byte _byte_15;

        private byte _byte_16;

        private byte _byte_17;

        private uint uint_0;

        private uint uint_1;
    }
}
