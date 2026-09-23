using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bots.DungeonBuddy;
using Bots.DungeonBuddy.Helpers;
using Styx;
using Styx.Common;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.DungeonBuddy.Avoidance;

public static class Helpers
{
	private const float PIx2 = (float)Math.PI * 2;
	private const float PI = (float)Math.PI;
	// path around avoid is generated at avoid radius + Padding from avoid center.
	internal const float Padding = 1f;
	internal const float PaddingSqr = Padding * Padding;
	// SubPadding + radius distance from center of avoid is avoided.
	internal const float SubPadding = Padding / 2;
	internal const float SubPaddingSqr = SubPadding * SubPadding;
	internal const int MaxRecursionLevel = 80;

	private static readonly Dictionary<Avoid, WoWPoint> PrevAvoids = new Dictionary<Avoid, WoWPoint>();
	private static AvoidPathResult _currentAvoidPath;
	private static WoWPoint _prevDestination;

	private static MeshNavigator MeshNavigator => (MeshNavigator)Navigator.NavigationProvider;

	/// <summary>
	///   Sets to nearest Z.
	/// </summary>
	/// <param name="point"> The point. </param>
	/// <returns> </returns>
	internal static Vector3 SetToNearestZ(Vector3 point)
	{
		try
		{
			var zlist = Navigator.FindHeights(point.X, point.Y);
			if (zlist != null && zlist.Count > 0)
			{
				point.Z = zlist.OrderBy(z => Math.Abs(z - point.Z)).FirstOrDefault();
			}
		}
		catch { }
		return point;
	}

	/// <summary>
	///   Returns a path around avoids or null if none is needed
	/// </summary>
	/// <param name="destination"> </param>
	/// <returns> </returns>
	public static AvoidPathResult GetAvoidPath(WoWPoint destination)
	{
		if (!StyxWoW.Me.IsAlive)
			return null;

		var myLoc = StyxWoW.Me.Location;

		var activeAvoids = AvoidanceManager.Avoids;

		var clusterAtFrom = AvoidanceManager.AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(myLoc)));

		// if we're standing in an avoidcluster then we neeed to get out.
		if (clusterAtFrom != null)
		{
			var currentTarget = StyxWoW.Me.CurrentTarget;
			var goalLoc = currentTarget != null ? currentTarget.Location : WoWPoint.Zero;

			var moveTo = GetBestLocationOutsideCluster(myLoc, goalLoc, 2, clusterAtFrom, 1.5f);
			if (!moveTo.Equals(Vector3.Zero))
			{
				if (_currentAvoidPath == null || _currentAvoidPath.Path == null || _currentAvoidPath.Path.Last().DistanceSqr(moveTo) > SubPaddingSqr)
				{
					_currentAvoidPath = new AvoidPathResult(PathResult.Changed, MeshNavigator.GeneratePath(myLoc, moveTo).Skip(1).Select(p => (Vector3)p).ToArray());
				}
				_prevDestination = moveTo;

				return _currentAvoidPath;
			}
		}

		var avoidsChanged = PrevAvoids.Count != activeAvoids.Count() ||
				!activeAvoids.All(a =>
									  {
										  if (!PrevAvoids.ContainsKey(a))
										  {
											  // avoidLocations with a non-default CollectionSelector are removed each pulse so we can't use references to check for equality
											  if (a is AvoidLocation && ((AvoidLocationInfo)a.AvoidInfo).CollectionSelector != AvoidLocationInfo.DefaultCollectionSelector)
											  {
												  return PrevAvoids.Any(k => k.Key is AvoidLocation && k.Value == a.Location && k.Key.Radius == a.Radius);
											  }
											  return false;
										  }
										  return PrevAvoids[a].DistanceSqr(a.Location) <= SubPaddingSqr;
									  });
		var destinationChanged = _prevDestination.DistanceSqr(destination) > SubPaddingSqr;

		// check if anything has changed since last visit.
		if (PrevAvoids.Any() && !avoidsChanged && !destinationChanged && !AvoidanceManager.IsRunningOutOfAvoid)
		{
			var validAvoidPath = _currentAvoidPath != null && _currentAvoidPath.Path != null && _currentAvoidPath.Index >= 0 &&
			 _currentAvoidPath.Index < _currentAvoidPath.Path.Length;
			return activeAvoids.Any() && validAvoidPath ? _currentAvoidPath : null;
		}

		if (MeshNavigator.CurrentMovePath == null)
			return null;

		var originalDestination = destination;

		var clusterAtEnd = AvoidanceManager.AvoidClusters.FirstOrDefault(c => c.Any(a => a.IsPointInAvoid(destination)));
		if (clusterAtEnd != null)
		{
			var goalLoc = MeshNavigator.CurrentMovePath.Path.Points.LastOrDefault(p => !clusterAtEnd.Any(c => c.IsPointInAvoid(p)));
			destination = GetBestLocationOutsideCluster(destination, goalLoc, 2, clusterAtEnd, 1.5f);
			if (destination == WoWPoint.Zero)
				return null;
			//regenerate the navigation path...
			MeshNavigator.CurrentMovePath.Path = MeshNavigator.Nav.FindPath(myLoc, destination);
			MeshNavigator.CurrentMovePath.Index = 0;
		}

		// check if we need to regenerate the navigation path...
		if (avoidsChanged && !destinationChanged && _currentAvoidPath != null && _currentAvoidPath.Result != PathResult.Unchanged)
		{
			MeshNavigator.CurrentMovePath.Path = MeshNavigator.Nav.FindPath(myLoc, destination);
			MeshNavigator.CurrentMovePath.Index = 0;
		}

		if (!activeAvoids.Any())
			return _currentAvoidPath = null;

		var currentNavigationPath = MeshNavigator.CurrentMovePath.Path.Points.Skip(MeshNavigator.CurrentMovePath.Index).ToArray();
		AvoidPathResult newPath;
		try
		{
			newPath = GenPath(myLoc, destination, currentNavigationPath, 0);
		}
		catch (AvoidPathNotFoundException ex)
		{
			Logging.WriteDiagnostic("Avoid path generation has failed: {0}", ex.Message);
			// dump some debug info
			Logger.WriteDebug("Start:{0}, End:{1}", myLoc, destination);
			foreach (var activeAvoid in activeAvoids)
			{
				Logger.WriteDebug("\tLocation:{0}, Radius {1} ", activeAvoid.Location, activeAvoid.Radius);
			}
			ClearAvoidPath();
			return _currentAvoidPath = null;
		}

		_prevDestination = originalDestination;

		// if there is a cluster at end of path then set path Result to partial if it's set to UnChanged since we moved destination point outside of the cluster above.
		if (newPath.Result > PathResult.Partial && clusterAtEnd != null)
			newPath = new AvoidPathResult(PathResult.Partial, newPath.Path);

		if (newPath.Result == PathResult.Unchanged || newPath.Result == PathResult.Failed)
			return _currentAvoidPath = null;

		PrevAvoids.Clear();
		foreach (var activeAvoid in activeAvoids)
		{
			PrevAvoids.Add(activeAvoid, activeAvoid.Location);
		}

		if (newPath.Path.Length > 1)
		{
			myLoc = StyxWoW.Me.Location;
			WoWPoint nextPoint = newPath.Path[1];
			if (MeshTraceline(myLoc, nextPoint) == false)
			{
				newPath.Index = 1;
			}
		}

		if (MeshNavigator.CurrentMovePath.Path.Points.Length > MeshNavigator.CurrentMovePath.Index + 1 && MeshNavigator.CurrentMovePath.Path.Flags[MeshNavigator.CurrentMovePath.Index].HasFlag(StraightPathFlags.OffmeshConnection))
		{
			AreaType areaType = MeshNavigator.CurrentMovePath.Path.PolyTypes[MeshNavigator.CurrentMovePath.Index];
			if (areaType != AreaType.Elevator && areaType != AreaType.Portal && areaType != AreaType.DefendersPortal && areaType != AreaType.HordePortal && areaType != AreaType.AlliancePortal && areaType != AreaType.InteractUnit && areaType != AreaType.InteractObject)
			{
				WoWPoint segmentStart = MeshNavigator.CurrentMovePath.Path.Points[MeshNavigator.CurrentMovePath.Index];
				WoWPoint segmentEnd = MeshNavigator.CurrentMovePath.Path.Points[MeshNavigator.CurrentMovePath.Index + 1];
				WoWPoint nearestPointOnSegment = myLoc.GetNearestPointOnSegment(segmentStart, segmentEnd);
				if (Navigator.AtLocation(myLoc, nearestPointOnSegment))
				{
					newPath.Index++;
				}
			}
		}

		_currentAvoidPath = newPath;
		return _currentAvoidPath;
	}

	public static void ClearAvoidPath()
	{
		PrevAvoids.Clear();
		_currentAvoidPath = null;
	}

	// returns the avoid path
	private static AvoidPathResult GenPath(Vector3 start, Vector3 end, Vector3[] navigatorPath, int recursionLevel)
	{
		for (int pathI = 0; pathI < navigatorPath.Length; pathI++)
		{
			var from = pathI - 1 < 0 ? start : navigatorPath[pathI - 1];
			var to = navigatorPath[pathI];
			var clusterHit = AvoidClusterTraceline(AvoidanceManager.AvoidClusters, from, to);

			if (clusterHit != null)
			{
				bool isBLocking = false;
				var tangents = clusterHit.Cluster.GetLineTangentPoints(from);
				if (tangents != null)
				{
					// find best side and try to genorate paths for it.
					var bestSide = BestClusterSide(clusterHit.Point, tangents.RightPoint, tangents.LeftPoint, from);

					var leftSide = new { Avoid = tangents.LeftAvoid, Point = tangents.LeftPoint, Side = AvoidSide.Left };
					var rightSide = new { Avoid = tangents.RightAvoid, Point = tangents.RightPoint, Side = AvoidSide.Right };

					var bestSideChoice = bestSide == AvoidSide.Right ? rightSide : leftSide;
					var worstSideChoice = bestSide == AvoidSide.Right ? leftSide : rightSide;

					AvoidPathResult returnVal1;
					// find the next waypoint that isn't inside this cluster.
					var nextWaypoint = navigatorPath.Skip(pathI).FirstOrDefault(p => !clusterHit.Cluster.Any(a => a.IsPointInAvoid(p)));
					if (nextWaypoint == Vector3.Zero)
						nextWaypoint = end;

					returnVal1 = ProcessBranch(
						clusterHit.Cluster, bestSideChoice.Avoid, bestSideChoice.Point, bestSideChoice.Side, start, end, nextWaypoint, recursionLevel);
					// if the best side fails then try other side.
					if (returnVal1.Result <= PathResult.Partial)
					{
						AvoidPathResult returnVal2;
						returnVal2 = ProcessBranch(
							clusterHit.Cluster, worstSideChoice.Avoid, worstSideChoice.Point, worstSideChoice.Side, start, end, nextWaypoint, recursionLevel);

						if (returnVal2.Result > PathResult.Partial || (returnVal1.Result == PathResult.Failed && returnVal2.Result == PathResult.Partial))
							return returnVal2;
						// if both branches fail then we return a path that ends near point of interception.
						if (returnVal1.Result == PathResult.Failed && returnVal2.Result == PathResult.Failed)
						{
							isBLocking = true;
							var path = navigatorPath.Take(pathI).ToList();
							if (!clusterHit.Avoid.IgnoreIfBlocking)
							{
								path.Add(WoWMathHelper.CalculatePointFrom(from, clusterHit.Point, Padding));
								returnVal1 = new AvoidPathResult(PathResult.Partial, path.ToArray());
							}
						}
					}
					if (isBLocking)
					{
						clusterHit.Avoid.IsBlocking = true;
						if (clusterHit.Avoid.IgnoreIfBlocking)
							continue;
					}
					else if (clusterHit.Avoid.IsBlocking)
						clusterHit.Avoid.IsBlocking = false;

					return returnVal1;
				}
			}
		}
		return new AvoidPathResult(PathResult.Unchanged, navigatorPath);
	}

	private static AvoidPathResult ProcessBranch(
		AvoidCluster cluster, Avoid avoidHit, Vector3 tangentPoint, AvoidSide avoidSide, Vector3 start, Vector3 end, Vector3 nextWaypoint, int recursionLevel)
	{
		if (recursionLevel >= MaxRecursionLevel)
			throw new AvoidPathNotFoundException($"Recursion limit was reached ({MaxRecursionLevel})");

		var newPath = new List<Vector3>();

		PathResult returnVal = PathResult.Changed;

		AvoidPathResult endPathResult1 = null;

		var relativePoint = tangentPoint - (Vector3)avoidHit.Location;
		var startTangentAngle = (float)Math.Atan2(relativePoint.Y, relativePoint.X);

		Vector3 circleStartPoint = SetToNearestZ(avoidHit.Location.RayCast(startTangentAngle, avoidHit.Radius + Padding));

		// point outside circle isn't on same mesh...
		if (MeshTraceline(avoidHit.Location, circleStartPoint) != false)
			return new AvoidPathResult(PathResult.Failed, new Vector3[0]);

		Vector3 previousCirclePoint = Vector3.Zero;

		var circlePoint = circleStartPoint;
		// if circle points is very close to end then we stop.
		if (circlePoint.Distance2DSqr(end) < SubPadding)
			return new AvoidPathResult(PathResult.Changed, new[] { start, end });

		var ret1 = GeneratePathSegment(start, circlePoint, avoidHit, recursionLevel);
		if (ret1.Result <= PathResult.Partial)
			return ret1;
		newPath.AddRange(ret1.Path.Take(ret1.Path.Length - 1));

		var circleSegment = new List<Vector3>();
		bool tryWorstPath = false;
		const float stepSize = 0.05f;
		float step = stepSize;
		int iterations = 0;
		do
		{
			// check if point is navigatable. we only need one hop.
			if ((!previousCirclePoint.Equals(Vector3.Zero) && MeshTraceline(previousCirclePoint, circlePoint) == false) || iterations++ > 100)
			{
				returnVal = PathResult.Failed;
				break;
			}
			// check if an avoid in current cluster is blocking path
			var traces = GetAvoidsHitByTraceline(cluster, circlePoint, nextWaypoint).OrderBy(t => t.Enter.DistanceSqr(circlePoint) - t.Avoid.RadiusSqr).ToArray();

			// end is in sight! we now just genorate path to it.
			if (traces.Length == 0)
			{
				var ret = GeneratePathSegment(circlePoint, end, avoidHit, recursionLevel);
				newPath.AddRange(circleSegment);
				newPath.AddRange(ret.Path);
				returnVal = ret.Result;
				break;
			}
			// we hit another avoid on same cluster..
			if (traces[0].Avoid != avoidHit)
			{
				LineCircleTangentPoints tang = null;
				bool obsticleBetweenCirclepointAndTangent = false;
				//  lets back up some
				for (int i = 0; i < circleSegment.Count; i++)
				{
					tang = GetLineCircleTangentPoints(traces[0].Avoid.Location, traces[0].Avoid.Radius + Padding, circleSegment[i]);
					if (tang != null)
					{
						var tangPoint = avoidSide == AvoidSide.Right ? tang.RightPoint : tang.LeftPoint;
						obsticleBetweenCirclepointAndTangent = GetAvoidsHitByTraceline(AvoidanceManager.Avoids, circleSegment[i], tangPoint).Any();
						if (!obsticleBetweenCirclepointAndTangent)
						{
							if (i + 1 < circleSegment.Count)
								circleSegment.RemoveRange(i + 1, circleSegment.Count - (i + 1));
							circlePoint = circleSegment[i];
							break;
						}
					}
				}
				if (tang == null || obsticleBetweenCirclepointAndTangent)
				{
					if (circleSegment.Count > 0)
						circleSegment.RemoveAt(circleSegment.Count - 1);
					newPath.AddRange(circleSegment);
					returnVal = PathResult.Partial;
					break;
				}
				avoidHit = traces[0].Avoid;
				relativePoint = (avoidSide == AvoidSide.Right ? tang.RightPoint : tang.LeftPoint) - (Vector3)avoidHit.Location;
				startTangentAngle = (float)Math.Atan2(relativePoint.Y, relativePoint.X);

				var newCircleStartPoint = avoidHit.Location.RayCast(startTangentAngle, avoidHit.Radius + Padding);

				var ret = GeneratePathSegment(circlePoint, newCircleStartPoint, avoidHit, recursionLevel);

				circleSegment.AddRange(ret.Path);
				if (ret.Result <= PathResult.Partial)
				{
					newPath.AddRange(circleSegment);
					returnVal = ret.Result;
					break;
				}

				step = stepSize;
				circlePoint = newCircleStartPoint;
				continue;
			}

			var clusterHit = AvoidClusterTraceline(AvoidanceManager.AvoidClusters.Where(c => c != cluster).ToList(), circlePoint, end);
			LineClusterTangentPoints tangents = clusterHit != null ? clusterHit.Cluster.GetLineTangentPoints(circlePoint) : null;

			// check if we have another cluster blocking path to end.
			if (tangents != null)
			{
				var bestSide = BestClusterSide(clusterHit.Point, tangents.RightPoint, tangents.LeftPoint, start);

				var leftSide = new { Avoid = tangents.LeftAvoid, Point = tangents.LeftPoint, Side = AvoidSide.Left };
				var rightSide = new { Avoid = tangents.RightAvoid, Point = tangents.RightPoint, Side = AvoidSide.Right };

				var bestSideChoice = bestSide == AvoidSide.Right ? rightSide : leftSide;
				var worstSideChoice = bestSide == AvoidSide.Right ? leftSide : rightSide;

				var selectedSide = !tryWorstPath ? bestSideChoice : worstSideChoice;

				// check if we can move to the tangent of cluster blocking our way.
				if (AvoidTraceline(avoidHit, circlePoint, selectedSide.Point).Hits == 0)
				{
					if (!tryWorstPath)
					{
						endPathResult1 = ProcessBranch(
							clusterHit.Cluster, selectedSide.Avoid, selectedSide.Point, selectedSide.Side, circlePoint, end, nextWaypoint, recursionLevel);
						// check if other side has better results
						if (endPathResult1.Result <= PathResult.Partial)
						{
							step = stepSize;
							tryWorstPath = true;
							continue;
						}
						newPath.AddRange(circleSegment);
						newPath.AddRange(endPathResult1.Path);
						break;
					}
					AvoidPathResult endPathResult2 = ProcessBranch(
						clusterHit.Cluster, selectedSide.Avoid, selectedSide.Point, selectedSide.Side, circlePoint, end, nextWaypoint, recursionLevel);
					newPath.AddRange(circleSegment);
					newPath.AddRange(endPathResult1.Result >= endPathResult2.Result ? endPathResult1.Path : endPathResult2.Path);
					returnVal = endPathResult1.Result >= endPathResult2.Result ? endPathResult1.Result : endPathResult2.Result;
					break;
				}
			}

			circleSegment.Add(circlePoint);

			float relativeAngle = WoWMathHelper.NormalizeRadian(PIx2 * step);
			float ang = (avoidSide != AvoidSide.Right ? -relativeAngle : relativeAngle) + startTangentAngle;
			previousCirclePoint = circlePoint;
			circlePoint = SetToNearestZ(avoidHit.Location.RayCast(ang, avoidHit.Radius + Padding));
			step += stepSize;
		} while (step < 1);
		return new AvoidPathResult(returnVal > PathResult.Partial ? PathResult.Changed : returnVal, newPath.ToArray());
	}

	private static AvoidPathResult GeneratePathSegment(Vector3 start, Vector3 end, Avoid currentAvoid, int recursionLevel)
	{
		var pathResult = MeshNavigator.Nav.FindPath(start, end);
		if (pathResult == null || pathResult.Points == null || !pathResult.Succeeded)
			return new AvoidPathResult(PathResult.Failed, null);

		// if point any of the points generated in in the current avoid then it has failed.
		if (pathResult.Points.Any(p => currentAvoid != null && currentAvoid.IsPointInAvoid(p)))
			return new AvoidPathResult(PathResult.Failed, pathResult.Points);

		if (pathResult.IsPartialPath)
			return new AvoidPathResult(PathResult.Partial, pathResult.Points);

		return GenPath(start, end, pathResult.Points, recursionLevel + 1);
	}

	/// <summary>
	/// Gets the nearest location outside cluster.
	/// </summary>
	/// <param name="from">Point inside cluster</param>
	/// <param name="goal">Direction we want to travel towards</param>
	/// <param name="goalPriority">A number representing how hard to try move in the direction of <c>goal</c> . Number between -9 and 9 with 9 being highest.</param>
	/// <param name="cluster">The cluster.</param>
	/// <param name="extendDistance">The extend distance.</param>
	/// <returns></returns>
	public static Vector3 GetBestLocationOutsideCluster(Vector3 from, Vector3 goal, int goalPriority, AvoidCluster cluster, float extendDistance)
	{
		goalPriority %= 10;
		if (!goal.Equals(Vector3.Zero) && !goal.Equals(from))
		{
			var toGoalVector = goal - from;
			var toGoalAng = WoWMathHelper.NormalizeRadian((float)Math.Atan2(toGoalVector.Y, toGoalVector.X));
			var dist = from.Distance2D(goal);
			if (dist > goalPriority)
				dist = goalPriority;
			// all we do is move 'from' location towards 'goal' using goalPrioirty as distance.
			from = from.RayCast(toGoalAng, dist);
			from = SetToNearestZ(from);
			if (!cluster.Any(a => a.IsPointInAvoid(from)))
				return from;
		}

		var exitLocations = new List<Vector3>();
		for (float step = 0; step < 1; step += 0.05f)
		{
			var ang = PIx2 * step;
			var externalPoint = WoWMathHelper.GetPointAt(from, 100, ang, 0);

			var loc = cluster.Where(a => !a.IsBlocking || !a.IgnoreIfBlocking).Select(c => AvoidTraceline(c, from, externalPoint, extendDistance)).Where(
					t => t.Hits > 0 && (t.Avoid.LeashPoint == WoWPoint.Zero || t.Avoid.LeashPoint.DistanceSqr(t.Exit) < t.Avoid.LeashRadiusSqr)).OrderByDescending(
						t => from.Distance2DSqr(t.Exit)).Select(t => t.Exit).FirstOrDefault();

			if (!loc.Equals(Vector3.Zero))
				exitLocations.Add(loc);
		}

		MeshNavigator.Nav.MeshQuery.FindNearestPolygon(NavHelper.ToNav(from), MeshNavigator.Nav.Extents, QueryFilter.Default, out var nearestPoint, out var startPolyRef);

		if (nearestPoint.Equals(Vector3.Zero))
			return WoWPoint.Zero;
		foreach (var exitLocation in exitLocations.OrderBy(loc => from.Distance2DSqr(loc)))
		{
			MeshNavigator.Nav.MeshQuery.Raycast(startPolyRef, nearestPoint, NavHelper.ToNav(exitLocation), QueryFilter.Default, 100, out var polyResult, out var distCoef, out var hitNormal);

			if (distCoef == float.MaxValue)
				return SetToNearestZ(exitLocation);
		}
		// shouldn't get to here in most cases.
		return Vector3.Zero;
	}

	internal static MoveResult MoveOutOfAvoidCluster(AvoidCluster cluster)
	{
		var currentTarget = StyxWoW.Me.CurrentTarget;
		var goalLoc = currentTarget != null ? currentTarget.Location : WoWPoint.Zero;
		var moveTo = GetBestLocationOutsideCluster(StyxWoW.Me.Location, goalLoc, 2, cluster, 1.5f);
		if (moveTo.Equals(Vector3.Zero))
			return MoveResult.Failed;

		return Navigator.MoveTo(moveTo);
	}

	internal static string DescribeAvoid(Avoid avoid)
	{
		if (avoid is AvoidObject { Object: var obj })
		{
			if (!obj.IsValid)
			{
				return $"(Invalid {obj.GetType().Name})";
			}
			if (obj is WoWDynamicObject)
			{
				return WoWSpell.FromId((int)obj.Entry).Name;
			}
			WoWAreaTrigger areaTrigger = obj as WoWAreaTrigger;
			if (areaTrigger != null)
			{
				WoWSpell spell = areaTrigger.Spell;
				if (spell == null)
				{
					return areaTrigger.SpellVisualId.ToString(CultureInfo.InvariantCulture);
				}
				return spell.Name;
			}
			WoWPlayer player = obj as WoWPlayer;
			if (player != null)
			{
				string name = player.UnitId();
				if (name == "none")
				{
					name = player.IsHostile ? "Unfriendly player" : "Random Player";
				}
				return name;
			}
			return obj.SafeName;
		}
		if (avoid is AvoidLocation { Content: var content } avoidLocation)
		{
			if (content == null)
			{
				return avoidLocation.Location.ToString();
			}
			return content.ToString();
		}
		return string.Empty;
	}

	private static AvoidSide BestClusterSide(Vector3 clusterHitPoint, Vector3 rightPoint, Vector3 leftPoint, Vector3 externalPoint)
	{
		var hitPointAngle = (float)Math.Atan2(clusterHitPoint.Y - externalPoint.Y, clusterHitPoint.X - externalPoint.X);
		var rightAngle = (float)Math.Atan2(rightPoint.Y - externalPoint.Y, rightPoint.X - externalPoint.X);
		var leftAngle = (float)Math.Atan2(leftPoint.Y - externalPoint.Y, leftPoint.X - externalPoint.X);

		return WoWMathHelper.NormalizeRadian(leftAngle - hitPointAngle) > WoWMathHelper.NormalizeRadian(hitPointAngle - rightAngle) ? AvoidSide.Right : AvoidSide.Left;
	}

	/// <summary>
	///   Returns a list of avoids from the list passed as a parameter that are hit by traceline in the ascending order by which they were hit 1st.
	/// </summary>
	/// <param name="start"> The start. </param>
	/// <param name="end"> The end. </param>
	/// <param name="avoids"> The avoids. </param>
	/// <returns> </returns>
	public static IEnumerable<AvoidTracelineResult> GetAvoidsHitByTraceline(IEnumerable<Avoid> avoids, Vector3 start, Vector3 end)
	{
		return from avoid in avoids
			   let trace = AvoidTraceline(avoid, start, end)
			   where trace.Hits > 0
			   // note: using distance check from end location since if there is an avoid inside start it can produce inaccurate results.
			   orderby end.Distance2DSqr(trace.Enter) descending
			   select trace;
	}

	public static ClusterHit AvoidClusterTraceline(List<AvoidCluster> avoidClusters, Vector3 start, Vector3 end)
	{
		return (from cluster in avoidClusters
				let nearestHit = GetAvoidsHitByTraceline(cluster, start, end).OrderBy(t => start.DistanceSqr(t.Enter)).FirstOrDefault()
				where nearestHit != null
				let relativePoint = nearestHit.Exit - nearestHit.Enter
				orderby start.DistanceSqr(nearestHit.Enter)
				select new ClusterHit(cluster, nearestHit.Avoid, nearestHit.Enter, (float)Math.Atan2(relativePoint.Y, relativePoint.X))).FirstOrDefault();
	}

	public static void BuildClusters(List<Avoid> avoids, List<AvoidCluster> clusters)
	{
		clusters.Clear();
		foreach (var outerAvoid in avoids)
		{
			if (clusters.Any(c => c.Contains(outerAvoid)))
				continue;
			var avoidCluster = new AvoidCluster(outerAvoid);
			while (true)
			{
				var match = avoids.Where(a => !avoidCluster.Contains(a)).FirstOrDefault(
					a => avoidCluster.Any(
						clusteredAvoid =>
						{
							var minDist = clusteredAvoid.Radius + a.Radius + Padding * 2;
							var distSqr = clusteredAvoid.Location.DistanceSqr(a.Location);
							return distSqr < minDist * minDist;
						}));
				if (match != null)
					avoidCluster.Add(match);
				else
					break;
			}
			clusters.Add(avoidCluster);
		}
	}

	public static AvoidTracelineResult AvoidTraceline(Avoid avoid, Vector3 start, Vector3 end, float padding = SubPadding)
	{
		Vector3 enter, exit;
		var hits = GetLineCircleIntersections(avoid.Location, avoid.Radius + padding, start, end, out enter, out exit);
		return new AvoidTracelineResult(hits, enter, exit, avoid);
	}

	public static bool LineAvoidCollision(Avoid avoid, Vector3 start, Vector3 end)
	{
		Vector3 enter, exit;
		return GetLineCircleIntersections(avoid.Location, avoid.Radius + SubPadding, start, end, out enter, out exit) > 0;
	}

	/// <summary>
	///   returns the number of collisions of a line passing through a circle. Note: algorithm counts small lines inside circle as hits.
	/// </summary>
	/// <param name="circlePos"> The circle pos. </param>
	/// <param name="radius"> The radius. </param>
	/// <param name="start"> The start. </param>
	/// <param name="end"> The end. </param>
	/// <param name="enterPoint"> The line enter point. </param>
	/// <param name="exitPoint"> The line exit point. </param>
	/// <returns> </returns>
	public static int GetLineCircleIntersections(Vector3 circlePos, float radius, Vector3 start, Vector3 end, out Vector3 enterPoint, out Vector3 exitPoint)
	{
		float avoidRadiusSqr = radius * radius;
		float t;
		float cx = start.X - circlePos.X;
		float cy = start.Y - circlePos.Y;

		float dx = end.X - start.X;
		float dy = end.Y - start.Y;

		float a = dx * dx + dy * dy; // dy·dy
		float b = 2 * (dx * cx + dy * cy); // 2 * (dy·cy)
		float c = cx * cx + cy * cy - avoidRadiusSqr; // cx·cy - radius²

		float discriminant = b * b - 4 * a * c;
		if ((a <= 0.0000001) || (discriminant < 0))
		{
			// No real solutions.
			enterPoint = new Vector3(float.NaN, float.NaN, 0f);
			exitPoint = new Vector3(float.NaN, float.NaN, 0f);
			return 0;
		}
		// 1 hit
		if (Math.Abs(discriminant - 0) < float.Epsilon)
		{
			// One solution.
			t = -b / (2 * a);
			enterPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);
			exitPoint = new Vector3(float.NaN, float.NaN, 0f);
			return 1;
		}
		// Two solutions.

		var discriminantRoot = (float)Math.Sqrt(discriminant);
		// enter point
		t = (-b - discriminantRoot) / (2 * a);
		enterPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);
		// exit point
		t = (-b + discriminantRoot) / (2 * a);
		exitPoint = new Vector3(start.X + t * dx, start.Y + t * dy, circlePos.Z);

		var testPoint = start.Distance2DSqr(circlePos) < end.Distance2DSqr(circlePos) ? end : start;
		if (testPoint.Distance2DSqr(enterPoint) > a && testPoint.Distance2DSqr(exitPoint) > a && start.Distance2DSqr(circlePos) > avoidRadiusSqr &&
			end.Distance2DSqr(circlePos) > avoidRadiusSqr)
			return 0;

		return 2;
	}

	public static LineCircleTangentPoints GetLineCircleTangentPoints(Vector3 circlePos, float circleRadius, Vector3 externalPoint)
	{
		var relativePoint = new Vector3(externalPoint.X - circlePos.X, externalPoint.Y - circlePos.Y, 0);
		var hypotenuseLengthSqr = (relativePoint.X * relativePoint.X) + (relativePoint.Y * relativePoint.Y);
		var radiusSqr = circleRadius * circleRadius;
		if (hypotenuseLengthSqr >= radiusSqr)
		{
			var ratio = 1.0f / hypotenuseLengthSqr;
			var oppositeLength = (float)Math.Sqrt(Math.Abs(hypotenuseLengthSqr - radiusSqr));

			var x1 = circlePos.X + circleRadius * (circleRadius * relativePoint.X - relativePoint.Y * oppositeLength) * ratio;
			var y1 = circlePos.Y + circleRadius * (circleRadius * relativePoint.Y + relativePoint.X * oppositeLength) * ratio;
			var x2 = circlePos.X + circleRadius * (circleRadius * relativePoint.X + relativePoint.Y * oppositeLength) * ratio;
			var y2 = circlePos.Y + circleRadius * (circleRadius * relativePoint.Y - relativePoint.X * oppositeLength) * ratio;
			var rightPoint = new Vector3(x1, y1, circlePos.Z);
			var leftPoint = new Vector3(x2, y2, circlePos.Z);
			return new LineCircleTangentPoints(rightPoint, leftPoint);
		}
		return null;
	}

	public static bool? MeshTraceline(WoWPoint wowPointSrc, WoWPoint wowPointDest)
	{
		return MeshTraceline(wowPointSrc, wowPointDest, out float t);
	}

	public static bool? MeshTraceline(WoWPoint wowPointSrc, WoWPoint wowPointDest, out WoWPoint hitLocation)
	{
		hitLocation = WoWPoint.Zero;
		if (MeshTraceline(wowPointSrc, wowPointDest, out float t) == true)
		{
			WoWPoint delta = wowPointDest - wowPointSrc;
			hitLocation = delta * t + wowPointSrc;
			return true;
		}
		return false;
	}

	public static bool? MeshTraceline(WoWPoint wowPointSrc, WoWPoint wowPointDest, out float t)
	{
		t = float.MaxValue;
		if (!(Navigator.NavigationProvider is MeshNavigator { Nav: var nav }))
		{
			return null;
		}
		Vector3 startPos = NavHelper.ToNav(wowPointSrc);
		Vector3 endPos = NavHelper.ToNav(wowPointDest);
		nav.LoadTile(TileIdentifier.GetByPosition(wowPointSrc));
		nav.LoadTile(TileIdentifier.GetByPosition(wowPointDest));
		if (!nav.MeshQuery.FindNearestPolygon(startPos, nav.Extents, nav.QueryFilter.InternalFilter, out var _, out var result).Failed && result.Id != 0)
		{
			if (nav.MeshQuery.Raycast(result, startPos, endPos, nav.QueryFilter.InternalFilter, 500, out var _, out var t2, out var _).Failed)
			{
				return null;
			}
			t = t2;
			return t2 != float.MaxValue;
		}
		return null;
	}
}
