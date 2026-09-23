using System.ComponentModel;
using Styx;
using Styx.CommonBot.Profiles;

namespace Bots.DungeonBuddy.Profiles.Handlers;

[ProfileElement("Blackspot")]
public struct Blackspot
{
	[ElementAttribute("x")]
	public float X;

	[ElementAttribute("y")]
	public float Y;

	[ElementAttribute("z")]
	public float Z;

	[ElementAttribute("radius")]
	public float Radius;

	[ElementAttribute("height")]
	public float Height;

	[DefaultValue("")]
	[ElementAttribute("name")]
	public string Name;

	[ElementAttribute("mapID")]
	[DefaultValue(-1)]
	public int MapID;

	public WoWPoint Location => new WoWPoint(X, Y, Z);

	public static implicit operator Styx.CommonBot.Profiles.Blackspot(Blackspot bs)
	{
		return new Styx.CommonBot.Profiles.Blackspot(bs.Location, bs.Radius, bs.Height, bs.Name, bs.MapID);
	}
}
