using System.Linq;
using Styx;

namespace Bots.ArchaeologyBuddy;

public class Fragment
{
	public int ID { get; private set; }
	public int DigsiteID { get; private set; }
	public string DigsiteName { get; private set; }
	public WoWPoint Location { get; private set; }
	public string Type { get; private set; }
	public bool OutDoors { get; private set; }
	public bool HigherZExists { get; private set; }
	public bool ShouldUseNav => !OutDoors;

	public Fragment(int id, int digsiteID, string digsiteName, WoWPoint location, string type, bool outDoors, bool higherZExists)
	{
		ID = id;
		DigsiteID = digsiteID;
		DigsiteName = digsiteName;
		Location = location;
		Type = type;
		OutDoors = outDoors;
		HigherZExists = higherZExists;
	}

	public static Fragment FromId(int id)
	{
		return ArchBuddy.Fragments.FirstOrDefault(fragment => fragment.ID == id);
	}

	public override string ToString()
	{
		return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", ID, DigsiteID, DigsiteName, Location, Type, OutDoors, HigherZExists);
	}
}
