using System;
using System.Runtime.InteropServices;

namespace Tripper.RecastManaged.Detour
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PolygonReference
	{
		public PolygonReference(uint id)
		{
			this.Id = id;
		}

		public override string ToString()
		{
			uint id = this.Id;
			return id.ToString();
		}

		public uint Id;
	}
}
