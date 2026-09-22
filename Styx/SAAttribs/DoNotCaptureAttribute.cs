using System;

namespace Styx.SAAttribs
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field, Inherited = true)]
	public sealed class DoNotCaptureAttribute : Attribute
	{
	}
}
