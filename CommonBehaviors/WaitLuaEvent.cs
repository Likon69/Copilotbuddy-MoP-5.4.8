using System;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace CommonBehaviors
{
    public class WaitLuaEvent : WaitContinue
    {
        private readonly string _luaEvent;
        private readonly CanRunDecoratorDelegate _alternateCondition;
        private bool _eventFired;
        private bool _eventAttached;

        public WaitLuaEvent(string luaEvent, int timeoutSeconds, Composite child)
            : base(timeoutSeconds, child)
        {
            _luaEvent = luaEvent;
        }

        public WaitLuaEvent(string luaEvent, WaitGetTimeoutDelegate timeoutRetriever, Composite child)
            : base(timeoutRetriever, child)
        {
            _luaEvent = luaEvent;
        }

        public WaitLuaEvent(string luaEvent, int timeoutSeconds,
            CanRunDecoratorDelegate alternateCondition, Composite child)
            : this(luaEvent, timeoutSeconds, child)
        {
            _luaEvent = luaEvent;
            _alternateCondition = alternateCondition;
        }

        public WaitLuaEvent(string luaEvent, TimeSpan timeout, Composite child)
            : base(timeout, child)
        {
            _luaEvent = luaEvent;
        }

        public WaitLuaEvent(string luaEvent, WaitGetTimeoutDelegate timeoutRetriever,
            CanRunDecoratorDelegate alternateCondition, Composite child)
            : this(luaEvent, timeoutRetriever, child)
        {
            _luaEvent = luaEvent;
            _alternateCondition = alternateCondition;
        }

        public override void Start(object context)
        {
            Lua.Events.AttachEvent(_luaEvent, WaitHandler);
            _eventAttached = true;
            base.Start(context);
        }

        private void WaitHandler(object sender, LuaEventArgs args)
        {
            _eventFired = true;
        }

        protected override bool CanRun(object context)
        {
            return _eventFired || (_alternateCondition != null && _alternateCondition(context));
        }

        public override void Stop(object context)
        {
            if (_eventAttached)
            {
                Lua.Events.DetachEvent(_luaEvent, WaitHandler);
                _eventAttached = false;
            }

            _eventFired = false;
            base.Stop(context);
        }
    }
}
