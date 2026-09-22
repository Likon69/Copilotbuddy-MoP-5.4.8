using System;

namespace Styx.Common
{
    public interface INativeObject
    {
        void UpdatePointer(IntPtr ptr);
        IntPtr BaseAddress { get; }
    }
}
