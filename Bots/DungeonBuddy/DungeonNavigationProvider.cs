using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Avoidance;
using Buddy.Coroutines;
using Styx;
using Styx.Pathing;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.Tools.Math;

namespace Bots.DungeonBuddy;

internal sealed class DungeonNavigationProvider : MeshNavigator
{
	/// <summary>
	/// Short-lived cache of whether a path exists (and its distance) between two points, so repeated
	/// CanNavigateFully/CanNavigateDistanceFully checks between the same two points don't re-run pathfinding.
	/// </summary>
	private class PathDistanceCache
	{
		private const float MatchToleranceSqr = 9f;

		private static readonly TimeSpan CacheEntryLifetime = TimeSpan.FromMilliseconds(2500.0);

		private static DateTime _lastCleanupTime = DateTime.MinValue;

		private static readonly List<PathDistanceCache> Cache = new List<PathDistanceCache>();

		private WoWPoint Start { get; set; }

		private WoWPoint Destination { get; set; }

		private float? Distance { get; set; }

		private DateTime TimeStamp { get; set; }

		private PathDistanceCache(WoWPoint start, WoWPoint destination, float? distance)
		{
			Start = start;
			Destination = destination;
			Distance = distance;
			TimeStamp = DateTime.Now;
		}

		internal static bool TryGetCached(WoWPoint start, WoWPoint destination, out float? distance)
		{
			PathDistanceCache match = null;
			DateTime now = DateTime.Now;
			bool doCleanup = now - _lastCleanupTime > CacheEntryLifetime;
			for (int i = Cache.Count - 1; i >= 0; i--)
			{
				PathDistanceCache entry = Cache[i];
				if (doCleanup && now - entry.TimeStamp > CacheEntryLifetime)
				{
					Cache.RemoveAt(i);
				}
				else if (match == null && start.DistanceSqr(entry.Start) < MatchToleranceSqr && destination.DistanceSqr(entry.Destination) < MatchToleranceSqr)
				{
					match = entry;
					if (!doCleanup)
					{
						break;
					}
				}
			}
			if (doCleanup)
			{
				_lastCleanupTime = now;
			}
			if (match == null)
			{
				distance = 0f;
				return false;
			}
			distance = match.Distance;
			return true;
		}

		internal static void Add(WoWPoint start, WoWPoint destination, float? distance)
		{
			Cache.Add(new PathDistanceCache(start, destination, distance));
		}
	}

	private bool _inDungeonMoveTo;

	private Coroutine _dungeonMoveToCoroutine;

	private WoWPoint _lastDungeonMoveToPoint;

	internal float PathPrecisionSqr => PathPrecision * PathPrecision;

	public WoWPoint Destination { get; private set; }

	public AvoidPathResult CurrentAvoidPath { get; private set; }

	private Coroutine DungeonMoveToCoroutine
	{
		get
		{
			return _dungeonMoveToCoroutine;
		}
		set
		{
			if (value != _dungeonMoveToCoroutine)
			{
				if (_dungeonMoveToCoroutine != null)
				{
					_dungeonMoveToCoroutine.Dispose();
				}
				_dungeonMoveToCoroutine = value;
			}
		}
	}

	public DungeonNavigationProvider()
	{
		PathPrecision = 1.5f;
	}

	public override void OnSetAsCurrent()
	{
		base.OnSetAsCurrent();
		base.Nav.RandomizePaths = true;
	}

	public override void OnRemoveAsCurrent()
	{
		base.OnRemoveAsCurrent();
		DungeonMoveToCoroutine = null;
	}

	public override MoveResult MoveTo(WoWPoint location)
	{
		Destination = location;
		if (TryDungeonMoveTo(location))
		{
			return MoveResult.Moved;
		}
		return base.MoveTo(location);
	}

	public override MoveResult MovePath(MeshMovePath path)
	{
		bool hadAvoidPath = CurrentAvoidPath != null;
		CurrentAvoidPath = Avoidance.Helpers.GetAvoidPath(Destination);
		bool avoidPathCleared = hadAvoidPath && CurrentAvoidPath == null;
		if (CurrentAvoidPath != null && CurrentAvoidPath.Index >= 0 && CurrentAvoidPath.Index < CurrentAvoidPath.Path.Length)
		{
			WoWPoint location = StyxWoW.Me.Location;
			bool isPartial = CurrentAvoidPath.Result == PathResult.Partial;
			WoWPoint endPoint = CurrentAvoidPath.Path[CurrentAvoidPath.Path.Length - 1];
			if (location.Distance2DSqr(endPoint) <= PathPrecisionSqr)
			{
				if (endPoint.Distance2DSqr(location) > 0.25)
				{
					Navigator.PlayerMover.MoveTowards(endPoint);
				}
				if (isPartial)
				{
					return MoveResult.Moved;
				}
				CurrentAvoidPath.Index = CurrentAvoidPath.Path.Length;
				return MoveResult.ReachedDestination;
			}
			while (CurrentAvoidPath.Index < CurrentAvoidPath.Path.Length)
			{
				Vector3 nextPoint = CurrentAvoidPath.Path[CurrentAvoidPath.Index];
				MoveTowards(CurrentAvoidPath);
				if (location.Distance2DSqr(nextPoint) > PathPrecisionSqr || (isPartial && CurrentAvoidPath.Index + 1 == CurrentAvoidPath.Path.Length))
				{
					break;
				}
				CurrentAvoidPath.Index++;
			}
			return MoveResult.Moved;
		}
		if (avoidPathCleared)
		{
			PathFindResult pathFindResult = FindPath(StyxWoW.Me.Location, Destination);
			if (!pathFindResult.Succeeded)
			{
				return MoveResult.PathGenerationFailed;
			}
			base.CurrentMovePath.Path = pathFindResult;
			base.CurrentMovePath.Index = 0;
			StuckHandler.Reset();
		}
		return base.MovePath(path);
	}

	public override bool CanNavigateFully(WoWPoint from, WoWPoint to)
	{
		if (DungeonManager.SelectedDungeon != null)
		{
			return DungeonManager.SelectedDungeon.CanNavigateFully(from, to);
		}
		return CanNavigateFullyInternal(from, to);
	}

	internal bool CanNavigateFullyInternal(WoWPoint from, WoWPoint to)
	{
		if (PathDistanceCache.TryGetCached(from, to, out var distance))
		{
			return distance.HasValue;
		}
		PathFindResult pathFindResult = FindPath(from, to);
		if (pathFindResult.Succeeded)
		{
			return !pathFindResult.IsPartialPath;
		}
		return false;
	}

	public bool CanNavigateDistanceFully(WoWPoint from, WoWPoint to, float maxDistance)
	{
		if (!PathDistanceCache.TryGetCached(from, to, out var distance))
		{
			distance = PathDistance(from, to, maxDistance);
			PathDistanceCache.Add(from, to, distance);
		}
		if (distance.HasValue)
		{
			return distance.Value < maxDistance;
		}
		return false;
	}

	public override bool Clear()
	{
		Avoidance.Helpers.ClearAvoidPath();
		return base.Clear();
	}

	private bool TryDungeonMoveTo(WoWPoint point)
	{
		if (DungeonManager.SelectedDungeon != null && !_inDungeonMoveTo)
		{
			try
			{
				_inDungeonMoveTo = true;
				if (DungeonManager.SelectedDungeon.MoveTo(point) != MoveResult.Failed)
				{
					return true;
				}
			}
			finally
			{
				_inDungeonMoveTo = false;
			}
			if (DungeonMoveToCoroutine == null || DungeonMoveToCoroutine.IsFinished || point.DistanceSqr(_lastDungeonMoveToPoint) > 1f)
			{
				DungeonMoveToCoroutine = new Coroutine(async () =>
				{
					Task<bool> task = DungeonManager.SelectedDungeon.HandleMovement(point);
					if (task != null)
					{
						return await task;
					}
					return false;
				});
				_lastDungeonMoveToPoint = point;
			}
			if (DungeonMoveToCoroutine != null && !DungeonMoveToCoroutine.IsFinished)
			{
				try
				{
					_inDungeonMoveTo = true;
					DungeonMoveToCoroutine.Resume();
					if (!DungeonMoveToCoroutine.IsFinished || (DungeonMoveToCoroutine.Status == CoroutineStatus.RanToCompletion && (bool)DungeonMoveToCoroutine.Result))
					{
						return true;
					}
				}
				finally
				{
					_inDungeonMoveTo = false;
				}
			}
			return false;
		}
		return false;
	}

	private void MoveTowards(AvoidPathResult avoidPath)
	{
		if (avoidPath.Path.Length > avoidPath.Index)
		{
			Vector3 point = avoidPath.Path[avoidPath.Index];
			Navigator.PlayerMover.MoveTowards(point);
		}
	}
}
