using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Styx.Common.Compression;

namespace Styx.Common
{
    public class FileCache<T> : IEnumerable<T>, IDisposable, IList<T>, ICollection<T>, IEnumerable where T : struct
    {
        public void Add(T value)
        {
            _internalList.Add(new CacheEntry(value, false));
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(T item)
        {
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            return _internalList.Any(s => equalityComparer.Equals(item, s.Value));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalList.Select(s => s.Value).ToList().CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _internalList.Count; i++)
            {
                if (equalityComparer.Equals(item, _internalList[i].Value))
                {
                    _internalList.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public int Count
        {
            get
            {
                return _internalList.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(T value, bool isTemporary)
        {
            _internalList.Add(new CacheEntry(value, isTemporary));
        }

        public int IndexOf(T item)
        {
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            return _internalList.FindIndex(s => equalityComparer.Equals(item, s.Value));
        }

        public void Insert(int index, T item)
        {
            _internalList.Insert(index, new CacheEntry(item, false));
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        T IList<T>.this[int index]
        {
            get
            {
                return _internalList[index].Value;
            }
            set
            {
                _internalList[index] = new CacheEntry(value, false);
            }
        }

        public FileCache(string cacheFilePath, bool createDirectoryStructure = true)
        {
            CacheFilePath = cacheFilePath;
            if (createDirectoryStructure)
            {
                string directoryName = Path.GetDirectoryName(cacheFilePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
            }
            LoadFile();
        }

        public string CacheFilePath { get; private set; }

        protected void LoadFile()
        {
            if (!File.Exists(CacheFilePath))
            {
                return;
            }
            byte[] array = LZMAHelper.Decompress(File.ReadAllBytes(CacheFilePath));
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    char[] array2 = binaryReader.ReadChars(4);
                    if (new string(array2) != "BDFC")
                    {
                        throw new Exception("File is not a valid FileCache file.");
                    }
                    int num = binaryReader.ReadInt32();
                    for (int i = 0; i < num; i++)
                    {
                        _internalList.Add(new CacheEntry(Read(binaryReader), false));
                    }
                }
            }
        }

        public void SaveFile()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
                {
                    binaryWriter.Write("BDFC".ToCharArray());
                    binaryWriter.Write(Count);
                    foreach (CacheEntry entry in _internalList)
                    {
                        if (!entry.IsTemporary)
                        {
                            Write(binaryWriter, entry.Value);
                        }
                    }
                }
                byte[] array = LZMAHelper.Compress(memoryStream.ToArray());
                if (!Directory.Exists(Path.GetDirectoryName(CacheFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath));
                }
                File.WriteAllBytes(CacheFilePath, array);
            }
        }

        protected T Read(BinaryReader reader)
        {
            byte[] array = reader.ReadBytes(StructureSize);
            GCHandle gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            T t;
            try
            {
                t = (T)((object)Marshal.PtrToStructure(gchandle.AddrOfPinnedObject(), typeof(T)));
            }
            finally
            {
                gchandle.Free();
            }
            return t;
        }

        protected void Write(BinaryWriter writer, T value)
        {
            byte[] array = new byte[StructureSize];
            GCHandle gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr<T>(value, gchandle.AddrOfPinnedObject(), false);
                writer.Write(array);
            }
            finally
            {
                gchandle.Free();
            }
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                SaveFile();
            }
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        protected bool Disposed { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            return _internalList.Select(s => s.Value).GetEnumerator();
        }

        ~FileCache()
        {
            Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return _internalList.Select(s => s.Value).ToList().AsReadOnly();
        }

        private const string MAGIC = "BDFC";

        private readonly List<CacheEntry> _internalList = new List<CacheEntry>();

        protected readonly int StructureSize = Marshal.SizeOf(typeof(T));

        private struct CacheEntry
        {
            public CacheEntry(T value, bool isTemporary = false)
            {
                this = default(CacheEntry);
                IsTemporary = isTemporary;
                Value = value;
            }

            public bool IsTemporary { get; private set; }

            public T Value { get; private set; }
        }
    }
}
