using System;
using JetBrains.Annotations;

namespace Bots.DungeonBuddy.Profiles;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ElementAttributeAttribute : Attribute
{
	private readonly string _attributeName;

	public ElementAttributeAttribute()
	{
	}

	public ElementAttributeAttribute(string attributeName)
	{
		_attributeName = attributeName;
	}

	public string AttributeName { get { return _attributeName; } }
}
