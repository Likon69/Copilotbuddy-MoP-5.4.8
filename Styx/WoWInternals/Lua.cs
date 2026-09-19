using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common;

namespace Styx.WoWInternals
{
    public static class Lua
    {
        public static string Escape(string unescaped)
        {
            string text = unescaped;
            foreach (KeyValuePair<string, string> escape in _escapes)
            {
                text = text.Replace(escape.Key, escape.Value);
            }
            return text;
        }

        public static List<string> GetReturnValues(string lua)
        {
            return GetReturnValues(lua, "lua.lua");
        }

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        public static T ParseLuaValue<T>(string val)
        {
            if (!string.IsNullOrEmpty(val) && val != "nil")
            {
                object result;
                if (typeof(T) == typeof(bool))
                {
                    string lowered = val.ToLower();
                    result = !(lowered == "false" || lowered == "0");
                }
                else
                {
                    result = Convert.ChangeType(val, typeof(T), CultureInfo.InvariantCulture);
                }
                return (T)result;
            }
            return default(T);
        }

        public static List<string> GetReturnValues(string lua, string scriptName = "lua.lua")
        {
            Executor executor = StyxWoW.Memory.Executor;
            IntPtr statePtr = State.Address;
            if (executor == null)
            {
                throw new ArgumentException("Invalid executor in Lua.GetReturnValues");
            }
            IntPtr moduleHandle = GetModuleHandle("kernel32.dll");
            if (moduleHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find kernel32 so GetReturnValues cannot be called");
            }
            IntPtr writeProcessMemoryPtr = GetProcAddress(moduleHandle, "WriteProcessMemory");
            if (writeProcessMemoryPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find WriteProcessMemory so GetReturnValues cannot be called");
            }
            IntPtr flushInstructionCachePtr = GetProcAddress(moduleHandle, "FlushInstructionCache");
            if (flushInstructionCachePtr == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find FlushInstructionCache");
            }
            string wrappedLua = "local t = \r\n(function(...)\r\n\tlocal t = {...}\r\n\tt.n = select('#', ...)\r\n\treturn t\r\nend)((function() " + lua + " end)())\r\nfor i=1, t.n do\r\n\tlocal v = t[i]\r\n\tlocal varType = type(v)\r\n\tif varType == 'boolean' then\r\n\t\tt[i] = v and '1' or '0'\r\n\telse\r\n\t\tt[i] = tostring(t[i])\r\n\tend\r\nend\r\n\r\nreturn unpack(t)";
            byte[] luaBytes = Encoding.UTF8.GetBytes(wrappedLua);
            byte[] scriptNameBytes = Encoding.UTF8.GetBytes(scriptName);
            List<byte> payload = new List<byte>(10 + luaBytes.Length + 1 + scriptNameBytes.Length + 1);
            int stubBytesOffset = payload.Count;
            payload.AddRange(new byte[] { 85, 139, 236, 131, 236 });
            int savedBytesOffset = payload.Count;
            for (int i = 0; i < 5; i++)
            {
                payload.Add(0);
            }
            int luaTextOffset = payload.Count;
            payload.AddRange(luaBytes);
            payload.Add(0);
            int scriptNameOffset = payload.Count;
            payload.AddRange(scriptNameBytes);
            payload.Add(0);
            List<string> returnValues;
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(payload.Count))
            {
                allocatedMemory.WriteBytes(0, payload.ToArray());
                IntPtr pStubBytes = allocatedMemory.Address + stubBytesOffset;
                IntPtr pSavedBytes = allocatedMemory.Address + savedBytesOffset;
                IntPtr pLuaText = allocatedMemory.Address + luaTextOffset;
                IntPtr pScriptName = allocatedMemory.Address + scriptNameOffset;
                if (_pRetArgs == null)
                {
                    _pRetArgs = StyxWoW.Memory.CreateAllocatedMemory(ArgsBytesAllocate);
                }
                _pRetArgs.WriteBytes(0, _clearBytes);
                lock (executor.AssemblyLock)
                {
                    executor.Clear();
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions._lua_gettop);
                    executor.AddLine("add esp, 0x4");
                    executor.AddLine("mov ebx, eax");
                    executor.AddLine("push ebx");
                    executor.AddLine("push 0");
                    executor.AddLine("push 5");
                    executor.AddLine("push {0}", StyxWoW.Offsets.Functions._lua_load);
                    executor.AddLine("push {0}", pSavedBytes);
                    executor.AddLine("push 0xFFFFFFFF");
                    executor.AddLine("call {0}", writeProcessMemoryPtr);
                    executor.AddLine("test eax, eax");
                    executor.AddLine("jz @ErrorWhileWPM");
                    executor.AddLine("push 0");
                    executor.AddLine("push 5");
                    executor.AddLine("push {0}", pStubBytes);
                    executor.AddLine("push {0}", StyxWoW.Offsets.Functions._lua_load);
                    executor.AddLine("push 0xFFFFFFFF");
                    executor.AddLine("call {0}", writeProcessMemoryPtr);
                    executor.AddLine("test eax, eax");
                    executor.AddLine("jz @ErrorWhileWPM");
                    executor.AddLine("push 5");
                    executor.AddLine("push {0}", StyxWoW.Offsets.Functions._lua_load);
                    executor.AddLine("push 0xFFFFFFFF");
                    executor.AddLine("call {0}", flushInstructionCachePtr);
                    executor.AddLine("test eax, eax");
                    executor.AddLine("jz @ErrorWhileFIC");
                    executor.AddLine("push {0}", pScriptName);
                    executor.AddLine("push {0}", luaBytes.Length);
                    executor.AddLine("push {0}", pLuaText);
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions._luaL_loadbuffer);
                    executor.AddLine("add esp, 0x10");
                    executor.AddLine("push eax");
                    executor.AddLine("push 0");
                    executor.AddLine("push 5");
                    executor.AddLine("push {0}", pSavedBytes);
                    executor.AddLine("push {0}", StyxWoW.Offsets.Functions._lua_load);
                    executor.AddLine("push 0xFFFFFFFF");
                    executor.AddLine("call {0}", writeProcessMemoryPtr);
                    executor.AddLine("test eax, eax");
                    executor.AddLine("pop eax");
                    executor.AddLine("jz @ErrorWhileWPM");
                    executor.AddLine("push eax");
                    executor.AddLine("push 5");
                    executor.AddLine("push {0}", StyxWoW.Offsets.Functions._lua_load);
                    executor.AddLine("push 0xFFFFFFFF");
                    executor.AddLine("call {0}", flushInstructionCachePtr);
                    executor.AddLine("test eax, eax");
                    executor.AddLine("pop eax");
                    executor.AddLine("jz @ErrorWhileFIC");
                    executor.AddLine("test eax, eax");
                    executor.AddLine("jnz @Finally");
                    executor.AddLine("push 0");
                    executor.AddLine("push {0}", LuaMultret);
                    executor.AddLine("push {0}", 0);
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions._lua_pcall);
                    executor.AddLine("add esp, 0x10");
                    executor.AddLine("test eax, eax");
                    executor.AddLine("jnz @Finally");
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions._lua_gettop);
                    executor.AddLine("add esp, 0x4");
                    executor.AddLine("cmp eax, ebx");
                    executor.AddLine("jle @FailNoRetValues");
                    executor.AddLine("sub eax, ebx");
                    executor.AddLine("mov ecx, {0}", _pRetArgs.Address);
                    executor.AddLine("mov [ecx], eax");
                    executor.AddLine("add eax, ebx");
                    executor.AddLine("@LoopStart:");
                    executor.AddLine("add ecx, 0x4");
                    executor.AddLine("inc ebx");
                    executor.AddLine("push eax");
                    executor.AddLine("push ecx");
                    executor.AddLine("push {0}", 0);
                    executor.AddLine("push ebx");
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions._lua_tolstring);
                    executor.AddLine("add esp, 0xC");
                    executor.AddLine("pop ecx");
                    executor.AddLine("mov [ecx], eax");
                    executor.AddLine("pop eax");
                    executor.AddLine("cmp ebx, eax");
                    executor.AddLine("jl @LoopStart");
                    executor.AddLine("mov eax, 0");
                    executor.AddLine("jmp @Finally");
                    executor.AddLine("@FailNoRetValues:");
                    executor.AddLine("mov eax, -1");
                    executor.AddLine("jmp @Finally");
                    executor.AddLine("@ErrorWhileWPM:");
                    executor.AddLine("mov eax, -2");
                    executor.AddLine("jmp @Finally");
                    executor.AddLine("@ErrorWhileFIC:");
                    executor.AddLine("mov eax, -3");
                    executor.AddLine("jmp @Finally");
                    executor.AddLine("@Finally:");
                    executor.AddLine("pop ebx");
                    executor.AddLine("push eax");
                    executor.AddLine("push ebx");
                    executor.AddLine("push {0}", statePtr);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions.FrameScript__SetTop);
                    executor.AddLine("add esp, 0x8");
                    executor.AddLine("pop eax");
                    executor.AddLine("retn");
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        int status = executor.Memory.Read<int>(executor.ReturnPointer);
                        if (status == 0)
                        {
                            int valueCount = _pRetArgs.Read<int>(0);
                            List<string> values = new List<string>(valueCount);
                            for (int j = 0; j < valueCount; j++)
                            {
                                IntPtr valuePtr = _pRetArgs.Read<IntPtr>((j + 1) * 4);
                                if (valuePtr != IntPtr.Zero)
                                {
                                    values.Add(executor.Memory.ReadString(valuePtr, Encoding.UTF8));
                                }
                                else
                                {
                                    values.Add("nil");
                                }
                            }
                            returnValues = values.ToList();
                        }
                        else if (status == -1)
                        {
                            returnValues = new List<string>();
                        }
                        else if (status < 0)
                        {
                            returnValues = null;
                        }
                        else
                        {
                            Logging.WriteDiagnostic("Lua failed! Status: {0}", (LuaRunStatus)status);
                            returnValues = null;
                        }
                    }
                }
            }
            return returnValues;
        }

        public static T GetReturnVal<T>(string lua, uint retVal)
        {
            List<string> returnValues = GetReturnValues(lua, "WoW.lua");
            if (returnValues == null)
            {
                return default(T);
            }
            if (retVal >= (uint)returnValues.Count)
            {
                return default(T);
            }
            string value = returnValues[(int)retVal];
            return ParseLuaValue<T>(value);
        }

        public static void DoString(string szLua, string szLuaFile = "WoW.lua")
        {
            DoString(szLua, szLuaFile, IntPtr.Zero);
        }

        public static void DoString(string format, params object[] args)
        {
            DoString(string.Format(format, args), "WoW.lua", IntPtr.Zero);
        }

        public static void DoString(string lua, string luaFile, IntPtr pState)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            if (executor == null)
            {
                throw new ArgumentException("Invalid executor used in DoString");
            }
            if (string.IsNullOrEmpty(lua))
            {
                throw new ArgumentNullException("lua");
            }
            if (string.IsNullOrEmpty(luaFile))
            {
                luaFile = "Foo.lua";
            }
            byte[] luaBytes = Encoding.UTF8.GetBytes(lua);
            byte[] luaFileBytes = Encoding.UTF8.GetBytes(luaFile);
            List<byte> finalBytes = new List<byte>(luaBytes.Length + 1 + luaFileBytes.Length + 1);
            finalBytes.AddRange(luaBytes);
            finalBytes.Add(0);
            finalBytes.AddRange(luaFileBytes);
            finalBytes.Add(0);
            IntPtr codeCave = executor.Memory.AllocateMemory(finalBytes.Count);
            try
            {
                executor.Memory.WriteBytes(codeCave, finalBytes.ToArray());
                IntPtr luaFilePointer = codeCave + luaBytes.Length + 1;
                lock (executor.AssemblyLock)
                {
                    executor.Clear();
                    AddDoStringASM(codeCave, luaFilePointer, pState);
                    executor.AddLine("retn");
                    executor.Execute();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic("Exception in DoString: {0} StackTrace:{1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                if (codeCave != (IntPtr)0)
                {
                    executor.Memory.FreeMemory(codeCave);
                }
            }
        }

        internal static void AddDoStringASM(IntPtr luaPtr, IntPtr luaFilePtr, IntPtr pState)
        {
            Executor executor = StyxWoW.Memory.Executor;
            if (executor == null)
            {
                throw new Exception("Invalid executor used in AddDoStringASM");
            }
            lock (executor.AssemblyLock)
            {
                executor.AddLine("push {0}", pState);
                executor.AddLine("push {0}", luaFilePtr);
                executor.AddLine("push {0}", luaPtr);
                executor.AddLine("call {0}", StyxWoW.Offsets.Functions.FrameScript__Execute);
                executor.AddLine("add esp, 0xC");
            }
        }

        internal static void AddDoStringASM(string lua, string luaFile, out IntPtr freePtr)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            if (string.IsNullOrEmpty(lua))
            {
                throw new ArgumentNullException("lua");
            }
            if (string.IsNullOrEmpty(luaFile))
            {
                luaFile = "Foo.lua";
            }
            byte[] luaBytes = Encoding.UTF8.GetBytes(lua);
            byte[] luaFileBytes = Encoding.UTF8.GetBytes(luaFile);
            List<byte> finalBytes = new List<byte>(luaBytes.Length + 1 + luaFileBytes.Length + 1);
            finalBytes.AddRange(luaBytes);
            finalBytes.Add(0);
            finalBytes.AddRange(luaFileBytes);
            finalBytes.Add(0);
            freePtr = executor.Memory.AllocateMemory(luaBytes.Length + 1 + luaFileBytes.Length + 1);
            IntPtr luaPtr = freePtr;
            IntPtr luaFilePtr = luaPtr + luaBytes.Length + 1;
            executor.Memory.WriteBytes(luaPtr, finalBytes.ToArray());
            AddDoStringASM(luaPtr, luaFilePtr, (IntPtr)0);
        }

        public static LuaState State
        {
            get { return new LuaState(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LuaState)); }
        }

        private const int LuaMultret = -1;

        private const int ArgsBytesAllocate = 4000;

        private static readonly Dictionary<string, string> _escapes = new Dictionary<string, string>
        {
            { "\\", "\\\\" },
            { "\"", "\\\"" },
            { "'", "\\'" },
            { "[", "\\[" },
            { "]", "\\]" }
        };

        private static AllocatedMemory _pRetArgs;

        private static readonly byte[] _clearBytes = Enumerable.Repeat<byte>(0, ArgsBytesAllocate).ToArray();
    }
}
