using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Bots.DungeonBuddy.Attributes;

/// <summary>
/// This attribute is for use with a property grid that needs to show a dynamic enumeration inside a combo box.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class DynamicStringListAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DynamicStringListAttribute"/> class.
	///  This attribute is for use with a property grid that needs to show a dynamic enumeration inside a combo box.
	/// </summary>
	/// <param name="typeName">Name of the type that contains method that returns a string array.</param>
	/// <param name="methodName">Name of the method. Method must take a parameter of type ITypeDescriptorContext and return a string array </param>
	public DynamicStringListAttribute(string typeName, string methodName)
	{
		var type = Assembly.GetExecutingAssembly().GetType(typeName);
		var methodI = type.GetMethod(methodName);
		var valueRetriever = (Func<ITypeDescriptorContext, ICollection>)Delegate.CreateDelegate(typeof(Func<ITypeDescriptorContext, ICollection>), methodI);
		ValueRetriever = valueRetriever;
	}

	public Func<ITypeDescriptorContext, ICollection> ValueRetriever { get; private set; }
}
