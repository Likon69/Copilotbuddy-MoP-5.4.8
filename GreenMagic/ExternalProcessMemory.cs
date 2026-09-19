using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using GreenMagic.Fasm;
using GreenMagic.Native;

namespace GreenMagic
{
    public class ExternalProcessMemory : MemoryBase
    {
        private void Init(Process p, bool startupExecutor, bool dx9, bool startupRasm, IntPtr hookFunc, bool defaultCacheValue, int copyBytes, bool veh)
        {
            _cache = new ThreadLocal<Dictionary<IntPtr, byte[]>>(() => new Dictionary<IntPtr, byte[]>());
            _cacheEnabled = new ThreadLocal<bool>(() => defaultCacheValue);
            if (_hProc != IntPtr.Zero)
            {
                _hThread = Imports.OpenThread(AccessRights.THREAD_ALL_ACCESS, false, (uint)_proc.Threads[0].Id);
                _hWnd = _proc.MainWindowHandle;
                _fasm = new ManagedFasm(_hProc);
                if (startupExecutor)
                {
                    IntPtr hookLocation = new DirectX(this, dx9).HookLocation;
                    if (hookFunc != IntPtr.Zero)
                    {
                        hookLocation = hookFunc;
                    }
                    _exec = new Executor(this, hookLocation, copyBytes, veh);
                }
            }
        }

        private unsafe void ReadWithPartialSupport(IntPtr address, void* buffer, int count, out int numRead)
        {
            numRead = 0;
            int bytesRead;
            bool success = Imports.ReadProcessMemory(_hProc, address, buffer, count, out bytesRead);
            if (success && bytesRead == count)
            {
                numRead = count;
                return;
            }
            if (Marshal.GetLastWin32Error() != 299)
            {
                throw new Win32Exception();
            }
            int low = 0;
            int high = count;
            while (low < high - 1)
            {
                int mid = ((high - low) >> 1) + low;
                IntPtr midAddress = address + low;
                int chunkSize = mid - low;
                success = Imports.ReadProcessMemory(_hProc, midAddress, (byte*)buffer + low, chunkSize, out bytesRead);
                if (success && bytesRead == chunkSize)
                {
                    low = mid;
                }
                else
                {
                    if (Marshal.GetLastWin32Error() != 299)
                    {
                        throw new Win32Exception();
                    }
                    high = mid;
                }
            }
            numRead = low;
        }

        public IEnumerable<string> GenerateAsm(IntPtr address, CallingConvention callingConvention, params object[] args)
        {
            bool extendedMem;
            return GenerateAsm(address, callingConvention, args, typeof(uint), out extendedMem);
        }

        private IEnumerable<string> GenerateAsm(IntPtr address, CallingConvention callingConvention, object[] args, Type returnType, out bool extendedMem)
        {
            List<string> list = new List<string>();
            int startArg = 0;
            if (callingConvention == CallingConvention.ThisCall)
            {
                if (args.Length <= 0)
                {
                    throw new Exception("First argument when specifying ThisCall must be > 0");
                }
                list.Add("mov ecx, " + args[0]);
                startArg = 1;
            }
            else if (callingConvention == CallingConvention.FastCall)
            {
                if (args.Length <= 0)
                {
                    throw new Exception("First argument when specifying FastCall must be > 0");
                }
                list.Add("mov ecx, " + args[0]);
                startArg = 1;
                if (args.Length > 1)
                {
                    list.Add("mov edx, " + args[1]);
                    startArg = 2;
                }
            }
            int stackClean = 0;
            for (int index = args.Length - 1; index >= startArg; index--)
            {
                switch (Type.GetTypeCode(args[index].GetType()))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        list.Add("push " + args[index]);
                        stackClean += 4;
                        break;
                    case TypeCode.Int64:
                        list.Add("push " + ((long)args[index] >> 32));
                        list.Add("push " + ((long)args[index] & 4294967295L));
                        stackClean += 8;
                        break;
                    case TypeCode.UInt64:
                        list.Add("push " + ((ulong)args[index] >> 32));
                        list.Add("push " + ((ulong)args[index] & 4294967295UL));
                        stackClean += 8;
                        break;
                    default:
                        if (args[index].GetType() != typeof(IntPtr))
                        {
                            throw new ArgumentException(string.Format("Argument of type {0} can not be pushed onto the argument stack", args[index].GetType().Name));
                        }
                        list.Add("push " + args[index]);
                        stackClean += 4;
                        break;
                }
            }
            list.Add("call " + address);
            if (callingConvention == CallingConvention.Cdecl || callingConvention == CallingConvention.FastCall)
            {
                list.Add("add esp, " + stackClean);
            }
            switch (Type.GetTypeCode(returnType))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    extendedMem = false;
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    extendedMem = true;
                    AllocateExtendedMemory();
                    list.Add(string.Format("mov dword [{0}], eax", _returnData.Address));
                    IntPtr edxAddress = _returnData.Address + 4;
                    list.Add(string.Format("mov dword [{0}], edx", edxAddress));
                    break;
                case TypeCode.Single:
                    extendedMem = true;
                    AllocateExtendedMemory();
                    list.Add(string.Format("fstp dword [{0}]", _returnData.Address));
                    break;
                case TypeCode.Double:
                    extendedMem = true;
                    AllocateExtendedMemory();
                    list.Add(string.Format("fstp qword [{0}]", _returnData.Address));
                    break;
                default:
                    if (returnType != typeof(IntPtr))
                    {
                        throw new Exception("Cannot generate ASM for return type " + returnType.FullName);
                    }
                    extendedMem = false;
                    break;
            }
            list.Add("retn");
            return list;
        }

        private void AllocateExtendedMemory()
        {
            if (_returnData == null)
            {
                _returnData = new AllocatedMemory(this, 8);
            }
        }

        public ExternalProcessMemory(Process p, bool startupExecutor, bool dx9, bool startupRasm)
            : base(p)
        {
            try
            {
                Init(p, startupExecutor, dx9, startupRasm, IntPtr.Zero, true, 0, false);
            }
            catch
            {
                base.Dispose(true);
                throw;
            }
        }

        public ExternalProcessMemory(Process p, bool startupExecutor, bool dx9, bool startupRasm, IntPtr hookFunc, bool defaultCacheValue)
            : base(p)
        {
            try
            {
                Init(p, startupExecutor, dx9, startupRasm, hookFunc, defaultCacheValue, 0, false);
            }
            catch
            {
                base.Dispose(true);
                throw;
            }
        }

        public ExternalProcessMemory(Process p, bool startupExecutor, bool dx9, bool startupRasm, IntPtr hookFunc, bool defaultCacheValue, int copyBytes)
            : base(p)
        {
            try
            {
                Init(p, startupExecutor, dx9, startupRasm, hookFunc, defaultCacheValue, copyBytes, false);
            }
            catch
            {
                base.Dispose(true);
                throw;
            }
        }

        public ExternalProcessMemory(Process p, bool startupExecutor, bool dx9, bool startupRasm, IntPtr hookFunc, bool defaultCacheValue, int copyBytes, bool veh)
            : base(p)
        {
            try
            {
                Init(p, startupExecutor, dx9, startupRasm, hookFunc, defaultCacheValue, copyBytes, veh);
            }
            catch
            {
                base.Dispose(true);
                throw;
            }
        }

        private void DisposeManagedResources()
        {
            if (_returnData != null)
            {
                _returnData.Dispose();
            }
            _returnData = null;
            if (_fasm != null)
            {
                _fasm.Dispose();
            }
            _fasm = null;
            if (_exec != null)
            {
                _exec.Dispose();
            }
            _exec = null;
        }

        #region Overrides of MemoryBase

        public override unsafe bool ReadByteBuffer(IntPtr addr, void* buffer, int count)
        {
            if (count == 0)
            {
                return true;
            }
            byte[] cached;
            if (_cacheEnabled.Value && _cache.Value.TryGetValue(addr, out cached))
            {
                if (cached.Length == count)
                {
                    fixed (byte* ptr = cached)
                    {
                        Buffer.MemoryCopy(ptr, buffer, count, count);
                    }
                    return true;
                }
                _cache.Value.Remove(addr);
            }
            int numberOfBytesRead;
            if (Imports.ReadProcessMemory(_hProc, addr, buffer, count, out numberOfBytesRead) && numberOfBytesRead == count)
            {
                if (_cacheEnabled.Value)
                {
                    byte[] array = new byte[count];
                    fixed (byte* ptr = array)
                    {
                        Buffer.MemoryCopy(buffer, ptr, count, count);
                    }
                    _cache.Value.Add(addr, array);
                }
                return true;
            }
            throw new Exception(new Win32Exception().Message + ", at addr: " + addr.ToString("X8") + ", Size: " + count);
        }

        public override unsafe int WriteByteBuffer(IntPtr addr, void* buffer, int count)
        {
            int numberOfBytesWritten;
            if (Imports.WriteProcessMemory(_hProc, addr, buffer, count, out numberOfBytesWritten) && numberOfBytesWritten == count)
            {
                return numberOfBytesWritten;
            }
            throw new Exception("Error while writing Memory!");
        }

        public override unsafe string ReadString(IntPtr address, Encoding encoding, int maxLength)
        {
            if (maxLength == 0)
            {
                return "";
            }
            byte[] buffer = new byte[maxLength];
            int numRead;
            fixed (byte* ptr = buffer)
            {
                ReadWithPartialSupport(address, ptr, maxLength, out numRead);
            }
            string text = encoding.GetString(buffer, 0, numRead);
            int nullIndex = text.IndexOf('\0');
            if (nullIndex != -1)
            {
                text = text.Remove(nullIndex);
            }
            return text;
        }

        public override unsafe IntPtr GetProcAddress(string module, string function)
        {
            if (!module.Contains("."))
            {
                module += ".dll";
            }
            ProcessModule targetModule = null;
            foreach (ProcessModule processModule in _proc.Modules)
            {
                if (string.Equals(Path.GetFileName(processModule.FileName), module, StringComparison.InvariantCultureIgnoreCase))
                {
                    targetModule = processModule;
                    break;
                }
            }
            if (targetModule == null)
            {
                return IntPtr.Zero;
            }
            IntPtr baseAddress = targetModule.BaseAddress;
            byte[] dosHeaderBytes = new byte[64];
            ImageDosHeader dosHeader;
            fixed (byte* ptr = dosHeaderBytes)
            {
                if (!ReadByteBuffer(baseAddress, ptr, dosHeaderBytes.Length))
                {
                    return IntPtr.Zero;
                }
                dosHeader = (ImageDosHeader)Marshal.PtrToStructure((IntPtr)ptr, typeof(ImageDosHeader));
            }
            if (dosHeader.e_magic != 0x5A4D)
            {
                return IntPtr.Zero;
            }
            IntPtr ntHeadersAddress = baseAddress + dosHeader.e_lfanew;
            byte[] ntHeaderBytes = new byte[248];
            ImageNtHeader ntHeaders;
            fixed (byte* ptr = ntHeaderBytes)
            {
                ReadByteBuffer(ntHeadersAddress, ptr, ntHeaderBytes.Length);
                ntHeaders = (ImageNtHeader)Marshal.PtrToStructure((IntPtr)ptr, typeof(ImageNtHeader));
            }
            if (ntHeaders.Signature != 0x4550)
            {
                return IntPtr.Zero;
            }
            uint exportDirectoryRva = ntHeaders.OptionalHeader.DataDirectory[0].VirtualAddress;
            uint exportDirectorySize = ntHeaders.OptionalHeader.DataDirectory[0].Size;
            IntPtr exportDirectoryAddress = baseAddress + (int)exportDirectoryRva;
            byte[] exportDirectoryBytes = new byte[40];
            ImageExportDirectory exportDirectory;
            fixed (byte* ptr = exportDirectoryBytes)
            {
                if (!ReadByteBuffer(exportDirectoryAddress, ptr, exportDirectoryBytes.Length))
                {
                    return IntPtr.Zero;
                }
                exportDirectory = (ImageExportDirectory)Marshal.PtrToStructure((IntPtr)ptr, typeof(ImageExportDirectory));
            }
            IntPtr addressOfFunctions = baseAddress + (int)exportDirectory.AddressOfFunctions;
            uint[] functions = ReadArray<uint>(addressOfFunctions, (int)exportDirectory.NumberOfFunctions);
            IntPtr addressOfNames = baseAddress + (int)exportDirectory.AddressOfNames;
            uint[] names = ReadArray<uint>(addressOfNames, (int)exportDirectory.NumberOfNames);
            IntPtr addressOfNameOrdinals = baseAddress + (int)exportDirectory.AddressOfNameOrdinals;
            ushort[] nameOrdinals = ReadArray<ushort>(addressOfNameOrdinals, (int)exportDirectory.NumberOfNames);
            if (!function.StartsWith("#"))
            {
                for (int index = 0; index < exportDirectory.NumberOfNames; index++)
                {
                    IntPtr nameAddress = baseAddress + (int)names[index];
                    if (ReadStringA(nameAddress) != function)
                    {
                        continue;
                    }
                    uint functionRva = functions[nameOrdinals[index]];
                    if (functionRva < exportDirectoryRva || functionRva >= exportDirectoryRva + exportDirectorySize)
                    {
                        return baseAddress + (int)functionRva;
                    }
                    IntPtr forwarderAddress = baseAddress + (int)functionRva;
                    string forwarder = ReadStringA(forwarderAddress);
                    int separatorIndex = forwarder.LastIndexOf('.');
                    if (separatorIndex == -1)
                    {
                        return IntPtr.Zero;
                    }
                    string forwarderModule = forwarder.Remove(separatorIndex);
                    string forwarderFunction = forwarder.Substring(separatorIndex + 1);
                    return GetProcAddress(forwarderModule, forwarderFunction);
                }
                return IntPtr.Zero;
            }
            ushort ordinal;
            if (!ushort.TryParse(function.Substring(1), out ordinal))
            {
                return IntPtr.Zero;
            }
            if (ordinal >= functions.Length)
            {
                return IntPtr.Zero;
            }
            return baseAddress + (int)functions[ordinal];
        }

        #endregion

        public FrameLock AcquireFrame(bool isHardLock)
        {
            FrameLock frameLock = new FrameLock(_exec);
            try
            {
                if (isHardLock)
                {
                    _exec.GrabFrame();
                }
                return frameLock;
            }
            catch (Exception)
            {
                if (frameLock != null)
                {
                    frameLock.Dispose();
                }
                throw;
            }
        }

        public FrameLock AcquireFrame()
        {
            return AcquireFrame(false);
        }

        public FrameLockRelease ReleaseFrame(bool reacquireAsHardLock)
        {
            return new FrameLockRelease(_exec, reacquireAsHardLock);
        }

        public FrameLockRelease ReleaseFrame()
        {
            return new FrameLockRelease(_exec, false);
        }

        #region Alloc

        public AllocatedMemory CreateAllocatedMemory(int bytes)
        {
            return new AllocatedMemory(this, bytes);
        }

        #endregion

        public ManagedFasm Asm
        {
            get { return _fasm; }
        }

        public Executor Executor
        {
            get { return _exec; }
        }

        public bool IsThreadOpen
        {
            get { return _hThread != IntPtr.Zero; }
        }

        public ExternalReadCache SaveCacheState()
        {
            return new ExternalReadCache(this);
        }

        public ExternalReadCache TemporaryCacheState(bool enabledTemporarily)
        {
            return new ExternalReadCache(this, enabledTemporarily);
        }

        public void ClearCache()
        {
            _cache.Value.Clear();
        }

        public void DisableCache()
        {
            _cacheEnabled.Value = false;
        }

        public void EnableCache()
        {
            _cacheEnabled.Value = true;
        }

        public bool CacheEnabled
        {
            get { return _cacheEnabled.Value; }
        }

        public void CallInjected(IntPtr address, CallingConvention callingConvention, params object[] args)
        {
            CallInjected<uint>(address, callingConvention, args);
        }

        public T CallInjected<T>(IntPtr address, CallingConvention callingConvention, params object[] args) where T : struct
        {
            using (new ExternalReadCache(this, false))
            {
                Executor exec = _exec;
                if (exec == null)
                {
                    throw new InvalidOperationException("Executor cannot be null");
                }
                lock (exec.AssemblyLock)
                {
                    exec.Clear();
                    bool extendedMem;
                    foreach (string line in GenerateAsm(address, callingConvention, args, typeof(T), out extendedMem))
                    {
                        exec.AddRandomLine(line);
                    }
                    exec.Execute();
                    if (extendedMem)
                    {
                        return Read<T>(_returnData.Address);
                    }
                    return Read<T>(exec.ReturnPointer);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    DisposeManagedResources();
                }
                finally
                {
                    base.Dispose(true);
                }
            }
            else
            {
                base.Dispose(false);
            }
        }

        private ThreadLocal<Dictionary<IntPtr, byte[]>> _cache;

        private ThreadLocal<bool> _cacheEnabled;

        private AllocatedMemory _returnData;

        private ManagedFasm _fasm;

        private IntPtr _hThread;

        private IntPtr _hWnd;

        private Executor _exec;
    }
}
