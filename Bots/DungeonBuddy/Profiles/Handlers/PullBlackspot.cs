using Styx;

namespace Bots.DungeonBuddy.Profiles.Handlers;

[ProfileElement("PullBlackspot")]
public struct PullBlackspot
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

	public WoWPoint Location => new WoWPoint(X, Y, Z);
}
