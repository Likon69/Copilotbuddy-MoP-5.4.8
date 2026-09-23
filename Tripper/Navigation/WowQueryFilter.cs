using System;
using Tripper.MeshMisc;
using Tripper.RecastManaged.Detour;

namespace Tripper.Navigation
{
	public class WowQueryFilter : IDisposable
	{
		public WowQueryFilter()
		{
			this.InternalFilter = new QueryFilter();
		}

		public WowQueryFilter(QueryFilter qf)
		{
			this.InternalFilter = qf;
		}

		public WowQueryFilter(Tripper.MeshMisc.AbilityFlags includeFlags, Tripper.MeshMisc.AbilityFlags excludeFlags)
			: this()
		{
			this.IncludeFlags = includeFlags;
			this.ExcludeFlags = excludeFlags;
		}

		public QueryFilter InternalFilter { get; private set; }

		public Tripper.MeshMisc.AbilityFlags IncludeFlags
		{
			get
			{
				return (Tripper.MeshMisc.AbilityFlags)this.InternalFilter.IncludeFlags;
			}
			set
			{
				this.InternalFilter.IncludeFlags = (ushort)value;
			}
		}

		public Tripper.MeshMisc.AbilityFlags ExcludeFlags
		{
			get
			{
				return (Tripper.MeshMisc.AbilityFlags)this.InternalFilter.ExcludeFlags;
			}
			set
			{
				this.InternalFilter.ExcludeFlags = (ushort)value;
			}
		}

		public void SetAreaCost(Tripper.MeshMisc.AreaType area, float cost)
		{
			if (cost < 1f)
			{
				throw new ArgumentOutOfRangeException("cost", "Cost must be above or equal to 1");
			}
			this.InternalFilter.SetAreaCost((byte)area, cost);
		}

		public float GetAreaCost(Tripper.MeshMisc.AreaType area)
		{
			return this.InternalFilter.GetAreaCost((byte)area);
		}

		public void Dispose()
		{
			this.InternalFilter.Dispose();
		}

		private QueryFilter queryFilter_0;
	}
}
