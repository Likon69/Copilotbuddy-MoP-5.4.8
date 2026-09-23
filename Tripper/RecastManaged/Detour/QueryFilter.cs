using System;

namespace Tripper.RecastManaged.Detour
{
    public class QueryFilter : IDisposable
    {
        internal const int DT_MAX_AREAS = 64;

        private readonly float[] _areaCost = new float[DT_MAX_AREAS];
        private ushort _includeFlags;
        private ushort _excludeFlags;

        internal uint Version { get; private set; }

        public QueryFilter(ushort includeFlags, ushort excludeFlags)
        {
            for (int i = 0; i < DT_MAX_AREAS; i++)
                _areaCost[i] = 1f;

            IncludeFlags = includeFlags;
            ExcludeFlags = excludeFlags;
        }

        public QueryFilter()
        {
            for (int i = 0; i < DT_MAX_AREAS; i++)
                _areaCost[i] = 1f;

            IncludeFlags = 0xffff;
            ExcludeFlags = 0;
        }

        public ushort IncludeFlags
        {
            get { return _includeFlags; }
            set { _includeFlags = value; Version++; }
        }

        public ushort ExcludeFlags
        {
            get { return _excludeFlags; }
            set { _excludeFlags = value; Version++; }
        }

        public float GetAreaCost(byte areaId)
        {
            return _areaCost[areaId];
        }

        public void SetAreaCost(byte areaId, float cost)
        {
            if (_areaCost[areaId] == cost)
                return;

            _areaCost[areaId] = cost;
            Version++;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public static readonly QueryFilter Default = new QueryFilter();
    }
}
