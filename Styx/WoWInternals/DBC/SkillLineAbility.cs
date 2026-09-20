using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class SkillLineAbility
    {
        private SkillLineAbility(SkillLineAbilityEntry entry)
        {
            _entry = entry;
        }

        private SkillLineAbility(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<SkillLineAbilityEntry>();
        }

        public uint Id
        {
            get { return _entry.Id; }
        }

        public SkillLine SkillLine
        {
            get { return _entry.SkillLine; }
        }

        public int SpellId
        {
            get { return _entry.SpellId; }
        }

        public uint ReqRaces
        {
            get { return _entry.ReqRaces; }
        }

        public uint ReqClasses
        {
            get { return _entry.ReqClasses; }
        }

        public int OrangeSkillLevel
        {
            get { return _entry.OrangeSkillLevel; }
        }

        public int NextSpellId
        {
            get { return _entry.NextSpellId; }
        }

        public RecipeAquireMethod AquireMethod
        {
            get { return _entry.AquireMethod; }
        }

        public int GreySkillLevel
        {
            get { return _entry.GreySkillLevel; }
        }

        public int GreenSkillLevel
        {
            get { return (_entry.YellowSkillLevel + _entry.GreySkillLevel) / 2; }
        }

        public int YellowSkillLevel
        {
            get { return _entry.YellowSkillLevel; }
        }

        public int SkillPointsEarned
        {
            get { return _entry.SkillPointsEarned; }
        }

        public int DisplayOrder
        {
            get { return _entry.DisplayOrder; }
        }

        public int TradeSkillCategoryId
        {
            get { return _entry.TradeSkillCategoryId; }
        }

        public override string ToString()
        {
            return string.Format("SpellId: {0}, SkillLine: {1}, NextSpellId: {2}, AquireMethod: {3}, OrangeSkillLevel: {4}, YellowSkillLevel: {5}, GreenSkillLevel: {6}, GreySkillLevel: {7}, SkillPointsEarned: {8}, DisplayOrder: {9}",
                SpellId, SkillLine, NextSpellId, AquireMethod, OrangeSkillLevel, YellowSkillLevel, GreenSkillLevel, GreySkillLevel, SkillPointsEarned, DisplayOrder);
        }

        public static SkillLineAbility FromId(uint id)
        {
            return DbcCache<SkillLineAbility>.GetOrAdd(ClientDb.SkillLineAbility, id, row => new SkillLineAbility(id, row));
        }

        public static IEnumerable<SkillLineAbility> FromSpellId(int spellId)
        {
            WoWDb.DbTable dbTable = StyxWoW.Db[ClientDb.SkillLineAbilitySortedSpell];
            int high = dbTable.NumRows;
            int low = 0;
            uint recordSize = dbTable.RecordSize;
            bool done = false;
            do
            {
                int mid = (high + low) / 2;
                IntPtr address = dbTable.FirstRow + (int)(mid * recordSize);
                SkillLineAbilitySortedSpellEntry entry = StyxWoW.Memory.Read<SkillLineAbilitySortedSpellEntry>(address);
                if (entry.SpellId == spellId)
                {
                    int scan = mid - 1;
                    while (mid > 0)
                    {
                        address = dbTable.FirstRow + (int)(scan * recordSize);
                        entry = StyxWoW.Memory.Read<SkillLineAbilitySortedSpellEntry>(address);
                        if (entry.SpellId != spellId)
                        {
                            break;
                        }
                        mid--;
                        scan--;
                    }
                    while (mid < dbTable.NumRows)
                    {
                        address = dbTable.FirstRow + (int)(mid * recordSize);
                        entry = StyxWoW.Memory.Read<SkillLineAbilitySortedSpellEntry>(address);
                        if (entry.SpellId != spellId)
                        {
                            done = true;
                            break;
                        }
                        yield return SkillLineAbility.FromId(entry.SkillLineAbilityId);
                        mid++;
                    }
                }
                else if (entry.SpellId > spellId)
                {
                    high = mid - 1;
                }
                else
                {
                    low = mid + 1;
                }
            }
            while (!done && low <= high);
        }

        public static IEnumerable<SkillLineAbility> GetAbilities()
        {
            foreach (WoWDb.Row row in StyxWoW.Db[ClientDb.SkillLineAbility])
            {
                yield return new SkillLineAbility(row.GetStruct<SkillLineAbilityEntry>());
            }
        }

        private readonly SkillLineAbilityEntry _entry;

        #region Nested type: SkillLineAbilityEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SkillLineAbilityEntry
        {
            public readonly uint Id;
            public readonly SkillLine SkillLine;
            public readonly int SpellId;
            public readonly uint ReqRaces;
            public readonly uint ReqClasses;
            public readonly int OrangeSkillLevel;
            public readonly int NextSpellId;
            public readonly RecipeAquireMethod AquireMethod;
            public readonly int GreySkillLevel;
            public readonly int YellowSkillLevel;
            public readonly int SkillPointsEarned;
            public readonly int DisplayOrder;
            public readonly int TradeSkillCategoryId;
        }

        #endregion

        #region Nested type: SkillLineAbilitySortedSpellEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SkillLineAbilitySortedSpellEntry
        {
            public readonly uint SkillLineAbilityId;
            public readonly uint SpellId;
        }

        #endregion
    }
}
