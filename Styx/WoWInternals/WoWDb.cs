using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class WoWDb
    {
        private readonly Dictionary<ClientDb, DbTable> _tables = new Dictionary<ClientDb, DbTable>();

        internal WoWDb()
        {
            foreach (object value in Enum.GetValues(typeof(ClientDb)))
            {
                ClientDb clientDb = (ClientDb)value;
                _tables.Add(clientDb, new DbTable(StyxWoW.Memory.ImageBase + ((int)clientDb - 0x400000), clientDb));
            }
        }

        public DbTable this[ClientDb db]
        {
            get { return _tables[db]; }
        }

        #region Nested type: DbTable

        public class DbTable : IEnumerable<Row>
        {
            private readonly IntPtr _tablePtr;
            private WoWClientDb _nativeDb;
            private ClientDb _db;
            private DBCFile _header;
            private readonly GetRowPtrDelegate _getRowPtr;

            internal DbTable(IntPtr tablePtr, ClientDb db)
            {
                _db = db;
                _tablePtr = tablePtr;
                _nativeDb = StyxWoW.Memory.Read<WoWClientDb>(_tablePtr);
                if (_nativeDb.Data != (IntPtr)0)
                {
                    _header = StyxWoW.Memory.Read<DBCFile>(_nativeDb.Data);
                }

                if (db == ClientDb.CreatureDisplayInfoExtra || db == ClientDb.CreatureDisplayInfo ||
                    db == ClientDb.GameObjectDisplayInfo || db == ClientDb.ItemDisplayInfo ||
                    db == ClientDb.SpellEffect || db == ClientDb.SpellMisc ||
                    db == ClientDb.Spell || db == ClientDb.FileData)
                {
                    _getRowPtr = GetRowPtrSplit;
                }
                else
                {
                    _getRowPtr = GetRowPtrHashed;
                }
            }

            public uint RecordSize
            {
                get { return _header.RecordSize; }
            }

            internal IntPtr FirstRow
            {
                get { return _nativeDb.FirstRow; }
            }

            public bool IsValid
            {
                get { return _tablePtr != IntPtr.Zero && MaxIndex != 0; }
            }

            public int NumRows
            {
                get { return _nativeDb.NumRows; }
            }

            public int MaxIndex
            {
                get { return _nativeDb.MaxIndex; }
            }

            public int MinIndex
            {
                get { return _nativeDb.MinIndex; }
            }

            public Row GetRow(uint index)
            {
                IntPtr address = _getRowPtr(index);
                if (address == IntPtr.Zero)
                {
                    return null;
                }
                return new Row(address);
            }

            private IntPtr GetRowPtrSplit(uint index)
            {
                return StyxWoW.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.WoWClientDb__GetRow, CallingConvention.ThisCall, _tablePtr, index);
            }

            private IntPtr GetRowPtrHashed(uint index)
            {
                if (index < MinIndex || index > MaxIndex)
                {
                    return IntPtr.Zero;
                }

                uint actualIndex = index - (uint)MinIndex;

                int v5 = StyxWoW.Memory.Read<int>(_nativeDb.Unk1 + (int)(4 * (actualIndex >> 5)));
                IntPtr result = (IntPtr)0;

                int a2a = (int)(actualIndex & 0x1Fu);

                if (((1 << a2a) & v5) != 0)
                {
                    byte bitsSet = CountBitsSet((uint)(v5 << (31 - a2a)));
                    uint v8 = bitsSet + GetArrayEntryBySizeType(_nativeDb, _nativeDb.Unk3, (index - (uint)MinIndex) >> 5) - 1;

                    if (_nativeDb.Unk2 == 0)
                    {
                        v8 = GetArrayEntryBySizeType(_nativeDb, _nativeDb.Rows, v8);
                    }
                    result = _nativeDb.FirstRow + (int)(RecordSize * v8);
                }

                return result;
            }

            private byte CountBitsSet(uint value)
            {
                return
                    (byte)
                    (0x1010101 *
                     ((((value - ((value >> 1) & 0x55555555)) & 0x33333333) +
                       (((value - ((value >> 1) & 0x55555555)) >> 2) & 0x33333333) +
                       ((((value - ((value >> 1) & 0x55555555)) & 0x33333333) +
                         (((value - ((value >> 1) & 0x55555555)) >> 2) & 0x33333333)) >> 4)) & 0xF0F0F0F) >> 24);
            }

            private uint GetArrayEntryBySizeType(WoWClientDb db, IntPtr arrayPtr, uint index)
            {
                if (db.RowEntrySize == 2)
                {
                    return StyxWoW.Memory.Read<ushort>(arrayPtr + (int)(2 * index));
                }
                return StyxWoW.Memory.Read<uint>(arrayPtr + (int)(4 * index));
            }

            public IEnumerator<Row> GetEnumerator()
            {
                int min = MinIndex;
                int max = MaxIndex;

                for (int i = min; i <= max; i++)
                {
                    Row row = GetRow((uint)i);
                    if (row != null && row.IsValid)
                    {
                        yield return row;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private delegate IntPtr GetRowPtrDelegate(uint index);
        }

        #endregion

        #region Nested type: Row

        public class Row
        {
            private readonly IntPtr _address;

            internal Row(IntPtr address)
            {
                _address = address;
            }

            public IntPtr Address
            {
                get { return _address; }
            }

            public bool IsValid
            {
                get { return _address != IntPtr.Zero; }
            }

            public T GetField<T>(uint index) where T : struct
            {
                try
                {
                    return StyxWoW.Memory.Read<T>(_address + (int)(index * 4));
                }
                catch
                {
                    return default(T);
                }
            }

            public string GetStringField(uint index)
            {
                try
                {
                    int offset = StyxWoW.Memory.Read<int>(_address + (int)(index * 4));
                    return StyxWoW.Memory.ReadString(_address + offset + (int)(index * 4), Encoding.UTF8);
                }
                catch
                {
                    return null;
                }
            }

            public string GetStringField<T>(string fieldName)
            {
                try
                {
                    uint offset = (uint)(int)Marshal.OffsetOf(typeof(T), fieldName);
                    return GetStringField(offset / 4);
                }
                catch
                {
                    return null;
                }
            }

            public T GetStruct<T>() where T : struct
            {
                try
                {
                    return StyxWoW.Memory.Read<T>(_address);
                }
                catch
                {
                    return default(T);
                }
            }
        }

        #endregion

        #region Nested type: WoWClientDb

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct WoWClientDb
        {
            private readonly IntPtr _vtable;
            public int NumRows;
            public int MaxIndex;
            public int MinIndex;
            public IntPtr Data;
            public IntPtr FirstRow;
            public IntPtr Rows;
            public IntPtr Unk1;
            public uint Unk2;
            public IntPtr Unk3;
            public uint RowEntrySize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DBCFile
        {
            public uint Magic;
            public int RecordsCount;
            public int FieldsCount;
            public uint RecordSize;
            public int StringTableSize;
        }

        #endregion
    }
}
