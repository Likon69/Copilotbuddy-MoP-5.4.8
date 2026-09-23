using System;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Components;

namespace Bots.Professionbuddy.Dynamic
{
    internal interface IDynamicProperty : IDynamicallyCompiledCode
    {
        PBAction AttachedComposite { get; set; }

		string Name { get;  }

		Type ReturnType { get; }

		object Value { get; }
	}
}