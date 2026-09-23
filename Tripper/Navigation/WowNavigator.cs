using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Styx;
using Styx.Pathing;
using Tripper.MeshMisc;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

namespace Tripper.Navigation
{
	public class WowNavigator : IDisposable
	{
		private static bool _nativeInitialized;

		public WowNavigator()
		{
			if (!_nativeInitialized)
			{
				NavigationApi.LoadMaps();
				_nativeInitialized = true;
			}

			this.MeshLock = new object();
			this.GarbageCollectTime = TimeSpan.FromMinutes(1.0);
			this.Extents = new Vector3(3f, 20f, 3f);
			this._defaultFilter = GetNewDefaultQueryFilter();
			this._hordeFilter = BuildFactionQueryFilter(true);
			this._allianceFilter = BuildFactionQueryFilter(false);
			this._namedFilters["Default"] = this._defaultFilter;
			this._namedFilters["Horde"] = this._hordeFilter;
			this._namedFilters["Alliance"] = this._allianceFilter;
			this._hordeDeathKnightStartFilter = BuildFactionQueryFilter(true);
			this._hordeDeathKnightStartFilter.ExcludeFlags &= ~Tripper.MeshMisc.AbilityFlags.Transport;
			this._hordeDeathKnightStartFilter.IncludeFlags |= Tripper.MeshMisc.AbilityFlags.Transport;
			this._namedFilters["Horde_DeathKnightStart"] = this._hordeDeathKnightStartFilter;
			this._allianceDeathKnightStartFilter = BuildFactionQueryFilter(false);
			this._allianceDeathKnightStartFilter.ExcludeFlags &= ~Tripper.MeshMisc.AbilityFlags.Transport;
			this._allianceDeathKnightStartFilter.IncludeFlags |= Tripper.MeshMisc.AbilityFlags.Transport;
			this._namedFilters["Alliance_DeathKnightStart"] = this._allianceDeathKnightStartFilter;
			this.ResetQueryFilter();
		}

		public object MeshLock { get; private set; }

		public event EventHandler<PathFindProgressEventArgs> OnPathFindProgress;

		public event NavigatorLogMessage OnNavigatorLogMessage;

		public event EventHandler<TileLoadedEventArgs> OnTileLoaded
		{
			add
			{
				this._onTileLoaded += value;
				this.InstallTileLoadedThunk();
			}
			remove { this._onTileLoaded -= value; }
		}

		public event EventHandler<TileLoadedEventArgs> OnSubTileLoaded
		{
			add
			{
				this._onSubTileLoaded += value;
				this.InstallTileLoadedThunk();
			}
			remove { this._onSubTileLoaded -= value; }
		}

		public event EventHandler<MapLoadedEventArgs> OnMapLoaded;

		private void InstallTileLoadedThunk()
		{
			if (_tileLoadedThunk == null)
			{
				_tileLoadedThunk = NativeTileLoaded;
				NavigationApi.SetTileLoadedCallback(_tileLoadedThunk);
			}
			_tileLoadedOwner = this;
		}

		private static NavTileLoadedCallback _tileLoadedThunk;
		private static WowNavigator _tileLoadedOwner;
		private EventHandler<TileLoadedEventArgs> _onTileLoaded;
		private EventHandler<TileLoadedEventArgs> _onSubTileLoaded;

		private static void NativeTileLoaded(uint mapId, int x, int y)
		{
			WowNavigator owner = _tileLoadedOwner;
			if (owner == null)
			{
				return;
			}

			EventHandler<TileLoadedEventArgs> subHandler = owner._onSubTileLoaded;
			if (subHandler != null)
			{
				int[] subX = new int[16];
				int[] subY = new int[16];
				int count = NativeMethods.GetAdtSubTiles_C(mapId, x, y, subX, subY, subX.Length);
				for (int i = 0; i < count; i++)
				{
					subHandler(owner, new TileLoadedEventArgs { Tile = new Tripper.MeshMisc.TileIdentifier(subX[i], subY[i]) });
				}
			}

			EventHandler<TileLoadedEventArgs> handler = owner._onTileLoaded;
			if (handler != null)
			{
				handler(owner, new TileLoadedEventArgs { Tile = new Tripper.MeshMisc.TileIdentifier(x, y) });
			}
		}

		public WowQueryFilter QueryFilter
		{
			get { return this._queryFilter; }
		}

		public NavMesh Mesh { get; private set; }

		public NavMeshQuery MeshQuery { get; private set; }

		public Vector3 Extents { get; set; }

		public bool RandomizePaths { get; set; }

		public string[] Maps { get; private set; }

		public string PrimaryMapName
		{
			get
			{
				if (this.Maps != null && this.Maps.Length > 0)
				{
					return this.Maps[this.Maps.Length - 1];
				}
				return null;
			}
		}

		public void ChangeMap(ICollection<string> maps)
		{
			if (maps == null)
			{
				throw new ArgumentNullException("maps");
			}
			lock (this.MeshLock)
			{
				if (this.Maps == null || !maps.SequenceEqual(this.Maps))
				{
					this.Maps = maps.ToArray();
					this._tileUsage.Clear();
					if (maps.Count >= 1)
					{
						this._mapId = StyxWoW.Me.MapId;
						this.Mesh = new NavMesh { MapId = this._mapId };
						this.MeshQuery = new NavMeshQuery { MapId = this._mapId };
						EventHandler<MapLoadedEventArgs> mapLoadedHandler = this.OnMapLoaded;
						if (mapLoadedHandler != null)
						{
							mapLoadedHandler(this, new MapLoadedEventArgs
							{
								Names = this.Maps.ToArray(),
								IsTiled = true
							});
						}
					}
					else
					{
						this.Mesh = null;
						this.MeshQuery = null;
					}
				}
			}
		}

		public bool LoadTile(Tripper.MeshMisc.TileIdentifier wowTile)
		{
			lock (this.MeshLock)
			{
				if (this._tileUsage.ContainsKey(wowTile))
				{
					return true;
				}

				WoWPoint center = TileCenter(wowTile);
				NavigationApi.EnsureTiles(this._mapId, center, 0);

				int nativeX, nativeY;
				NativeTileIndex(wowTile, out nativeX, out nativeY);
				if (!NavigationApi.IsTileLoaded(this._mapId, nativeX, nativeY))
				{
					return false;
				}

				this._tileUsage.Add(wowTile, Stopwatch.StartNew());
				return true;
			}
		}

		public bool UnloadTile(Tripper.MeshMisc.TileIdentifier wowTile)
		{
			lock (this.MeshLock)
			{
				if (!this._tileUsage.ContainsKey(wowTile))
				{
					return true;
				}

				int nativeX, nativeY;
				NativeTileIndex(wowTile, out nativeX, out nativeY);
				NavigationApi.UnloadTile(this._mapId, nativeX, nativeY);
				this._tileUsage.Remove(wowTile);
				return true;
			}
		}

		public void UnloadAllTiles()
		{
			lock (this.MeshLock)
			{
				foreach (Tripper.MeshMisc.TileIdentifier wowTile in this._tileUsage.Keys.ToArray())
				{
					int nativeX, nativeY;
					NativeTileIndex(wowTile, out nativeX, out nativeY);
					NavigationApi.UnloadTile(this._mapId, nativeX, nativeY);
				}
				this._tileUsage.Clear();
			}
		}

		public bool UpdateTileUsageTime(Tripper.MeshMisc.TileIdentifier ti)
		{
			lock (this.MeshLock)
			{
				Stopwatch stopwatch;
				if (!this._tileUsage.TryGetValue(ti, out stopwatch))
				{
					return false;
				}
				stopwatch.Restart();
				return true;
			}
		}

		public TimeSpan GarbageCollectTime { get; set; }

		public void GarbageCollectTiles(bool log = true)
		{
			lock (this.MeshLock)
			{
				List<Tripper.MeshMisc.TileIdentifier> expired = null;
				foreach (KeyValuePair<Tripper.MeshMisc.TileIdentifier, Stopwatch> kvp in this._tileUsage)
				{
					if (kvp.Value.Elapsed > this.GarbageCollectTime)
					{
						if (expired == null)
						{
							expired = new List<Tripper.MeshMisc.TileIdentifier>();
						}
						expired.Add(kvp.Key);
					}
				}
				if (expired != null)
				{
					expired.ForEach(ti => this.UnloadTile(ti));
				}
				if (log && expired != null)
				{
					this.LogMessage(string.Format("Garbage collected {0} nav tiles", expired.Count));
				}
			}
		}

		public void SetFactionQueryFilter(bool isHorde)
		{
			lock (this.MeshLock)
			{
				this._queryFilter = isHorde ? this._hordeFilter : this._allianceFilter;
			}
		}

		public void ResetQueryFilter()
		{
			lock (this.MeshLock)
			{
				this._queryFilter = this._defaultFilter;
			}
		}

		public bool HasQueryFilter(string name)
		{
			lock (this.MeshLock)
			{
				return this._namedFilters.ContainsKey(name);
			}
		}

		public void StoreQueryFilter(string name, WowQueryFilter filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException("filter");
			}
			lock (this.MeshLock)
			{
				this._namedFilters[name] = filter;
			}
		}

		public WowQueryFilter GetStoredQueryFilter(string name)
		{
			lock (this.MeshLock)
			{
				WowQueryFilter filter;
				return this._namedFilters.TryGetValue(name, out filter) ? filter : null;
			}
		}

		public bool SetQueryFilterByStored(string name)
		{
			lock (this.MeshLock)
			{
				WowQueryFilter storedQueryFilter = this.GetStoredQueryFilter(name);
				if (storedQueryFilter != null)
				{
					this._queryFilter = storedQueryFilter;
					return true;
				}
				return false;
			}
		}

		public PathFindResult FindPath(Vector3 start, Vector3 end)
		{
			lock (this.MeshLock)
			{
				PathFindResult result = new PathFindResult
				{
					Start = start,
					End = end
				};

				Stopwatch stopwatch = Stopwatch.StartNew();
				bool cancelled = false;

				NavPathFindProgressCallback progress = delegate
				{
					EventHandler<PathFindProgressEventArgs> handler = this.OnPathFindProgress;
					if (handler == null)
					{
						return true;
					}

					PathFindProgressEventArgs args = new PathFindProgressEventArgs(stopwatch.Elapsed);
					handler(this, args);
					cancelled |= args.Cancel;
					return !args.Cancel;
				};

				NavMeshQuery.PushFilter(this._mapId, this.QueryFilter.InternalFilter);

				WoWPoint startPoint = start;
				WoWPoint endPoint = end;
				NavPathResult navResult;
				NativeMethods.SetPathFindProgressCallback_C(progress);
				try
				{
					navResult = NavigationApi.CalculatePathEx(this._mapId, startPoint, endPoint, true);
				}
				finally
				{
					NativeMethods.SetPathFindProgressCallback_C(null);
					GC.KeepAlive(progress);
					stopwatch.Stop();
				}

				result.Elapsed = stopwatch.Elapsed;
				result.Aborted = cancelled;

				if (navResult == null)
				{
					result.Status = Status.Failure;
					result.FailStep = PathFindStep.None;
					return result;
				}

				result.Status = new Status(navResult.Status);
				result.FailStep = MapFailStep(navResult.FailStep);
				result.IsPartialPath = navResult.IsPartial;

				Vector3[] points = new Vector3[navResult.Points.Length];
				for (int i = 0; i < points.Length; i++)
				{
					points[i] = NavHelper.ToNav(navResult.Points[i]);
				}

				StraightPathFlags[] flags = new StraightPathFlags[navResult.StraightPathFlags.Length];
				for (int i = 0; i < flags.Length; i++)
				{
					flags[i] = (StraightPathFlags)navResult.StraightPathFlags[i];
				}

				PolygonReference[] polygons = new PolygonReference[navResult.PolyRefs.Length];
				for (int i = 0; i < polygons.Length; i++)
				{
					polygons[i] = new PolygonReference(navResult.PolyRefs[i]);
				}

				if (this.RandomizePaths)
				{
					this.RandomizePath(ref points, ref polygons, ref flags, 2f, 6f, 4f);
				}
				else
				{
					this.FixPathWalkabilityOnly(ref points, ref polygons, ref flags, 2f);
				}

				for (int i = 0; i < points.Length; i++)
				{
					NavHelper.ToWow(ref points[i]);
					this.UpdateTileUsageTime(Tripper.MeshMisc.TileIdentifier.GetByPosition(points[i]));
				}

				result.Points = points;
				result.Flags = flags;
				result.Polygons = polygons;

				Tripper.MeshMisc.AbilityFlags[] abilityFlags = new Tripper.MeshMisc.AbilityFlags[polygons.Length];
				Tripper.MeshMisc.AreaType[] polyTypes = new Tripper.MeshMisc.AreaType[polygons.Length];
				for (int i = 0; i < polygons.Length; i++)
				{
					if (this.Mesh.GetPolyArea(polygons[i], out byte area).Succeeded)
					{
						polyTypes[i] = (Tripper.MeshMisc.AreaType)area;
					}
					if (this.Mesh.GetPolyFlags(polygons[i], out ushort polyFlags).Succeeded)
					{
						abilityFlags[i] = (Tripper.MeshMisc.AbilityFlags)polyFlags;
					}
				}
				result.AbilityFlags = abilityFlags;
				result.PolyTypes = polyTypes;

				return result;
			}
		}

		public void Dispose()
		{
			lock (this.MeshLock)
			{
				this.Mesh = null;
				this.MeshQuery = null;
				this._defaultFilter.Dispose();
				this._hordeFilter.Dispose();
				this._allianceFilter.Dispose();
				this._hordeDeathKnightStartFilter.Dispose();
				this._allianceDeathKnightStartFilter.Dispose();
				this._namedFilters.Clear();
				this._tileUsage.Clear();
			}
		}

		private void LogMessage(string msg)
		{
			NavigatorLogMessage handler = this.OnNavigatorLogMessage;
			if (handler != null)
			{
				handler(msg);
			}
		}

		private enum EdgeMoveResult
		{
			NotNearEdge,
			Failed,
			Moved
		}

		private delegate void StraightPathFixupCallback(ref Vector3[] points, ref PolygonReference[] polys, ref StraightPathFlags[] flags);

		private bool SnapPointToMesh(ref Vector3 point, out PolygonReference poly, QueryFilter filter, bool logErrors = true)
		{
			Status status = this.MeshQuery.FindNearestPolygon(point, new Vector3(0.5f, 5f, 0.5f), filter, out Vector3 nearestPt, out poly);
			if (!status.Failed && poly.Id != 0)
			{
				point = nearestPt;
				return true;
			}
			if (status.Failed && logErrors)
			{
				this.LogMessage($"SnapPointToMesh failed with {status.Flags} for point {point}");
			}
			else if (logErrors)
			{
				this.LogMessage($"SnapPointToMesh failed for point {point}");
			}
			return false;
		}

		private bool RandomizePoint(ref Vector3 point, out PolygonReference poly, QueryFilter filter, float radius, Random random)
		{
			float radiusSqr = radius * radius;
			Vector3 offset = default(Vector3);
			do
			{
				offset.X = (float)((random.NextDouble() - 0.5) * 2.0 * (double)radius);
				offset.Y = 0f;
				offset.Z = (float)((random.NextDouble() - 0.5) * 2.0 * (double)radius);
			}
			while (offset.MagnitudeSqr > radiusSqr);
			point += offset;
			return this.SnapPointToMesh(ref point, out poly, filter);
		}

		private EdgeMoveResult TryMoveAwayFromEdge(ref Vector3 point, ref PolygonReference poly, QueryFilter filter, float distance)
		{
			Status status = this.MeshQuery.FindDistanceToWall(poly, point, distance, filter, out float dist, out Vector3 hitPos, out Vector3 hitNormal);
			if (status.Failed)
			{
				this.LogMessage($"FindDistanceToWall failed with {status.Flags} - point {point} (TryMoveAwayFromEdge)");
				return EdgeMoveResult.Failed;
			}
			if (dist >= distance)
			{
				return EdgeMoveResult.NotNearEdge;
			}
			Vector3 nearestPt = hitPos;
			status = this.MeshQuery.FindNearestPolygon(nearestPt, new Vector3(0.5f, 5f, 0.5f), filter, out nearestPt, out PolygonReference nearPoly);
			if (nearPoly.Id == 0)
			{
				this.LogMessage($"First FindNearestPolygon got empty poly ref at point {hitPos}, distance {dist}, normal {hitNormal} - please report this on the forums!");
				return EdgeMoveResult.Failed;
			}
			Vector3 target = hitPos + hitNormal * distance * 2f;
			status = this.MeshQuery.Raycast(nearPoly, nearestPt, target, filter, 512, out PolygonReference[] _, out float t, out Vector3 _);
			if (status.Failed)
			{
				this.LogMessage($"Raycast failed with {status.Flags} - point {point} (TryMoveAwayFromEdge)");
				return EdgeMoveResult.Failed;
			}
			if (t == float.MaxValue)
			{
				point = hitPos + hitNormal * distance;
			}
			else
			{
				Vector3 hitPoint = nearestPt + (target - nearestPt) * t;
				point = (hitPos + hitPoint) / 2f;
			}
			if (!this.SnapPointToMesh(ref point, out poly, filter))
			{
				return EdgeMoveResult.Failed;
			}
			return EdgeMoveResult.Moved;
		}

		private void MovePathPointsAwayFromEdges(List<Vector3> points, List<PolygonReference> polys, List<StraightPathFlags> flags, float distance)
		{
			for (int i = 1; i < points.Count - 1; i++)
			{
				if ((flags[i] & StraightPathFlags.OffmeshConnection) == 0 && (flags[i - 1] & StraightPathFlags.OffmeshConnection) == 0)
				{
					Vector3 point = points[i];
					PolygonReference poly = polys[i];
					if (this.TryMoveAwayFromEdge(ref point, ref poly, this.QueryFilter.InternalFilter, distance) == EdgeMoveResult.Moved)
					{
						points[i] = point;
						polys[i] = poly;
					}
				}
			}
		}

		private void RandomizePathPoints(List<Vector3> points, List<PolygonReference> polys, List<StraightPathFlags> flags, float minDistance, float maxDistance, float jitterCap, Random random)
		{
			for (int i = 1; i < points.Count - 1; i++)
			{
				if ((flags[i] & StraightPathFlags.OffmeshConnection) == 0 && (flags[i - 1] & StraightPathFlags.OffmeshConnection) == 0)
				{
					float distance = (float)(random.NextDouble() * (double)(maxDistance - minDistance) + (double)minDistance);
					bool moved = false;
					Vector3 point = points[i];
					PolygonReference poly = polys[i];
					switch (this.TryMoveAwayFromEdge(ref point, ref poly, this.QueryFilter.InternalFilter, distance))
					{
						case EdgeMoveResult.NotNearEdge:
						{
							float jitter = (float)random.NextDouble() * Math.Min(distance, jitterCap);
							moved = this.RandomizePoint(ref point, out poly, this.QueryFilter.InternalFilter, jitter, random);
							break;
						}
						case EdgeMoveResult.Moved:
							moved = true;
							break;
					}
					if (moved)
					{
						points[i] = point;
						polys[i] = poly;
					}
				}
			}
		}

		private void FixPathWalkability(List<Vector3> points, List<PolygonReference> polys, List<StraightPathFlags> flags, StraightPathFixupCallback fixup)
		{
			for (int i = 0; i < points.Count - 1; i++)
			{
				if ((flags[i] & StraightPathFlags.OffmeshConnection) != StraightPathFlags.None || (flags[i + 1] & StraightPathFlags.OffmeshConnection) != StraightPathFlags.None)
				{
					continue;
				}
				Vector3 a = points[i];
				Vector3 b = points[i + 1];
				if (a.DistanceSqr(b) < 0.010000001f)
				{
					continue;
				}
				Status status = this.MeshQuery.Raycast(polys[i], a, b, this.QueryFilter.InternalFilter, 512, out PolygonReference[] hitPath, out float t, out Vector3 hitNormal);
				if (status.Failed)
				{
					this.LogMessage($"Raycast from {NavHelper.ToWow(a)} to {NavHelper.ToWow(b)} (high: {NavHelper.ToWow(points.First())} to {NavHelper.ToWow(points.Last())}) failed with {status.Flags} (FixPathWalkability)");
					continue;
				}
				if (t <= 1E-07f)
				{
					Vector3 nudged = a;
					Vector3 dir = b - nudged;
					dir.Normalize();
					nudged += dir * 0.1f;
					if (this.SnapPointToMesh(ref nudged, out PolygonReference nudgedPoly, this.QueryFilter.InternalFilter))
					{
						status = this.MeshQuery.Raycast(nudgedPoly, nudged, b, this.QueryFilter.InternalFilter, 512, out hitPath, out t, out hitNormal);
						if (status.Failed)
						{
							this.LogMessage($"Second raycast from {NavHelper.ToWow(nudged)} to {NavHelper.ToWow(b)} (high: {NavHelper.ToWow(points.First())} to {NavHelper.ToWow(points.Last())}) failed with {status.Flags} (FixPathWalkability)");
							continue;
						}
						if (t > 1E-07f)
						{
							points[i] = nudged;
							polys[i] = nudgedPoly;
						}
					}
				}
				if (t == float.MaxValue)
				{
					continue;
				}
				PolygonReference endPoly = polys[i + 1];
				if (endPoly.Id == 0)
				{
					status = this.MeshQuery.FindNearestPolygon(b, new Vector3(0.5f, 5f, 0.5f), this.QueryFilter.InternalFilter, out Vector3 _, out endPoly);
					if (status.Failed || endPoly.Id == 0)
					{
						if (status.Failed)
						{
							this.LogMessage($"FindNearestPolygon failed with {status.Flags} - point {NavHelper.ToWow(b)} (FixPathWalkability)");
						}
						else
						{
							this.LogMessage($"FindNearestPolygon couldn't find point {NavHelper.ToWow(b)} (FixPathWalkability)");
						}
						continue;
					}
				}
				status = this.MeshQuery.FindPath(polys[i], endPoly, a, b, this.QueryFilter.InternalFilter, 512, out PolygonReference[] corridor);
				if (status.Failed)
				{
					this.LogMessage($"Path find from {NavHelper.ToWow(a)} to {NavHelper.ToWow(b)} (high: {NavHelper.ToWow(points.First())} to {NavHelper.ToWow(points.Last())}) failed with {status.Flags} (FixPathWalkability)");
					continue;
				}
				if (status.HasFlag(StatusDetailFlag.PartialResult))
				{
					this.LogMessage($"Path find from {NavHelper.ToWow(a)} to {NavHelper.ToWow(b)} (high: {NavHelper.ToWow(points.First())} to {NavHelper.ToWow(points.Last())}) resulted in partial path (FixPathWalkability)!");
					continue;
				}
				status = this.MeshQuery.FindStraightPath(a, b, corridor, 512, out Vector3[] subPoints, out StraightPathFlags[] subFlags, out PolygonReference[] subRefs);
				if (status.Failed)
				{
					this.LogMessage($"FindStraightPath from {NavHelper.ToWow(a)} to {NavHelper.ToWow(b)} (high: {NavHelper.ToWow(points.First())} to {NavHelper.ToWow(points.Last())}) failed with {status.Flags} (FixPathWalkability)!");
				}
				else
				{
					fixup?.Invoke(ref subPoints, ref subRefs, ref subFlags);
					points.InsertRange(i + 1, subPoints.Skip(1).Take(subPoints.Length - 2));
					polys.InsertRange(i + 1, subRefs.Skip(1).Take(subRefs.Length - 2));
					flags.InsertRange(i + 1, subFlags.Skip(1).Take(subFlags.Length - 2));
					i += subPoints.Length - 2;
				}
			}
		}

		private void FixPathWalkabilityForRandomize(List<Vector3> points, List<PolygonReference> polys, List<StraightPathFlags> flags, float minDistance, float maxDistance, float jitterCap, Random random, int depth)
		{
			StraightPathFixupCallback fixup = delegate (ref Vector3[] fixupPoints, ref PolygonReference[] fixupPolys, ref StraightPathFlags[] fixupFlags)
			{
				this.RandomizePathIteration(ref fixupPoints, ref fixupPolys, ref fixupFlags, minDistance, maxDistance, jitterCap, random, depth + 1);
			};
			this.FixPathWalkability(points, polys, flags, fixup);
		}

		private void RandomizePathIteration(ref Vector3[] points, ref PolygonReference[] polys, ref StraightPathFlags[] flags, float minDistance, float maxDistance, float jitterCap, Random random, int depth)
		{
			if (depth <= 5)
			{
				List<Vector3> pointList = points.ToList();
				List<PolygonReference> polyList = polys.ToList();
				List<StraightPathFlags> flagList = flags.ToList();
				this.RandomizePathPoints(pointList, polyList, flagList, minDistance, maxDistance, jitterCap, random);
				this.FixPathWalkabilityForRandomize(pointList, polyList, flagList, minDistance, maxDistance, jitterCap, random, depth);
				points = pointList.ToArray();
				polys = polyList.ToArray();
				flags = flagList.ToArray();
			}
		}

		private void RandomizePath(ref Vector3[] points, ref PolygonReference[] polys, ref StraightPathFlags[] flags, float minDistance, float maxDistance, float jitterCap)
		{
			this.RandomizePathIteration(ref points, ref polys, ref flags, minDistance, maxDistance, jitterCap, new Random(), 0);
		}

		private void FixPathWalkabilityForFixup(List<Vector3> points, List<PolygonReference> polys, List<StraightPathFlags> flags, float distance, int depth)
		{
			StraightPathFixupCallback fixup = delegate (ref Vector3[] fixupPoints, ref PolygonReference[] fixupPolys, ref StraightPathFlags[] fixupFlags)
			{
				this.FixPathWalkabilityIteration(ref fixupPoints, ref fixupPolys, ref fixupFlags, distance, depth + 1);
			};
			this.FixPathWalkability(points, polys, flags, fixup);
		}

		private void FixPathWalkabilityIteration(ref Vector3[] points, ref PolygonReference[] polys, ref StraightPathFlags[] flags, float distance, int depth)
		{
			if (depth <= 5)
			{
				List<Vector3> pointList = points.ToList();
				List<PolygonReference> polyList = polys.ToList();
				List<StraightPathFlags> flagList = flags.ToList();
				this.MovePathPointsAwayFromEdges(pointList, polyList, flagList, distance);
				this.FixPathWalkabilityForFixup(pointList, polyList, flagList, distance, depth);
				points = pointList.ToArray();
				polys = polyList.ToArray();
				flags = flagList.ToArray();
			}
		}

		private void FixPathWalkabilityOnly(ref Vector3[] points, ref PolygonReference[] polys, ref StraightPathFlags[] flags, float distance)
		{
			this.FixPathWalkabilityIteration(ref points, ref polys, ref flags, distance, 0);
		}

		public static WowQueryFilter GetNewDefaultQueryFilter()
		{
			WowQueryFilter filter = new WowQueryFilter(Tripper.MeshMisc.AbilityFlags.All, Tripper.MeshMisc.AbilityFlags.Unwalkable | Tripper.MeshMisc.AbilityFlags.Transport);
			SetDefaultQueryFilterCosts(filter);
			return filter;
		}

		public static void SetDefaultQueryFilterCosts(WowQueryFilter filter)
		{
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.HordePortal, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.AlliancePortal, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.DefendersPortal, 3.16f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Portal, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Elevator, 3.16f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Lava, 55f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Blocked, 100f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Fall, 1.7f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Gate, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Ground, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Road, 1f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Water, 3.33f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Alliance, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Horde, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.Blackspot, 60f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.InteractObject, 1.66f);
			filter.SetAreaCost(Tripper.MeshMisc.AreaType.InteractUnit, 1.66f);
		}

		public static WowQueryFilter GetNewFactionQueryFilter(bool horde)
		{
			return BuildFactionQueryFilter(horde);
		}

		private const float ProhibitiveFactionAreaCost = 1000f;

		private static WowQueryFilter BuildFactionQueryFilter(bool isHorde)
		{
			WowQueryFilter filter = GetNewDefaultQueryFilter();
			if (isHorde)
			{
				filter.SetAreaCost(Tripper.MeshMisc.AreaType.Alliance, ProhibitiveFactionAreaCost);
				filter.SetAreaCost(Tripper.MeshMisc.AreaType.AlliancePortal, ProhibitiveFactionAreaCost);
			}
			else
			{
				filter.SetAreaCost(Tripper.MeshMisc.AreaType.Horde, ProhibitiveFactionAreaCost);
				filter.SetAreaCost(Tripper.MeshMisc.AreaType.HordePortal, ProhibitiveFactionAreaCost);
			}
			return filter;
		}

		private static PathFindStep MapFailStep(int nativeFailStep)
		{
			switch (nativeFailStep)
			{
				case 0:
					return PathFindStep.FindStartPoly;
				case 1:
					return PathFindStep.FindEndPoly;
				case 2:
					return PathFindStep.InitPathFind;
				case 3:
					return PathFindStep.UpdatePathFind;
				case 4:
					return PathFindStep.FinalizePathFind;
				case 5:
					return PathFindStep.FindStraightPath;
				default:
					return PathFindStep.None;
			}
		}

		private static WoWPoint TileCenter(Tripper.MeshMisc.TileIdentifier ti)
		{
			const float TileSize = 533.3333f;
			float worldX = (32f - ti.Y - 0.5f) * TileSize;
			float worldY = (32f - ti.X - 0.5f) * TileSize;
			return new WoWPoint(worldX, worldY, 0f);
		}

		private static void NativeTileIndex(Tripper.MeshMisc.TileIdentifier ti, out int nativeX, out int nativeY)
		{
			nativeX = ti.Y;
			nativeY = ti.X;
		}

		private readonly Dictionary<Tripper.MeshMisc.TileIdentifier, Stopwatch> _tileUsage = new Dictionary<Tripper.MeshMisc.TileIdentifier, Stopwatch>();

		private readonly WowQueryFilter _defaultFilter;
		private readonly WowQueryFilter _hordeFilter;
		private readonly WowQueryFilter _allianceFilter;
		private readonly WowQueryFilter _hordeDeathKnightStartFilter;
		private readonly WowQueryFilter _allianceDeathKnightStartFilter;
		private readonly Dictionary<string, WowQueryFilter> _namedFilters = new Dictionary<string, WowQueryFilter>();
		private WowQueryFilter _queryFilter;

		private uint _mapId;
	}
}
