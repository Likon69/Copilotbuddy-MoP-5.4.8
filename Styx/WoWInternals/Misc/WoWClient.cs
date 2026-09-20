using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Styx.Common.Helpers;

namespace Styx.WoWInternals.Misc
{
    public class WoWClient
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        internal WoWClient()
        {
        }

        public uint Latency
        {
            get
            {
                float downKBs;
                float upKBs;
                uint latency;
                GetNetStats(out downKBs, out upKBs, out latency);
                return latency;
            }
        }

        public NetStats NetStats
        {
            get { return StyxWoW.Memory.Read<NetStats>(false, StyxWoW.Offsets.Statics.CurrentNetClient, StyxWoW.Offsets.Misc.NetStatsStart); }
        }

        public ulong PerformanceCounter()
        {
            TimerInfo timer = StyxWoW.Memory.Read<TimerInfo>(StyxWoW.Offsets.Statics.TimerStruct);
            double factor = timer.Factor;
            uint timingMethod = timer.TimingMethod;
            double additionalTime = timer.AdditionalTime;
            if (timingMethod == 1 || timingMethod != 2)
            {
                return (ulong)(GetTickCount() * factor + additionalTime);
            }
            long performanceCount;
            QueryPerformanceCounter(out performanceCount);
            return (ulong)(performanceCount * factor + additionalTime);
        }

        public void GetNetStats(out float downKBs, out float upKBs, out uint latency)
        {
            if (!NetStatsQueryTimer.IsFinished)
            {
                downKBs = _lastDown;
                upKBs = _lastUp;
                latency = _lastLatency;
                return;
            }
            NetStatsQueryTimer.Reset();
            List<string> returnValues = Lua.GetReturnValues("return GetNetStats()");
            downKBs = float.Parse(returnValues[0], CultureInfo.InvariantCulture);
            upKBs = float.Parse(returnValues[1], CultureInfo.InvariantCulture);
            latency = uint.Parse(returnValues[3], CultureInfo.InvariantCulture);
            _lastDown = downKBs;
            _lastUp = upKBs;
            _lastLatency = latency;
        }

        public DateTime PerformanceCounterToDateTime(ulong performanceCounter)
        {
            long now = (long)PerformanceCounter();
            long delta = (long)(performanceCounter - (ulong)now);
            return DateTime.Now.AddMilliseconds(delta);
        }

        private static readonly WaitTimer NetStatsQueryTimer = WaitTimer.TenSeconds;

        private static float _lastDown;

        private static float _lastUp;

        private static uint _lastLatency;

        #region Nested type: TimerInfo

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TimerInfo
        {
            public uint TimingMethod;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private uint[] _unk;

            public double Factor;

            public double AdditionalTime;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            private readonly byte[] _f20;

            private readonly uint _dword48;

            private readonly uint _dword52;
        }

        #endregion
    }
}
