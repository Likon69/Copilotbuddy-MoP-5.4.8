using System;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class ResearchSite
    {
        private ResearchSite(uint id, WoWDb.Row row)
        {
            ID = id;
            _entry = row.GetStruct<ResearchSiteEntry>();
            Name = row.GetStringField<ResearchSiteEntry>("Name");
        }

        public uint ID { get; private set; }

        public uint QuestPoiBlobID
        {
            get { return _entry.QuestPoiBlobID; }
        }

        public string Name { get; private set; }

        public int TextureIndex
        {
            get { return _entry.TextureIndex; }
        }

        public static ResearchSite FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.ResearchSite].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new ResearchSite(id, row);
        }

        private readonly ResearchSiteEntry _entry;

        #region Nested type: ResearchSiteEntry

        private struct ResearchSiteEntry
        {
            public uint Entry;
            public uint dword4;
            public uint QuestPoiBlobID;
            public IntPtr Name;
            public int TextureIndex;
        }

        #endregion
    }
}
