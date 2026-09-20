using System;
using System.Runtime.InteropServices;
using Styx.Patchables;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals.DBC
{
    public class SpellItemEnchantment
    {
        private SpellItemEnchantment(uint id, WoWDb.Row row)
        {
            Id = id;
            _record = row.GetStruct<SpellItemEnchantmentRecord>();
            Name = row.GetStringField<SpellItemEnchantmentRecord>("Name");
        }

        public uint Id { get; private set; }

        public string Name { get; private set; }

        public WoWItem.WoWItemStat GetStat(int index)
        {
            if (index < 0 || index > 2)
            {
                throw new ArgumentOutOfRangeException("index must be 0-2", "index");
            }
            if (_record.EnchantmentType[index] != 5)
            {
                return null;
            }
            return new WoWItem.WoWItemStat((WoWItemStatType)_record.SpellId[index], _record.Unk1[index]);
        }

        public override string ToString()
        {
            return _record.ToString();
        }

        public static SpellItemEnchantment FromId(uint id)
        {
            return DbcCache<SpellItemEnchantment>.GetOrAdd(ClientDb.SpellItemEnchantment, id, row => new SpellItemEnchantment(id, row));
        }

        private SpellItemEnchantmentRecord _record;

        #region Nested type: SpellItemEnchantmentRecord

        public struct SpellItemEnchantmentRecord
        {
            public uint dword0;

            public uint dword4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
            public int[] EnchantmentType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
            public int[] Unk1;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I4)]
            public int[] SpellId;

            public IntPtr Name;

            public uint dword30_UnkBool;

            public uint Flag;

            public uint ItemCacheId;

            public uint dword3C;

            public uint SkillId;

            public uint SkillValue;

            public uint dword48;

            public uint dword4C;

            public uint dword50;

            public uint dword54;

            public uint dword58;

            public uint dword5C;

            public uint dword60;
        }

        #endregion
    }
}
