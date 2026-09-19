using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FunctionOffsets
    {
        public IntPtr FrameScript__Execute;
        public IntPtr _lua_gettop;
        public IntPtr FrameScript__SetTop;
        public IntPtr _lua_tolstring;
        public IntPtr _lua_pcall;
        public IntPtr _luaL_loadbuffer;
        public IntPtr ClntObjMgrObjectPtr;
    }
}
