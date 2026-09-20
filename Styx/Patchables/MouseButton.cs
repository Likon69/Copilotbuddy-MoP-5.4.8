using System;

namespace Styx.Patchables
{
    [Flags]
    public enum MouseButton : uint
    {
        None = 0x0,
        Left = 0x1,
        Middle = 0x2,
        Right = 0x4,
        XButton1 = 0x8,
        XButton2 = 0x10,
    }
}
