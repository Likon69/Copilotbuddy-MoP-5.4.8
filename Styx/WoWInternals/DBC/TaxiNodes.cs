using System;
using System.Runtime.InteropServices;
using System.Text;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class TaxiNodes
    {
        private TaxiNodes(uint id, WoWDb.Row row)
        {
            _entry = row.GetStruct<TaxiNodesRecord>();
            Name = row.GetStringField<TaxiNodesRecord>("Name");
        }

        public bool IsValid
        {
            get { return Id != 0; }
        }

        public int Id
        {
            get { return _entry.Id; }
        }

        public int MapId
        {
            get { return _entry.MapId; }
        }

        public WoWPoint Location
        {
            get { return _entry.Location; }
        }

        public string Name { get; private set; }

        public int HordeMountCreatureId
        {
            get { return _entry.HordeMountId; }
        }

        public int AllianceMountCreatureId
        {
            get { return _entry.AllianceMountId; }
        }

        public bool AllianceOnly
        {
            get { return HordeMountCreatureId == 0; }
        }

        public bool HordeOnly
        {
            get { return AllianceMountCreatureId == 0; }
        }

        public bool FlightPath
        {
            get { return !AllianceOnly || !HordeOnly; }
        }

        public static TaxiNodes FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.TaxiNodes].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new TaxiNodes(id, row);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("ID: {0}, Map: {1}, Name: {2}", Id, MapId, Name);
            if (Location == WoWPoint.Empty)
            {
                stringBuilder.Append(", Type: Transport");
            }
            else
            {
                stringBuilder.Append(", Type: FlightPath, Location: " + Location);
            }
            return stringBuilder.ToString();
        }

        private readonly TaxiNodesRecord _entry;

        #region Nested type: TaxiNodesRecord

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TaxiNodesRecord
        {
            public int Id;
            public int MapId;
            public WoWPoint Location;
            public IntPtr Name;
            public int HordeMountId;
            public int AllianceMountId;
        }

        #endregion
    }
}
