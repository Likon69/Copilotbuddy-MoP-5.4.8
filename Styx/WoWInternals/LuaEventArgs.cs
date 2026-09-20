using System;

namespace Styx.WoWInternals
{
    public class LuaEventArgs : EventArgs
    {
        public LuaEventArgs(string eventName, uint fireTimeStamp, object[] args)
        {
            EventName = eventName;
            FireTimeStamp = fireTimeStamp;
            Args = args;
        }

        public string EventName { get; private set; }

        public uint FireTimeStamp { get; private set; }

        public object[] Args { get; private set; }
    }
}
