using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic.Native;

namespace GreenMagic.Fasm
{
    public class ManagedFasm : IDisposable
    {
        private const int DefaultMemorySize = 4096;

        private const int DefaultPassLimit = 100;

        public ManagedFasm()
        {
            _assemblyString = new StringBuilder("use32\n");
            _threadHandles = new List<IntPtr>();
            _memorySize = DefaultMemorySize;
            _passLimit = DefaultPassLimit;
        }

        public ManagedFasm(IntPtr hProcess)
        {
            _hProcess = hProcess;
            _assemblyString = new StringBuilder("use32\n");
            _threadHandles = new List<IntPtr>();
            _memorySize = DefaultMemorySize;
            _passLimit = DefaultPassLimit;
        }

        private void CloseThreadHandles()
        {
            for (int index = 0; index < _threadHandles.Count; index++)
            {
                Imports.CloseHandle(_threadHandles[index]);
            }
            _threadHandles.Clear();
        }

        public void AddLine(string format, params object[] args)
        {
            _assemblyString.AppendFormat(format + "\n", args);
        }

        public void AddLine(string line)
        {
            _assemblyString.Append(line + "\n");
        }

        public void Add(string format, params object[] args)
        {
            _assemblyString.AppendFormat(format, args);
        }

        public void Add(string line)
        {
            _assemblyString.Append(line);
        }

        public void InsertLine(string line, int index)
        {
            _assemblyString.Insert(index, line + "\n");
        }

        public void Insert(string line, int index)
        {
            _assemblyString.Insert(index, line);
        }

        public void Clear()
        {
            _assemblyString = new StringBuilder("use32\n");
        }

        [DllImport("FASM.DLL", CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe uint fasm_Assemble(sbyte* lpSource, byte* lpMemory, uint cbMemorySize, uint nPassesLimit, IntPtr hDisplayPipe);

        private static unsafe uint AssembleSynchronized(sbyte* source, byte* memory, uint memorySize, uint passesLimit)
        {
            lock (_assembleLock)
            {
                return fasm_Assemble(source, memory, memorySize, passesLimit, IntPtr.Zero);
            }
        }

        public static unsafe byte[] Assemble(string source, int memorySize, int passLimit)
        {
            IntPtr sourcePtr = IntPtr.Zero;
            try
            {
                sourcePtr = Marshal.StringToHGlobalAnsi(source);
                if (_memory == null || _memory.Length < memorySize)
                {
                    _memory = new byte[memorySize];
                }
                fixed (byte* memoryPtr = _memory)
                {
                    AssembleSynchronized((sbyte*)sourcePtr.ToPointer(), memoryPtr, (uint)memorySize, (uint)passLimit);
                    FasmState* state = (FasmState*)memoryPtr;
                    if (state->condition == FasmConditions.FASM_OK)
                    {
                        byte[] bytecode = new byte[state->output_length];
                        Marshal.Copy(state->output_data, bytecode, 0, (int)state->output_length);
                        return bytecode;
                    }
                    FasmLineHeader* errorLine = (FasmLineHeader*)state->error_line;
                    throw new Exception(string.Format("Assembly failed!  Error code: {0};  Error Line: {1}", state->error_code, errorLine->line_number));
                }
            }
            finally
            {
                if (sourcePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(sourcePtr);
                }
            }
        }

        public static byte[] Assemble(string source, int memorySize)
        {
            return Assemble(source, memorySize, DefaultPassLimit);
        }

        public static byte[] Assemble(string source)
        {
            return Assemble(source, DefaultMemorySize, DefaultPassLimit);
        }

        public byte[] Assemble()
        {
            return Assemble(_assemblyString.ToString(), _memorySize, _passLimit);
        }

        public bool Inject(IntPtr address)
        {
            return Inject(_hProcess, address);
        }

        public unsafe bool Inject(IntPtr hProcess, IntPtr address)
        {
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }
            if (_assemblyString.ToString().Contains("use64") || _assemblyString.ToString().Contains("use16"))
            {
                _assemblyString.Replace("use32\n", "");
            }
            if (!_assemblyString.ToString().Contains("org "))
            {
                _assemblyString.Insert(0, string.Format("org 0x{0:X8}\n", address.ToInt32()));
            }
            if (_memory == null || _memory.Length < _memorySize)
            {
                _memory = new byte[_memorySize];
            }
            IntPtr sourcePtr = IntPtr.Zero;
            fixed (byte* memoryPtr = _memory)
            {
                try
                {
                    try
                    {
                        sourcePtr = Marshal.StringToHGlobalAnsi(_assemblyString.ToString());
                        AssembleSynchronized((sbyte*)sourcePtr.ToPointer(), memoryPtr, (uint)_memorySize, (uint)_passLimit);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
                finally
                {
                    if (sourcePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(sourcePtr);
                    }
                }
                FasmState* state = (FasmState*)memoryPtr;
                if (state->condition != FasmConditions.FASM_OK)
                {
                    FasmLineHeader* errorLine = (FasmLineHeader*)state->error_line;
                    throw new Exception(string.Format("Assembly failed!  Error code: {0};  Error Line: {1}; ASM: {2}", state->error_code, errorLine->line_number, _assemblyString.ToString()));
                }
                int bytesWritten;
                return Imports.WriteProcessMemory(hProcess, address, state->output_data.ToPointer(), (int)state->output_length, out bytesWritten);
            }
        }

        public uint InjectAndExecute(IntPtr address)
        {
            return InjectAndExecute(_hProcess, address, 0);
        }

        public uint InjectAndExecute(IntPtr hProcess, IntPtr address)
        {
            return InjectAndExecute(hProcess, address, 0);
        }

        public uint InjectAndExecute(IntPtr hProcess, IntPtr address, uint parameter)
        {
            if (hProcess == IntPtr.Zero)
            {
                throw new ArgumentNullException("hProcess");
            }
            if (address == IntPtr.Zero)
            {
                throw new ArgumentNullException("dwAddress");
            }
            if (string.IsNullOrWhiteSpace(_assemblyString.ToString()))
            {
                throw new ArgumentNullException("asm");
            }
            uint exitCode = 0;
            try
            {
                if (!Inject(hProcess, address))
                {
                    throw new Exception("Injection failed for some reason.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            IntPtr threadId;
            IntPtr hThread = Imports.CreateRemoteThread(hProcess, IntPtr.Zero, 0, address, (IntPtr)parameter, 0, out threadId);
            if (hThread == IntPtr.Zero)
            {
                throw new Exception("Remote thread failed.");
            }
            try
            {
                if (Imports.WaitForSingleObject(hThread, 10000) == WaitValues.WAIT_OBJECT_0)
                {
                    if (!Imports.GetExitCodeThread(hThread, out exitCode))
                    {
                        throw new Exception("Could not get thread exit code.");
                    }
                }
            }
            finally
            {
                Imports.CloseHandle(hThread);
            }
            return exitCode;
        }

        public IntPtr InjectAndExecuteEx(IntPtr address)
        {
            return InjectAndExecuteEx(_hProcess, address, 0);
        }

        public IntPtr InjectAndExecuteEx(IntPtr hProcess, IntPtr address)
        {
            return InjectAndExecuteEx(hProcess, address, 0);
        }

        public IntPtr InjectAndExecuteEx(IntPtr hProcess, IntPtr address, uint parameter)
        {
            Inject(hProcess, address);
            IntPtr threadId;
            IntPtr hThread = Imports.CreateRemoteThread(hProcess, IntPtr.Zero, 0, address, (IntPtr)parameter, 0, out threadId);
            _threadHandles.Add(hThread);
            return hThread;
        }

        public IntPtr GetProcessHandle()
        {
            return _hProcess;
        }

        public void SetProcessHandle(IntPtr value)
        {
            _hProcess = value;
        }

        public int GetMemorySize()
        {
            return _memorySize;
        }

        public void SetMemorySize(int value)
        {
            _memorySize = value;
        }

        public int GetPassLimit()
        {
            return _passLimit;
        }

        public void SetPassLimit(int value)
        {
            _passLimit = value;
        }

        public StringBuilder AssemblyString
        {
            get { return _assemblyString; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseThreadHandles();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static readonly object _assembleLock = new object();

        [ThreadStatic]
        private static byte[] _memory;

        private StringBuilder _assemblyString;

        private List<IntPtr> _threadHandles;

        private IntPtr _hProcess;

        private int _memorySize;

        private int _passLimit;
    }
}
