using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class ItemRandomSuffix
    {
        private ItemRandomSuffix(uint id, WoWDb.Row row)
        {
            Id = id;
            _entry = row.GetStruct<ItemRandomSuffixEntry>();
            Name = row.GetStringField<ItemRandomSuffixEntry>("NameSuffix");
        }

        public uint Id { get; private set; }

        public string Name { get; private set; }

        public List<SpellItemEnchantment> Enchantments
        {
            get
            {
                IEnumerable<SpellItemEnchantment> enumerable = _entry.EnchantId.Select(SpellItemEnchantment.FromId);
                return enumerable.Where(spellItemEnchantment => spellItemEnchantment != null).ToList();
            }
        }

        public int[] Prefix
        {
            get { return _entry.Prefix; }
        }

        public static ItemRandomSuffix FromId(uint id)
        {
            return DbcCache<ItemRandomSuffix>.GetOrAdd(ClientDb.ItemRandomSuffix, id, row => new ItemRandomSuffix(id, row));
        }

        private ItemRandomSuffixEntry _entry;

        #region Nested type: ItemRandomSuffixEntry

        public struct ItemRandomSuffixEntry
        {
            public readonly uint Id;

            public readonly IntPtr NameSuffix;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public readonly uint[] EnchantId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public readonly int[] Prefix;
        }

        #endregion
    }
}
