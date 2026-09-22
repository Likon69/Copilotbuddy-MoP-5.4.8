using System;

namespace Buddy.Overlay.Internal
{
    [Flags]
    internal enum FormatMessageFlags : uint
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 256U,
        FORMAT_MESSAGE_IGNORE_INSERTS = 512U,
        FORMAT_MESSAGE_FROM_SYSTEM = 4096U,
        FORMAT_MESSAGE_ARGUMENT_ARRAY = 8192U,
        FORMAT_MESSAGE_FROM_HMODULE = 2048U,
        FORMAT_MESSAGE_FROM_STRING = 1024U
    }
}
