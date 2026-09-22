using System;

namespace Styx.Common
{
    [Flags]
    public enum FlashFlags
    {
        Stop = 0,
        Caption = 1,
        Tray = 2,
        All = 3,
        Timer = 4,
        TimerNoForeground = 12
    }
}
