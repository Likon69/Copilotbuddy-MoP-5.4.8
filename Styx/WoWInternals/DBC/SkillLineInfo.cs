using System;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class SkillLineInfo
    {
        private SkillLineInfo(uint id, WoWDb.Row row)
        {
            ID = id;
            _entry = row.GetStruct<SkillLineEntry>();
            Name = row.GetStringField<SkillLineEntry>("Name");
            Description = row.GetStringField<SkillLineEntry>("Description");
            Verb = row.GetStringField<SkillLineEntry>("Verb");
        }

        public uint ID { get; private set; }

        public SkillLineCategory Category
        {
            get { return _entry.Category; }
        }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public int SpellIconId
        {
            get { return _entry.SpellIconId; }
        }

        public string Verb { get; private set; }

        public bool CanLink
        {
            get { return _entry.CanLink == 1; }
        }

        public int ParentSkillLineId
        {
            get { return _entry.ParentSkillLineId; }
        }

        public uint Flags
        {
            get { return _entry.Flags; }
        }

        public static SkillLineInfo FromId(uint id)
        {
            return DbcCache<SkillLineInfo>.GetOrAdd(ClientDb.SkillLine, id, row => new SkillLineInfo(id, row));
        }

        public static SkillLineInfo FromId(SkillLine skillLine)
        {
            return SkillLineInfo.FromId((uint)skillLine);
        }

        private readonly SkillLineEntry _entry;

        #region Nested type: SkillLineEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SkillLineEntry
        {
            public uint ID;
            public SkillLineCategory Category;
            public IntPtr Name;
            public IntPtr Description;
            public int SpellIconId;
            public IntPtr Verb;
            public int CanLink;
            public int ParentSkillLineId;
            public uint Flags;
        }

        #endregion
    }
}
