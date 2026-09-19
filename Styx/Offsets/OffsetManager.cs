using System;
using Styx.Offsets;

namespace Styx
{
    internal class OffsetManager
    {
        internal FunctionOffsets Functions;
        internal MiscOffsets Misc;
        internal StaticOffsets Statics;
        internal VFTableIndex VFTableIndex;

        internal void Rebase(IntPtr newBase)
        {
            Functions.FrameScript__Execute = newBase + 0x4FD12;
            Functions._lua_gettop = newBase + 0xD5E84;
            Functions.FrameScript__SetTop = newBase + 0xD5E95;
            Functions._lua_tolstring = newBase + 0xD6404;
            Functions._lua_pcall = newBase + 0xD6EFC;
            Functions._luaL_loadbuffer = newBase + 0xD7962;
            Functions.ClntObjMgrObjectPtr = newBase + 0x39BA5B;

            Misc.WoWObjType = (IntPtr)0xC;
            Misc.WoWObjGUID = (IntPtr)0x28;
            Misc.WoWObjNextObject = (IntPtr)0x34;
            Misc.FirstObject = (IntPtr)0xCC;
            Misc.LocalGuidOffset = (IntPtr)0xE8;
            Misc.WoWUnitMovementDataDirect = (IntPtr)0x828;
            Misc.ClientConnectionOffset = (IntPtr)0x462C;

            Statics.LuaState = newBase + 0xBA6178;
            Statics.GxApi = newBase + 0xC85C3C;
            Statics.IsInGame = newBase + 0xD65B16;
            Statics.ClientConnection = newBase + 0xEC4628;
            Statics.LocalPlayerName = newBase + 0xEC4668;

            VFTableIndex.GetPosition = (IntPtr)0x27;
            VFTableIndex.GetName = (IntPtr)0x4D;
        }

        public void Clear()
        {
            Functions = new FunctionOffsets();
            Misc = new MiscOffsets();
            Statics = new StaticOffsets();
            VFTableIndex = new VFTableIndex();
        }
    }
}
