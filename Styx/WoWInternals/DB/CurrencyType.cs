using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Styx.Patchables;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals.DB
{
    public class CurrencyType
    {
        private readonly CurrencyTypeRecord _record;

        private CurrencyType(WoWDb.Row row)
        {
            _record = row.GetStruct<CurrencyTypeRecord>();
            Name = row.GetStringField<CurrencyTypeRecord>("Name");
            Description = row.GetStringField<CurrencyTypeRecord>("Description");
            var icons = new List<string>();
            var icon = row.GetStringField<CurrencyTypeRecord>("InventoryIcon1");
            if (!string.IsNullOrEmpty(icon)) icons.Add(icon);
            icon = row.GetStringField<CurrencyTypeRecord>("InventoryIcon2");
            if (!string.IsNullOrEmpty(icon)) icons.Add(icon);
            InventoryIcons = icons.AsReadOnly();
        }

        public uint CategoryId => _record.CategoryId;
        public string Description { get; private set; }
        public uint Flags => _record.Flags;
        public uint Id => _record.Id;
        public ReadOnlyCollection<string> InventoryIcons { get; private set; }
        public int MaxEarnablePerWeek => _record.MaxEarnablePerWeek;
        public int MaxQuantity => _record.MaxQuantity;
        public string Name { get; private set; }
        public WoWItemQuality Quality => (WoWItemQuality)_record.Quality;
        public int SpellCategory => _record.SpellCategory;
        public int SpellWeight => _record.SpellWeight;

        public static CurrencyType FromId(uint id)
        {
            return DbcCache<CurrencyType>.GetOrAdd(ClientDb.CurrencyTypes, id, row => new CurrencyType(row));
        }

        public override string ToString()
        {
            return string.Format("[{0}] CategoryId: {1}, Description: {2}, Flags: {3}, Id: {4}, InventoryIcons: ({5}), MaxEarnablePerWeek: {6}, MaxQuantity: {7}, Quality: {8}, SpellCategory: {9}, SpellWeight: {10}",
                Name, CategoryId, Description, Flags, Id, string.Join(", ", InventoryIcons), MaxEarnablePerWeek,
                MaxQuantity, Quality, SpellCategory, SpellWeight);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct CurrencyTypeRecord
        {
            internal readonly uint Id;
            internal readonly uint CategoryId;
            private readonly IntPtr Name;
            private readonly IntPtr InventoryIcon1;
            private readonly IntPtr InventoryIcon2;
            internal readonly int SpellWeight;
            internal readonly int SpellCategory;
            internal readonly int MaxQuantity;
            internal readonly int MaxEarnablePerWeek;
            internal readonly uint Flags;
            internal readonly int Quality;
            private readonly IntPtr Description;
        }
    }
}
