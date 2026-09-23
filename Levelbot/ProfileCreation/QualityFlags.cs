using System;

namespace Levelbot.ProfileCreation;

[Flags]
public enum QualityFlags : byte
{
	None = 0,
	Grey = 1,
	White = 2,
	Green = 4,
	Blue = 8,
	Purple = 0x10
}
