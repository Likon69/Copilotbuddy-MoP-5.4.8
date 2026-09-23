using System;
using System.Linq;

namespace Bots.Professionbuddy
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class PBXmlAttributeAttribute : Attribute
	{
		public PBXmlAttributeAttribute() : this(null) {}

		public PBXmlAttributeAttribute(string name, string[] aliases = null)
		{
			Name = name;
			Aliases = aliases ?? new string[0];
		}

		public string Name { get; private set; }
		private string[] Aliases { get; set; }

		/// <summary>Returns true if <paramref name="name" /> matches <see cref="Name" /> or a name inside
		///     <see cref="Aliases" />
		/// </summary>
		/// <param name="name">The name.</param>
		public bool Matches(string name)
		{
			return Name == name || Aliases.Contains(name);
		}
	}
}
