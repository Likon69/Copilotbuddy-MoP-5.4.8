using System;
using System.Runtime.InteropServices;

namespace Tripper.RecastManaged.Detour
{
	/// <summary>
	/// Deliberate deviation from Honorbuddy 5.4.8: HB's Id was <c>uint</c> because HB's
	/// Detour build used 32-bit dtPolyRef. Ours does not — "Navigation C++/Navigation/
	/// Navigation.vcxproj" defines DT_POLYREF64 for both Debug and Release, which makes
	/// "Navigation C++/Navigation/Detour/Include/DetourNavMesh.h"'s
	/// <c>typedef uint64_t dtPolyRef;</c> branch the live one (salt/tile/poly bit split
	/// 16/28/20), not the 32-bit <c>typedef unsigned int dtPolyRef;</c> fallback. Widening
	/// Id to <c>ulong</c> here keeps polygon references lossless end to end through
	/// Navigation.dll's ulong-based exports, at the cost of no longer matching HB's field
	/// width exactly.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PolygonReference
	{
		public PolygonReference(ulong id)
		{
			this.Id = id;
		}

		public override string ToString()
		{
			ulong id = this.Id;
			return id.ToString();
		}

		public ulong Id;
	}
}
