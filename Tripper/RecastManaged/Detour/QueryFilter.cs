using System;

namespace Tripper.RecastManaged.Detour
{
    /// <summary>
    /// 1:1 API port of Honorbuddy 5.4.8's <c>Tripper.RecastManaged.Detour.QueryFilter</c>.
    ///
    /// In Honorbuddy this class was a thin managed wrapper around a heap-allocated
    /// native <c>dtQueryFilter*</c> (see Tripper.RecastManaged\Detour\QueryFilter.cs in
    /// the HB 6.2.3 decompile): the constructors called <c>new</c> + the C++ ctor on the
    /// unmanaged heap, and every member forwarded straight through <c>&lt;Module&gt;.dtQueryFilter.*</c>
    /// native calls against that pointer.
    ///
    /// Navigation.dll has no equivalent per-instance native filter object — filter state
    /// is pushed into the DLL at call time by <see cref="NavMeshQuery"/>/<see cref="NavMesh"/>
    /// (see the "Filter scope" note there). So this class is reproduced here as pure
    /// managed state: an include/exclude flag pair plus the DT_MAX_AREAS-sized area cost
    /// table that <c>dtQueryFilter</c> keeps internally. There is no unmanaged object to
    /// allocate, free, or expose a pointer to.
    /// </summary>
    public class QueryFilter : IDisposable
    {
        // Detour's DT_MAX_AREAS, confirmed at
        // "Navigation C++/Navigation/Detour/Include/DetourNavMesh.h:95":
        //   static const int DT_MAX_AREAS = 64;
        // dtQueryFilter keeps exactly this many area costs (float m_areaCost[DT_MAX_AREAS]).
        internal const int DT_MAX_AREAS = 64;

        private readonly float[] _areaCost = new float[DT_MAX_AREAS];
        private ushort _includeFlags;
        private ushort _excludeFlags;

        /// <summary>
        /// Bumped on every mutation. Honorbuddy handed Detour a native filter pointer per
        /// call, so re-sending state cost nothing; Navigation.dll instead has to be told the
        /// filter one setter at a time. NavMeshQuery/NavMesh use this counter to skip that
        /// push when nothing has changed since the last query — without it every single
        /// query would spend 66 P/Invoke calls restating an identical filter.
        /// </summary>
        internal uint Version { get; private set; }

        /// <summary>
        /// Matches HB's <c>QueryFilter(ushort, ushort)</c>: the native ctor first ran
        /// dtQueryFilter's default constructor (all area costs = 1.0f), then HB called
        /// setIncludeFlags/setExcludeFlags with the given values.
        /// </summary>
        public QueryFilter(ushort includeFlags, ushort excludeFlags)
        {
            for (int i = 0; i < DT_MAX_AREAS; i++)
                _areaCost[i] = 1f;

            IncludeFlags = includeFlags;
            ExcludeFlags = excludeFlags;
        }

        /// <summary>
        /// Matches HB's parameterless <c>QueryFilter()</c>, which just ran dtQueryFilter's
        /// native default constructor and left its defaults untouched. Those defaults,
        /// confirmed in "Navigation C++/Navigation/Detour/Source/DetourNavMeshQuery.cpp:63-69"
        /// (<c>dtQueryFilter::dtQueryFilter() : m_includeFlags(0xffff), m_excludeFlags(0)</c>,
        /// every area cost set to 1.0f), are reproduced verbatim here.
        /// </summary>
        public QueryFilter()
        {
            for (int i = 0; i < DT_MAX_AREAS; i++)
                _areaCost[i] = 1f;

            IncludeFlags = 0xffff;
            ExcludeFlags = 0;
        }

        /// <summary>Backed by dtQueryFilter's m_includeFlags; wraps setIncludeFlags/getIncludeFlags.</summary>
        public ushort IncludeFlags
        {
            get { return _includeFlags; }
            set { _includeFlags = value; Version++; }
        }

        /// <summary>Backed by dtQueryFilter's m_excludeFlags; wraps setExcludeFlags/getExcludeFlags.</summary>
        public ushort ExcludeFlags
        {
            get { return _excludeFlags; }
            set { _excludeFlags = value; Version++; }
        }

        /// <summary>Wraps dtQueryFilter::getAreaCost(int).</summary>
        public float GetAreaCost(byte areaId)
        {
            return _areaCost[areaId];
        }

        /// <summary>Wraps dtQueryFilter::setAreaCost(int, float).</summary>
        public void SetAreaCost(byte areaId, float cost)
        {
            if (_areaCost[areaId] == cost)
                return;

            _areaCost[areaId] = cost;
            Version++;
        }

        /// <summary>
        /// HB's GetPointer() (returning the native <c>dtQueryFilter*</c>) is intentionally
        /// omitted: there is no such native object behind this class any more. Callers that
        /// need filter state to reach Navigation.dll go through NavMeshQuery/NavMesh, which
        /// push IncludeFlags/ExcludeFlags/area costs into the DLL immediately before each
        /// query (see their ApplyFilter helper).
        /// </summary>
        // (intentionally not implemented — see summary above)

        /// <summary>
        /// This class owns no unmanaged resource (see the class summary): Navigation.dll
        /// has no per-instance native filter to free, so there is nothing to release here.
        /// Kept only to preserve HB's IDisposable shape for callers using `using`/Dispose().
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>Matches HB's <c>static readonly QueryFilter Default</c>.</summary>
        public static readonly QueryFilter Default = new QueryFilter();
    }
}
