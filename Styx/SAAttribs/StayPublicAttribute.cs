using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
	public sealed class StayPublicAttribute : Attribute
	{
	}
}
