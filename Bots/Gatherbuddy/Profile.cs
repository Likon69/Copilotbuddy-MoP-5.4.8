using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bots.GatherBuddy.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals.World;

namespace Bots.Gatherbuddy;

public class Profile
{
	private static bool _firstRun = true;
	private static WoWPoint _cachedPeek;
	private static readonly Random Random = new Random();

	public static Profile CurrentProfile { get; set; }
	public List<WoWPoint> Waypoints { get; private set; }
	public CircularQueue<WoWPoint> Path { get; private set; }

	public WoWPoint Current
	{
		get
		{
			WoWPoint waypoint = Path.Peek();
			if (_cachedPeek == WoWPoint.Empty && GatherbuddySettings.Instance.RandomizeHotspots && waypoint.DistanceSqr(StyxWoW.Me.Location) < 40000f)
			{
				_cachedPeek = RandomizeHotspot(waypoint);
			}
			if (_cachedPeek != WoWPoint.Empty)
			{
				return _cachedPeek;
			}
			return waypoint;
		}
	}

	public List<Blackspot> Blackspots { get; private set; }

	internal Profile()
	{
		Blackspots = new List<Blackspot>();
		if (ProfileManager.CurrentProfile == null)
		{
			return;
		}
		foreach (Blackspot blackspot in ProfileManager.CurrentProfile.Blackspots)
		{
			Blackspots.Add(blackspot);
		}
		Waypoints = new List<WoWPoint>();
		if (ProfileManager.CurrentProfile.GrindArea.Hotspots != null)
		{
			foreach (Hotspot hotspot in ProfileManager.CurrentProfile.GrindArea.Hotspots)
			{
				Waypoints.Add(hotspot.ToWoWPoint().Add(0f, 0f, GatherbuddySettings.Instance.HeightModifier));
			}
		}
		else
		{
			if (ProfileManager.CurrentProfile.HotspotManager.Hotspots == null)
			{
				throw new ProfileException(GatherBuddyResources.ProfileDoesntContainAnyHotspots);
			}
			foreach (WoWPoint hotspot in ProfileManager.CurrentProfile.HotspotManager.Hotspots)
			{
				Waypoints.Add(hotspot.Add(0f, 0f, GatherbuddySettings.Instance.HeightModifier));
			}
		}
		InitializePath();
	}

	internal Profile(Styx.CommonBot.Profiles.Profile profile)
	{
		Blackspots = new List<Blackspot>();
		foreach (Blackspot blackspot in ProfileManager.CurrentProfile.Blackspots)
		{
			Blackspots.Add(blackspot);
		}
		Waypoints = new List<WoWPoint>();
		foreach (WoWPoint hotspot in profile.HotspotManager.Hotspots)
		{
			Waypoints.Add(hotspot.Add(0f, 0f, GatherbuddySettings.Instance.HeightModifier));
		}
		InitializePath();
	}

	public void DequeueHotspot()
	{
		_cachedPeek = WoWPoint.Empty;
		Path.Dequeue();
	}

	private void InitializePath()
	{
		Path = new CircularQueue<WoWPoint>();
		foreach (WoWPoint waypoint in Waypoints)
		{
			Path.Enqueue(waypoint);
		}
		Path.OnEndOfQueue += PathOnEndOfQueue;
		Path.OnStartOfQueue += PathOnEndOfQueue;
		Path.CycleTo(Waypoints.OrderBy(waypoint => waypoint.DistanceSqr(StyxWoW.Me.Location)).First());
	}

	private static WoWPoint RandomizeHotspot(WoWPoint hotspot)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		using (StyxWoW.Memory.AcquireFrame())
		{
			while (stopwatch.ElapsedMilliseconds <= 1000L)
			{
				double distance = Random.NextDouble() * 20.0 * GatherbuddySettings.Instance.RandomizationFactor + 5.0 * GatherbuddySettings.Instance.RandomizationFactor;
				double height = Random.NextDouble() * 10.0 - 5.0;
				WoWPoint direction = GetRandomDirection(Random);
				WoWPoint point = hotspot + direction * (float)distance;
				point.Z += (float)height;
				if (GameWorld.IsInLineOfSight(hotspot, point))
				{
					return point;
				}
			}
			return hotspot;
		}
	}

	private static WoWPoint GetRandomDirection(Random random)
	{
		Vector3 vector = default(Vector3);
		vector.Z = 0f;
		do
		{
			vector.X = (float)((random.NextDouble() - 0.5) * 2.0);
			vector.Y = (float)((random.NextDouble() - 0.5) * 2.0);
		}
		while (vector.MagnitudeSqr > 1f);
		return vector;
	}

	private void PathOnEndOfQueue(object sender, EventArgs e)
	{
		if (_firstRun)
		{
			_firstRun = false;
		}
		else if (GatherbuddySettings.Instance.PathingType == PathType.Bounce)
		{
			Waypoints.Reverse();
			Path = new CircularQueue<WoWPoint>();
			foreach (WoWPoint waypoint in Waypoints)
			{
				Path.Enqueue(waypoint);
			}
		}
	}
}
