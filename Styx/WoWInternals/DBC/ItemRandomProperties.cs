using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class ItemRandomProperties
    {
        private ItemRandomProperties(uint id, WoWDb.Row row)
        {
            Id = id;
            _record = row.GetStruct<ItemRandomPropertiesRecord>();
            Name = row.GetStringField<ItemRandomPropertiesRecord>("Name");
            Suffix = row.GetStringField<ItemRandomPropertiesRecord>("Suffix");
        }

        public uint Id { get; private set; }

        public string Name { get; private set; }

        public string Suffix { get; private set; }

        private IEnumerable<uint> GetEnchantmentIds()
        {
            yield return _record.E1;
            yield return _record.E2;
            yield return _record.E3;
            yield return _record.E4;
            yield return _record.E5;
            yield break;
        }

        public List<SpellItemEnchantment> Enchantments
        {
            get
            {
                IEnumerable<SpellItemEnchantment> enumerable = GetEnchantmentIds().Select(SpellItemEnchantment.FromId);
                return enumerable.Where(spellItemEnchantment => spellItemEnchantment != null).ToList();
            }
        }

        public static ItemRandomProperties FromId(uint id)
        {
            return DbcCache<ItemRandomProperties>.GetOrAdd(ClientDb.ItemRandomProperties, id, row => new ItemRandomProperties(id, row));
        }

        private readonly ItemRandomPropertiesRecord _record;

        #region Nested type: ItemRandomPropertiesRecord

        public struct ItemRandomPropertiesRecord
        {
            public int Id;

            public IntPtr Name;

            public uint E1;

            public uint E2;

            public uint E3;

            public uint E4;

            public uint E5;

            public IntPtr Suffix;
        }

        #endregion
    }
}
