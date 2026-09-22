using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class DoNotMoveMethodsAttribute : Attribute
	{
	}
}
