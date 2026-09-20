using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.WoWObjects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FactionStanding
    {
        public int TotalReputation
        {
            get
            {
                return Reputation + ReputationBonus;
            }
        }

        public bool HasBonusRepGain
        {
            get
            {
                return _hasBonusRepGain != 0;
            }
        }

        public uint FactionId;

        public int ReputationBonus;

        public int Reputation;

        private readonly byte _hasBonusRepGain;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private readonly byte[] _unk2;
    }
}
