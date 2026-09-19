using System;
using System.Collections.Generic;
using System.Text;
using GreenMagic.Native;

namespace GreenMagic
{
    public class AllocatedMemory : IDisposable
    {
        public AllocatedMemory(ExternalProcessMemory memory, int bytes)
        {
            _allocated = new Dictionary<string, IntPtr>();
            _memory = memory;
            _tempReadCache = memory.TemporaryCacheState(false);
            AllocateMemory(bytes);
        }

        private void ReleaseMemory()
        {
            if (!_isDisposed)
            {
                IDisposable tempReadCache = _tempReadCache;
                if (tempReadCache != null)
                {
                    tempReadCache.Dispose();
                }
                _memory.FreeMemory(Address);
                _isDisposed = true;
            }
        }

        public void Write<T>(string allocatedName, int offsetInBytes, T value) where T : struct
        {
            IntPtr address = GetAllocatedChunk(allocatedName) + offsetInBytes;
            _memory.Write(address, value);
        }

        public void Write<T>(string allocatedName, T value) where T : struct
        {
            if (!_allocated.ContainsKey(allocatedName))
            {
                AllocateOfChunk(allocatedName, MarshalCache<T>.Size);
            }
            IntPtr allocatedChunk = GetAllocatedChunk(allocatedName);
            _memory.Write(allocatedChunk, value);
        }

        public void Write<T>(int offsetInBytes, T value) where T : struct
        {
            IntPtr address = Address + offsetInBytes;
            _memory.Write(address, value);
        }

        public void WriteString(string allocatedName, string value, Encoding encoding)
        {
            IntPtr allocatedChunk = GetAllocatedChunk(allocatedName);
            _memory.WriteString(allocatedChunk, value, encoding);
        }

        public void WriteString(int offsetInBytes, string value, Encoding encoding)
        {
            IntPtr address = Address + offsetInBytes;
            _memory.WriteString(address, value, encoding);
        }

        public void WriteString(string allocatedName, int offsetInBytes, string value, Encoding encoding)
        {
            IntPtr address = GetAllocatedChunk(allocatedName) + offsetInBytes;
            _memory.WriteString(address, value, encoding);
        }

        public void WriteBytes(string allocatedName, byte[] bytes)
        {
            if (!_allocated.ContainsKey(allocatedName))
            {
                AllocateOfChunk(allocatedName, bytes.Length);
            }
            IntPtr allocatedChunk = GetAllocatedChunk(allocatedName);
            _memory.WriteBytes(allocatedChunk, bytes);
        }

        public void WriteBytes(int offsetInBytes, byte[] bytes)
        {
            IntPtr address = Address + offsetInBytes;
            _memory.WriteBytes(address, bytes);
        }

        public T Read<T>(string allocatedName, int offsetInBytes) where T : struct
        {
            T value;
            using (_memory.SaveCacheState())
            {
                _memory.DisableCache();
                IntPtr allocatedChunk = GetAllocatedChunk(allocatedName);
                IntPtr address = allocatedChunk + offsetInBytes;
                value = _memory.Read<T>(address);
            }
            return value;
        }

        public T Read<T>(string allocatedName) where T : struct
        {
            return Read<T>(allocatedName, 0);
        }

        public T Read<T>(int offsetInBytes) where T : struct
        {
            T value;
            using (_memory.SaveCacheState())
            {
                _memory.DisableCache();
                IntPtr address = Address + offsetInBytes;
                value = _memory.Read<T>(address);
            }
            return value;
        }

        public IntPtr GetAddress(int offsetInBytes)
        {
            return Address + offsetInBytes;
        }

        public void AllocateOfChunk(string allocatedName, int bytes)
        {
            IntPtr offset = (IntPtr)_currentOffset;
            _allocated.Add(allocatedName, offset);
            int newOffset = bytes + _currentOffset;
            _currentOffset = newOffset;
            int remainder = newOffset % 4;
            if (remainder != 0)
            {
                _currentOffset = newOffset - remainder + 4;
            }
        }

        public void AllocateOfChunk<T>(string allocatedName) where T : struct
        {
            AllocateOfChunk(allocatedName, MarshalCache<T>.Size);
        }

        public IntPtr GetAllocatedChunk(string allocatedName)
        {
            IntPtr offset = _allocated[allocatedName];
            return new IntPtr(Address.ToInt64() + offset.ToInt64());
        }

        public void AlignTo(ref int address, int align)
        {
            int value = address;
            int remainder = value % align;
            if (remainder != 0)
            {
                address = value - remainder + align;
            }
        }

        public IntPtr this[string s]
        {
            get { return GetAllocatedChunk(s); }
        }

        protected virtual void AllocateMemory(int bytes)
        {
            Address = _memory.AllocateMemory(bytes, (uint)MemoryAllocationType.MEM_COMMIT, (uint)MemoryProtectionType.PAGE_EXECUTE_READWRITE);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseMemory();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IntPtr Address;

        private ExternalReadCache _tempReadCache;

        private Dictionary<string, IntPtr> _allocated;

        private ExternalProcessMemory _memory;

        private int _currentOffset;

        private bool _isDisposed;
    }
}
