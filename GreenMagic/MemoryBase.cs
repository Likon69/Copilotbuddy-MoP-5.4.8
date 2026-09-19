using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic.Native;

namespace GreenMagic
{
    public abstract class MemoryBase : IDisposable
    {
        public MemoryBase(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }
            _proc = process;
            process.EnableRaisingEvents = true;
            _proc.Exited += OnProcessExited;
            _hProc = Imports.OpenProcess(ProcessAccessFlags.PROCESS_CREATE_THREAD | ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessAccessFlags.PROCESS_SET_INFORMATION | ProcessAccessFlags.PROCESS_TERMINATE | ProcessAccessFlags.PROCESS_VM_OPERATION | ProcessAccessFlags.PROCESS_VM_READ | ProcessAccessFlags.PROCESS_VM_WRITE | ProcessAccessFlags.SYNCHRONIZE, false, _proc.Id);
        }

        private void DisposeUnmanaged()
        {
            Imports.CloseHandle(_hProc);
        }

        private void DisposeManaged()
        {
            if (!_disposed)
            {
                Imports.CloseHandle(_hProc);
                _disposed = true;
            }
        }

        public abstract unsafe bool ReadByteBuffer(IntPtr addr, void* buffer, int count);

        public abstract unsafe int WriteByteBuffer(IntPtr addr, void* buffer, int count);

        #region Read-Write

        public byte[] ReadBytes<T>(IntPtr addr) where T : struct
        {
            return ReadBytes(addr, MarshalCache<T>.Size);
        }

        public byte[] ReadBytes(IntPtr addr, int count, bool isRelative)
        {
            IntPtr address = isRelative ? GetAbsolute(addr) : addr;
            return ReadBytes(address, count);
        }

        public unsafe byte[] ReadBytes(IntPtr addr, int count)
        {
            byte[] array = new byte[count];
            fixed (byte* ptr = array)
            {
                ReadByteBuffer(addr, ptr, count);
            }
            return array;
        }

        public T Read<T>(bool isRelative, params IntPtr[] addrs) where T : struct
        {
            int addressCount = addrs.Length;
            if (addressCount == 0)
            {
                throw new InvalidOperationException("Cannot read a value from unspecified addresses.");
            }
            if (addressCount == 1)
            {
                IntPtr address;
                if (isRelative)
                {
                    IntPtr relative = addrs[0];
                    address = new IntPtr(ImageBase.ToInt64() + relative.ToInt64());
                }
                else
                {
                    address = addrs[0];
                }
                return Read<T>(address);
            }
            IntPtr pointer = Read<IntPtr>(addrs[0], isRelative);
            for (int index = 1; index < addrs.Length - 1; index++)
            {
                IntPtr nextAddress = addrs[index] + pointer.ToInt32();
                pointer = Read<IntPtr>(nextAddress);
            }
            IntPtr finalAddress = pointer + addrs[addrs.Length - 1].ToInt32();
            return Read<T>(finalAddress);
        }

        public T Read<T>(IntPtr addr, bool isRelative) where T : struct
        {
            IntPtr address = isRelative ? GetAbsolute(addr) : addr;
            return Read<T>(address);
        }

        public unsafe T Read<T>(IntPtr addr) where T : struct
        {
            T value = default(T);
            if (MarshalCache<T>.RequiresMarshal)
            {
                bool wasRead;
                value = ReadMarshaled<T>(addr, out wasRead);
                if (wasRead)
                {
                    return value;
                }
            }
            ReadByteBuffer(addr, Unsafe.AsPointer(ref value), MarshalCache<T>.Size);
            return value;
        }

        public T[] ReadArray<T>(IntPtr addr, int count, bool isRelative) where T : struct
        {
            IntPtr address = isRelative ? GetAbsolute(addr) : addr;
            return ReadArray<T>(address, count);
        }

        public unsafe T[] ReadArray<T>(IntPtr addr, int count) where T : struct
        {
            T[] array = new T[count];
            if (count > 0)
            {
                if (MarshalCache<T>.RequiresMarshal)
                {
                    byte[] rawData = new byte[MarshalCache<T>.Size * count];
                    fixed (byte* ptr = rawData)
                    {
                        ReadByteBuffer(addr, ptr, rawData.Length);
                        for (int index = 0; index < count; index++)
                        {
                            int offset = MarshalCache<T>.Size * index;
                            IntPtr elementPtr = new IntPtr(ptr + offset);
                            array[index] = (T)Marshal.PtrToStructure(elementPtr, typeof(T));
                        }
                    }
                }
                else
                {
                    fixed (byte* ptr = MemoryMarshal.AsBytes<T>(array))
                    {
                        int size = MarshalCache<T>.Size;
                        ReadByteBuffer(addr, ptr, size * count);
                    }
                }
            }
            return array;
        }

        public string ReadString(IntPtr address, Encoding encoding)
        {
            return ReadString(address, encoding, 512);
        }

        public string ReadString(IntPtr address, Encoding encoding, int maxLength, bool isRelative)
        {
            IntPtr resolvedAddress = isRelative ? GetAbsolute(address) : address;
            return ReadString(resolvedAddress, encoding, maxLength);
        }

        public virtual string ReadString(IntPtr address, Encoding encoding, int maxLength)
        {
            if (maxLength == 0)
            {
                return null;
            }
            string text = encoding.GetString(ReadBytes(address, maxLength));
            int nullIndex = text.IndexOf('\0');
            if (nullIndex != -1)
            {
                text = text.Remove(nullIndex);
            }
            return text;
        }

        public string ReadStringA(IntPtr address)
        {
            return ReadString(address, Encoding.ASCII, 512);
        }

        public string ReadStringW(IntPtr address)
        {
            return ReadString(address, Encoding.Unicode, 512);
        }

        public string ReadStringUTF8(IntPtr address)
        {
            return ReadString(address, Encoding.UTF8, 512);
        }

        public int WriteBytes(IntPtr addr, byte[] bytes, bool isRelative)
        {
            IntPtr address = isRelative ? GetAbsolute(addr) : addr;
            return WriteBytes(address, bytes);
        }

        public unsafe int WriteBytes(IntPtr addr, byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return 0;
            }
            fixed (byte* ptr = bytes)
            {
                return WriteByteBuffer(addr, ptr, bytes.Length);
            }
        }

        public void Write<T>(IntPtr addr, T value, bool isRelative) where T : struct
        {
            IntPtr address = isRelative ? GetAbsolute(addr) : addr;
            Write(address, value);
        }

        public unsafe void Write<T>(IntPtr addr, T value) where T : struct
        {
            bool wasWritten;
            WriteMarshaled(addr, value, out wasWritten);
            if (!wasWritten)
            {
                WriteByteBuffer(addr, Unsafe.AsPointer(ref value), MarshalCache<T>.Size);
            }
        }

        public bool WriteString(IntPtr addr, string value, Encoding encoding)
        {
            byte[] bytes;
            if (value[value.Length - 1] != '\0')
            {
                List<byte> list = new List<byte>(encoding.GetBytes(value));
                list.Add(0);
                if (!encoding.IsSingleByte)
                {
                    list.Add(0);
                }
                bytes = list.ToArray();
            }
            else
            {
                bytes = encoding.GetBytes(value);
            }
            return WriteBytes(addr, bytes) == bytes.Length;
        }

        #endregion

        public IntPtr GetAbsolute(IntPtr relative)
        {
            return new IntPtr(ImageBase.ToInt64() + relative.ToInt64());
        }

        public IntPtr GetRelative(IntPtr absolute)
        {
            IntPtr imageBase = ImageBase;
            return new IntPtr(absolute.ToInt64() - imageBase.ToInt64());
        }

        public IntPtr AllocateMemory(int size)
        {
            return Imports.VirtualAllocEx(_hProc, IntPtr.Zero, size, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_EXECUTE_READWRITE);
        }

        public IntPtr AllocateMemory(int size, uint allocationType, uint protect)
        {
            return Imports.VirtualAllocEx(_hProc, IntPtr.Zero, size, allocationType, protect);
        }

        public bool FreeMemory(IntPtr memory, int size, uint freeType)
        {
            size = (freeType == (uint)MemoryFreeType.MEM_RELEASE) ? 0 : size;
            return Imports.VirtualFreeEx(_hProc, memory, size, freeType);
        }

        public bool FreeMemory(IntPtr memory)
        {
            return FreeMemory(memory, 0, (uint)MemoryFreeType.MEM_RELEASE);
        }

        public event EventHandler ProcessExited;

        public IntPtr ImageBase
        {
            get
            {
                Process proc = _proc;
                if (proc != null && !proc.HasExited)
                {
                    if (_cachedImgBase == IntPtr.Zero)
                    {
                        _cachedImgBase = _proc.MainModule.BaseAddress;
                    }
                    return _cachedImgBase;
                }
                return IntPtr.Zero;
            }
        }

        public bool IsProcessOpen
        {
            get { return _hProc != IntPtr.Zero; }
        }

        public IntPtr ProcessHandle
        {
            get { return _hProc; }
        }

        public Process Process
        {
            get { return _proc; }
        }

        public virtual IntPtr GetProcAddress(string module, string function)
        {
            return Imports.GetProcAddress(Imports.GetModuleHandleW(module), function);
        }

        public IntPtr GetVFTableEntry(IntPtr address, int index)
        {
            IntPtr vftable = Read<IntPtr>(address) + IntPtr.Size * index;
            return Read<IntPtr>(vftable);
        }

        internal unsafe T ReadMarshaled<T>(IntPtr addr, out bool wasRead) where T : struct
        {
            T value = default(T);
            if (!MarshalCache<T>.RequiresMarshal)
            {
                wasRead = false;
                return value;
            }
            int size = MarshalCache<T>.Size;
            byte[] buffer = new byte[size];
            fixed (byte* ptr = buffer)
            {
                ReadByteBuffer(addr, ptr, size);
                value = (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
            }
            wasRead = true;
            return value;
        }

        internal unsafe int WriteMarshaled<T>(IntPtr addr, T value, out bool wasWritten) where T : struct
        {
            if (!MarshalCache<T>.RequiresMarshal)
            {
                wasWritten = false;
                return 0;
            }
            IntPtr unmanagedPtr = Marshal.AllocHGlobal(MarshalCache<T>.Size);
            Marshal.StructureToPtr(value, unmanagedPtr, false);
            int written = WriteByteBuffer(addr, (void*)unmanagedPtr, MarshalCache<T>.Size);
            Marshal.FreeHGlobal(unmanagedPtr);
            wasWritten = true;
            return written;
        }

        internal void OnProcessExited(object sender, EventArgs e)
        {
            ProcessExited?.Invoke(sender, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeManaged();
            }
            else
            {
                DisposeUnmanaged();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MemoryBase()
        {
            Dispose(false);
        }

        internal Process _proc;

        internal IntPtr _hProc;

        private IntPtr _cachedImgBase;

        private bool _disposed;
    }
}
