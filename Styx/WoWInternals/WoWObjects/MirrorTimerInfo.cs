using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals.WoWObjects
{
    [StructLayout(LayoutKind.Sequential, Size = 28)]
    public struct MirrorTimerInfo
    {
        public uint CurrentTime
        {
            get { return (uint)((long)InitialValue + (long)ChangePerMillisecond * (long)((ulong)((uint)StyxWoW.WoWClient.PerformanceCounter() - StartTime))); }
        }

        public bool IsVisible
        {
            get { return StartTime != 0U; }
        }

        public override string ToString()
        {
            return string.Format("Type: {0}, InitialValue: {1}, MaxValue: {2}, ChangePerMillisecond: {3}, Paused: {4}, StartTime: {5}, SpellID: {6}", new object[] { Type, InitialValue, MaxValue, ChangePerMillisecond, _paused, StartTime, SpellID });
        }

        [MarshalAs(UnmanagedType.U4)]
        public MirrorTimerType Type;

        public int InitialValue;

        public int MaxValue;

        public int ChangePerMillisecond;

        private readonly uint _paused;

        public uint SpellID;

        public uint StartTime;
    }
}
