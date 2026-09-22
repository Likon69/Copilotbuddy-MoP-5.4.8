using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
	public sealed class DoNotPruneTypeAttribute : Attribute
	{
	}
}
