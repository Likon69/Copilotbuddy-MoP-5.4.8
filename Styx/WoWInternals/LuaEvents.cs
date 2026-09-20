using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Resources;

namespace Styx.WoWInternals
{
    public class LuaEvents
    {
        internal LuaEvents()
        {
            _purgeTimer = WaitTimer.FiveSeconds;
            _purgeTimer.Reset();
            Styx.CommonBot.BotEvents.OnBotStarted += OnBotStarted;
            Styx.CommonBot.BotEvents.OnBotStopped += OnBotStopped;
        }

        private void OnBotStarted(EventArgs args)
        {
            if (IsInitialized)
            {
                Lua.DoString("table.wipe({0}) {1}=1", _eventTableName, _isBotRunningVarName);
            }
        }

        private void OnBotStopped(EventArgs args)
        {
            if (IsInitialized)
            {
                Lua.DoString("table.wipe({0}) {1}=nil", _eventTableName, _isBotRunningVarName);
            }
        }

        internal void TearDownEventFrame()
        {
            if (StyxWoW.WowExited || !StyxWoW.IsInGame)
            {
                return;
            }

            if (_frameName == null)
            {
                return;
            }

            Lua.DoString(string.Format("if {0} then {0}:UnregisterAllEvents(); {0}:SetScript('OnEvent', nil); {0} = nil; end if {1} then {1} = nil; end if {2} then {2} = nil; end", _frameName, _eventTableName, _filtersTableName), "WoW.lua");
        }

        public bool AddFilter(string eventName, string filterCode)
        {
            bool added;
            lock (StyxWoW.Memory.Executor.AssemblyLock)
            {
                if (_filters.ContainsKey(eventName))
                {
                    added = false;
                }
                else
                {
                    Lua.DoString("if {0} then {0}[\"{1}\"] = function(args) {2} end; end;", _filtersTableName, eventName, filterCode);
                    _filters.Add(eventName, filterCode);
                    Logging.WriteDiagnostic("Applied filter to event " + eventName + ". Filter: " + filterCode);
                    added = true;
                }
            }

            return added;
        }

        public void RemoveFilter(string eventName)
        {
            lock (StyxWoW.Memory.Executor.AssemblyLock)
            {
                Lua.DoString("if {0} then {0}[\"{1}\"] = nil end", _filtersTableName, eventName);
                _filters.Remove(eventName);
            }
        }

        public void AttachEvent(string eventName, LuaEventHandlerDelegate handler)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name cannot be null or white space", "eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            lock (StyxWoW.Memory.Executor.AssemblyLock)
            {
                if (!IsInitialized)
                {
                    SetUpEventFrame();
                }

                LuaEventHandlerDelegate handlers;
                if (!_eventHandlers.TryGetValue(eventName, out handlers) || handlers == null)
                {
                    _eventHandlers[eventName] = null;
                    Lua.DoString("{0}:RegisterEvent('{1}')", _frameName, eventName);
                }

                _eventHandlers[eventName] = (LuaEventHandlerDelegate)Delegate.Combine(_eventHandlers[eventName], handler);
            }
        }

        public void DetachEvent(string eventName, LuaEventHandlerDelegate handler)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name cannot be null or white space", "eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            if (StyxWoW.WowExited || !StyxWoW.IsInGame)
            {
                return;
            }

            lock (StyxWoW.Memory.Executor.AssemblyLock)
            {
                Action logNotAttached = () => Logging.WriteDiagnostic("[Lua.Events] Could not detach handler {0} from event {1}; it is not a member", handler.Method.Name, eventName);

                LuaEventHandlerDelegate handlers;
                if (_eventHandlers.TryGetValue(eventName, out handlers) && handlers != null)
                {
                    int before = handlers.GetInvocationList().Length;
                    handlers = (LuaEventHandlerDelegate)Delegate.Remove(handlers, handler);
                    int after = handlers != null ? handlers.GetInvocationList().Length : 0;
                    if (after < before)
                    {
                        _eventHandlers[eventName] = handlers;
                    }
                    else
                    {
                        logNotAttached();
                    }

                    if (after == 0)
                    {
                        Lua.DoString("if {0} then {0}:UnregisterEvent('{1}') end", _frameName, eventName);
                    }
                }
                else
                {
                    logNotAttached();
                }
            }
        }

        public bool IsInitialized
        {
            get { return GetEventTable() != null; }
        }

        internal void Pulse()
        {
            lock (StyxWoW.Memory.Executor.AssemblyLock)
            {
                LuaTable globals = Lua.State.Globals;
                LuaTValue eventTableVal;
                if ((eventTableVal = GetEventTable()) == null)
                {
                    SetUpEventFrame();
                }
                else if (_purgeTimer.IsFinished)
                {
                    _purgeTimer.Reset();
                    Lua.DoString(string.Format("local dumpedTo = {0}; local eventCount = #{1}; if eventCount > dumpedTo then local eventCopy = {{}}; for i=dumpedTo + 1,eventCount do tinsert(eventCopy, {1}[i]); end; wipe({1}); for i=1,#eventCopy do tinsert({1}, eventCopy[i]); end; else wipe({1}); end;", _lastParseEnd, _eventTableName), "WoW.lua");
                    _lastParseEnd = 0;
                }
                else
                {
                    LuaTable eventTable = eventTableVal.Value.Table;
                    uint valuesCount = eventTable.ValuesCount;
                    if (valuesCount > 131072U)
                    {
                        Logging.WriteDiagnostic("Memory moved for lua events ({0} new values); skipped for now.", valuesCount);
                    }
                    else
                    {
                        int tableSize = eventTable.Count;
                        int newEvents = (tableSize - _lastParseEnd) / 3;
                        if (newEvents > 15000)
                        {
                            Logging.WriteDiagnostic("Memory moved for lua events ({0} new events); skipped for now.", newEvents);
                        }
                        else
                        {
                            LuaTValue[] eventVals = eventTable.GetValues(_lastParseEnd, newEvents * 3);
                            int i = 0;
                            while (i < eventVals.Length / 3)
                            {
                                LuaTValue eventNameL = eventVals[i * 3];
                                LuaTValue fireTimeStampL = eventVals[i * 3 + 1];
                                LuaTValue eventArgsL = eventVals[i * 3 + 2];
                                if (eventNameL.Type == LuaType.String && fireTimeStampL.Type == LuaType.Number)
                                {
                                    if (eventArgsL.Type == LuaType.Table)
                                    {
                                        string eventName = eventNameL.Value.String.Value;
                                        uint fireTimeStamp = (uint)fireTimeStampL.Value.Double;
                                        LuaTable eventArgsTable = eventArgsL.Value.Table;
                                        if (eventArgsTable.ValuesCount > 2500U)
                                        {
                                            Logging.WriteDiagnostic("Memory moved for lua events (ValuesCount); skipped for now.");
                                            break;
                                        }

                                        int eventArgsCount = eventArgsTable.Count;
                                        if (eventArgsCount <= 1000)
                                        {
                                            LuaTValue[] eventArgValuesL = eventArgsTable.GetValues(0, eventArgsCount);
                                            object[] eventArgs = GetArgsFromValues(eventArgValuesL);
                                            LuaEventHandlerDelegate eventHandlers;
                                            if (_eventHandlers.TryGetValue(eventName, out eventHandlers))
                                            {
                                                InvokeAll(eventHandlers, this, new LuaEventArgs(eventName, fireTimeStamp, eventArgs));
                                            }

                                            _lastParseEnd += 3;
                                            if (PrintAllEvents)
                                            {
                                                Logging.WriteDiagnostic(string.Format(StyxResources._EVENT___0___Args___1_, eventName, string.Join(", ", eventArgs.Select(o => o.ToString()).ToArray())));
                                            }

                                            i++;
                                            continue;
                                        }

                                        Logging.WriteDiagnostic("Memory moved for lua events (eventArgsCount); skipped for now.");
                                        break;
                                    }
                                }

                                Logging.WriteDiagnostic("Memory moved for lua events (type); skipped for now.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private LuaTValue GetEventTable()
        {
            if (_eventTableName != null)
            {
                LuaTValue field = Lua.State.Globals.GetField(_eventTableName);
                if (field != null && field.Type == LuaType.Table)
                {
                    return field;
                }
            }

            return null;
        }

        public static bool PrintAllEvents { get; set; }

        private static void InvokeAll(Delegate d, params object[] args)
        {
            if (d == null)
            {
                return;
            }

            foreach (Delegate d2 in d.GetInvocationList())
            {
                try
                {
                    d2.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    Logging.WriteDiagnostic("Exception in Lua event handler " + d2.Method.Name);
                    Logging.WriteException(ex);
                }
            }
        }

        private object[] GetArgsFromValues(IList<LuaTValue> values)
        {
            object[] ret = new object[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                LuaValue val = values[i].Value;
                switch (values[i].Type)
                {
                    case LuaType.Boolean:
                        ret[i] = val.Bool;
                        break;
                    case LuaType.Number:
                        ret[i] = val.Double;
                        break;
                    case LuaType.String:
                        ret[i] = val.String.Value;
                        break;
                    default:
                        ret[i] = values[i];
                        break;
                }
            }

            return ret;
        }

        private void SetUpEventFrame()
        {
            _frameName = GenerateRandomString(9, 15);
            _eventTableName = GenerateRandomString(9, 15);
            _filtersTableName = GenerateRandomString(9, 15);
            _isBotRunningVarName = GenerateRandomString(9, 15);
            _lastParseEnd = 0;
            StringBuilder initLua = new StringBuilder();
            foreach (KeyValuePair<string, LuaEventHandlerDelegate> kvp in _eventHandlers)
            {
                if (kvp.Value != null)
                {
                    initLua.AppendFormat("{0}:RegisterEvent('{1}') ", _frameName, kvp.Key);
                }
            }

            foreach (KeyValuePair<string, string> kvp in _filters)
            {
                initLua.AppendFormat("{0}[\"{1}\"] = function(args) {2} end; ", _filtersTableName, kvp.Key, kvp.Value);
            }

            string lua = string.Format("{0} = {{}}; {2} = {{}}; {1} = CreateFrame('Frame'); {4} = {5}; {1}:SetScript('OnEvent', function(self, event, ...) if not {4} then return end local args = {{ ... }} if not {2} or not {2}[event] or {2}[event](args) then tinsert({0}, event); tinsert({0}, GetTime()*1000); tinsert({0}, args); end end); {3}",
                _eventTableName,
                _frameName,
                _filtersTableName,
                initLua,
                _isBotRunningVarName,
                Styx.CommonBot.TreeRoot.IsRunning ? "1" : "nil");
            Lua.DoString(lua, "WoW.lua");
        }

        private static string GenerateRandomString(int minLength, int maxLength)
        {
            int length = _rand.Next(minLength, maxLength + 1);
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(_charPool[_rand.Next(0, _charPool.Length)]);
            }

            return sb.ToString();
        }

        private static readonly Random _rand = new Random();

        private static readonly char[] _charPool = "ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvxyz".ToCharArray();

        private readonly Dictionary<string, LuaEventHandlerDelegate> _eventHandlers = new Dictionary<string, LuaEventHandlerDelegate>();

        private readonly WaitTimer _purgeTimer;

        private string _eventTableName;

        private string _filtersTableName;

        private string _frameName;

        private string _isBotRunningVarName;

        private int _lastParseEnd;

        private readonly Dictionary<string, string> _filters = new Dictionary<string, string>();
    }
}
