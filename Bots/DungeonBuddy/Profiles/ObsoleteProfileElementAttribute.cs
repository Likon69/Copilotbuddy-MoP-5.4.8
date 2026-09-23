using System;

namespace Bots.DungeonBuddy.Profiles;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class ObsoleteProfileElementAttribute : Attribute
{
	public ObsoleteProfileElementAttribute(string elementName, string message)
	{
		if (string.IsNullOrEmpty(elementName))
			throw new ArgumentException("Name can not be null or empty.");

		ElementName = elementName;
		Message = message;
	}

	public string ElementName { get; private set; }
	public string Message { get; private set; }
}
