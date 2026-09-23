using System;
using JetBrains.Annotations;

namespace Bots.DungeonBuddy.Profiles;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ProfileElementAttribute : Attribute
{
	public ProfileElementAttribute()
	{
	}

	private readonly string _elementName;

	public ProfileElementAttribute(string elementName)
	{
		_elementName = elementName;
	}

	public string ElementName { get { return _elementName; } }

	internal static string GetNameFromType(Type type)
	{
		object[] typeAttribs = type.GetCustomAttributes(typeof(ProfileElementAttribute), false);

		return typeAttribs.Length <= 0 ?
			null :
			((ProfileElementAttribute)typeAttribs[0]).ElementName;
	}
}
