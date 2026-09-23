using System;
using System.ComponentModel;
using System.Globalization;
using Bots.DungeonBuddy.Attributes;

namespace Bots.DungeonBuddy.Helpers;

internal class StringArrayConverter : ArrayConverter
{
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is string[])
		{
			return string.Join(",", value as string[]);
		}
		return "(All)";
	}
}

public class DynamicStringListConverter : TypeConverter
{
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
	{
		return true;
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		DynamicStringListAttribute dynamicStringListAttribute = (DynamicStringListAttribute)context.PropertyDescriptor.Attributes[typeof(DynamicStringListAttribute)];
		if (dynamicStringListAttribute == null)
		{
			throw new InvalidOperationException($"No DynamicStringBuilderAttribute is attached to property {context.PropertyDescriptor.Name}");
		}
		return new StandardValuesCollection(dynamicStringListAttribute.ValueRetriever(context));
	}
}
