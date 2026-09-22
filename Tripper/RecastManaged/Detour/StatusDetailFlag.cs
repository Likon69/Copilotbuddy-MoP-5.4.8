using System;

namespace Tripper.RecastManaged.Detour
{
	[Flags]
	public enum StatusDetailFlag : uint
	{
		Failure = 2147483648U,
		Success = 1073741824U,
		InProgress = 536870912U,
		WrongMagic = 1U,
		WrongVersion = 2U,
		OutOfMemory = 4U,
		InvalidParam = 8U,
		BufferTooSmall = 16U,
		OutOfNodes = 32U,
		PartialResult = 64U
	}
}
