using System;
using Bots.Professionbuddy.ComponentBase;

namespace Bots.Professionbuddy.Dynamic
{
	public interface IDynamicallyCompiledCode
	{
		int CodeLineNumber { get; set; }
		string CompileError { get; set; }
		CsharpCodeType CodeType { get; }
		string Code { get; }
		Delegate CompiledMethod { get; set; }
		IPBComponent AttachedComponent { get; }
	}
}
