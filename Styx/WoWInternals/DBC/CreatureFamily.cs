using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class CreatureFamily
    {
        private CreatureFamily(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<CreatureFamilyEntry>();
            Name = row.GetStringField<CreatureFamilyEntry>("Name");
        }

        public bool IsValid
        {
            get { return true; }
        }

        public uint Id
        {
            get { return _entry.Id; }
        }

        public float MinScale
        {
            get { return _entry.MinScale; }
        }

        public int MinScaleLevel
        {
            get { return _entry.minScaleLevel; }
        }

        public float MaxScale
        {
            get { return _entry.MaxScale; }
        }

        public int MaxScaleLevel
        {
            get { return _entry.maxScaleLevel; }
        }

        public PetFoodFlags Diet
        {
            get { return _entry.PetFoodMask; }
        }

        public int PetTalentType
        {
            get { return _entry.PetTalentType; }
        }

        public SkillLine[] SkillLine
        {
            get { return _entry.SkillLine; }
        }

        public string Name { get; private set; }

        public override string ToString()
        {
            return string.Format("[{0}] Id:{1} MinScale:{2} MinScaleLevel:{3} MaxScale:{4} MaxScaleLevel:{5} SkillLine:{6} Diet:{7} PetTalentType:{8}",
                Name,
                Id,
                MinScale,
                MinScaleLevel,
                MaxScale,
                MaxScaleLevel,
                SkillLine.Aggregate(string.Empty, (current, skillLine) => current + skillLine + " |"),
                Diet,
                PetTalentType);
        }

        public static CreatureFamily FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.CreatureFamily].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new CreatureFamily(id, row);
        }

        private readonly CreatureFamilyEntry _entry;

        #region Nested type: CreatureFamilyEntry

        public struct CreatureFamilyEntry
        {
            public readonly uint Id;
            public readonly float MinScale;
            public readonly int minScaleLevel;
            public readonly float MaxScale;
            public readonly int maxScaleLevel;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U2)]
            public readonly SkillLine[] SkillLine;

            public readonly PetFoodFlags PetFoodMask;
            public readonly int PetTalentType;
            private readonly int CategoryEnumId;
            public readonly IntPtr Name;
        }

        #endregion
    }
}
