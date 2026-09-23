using Styx;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Profiles.Handlers;

[ProfileElement("Hotspot")]
public struct Hotspot
{
	[ElementAttribute("x")]
	private float _x;

	[ElementAttribute("y")]
	private float _y;

	[ElementAttribute("z")]
	private float _z;

	public Vector3 Location { get { return new Vector3(_x, _y, _z); } }

	public static implicit operator WoWPoint(Hotspot hotspot)
	{
		return new WoWPoint(hotspot.Location.X, hotspot.Location.Y, hotspot.Location.Z);
	}
}
