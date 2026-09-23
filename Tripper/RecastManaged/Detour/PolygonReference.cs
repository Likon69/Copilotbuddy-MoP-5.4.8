using System;
using System.Runtime.InteropServices;

namespace Tripper.RecastManaged.Detour
{
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
