using System;

namespace Styx.Common
{
    [Flags]
    public enum ModifierKeys
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8,
        NoRepeat = 16384
    }
}
