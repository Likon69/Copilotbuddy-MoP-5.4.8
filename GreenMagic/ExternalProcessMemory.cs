using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        #region Alloc

        public AllocatedMemory CreateAllocatedMemory(int bytes)
        {
            return new AllocatedMemory(this, bytes);
        }

        #endregion

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

        private IntPtr _hThread;

        private IntPtr _hWnd;
    }
}
