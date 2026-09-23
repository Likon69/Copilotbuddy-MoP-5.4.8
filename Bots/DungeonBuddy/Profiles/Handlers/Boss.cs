using System.Collections.Generic;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.Common;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System.Linq;
using Bots.DungeonBuddy.Enums;

namespace Bots.DungeonBuddy.Profiles.Handlers;

/// <summary>Represents a boss</summary>
[ProfileElement("Boss")]
public class Boss
{
	public Boss()
	{
		IsAlive = true;
	}

	private CircularQueue<WoWPoint> _pathBreadCrumbs;
	public CircularQueue<WoWPoint> PathBreadCrumbs
	{
		get
		{
			if (_pathBreadCrumbs == null)
			{
				_pathBreadCrumbs = new CircularQueue<WoWPoint>();
				// if Path is not empty then add the points that it contains to navigate to the boss
				if (Hotspots != null && Hotspots.Count > 0)
				{
					foreach (WoWPoint point in Hotspots)
						_pathBreadCrumbs.Add(point);
				}
				else // use boss location instead.
					_pathBreadCrumbs.Add(Location);
			}
			return _pathBreadCrumbs;
		}
	}

	public bool IsAlive { get; private set; }
	/// <summary>
	/// This Marks the boss as dead.
	/// </summary>
	public void MarkAsDead(string reason = null)
	{
		Logger.WriteDebug("Marking {0} as dead. {1}", Name, reason != null ? "Reason: " + reason : "");
		IsAlive = false;
	}

	public void Reset()
	{
		IsAlive = true;
	}

	public WoWUnit ToWoWUnit()
	{
		var bossEnty = Entry; // needed because 'this' can't be accessed in an anonymous method in a struct
		return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == bossEnty);
	}

	/// <summary>Represents a value indicating if this is the final boss.</summary>
	/// <value>The value.</value>
	[ElementAttribute("isFinal")]
	public bool IsFinal { get; private set; }

	/// <summary>The entry of this boss.</summary>
	/// <value>The entry.</value>
	[ElementAttribute("entry")]
	public uint Entry { get; private set; }

	/// <summary>the order in which this boss is killed. 1st boss to be killed starts at 1. Optional bosses should be included.</summary>
	/// <value>The entry.</value>
	[ElementAttribute("killOrder")]
	public int KillOrder { get; private set; }

	/// <summary>The name of this boss.</summary>
	/// <value>The name.</value>
	[ElementAttribute("name")]
	public string Name { get; private set; }

	/// <summary>True if the boss doesn't need to be killed to complete the dungeon</summary>
	/// <value>The name.</value>
	[ElementAttribute("optional")]
	public bool Optional { get; private set; }
	/// <summary>
	/// The faction that this boss is killable by e.g. Horde, Alliance, Both
	/// </summary>
	[ElementAttribute("faction")]
	public BossAvailableToFaction Faction { get; private set; }

	[ProfileElement("Path")]
	private List<Hotspot> _hotspots;
	/// <summary>The 'breadcrumb' points to follow when looking for a boss that paths</summary>
	/// <value>The waypoints</value>
	public List<Hotspot> Hotspots { get { return _hotspots; } }

	[ElementAttribute("x")]
	private float _x;
	[ElementAttribute("y")]
	private float _y;
	[ElementAttribute("z")]
	private float _z;
	/// <summary>
	/// Location of the boss.If Location and Path isn't set than boss is considered a random spawn
	/// </summary>
	public WoWPoint Location { get { return new WoWPoint(_x, _y, _z); } }
}
