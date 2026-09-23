using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;

namespace Bots.DungeonBuddy.Profiles;

[ProfileElement("DungeonBuddyProfile")]
public class Profile
{
	private Profile(XElement element, string path = null)
	{
		Element = element;
		Path = path;
	}

	private void Process(out ErrorCollection errors)
	{
		errors = new ErrorCollection();
		ProfileLoader.Process(this, Element, errors);
	}

	/// <summary>
	/// Loads a new <see cref="Profile"/> instance from a path.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="errors"></param>
	/// <exception cref="System.IO.FileNotFoundException">Thrown when there exists no file at <see cref="Path"/>.</exception>
	/// <exception cref="System.Xml.XmlException">Thrown if the XML file at <see cref="Path"/> is not valid XML.</exception>
	public static Profile Load(string path, out ErrorCollection errors)
	{
		if (!File.Exists(path))
			throw new FileNotFoundException("Path to profile element is not valid; file not found", path);

		XElement element = XElement.Load(path, LoadOptions.SetLineInfo);

		var profile = new Profile(element, path);
		profile.Process(out errors);
		return profile;
	}

	/// <summary>
	/// Loads a new <see cref="Profile"/> instance from an element.
	/// </summary>
	/// <param name="element"></param>
	/// <param name="errors"></param>
	/// <returns></returns>
	public static Profile Load(XElement element, out ErrorCollection errors)
	{
		var profile = new Profile(element);
		profile.Process(out errors);
		return profile;
	}

	/// <summary>
	/// The full path to the XML file, or null if it was not loaded from file.
	/// </summary>
	public string Path { get; private set; }
	/// <summary>
	/// The underlying element for this profile.
	/// </summary>
	public XElement Element { get; private set; }

	[ProfileElement("Name")]
	private string _name;
	/// <summary>The name of this profile.</summary>
	/// <value>The name.</value>
	public string Name { get { return _name; } }

	[ProfileElement("Blackspots")]
	private List<Handlers.Blackspot> _blackspots;
	/// <summary>The blackspots of this profile.</summary>
	/// <value>The blackspots.</value>
	public List<Styx.CommonBot.Profiles.Blackspot> Blackspots
	{
		get
		{
			return _blackspots != null && _blackspots.Any()
				? _blackspots.Select(blackspot => (Styx.CommonBot.Profiles.Blackspot)blackspot).ToList()
				: new List<Styx.CommonBot.Profiles.Blackspot>();
		}
	}

	[ProfileElement("PullBlackspots")]
	private List<PullBlackspot> _pullBlackspots;
	/// <summary>The pull blackspots of this profile.</summary>
	/// <value>The pull blackspots.</value>
	public List<PullBlackspot> PullBlackspots { get { return _pullBlackspots; } }

	[ProfileElement("DungeonId")]
	private uint _dungeonId;
	/// <summary>The dungeon id this profile can be used with</summary>
	/// <value>The dungeon id.</value>
	public uint DungeonId { get { return _dungeonId; } }

	[ProfileElement("BossEncounters")]
	private List<Boss> _encounters;
	/// <summary>All the encoutners in this dungeon.</summary>
	/// <value>The encounters.</value>
	public List<Boss> BossEncounters { get { return _encounters; } }

	[ElementAttribute("noNamespaceSchemaLocation")]
	private string _schemaLocation;

	[ElementAttribute("xsi")]
	private string _xsi;

	public static implicit operator Styx.CommonBot.Profiles.Profile(Profile profile)
	{
		return new Styx.CommonBot.Profiles.Profile(profile.Element, null);
	}
}
