using System.Collections.Generic;
using System.Linq;
using Styx;

namespace Bots.ArchaeologyBuddy;

public class Digsite
{
	public int ID { get; private set; }
	public string Name { get; private set; }
	public WoWPoint Location { get; private set; }
	public uint ZoneId { get; private set; }
	public uint ContinentId { get; private set; }
	public string Type { get; private set; }
	public Dictionary<Fragment, bool> Fragments { get; set; }
	public static Digsite Empty => new Digsite(999, "", WoWPoint.Zero, 999u, 9u, "", new List<Fragment>());

	public Digsite(int id, string name, WoWPoint location, uint zoneId, uint continentId, string type, IEnumerable<Fragment> fragments)
	{
		ID = id;
		Name = name;
		Location = location;
		ZoneId = zoneId;
		ContinentId = continentId;
		Type = type;
		Fragments = fragments.ToDictionary(fragment => fragment, fragment => false);
	}

	public static Digsite FromId(int id)
	{
		return ArchBuddy.DigSites.FirstOrDefault(digsite => digsite.ID == id) ?? Empty;
	}

	public static Digsite FromName(string name)
	{
		return ArchBuddy.DigSites.FirstOrDefault(digsite => digsite.Name.Equals(name)) ?? Empty;
	}

	public override string ToString()
	{
		return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", ID, Name, Location, ZoneId, ContinentId, Type, Fragments);
	}
}
