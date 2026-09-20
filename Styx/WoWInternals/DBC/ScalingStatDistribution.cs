using System;
using System.Runtime.InteropServices;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class ScalingStatDistribution
    {
        private ScalingStatDistribution(uint id, WoWDb.Row row)
        {
            ID = id;
            _entry = row.GetStruct<ScalingStatDistributionEntry>();
        }

        public uint ID { get; private set; }

        public int MinLevel
        {
            get { return _entry.MinLevel; }
        }

        public int MaxLevel
        {
            get { return _entry.MaxLevel; }
        }

        public static ScalingStatDistribution FromId(uint id)
        {
            WoWDb.Row row = StyxWoW.Db[ClientDb.ScalingStatDistribution].GetRow(id);
            if (row == null)
            {
                return null;
            }
            return new ScalingStatDistribution(id, row);
        }

        private readonly ScalingStatDistributionEntry _entry;

        #region Nested type: ScalingStatDistributionEntry

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct ScalingStatDistributionEntry
        {
            public int Id;
            public fixed int StatID[10];
            public fixed int Bonus[10];
            public int MinLevel;
            public int MaxLevel;
        }

        #endregion
    }
}
