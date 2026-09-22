using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method)]
	public sealed class ExcludeFromMemberRefsProxyAttribute : Attribute
	{
	}
}
