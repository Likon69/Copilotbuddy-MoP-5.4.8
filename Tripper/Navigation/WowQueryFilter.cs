using System;
using Tripper.MeshMisc;
using Tripper.RecastManaged.Detour;

namespace Tripper.Navigation
{
	/// <summary>
	/// Strict 1:1 API port of Honorbuddy 5.4.8's <c>Tripper.Navigation.WowQueryFilter</c>
	/// (see "hb decompile\.hb 5.4.8\Honorbuddy\Tripper\Navigation\WowQueryFilter.cs",
	/// names recovered against the unobfuscated
	/// "hb decompile\.hb 5.3\Tripper\Navigation\WowQueryFilter.cs").
	///
	/// This class needs no rewiring of its own: it is a thin wrapper around
	/// <see cref="QueryFilter"/>, and all of the Navigation.dll rewiring already lives in
	/// that class (see Tripper/RecastManaged/Detour/QueryFilter.cs). Every member here
	/// forwards to <see cref="InternalFilter"/> exactly as HB's original did against its
	/// native <c>dtQueryFilter*</c>-backed QueryFilter.
	/// </summary>
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

		// Unused: present in Honorbuddy 5.4.8's ILSpy decompile as a [CompilerGenerated]
		// backing field (Token 0x0400513F) that the decompiled body never actually reads
		// or writes (InternalFilter is a normal auto-property backed by its own hidden
		// field). Kept here, minus the misleading [CompilerGenerated] attribute (nothing
		// in this hand-written file actually generates it), purely for 1:1 member-count
		// fidelity with the decompile per the porting brief.
		private QueryFilter queryFilter_0;
	}
}
