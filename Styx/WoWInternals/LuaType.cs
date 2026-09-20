using System;

namespace Styx.WoWInternals
{
    public enum LuaType : int
    {
        None = -1,
        Nil,
        Boolean,
        LightUserData,
        Number,
        String,
        Table,
        Function,
        UserData,
        Thread
    }
}
