using System;
using System.Collections.Generic;
using System.Linq;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidanceManager
{
	private static string _lastAvoidDescription;

	public static readonly List<AvoidInfo> AvoidInfos;

	public static readonly List<Avoid> Avoids;

	public static readonly List<AvoidCluster> AvoidClusters;

	public static bool IsRunningOutOfAvoid { get; private set; }

	static AvoidanceManager()
	{
		AvoidInfos = new List<AvoidInfo>();
		Avoids = new List<Avoid>();
		AvoidClusters = new List<AvoidCluster>();
		AvoidInfos = new List<AvoidInfo>();
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, float radius, params uint[] unitIds)
	{
		AddAvoidObject(canRun, null, 40f, obj => radius, unitIds);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWObject, float> radiusSelector, params uint[] unitIds)
	{
		AddAvoidObject(canRun, null, 40f, radiusSelector, unitIds);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, float radius, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, null, 40f, obj => radius, objectSelector, locationSelector, ignoreIfBlocking);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWObject, float> radiusSelector, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, null, 40f, radiusSelector, objectSelector, locationSelector, ignoreIfBlocking);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, float radius, params uint[] unitIds)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, obj => radius, obj => unitIds.Contains(obj.Entry));
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, Func<WoWObject, float> radiusSelector, params uint[] unitIds)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, radiusSelector, obj => unitIds.Contains(obj.Entry));
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, float radius, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, obj => radius, objectSelector, locationSelector, ignoreIfBlocking);
	}

	/// <summary>
	///   Runs away from a unit or object if within range.
	/// </summary>
	/// <param name="canRun"> The 'can run' condition. </param>
	/// <param name="leashPointSelector"> The leash center point selector. Unit location if null </param>
	/// <param name="leashRadius"> The max distance to run from leash. </param>
	/// <param name="radiusSelector"> The distance to avoid </param>
	/// <param name="objectSelector"> The object selector. </param>
	/// <param name="locationSelector"> The location selector </param>
	/// <param name="ignoreIfBlocking"> If set to true the object will be ignored if no path can be generated around it </param>
	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, Func<WoWObject, float> radiusSelector, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AvoidObjectInfo avoidInfo = new AvoidObjectInfo(canRun, objectSelector, radiusSelector, leashPointSelector, leashRadius, locationSelector, ignoreIfBlocking);
		AddAvoid(avoidInfo);
	}

	protected void AddAvoidLocation(CanRunDecoratorDelegate canRun, float radius, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, null, 40f, obj => radius, locationSelector, collectionSelector, ignoreIfBlocking);
	}

	/// <summary>
	///   Run away from location.
	/// </summary>
	/// <param name="canRun"> The condition. </param>
	/// <param name="radiusSelector"> The max distance to run. </param>
	/// <param name="locationSelector"> The location selector. </param>
	/// <param name="collectionSelector"> Optional collection of objects that are passed as args to LocationSelector </param>
	/// <param name="ignoreIfBlocking"> If set to true the object will be ignored if no path can be generated around it (default: false) </param>
	protected void AddAvoidLocation(CanRunDecoratorDelegate canRun, Func<object, float> radiusSelector, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, null, 40f, radiusSelector, locationSelector, collectionSelector, ignoreIfBlocking);
	}

	protected void AddAvoidLocation(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, float radius, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, leashPointSelector, leashRadius, obj => radius, locationSelector, collectionSelector, ignoreIfBlocking);
	}

	protected void AddAvoidLocation(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, Func<object, float> radiusSelector, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AvoidLocationInfo avoidInfo = new AvoidLocationInfo(canRun, locationSelector, radiusSelector, leashPointSelector, leashRadius, collectionSelector, ignoreIfBlocking);
		AddAvoid(avoidInfo);
	}

	public static void AddAvoid(AvoidInfo avoid)
	{
		AvoidInfos.Add(avoid);
	}

	public static void AddAvoidRange(IEnumerable<AvoidInfo> avoids)
	{
		AvoidInfos.AddRange(avoids);
	}

	public static void RemoveAvoid(AvoidInfo avoid)
	{
		AvoidInfos.Remove(avoid);
	}

	public static void RemoveAllAvoids(Predicate<AvoidInfo> match)
	{
		AvoidInfos.RemoveAll(match);
	}

	public static void ClearAvoids()
	{
		AvoidInfos.Clear();
	}

	internal static void Pulse()
	{
		bool changed = Avoids.RemoveAll(avoid =>
		{
			if (!avoid.IsValid)
			{
				return true;
			}
			return avoid is AvoidLocation && avoid.AvoidInfo is AvoidLocationInfo avoidLocationInfo && avoidLocationInfo.CollectionSelector != AvoidLocationInfo.DefaultCollectionSelector;
		}) > 0;
		foreach (WoWObject obj in ObjectManager.ObjectList.Where(o => !(o is WoWItem)))
		{
			foreach (AvoidObjectInfo avoidObjectInfo in AvoidInfos.OfType<AvoidObjectInfo>())
			{
				if (avoidObjectInfo.ObjectSelector(obj) && avoidObjectInfo.CanRun(obj) && !Avoids.Any(a => a.AvoidInfo == avoidObjectInfo && ((AvoidObject)a).Object == obj))
				{
					if (!changed)
					{
						changed = true;
					}
					Avoids.Add(new AvoidObject(avoidObjectInfo, obj));
				}
			}
		}
		foreach (AvoidLocationInfo avoidLocationInfo in AvoidInfos.OfType<AvoidLocationInfo>())
		{
			foreach (object content in avoidLocationInfo.CollectionSelector())
			{
				if (avoidLocationInfo.CanRun(null) && (avoidLocationInfo.CollectionSelector != AvoidLocationInfo.DefaultCollectionSelector || Avoids.All(a => a.AvoidInfo != avoidLocationInfo)))
				{
					if (!changed)
					{
						changed = true;
					}
					Avoids.Add(new AvoidLocation(avoidLocationInfo, content));
				}
			}
		}
		foreach (Avoid avoid2 in Avoids)
		{
			WoWPoint location = avoid2.Location;
			float radius = avoid2.Radius;
			avoid2.UpdateLocationAndRadius();
			if ((location != avoid2.Location || radius != avoid2.Radius) && !changed)
			{
				changed = true;
			}
		}
		if (changed)
		{
			Helpers.BuildClusters(Avoids, AvoidClusters);
		}
		WoWPoint myLoc = StyxWoW.Me.Location;
		AvoidCluster clusterImStandingIn = AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(myLoc)));
		if (clusterImStandingIn != null)
		{
			MoveResult moveResult = Helpers.MoveOutOfAvoidCluster(clusterImStandingIn);
			IsRunningOutOfAvoid = moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
			if (IsRunningOutOfAvoid)
			{
				Avoid avoid = clusterImStandingIn.First();
				_lastAvoidDescription = Helpers.DescribeAvoid(avoid);
				if (avoid is AvoidLocation)
				{
					_lastAvoidDescription = "location: " + _lastAvoidDescription;
				}
				Logger.WriteDebug("Running from {0}", _lastAvoidDescription);
			}
		}
		else if (IsRunningOutOfAvoid)
		{
			Logger.WriteDebug("Done running from {0}", _lastAvoidDescription);
			IsRunningOutOfAvoid = false;
		}
	}
}
