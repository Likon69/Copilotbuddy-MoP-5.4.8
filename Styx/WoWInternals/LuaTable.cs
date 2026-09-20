using System;
using System.Text;
using GreenMagic;

namespace Styx.WoWInternals
{
    public class LuaTable
    {
        internal LuaTable(IntPtr address)
        {
            Address = address;
            _table = StyxWoW.Memory.Read<NativeLuaTable>(address);
        }

        public static int NativeSize
        {
            get { return MarshalCache<NativeLuaTable>.Size; }
        }

        public IntPtr Address { get; private set; }

        public byte Flags
        {
            get { return _table.Flags; }
        }

        public LuaTable MetaTable
        {
            get
            {
                if (!_metaTableConstructed)
                {
                    _metaTable = _table.MetaTablePtr != IntPtr.Zero ? new LuaTable(_table.MetaTablePtr) : null;
                    _metaTableConstructed = true;
                }
                return _metaTable;
            }
        }

        internal uint ValuesCount
        {
            get { return _table.ValuesCount; }
        }

        internal uint NodesCount
        {
            get { return _table.NodesCount; }
        }

        public int Count
        {
            get { return GetCount(); }
        }

        private static uint Hashstr(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            uint h = (uint)s.Length;
            uint step = (h >> 5) + 1;
            for (uint l1 = h; l1 >= step; l1 -= step)
            {
                h = h ^ ((h << 5) + (h >> 2) + bytes[l1 - 1]);
            }
            return h;
        }

        private static unsafe uint Hashnum(double n)
        {
            n += 1;
            uint* vals = (uint*)&n;
            uint hash = vals[1] + vals[0];
            return hash;
        }

        public LuaTValue GetField(string key)
        {
            uint hash = Hashstr(key);
            LuaNode node = GetNode(hash & (NodesCount - 1));
            do
            {
                if (node.Key.Type == LuaType.String && string.Equals(key, node.Key.Value.String.Value))
                {
                    return node.Value;
                }
                node = node.Key.Next;
            }
            while (node != null);
            return null;
        }

        internal LuaTValue[] GetValues(int index, int count)
        {
            if (count == 0)
            {
                return new LuaTValue[0];
            }
            NativeLuaTValue[] vals = StyxWoW.Memory.ReadArray<NativeLuaTValue>(_table.ValuesPtr + index * MarshalCache<NativeLuaTValue>.Size, count);
            LuaTValue[] ret = new LuaTValue[count];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new LuaTValue(_table.ValuesPtr + (index + i) * MarshalCache<NativeLuaTValue>.Size, vals[i]);
            }
            return ret;
        }

        public LuaTValue GetField(double key)
        {
            uint uKey = (uint)key;
            double hashVal;
            if (uKey == key)
            {
                if (uKey < ValuesCount)
                {
                    return GetValue(uKey);
                }
                hashVal = uKey;
            }
            else
            {
                hashVal = key;
            }
            uint hash = Hashnum(hashVal);
            uint index = hash % ((NodesCount - 1u) | 1u);
            LuaNode node = GetNode(index);
            do
            {
                if (node.Key.Type == LuaType.Number && node.Key.Value.Double == key)
                {
                    return node.Value;
                }
                node = node.Key.Next;
            }
            while (node != null);
            return null;
        }

        private LuaTValue[] GetAllValues()
        {
            NativeLuaTValue[] vals = StyxWoW.Memory.ReadArray<NativeLuaTValue>(_table.ValuesPtr, (int)ValuesCount);
            LuaTValue[] ret = new LuaTValue[vals.Length];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new LuaTValue(_table.ValuesPtr + i * MarshalCache<NativeLuaTValue>.Size, vals[i]);
            }
            return ret;
        }

        private LuaTValue GetValue(uint index)
        {
            return new LuaTValue(_table.ValuesPtr + LuaTValue.NativeSize * (int)index);
        }

        private LuaNode GetNode(uint index)
        {
            return new LuaNode(_table.NodePtr + LuaNode.NativeSize * (int)index);
        }

        private int UnboundSearch(LuaTValue[] values, uint j)
        {
            uint i = j;
            j++;
            LuaTValue val;
            while ((val = GetField(j - 1)) != null && val.Type != LuaType.Nil)
            {
                i = j;
                j *= 2;
                const int maxInt = int.MaxValue - 2;
                if (j > maxInt)
                {
                    i = 1;
                    while ((val = GetField(i - 1)) != null && val.Type != LuaType.Nil)
                    {
                        i++;
                    }
                    return (int)(i - 1);
                }
            }
            while (j - i > 1)
            {
                uint m = (i + j) / 2;
                if ((val = GetField(m - 1)) != null && val.Type == LuaType.Nil)
                {
                    j = m;
                }
                else
                {
                    i = m;
                }
            }
            return (int)i;
        }

        private int GetCount()
        {
            uint j = ValuesCount;
            LuaTValue[] values = GetAllValues();
            if (j > 0 && values[j - 1].Type == LuaType.Nil)
            {
                uint i = 0;
                while (j - i > 1)
                {
                    uint m = (i + j) / 2;
                    if (values[m - 1].Type == LuaType.Nil)
                    {
                        j = m;
                    }
                    else
                    {
                        i = m;
                    }
                }
                return (int)i;
            }
            if (_table.NodePtr == StyxWoW.Offsets.Statics.LuaDummyNode)
            {
                return (int)j;
            }
            return UnboundSearch(values, j);
        }

        private LuaTable _metaTable;

        private bool _metaTableConstructed;

        private NativeLuaTable _table;
    }
}
