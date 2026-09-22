using System;

namespace Tripper.RecastManaged.Detour
{
	[Flags]
	public enum StraightPathFlags : byte
	{
		None = 0,
		Start = 1,
		End = 2,
		OffmeshConnection = 4
	}
}
