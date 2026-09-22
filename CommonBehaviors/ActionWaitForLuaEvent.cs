using System;
using CommonBehaviors.Resources;
using Styx.Common;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace CommonBehaviors
{
    public class ActionWaitForLuaEvent : Styx.TreeSharp.Action
    {
        private readonly string _luaEvent;
        private readonly int _timeoutSeconds;
        private DateTime _endTime;
        private bool _eventFired;
        private bool _eventAttached;

        public ActionWaitForLuaEvent(string luaEvent, int timeoutSeconds)
        {
            _luaEvent = luaEvent;
            _timeoutSeconds = timeoutSeconds;
        }

        public override void Start(object context)
        {
            _eventFired = false;
            _endTime = DateTime.Now.AddSeconds(_timeoutSeconds);
            Lua.Events.AttachEvent(_luaEvent, WaitHandler);
            _eventAttached = true;
            base.Start(context);
        }

        private void WaitHandler(object sender, LuaEventArgs args)
        {
            _eventFired = true;
        }

        protected override RunStatus Run(object context)
        {
            if (DateTime.Now > _endTime)
            {
                Logging.Write(_luaEvent + CommonBehaviorsResources.ActionWaitForLuaEvent_Run__wait_timed_out_);
                return RunStatus.Failure;
            }

            if (_eventFired)
                return RunStatus.Success;

            return RunStatus.Running;
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
