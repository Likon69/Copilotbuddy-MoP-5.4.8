using System;
using System.Linq;

namespace Bots.Professionbuddy
{
	/// <summary>Specifies the XML element name for a Professionbuddy component.</summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class PBXmlElementAttribute : Attribute
	{
		public PBXmlElementAttribute() : this(null) {}

		/// <summary>Initializes a new instance of the <see cref="PBXmlElementAttribute" /> class.</summary>
		/// <param name="name">The primary element name. This name will be used when saving to xml.</param>
		/// <param name="aliases">The aliases.</param>
		public PBXmlElementAttribute(string name, string[] aliases = null)
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
