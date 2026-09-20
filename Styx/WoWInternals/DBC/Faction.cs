using System;
using Styx.Patchables;
using Styx.Resources;

namespace Styx.WoWInternals.DBC
{
    public class Faction
    {
        private Faction(uint id, WoWDb.Row row)
        {
            Id = id;
            Record = row.GetStruct<FactionDbcRecord>();
            Name = row.GetStringField<FactionDbcRecord>("Name");
            Description = row.GetStringField<FactionDbcRecord>("Description");
        }

        public FactionDbcRecord Record { get; private set; }

        public uint Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public Faction ParentFaction
        {
            get
            {
                if (!_parentFactionInitialized)
                {
                    _parentFaction = Faction.FromId(Record.ParentFactionId);
                    _parentFactionInitialized = true;
                }
                return _parentFaction;
            }
        }

        public override string ToString()
        {
            return string.Format(StyxResources.FactionStringPrintOut, Name, ParentFaction);
        }

        public static Faction FromId(uint id)
        {
            return DbcCache<Faction>.GetOrAdd(ClientDb.Faction, id, row => new Faction(id, row));
        }

        private Faction _parentFaction;

        private bool _parentFactionInitialized;

        #region Nested type: FactionDbcRecord

        public struct FactionDbcRecord
        {
            public int Id;
            public int RepGainId;
            public int Allied;
            public int AtWar;
            private readonly int _unk0;
            private readonly int _unk1;
            private readonly int _unk2;
            private readonly int _unk3;
            private readonly int _unk4;
            private readonly int _unk5;
            public int Reputation;
            public int Mod1;
            public int Mod2;
            public int Mod3;
            private readonly int _unk6;
            private readonly int _unk7;
            private readonly int _unk8;
            private readonly int _unk9;
            public uint ParentFactionId;
            private readonly int _unk10;
            private readonly int _unk11;
            private readonly int _unk12;
            private readonly int _unk13;
            public IntPtr Name;
            public IntPtr Description;
        }

        #endregion
    }
}
