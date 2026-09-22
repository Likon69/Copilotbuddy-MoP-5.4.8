using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Interface)]
	public sealed class ObfuscateToAttribute : Attribute
	{
		public ObfuscateToAttribute(string newName)
		{
		}
	}
}
