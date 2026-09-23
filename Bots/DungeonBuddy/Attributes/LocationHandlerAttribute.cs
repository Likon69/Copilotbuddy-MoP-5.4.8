using System;
using Styx;

namespace Bots.DungeonBuddy.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class LocationHandlerAttribute : Attribute
{
	// note: optional parameters in attributes cause compiler errors in some .net 4 and lower installations.

	public LocationHandlerAttribute(double x, double y, double z) : this(x, y, z, 50, "") { }

	public LocationHandlerAttribute(double x, double y, double z, int radius) : this(x, y, z, radius, "") { }

	public LocationHandlerAttribute(double x, double y, double z, int radius, string locationDisplayName)
	{
		Location = new WoWPoint(x, y, z);
		LocationDisplayName = locationDisplayName;
		Radius = radius;
		RadiusSqr = radius * radius;
	}

	public LocationHandlerAttribute(float x, float y, float z) : this(x, y, z, 50, "") { }

	public LocationHandlerAttribute(float x, float y, float z, int radius) : this(x, y, z, radius, "") { }

	public LocationHandlerAttribute(float x, float y, float z, int radius, string locationDisplayName)
	{
		Location = new WoWPoint(x, y, z);
		LocationDisplayName = locationDisplayName;
		Radius = radius;
		RadiusSqr = radius * radius;
	}

	public WoWPoint Location { get; private set; }
	public int Radius { get; private set; }
	public string LocationDisplayName { get; private set; }

	public readonly float RadiusSqr;
}
