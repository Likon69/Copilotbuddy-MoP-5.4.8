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
	/// <summary>
	/// 1:1 API-shape port of Honorbuddy 5.4.8's <c>Tripper.Navigation.WowNavigator</c>
	/// (see "hb decompile\.hb 5.4.8\Honorbuddy\Tripper\Navigation\WowNavigator.cs", names
	/// recovered against the unobfuscated
	/// "hb decompile\.hb 5.3\Tripper\Navigation\WowNavigator.cs"). Only the members that
	/// Styx/Pathing/MeshNavigator.cs and Bots/ actually call are ported (verified by the
	/// caller of this port against every WowNavigator/Nav.* call site) — HB's
	/// MeshProvider/IMeshProvider, the named-filter store (HasQueryFilter/
	/// StoreQueryFilter/GetStoredQueryFilter/SetQueryFilterByStored), the
	/// OnTileLoaded/OnSubTileLoaded/OnMapLoaded events and the static
	/// GetNewDefaultQueryFilter/GetNewFactionQueryFilter helpers are all intentionally
	/// left out; every method body below is rewired onto Navigation.dll via
	/// Styx.Pathing.NavigationApi.
	///
	/// Deliberate deviations from HB, all authorized up front:
	///  - MeshProvider is not ported. HB's only implementation (ns7.Class711) pulled in
	///    Buddy.Auth (forbidden) to fetch tiles from a mesh server (dead). Tile supply is
	///    now native (MoveMap.cpp), reached only through NavigationApi.
	///  - Map identity: HB's Maps/ChangeMap/PrimaryMapName deal in map *names* (matching
	///    MeshNavigator.cs's `list.SequenceEqual(this.Nav.Maps)`); Navigation.dll deals in
	///    a numeric mapId. The public shape stays name-based; internally a `uint _mapId`
	///    is refreshed from StyxWoW.Me.MapId every time ChangeMap actually changes the map
	///    list, and pushed into Mesh.MapId / MeshQuery.MapId.
	///  - SetFactionQueryFilter: see its own XML doc.
	///  - FindPath: see its own XML doc.
	/// </summary>
	public class WowNavigator : IDisposable
	{
		public WowNavigator()
		{
			this.MeshLock = new object();
			this.GarbageCollectTime = TimeSpan.FromMinutes(1.0);
			this.Extents = new Vector3(3f, 20f, 3f);
			this._defaultFilter = BuildDefaultQueryFilter();
			this._hordeFilter = BuildFactionQueryFilter(true);
			this._allianceFilter = BuildFactionQueryFilter(false);
			this.ResetQueryFilter();
		}

		public object MeshLock { get; private set; }

		public event EventHandler<PathFindProgressEventArgs> OnPathFindProgress;

		public event NavigatorLogMessage OnNavigatorLogMessage;

		public WowQueryFilter QueryFilter
		{
			get { return this._queryFilter; }
		}

		public NavMesh Mesh { get; private set; }

		public NavMeshQuery MeshQuery { get; private set; }

		public Vector3 Extents { get; set; }

		/// <summary>
		/// Kept for API-shape fidelity only. HB's RandomizePaths drove a whole family of
		/// private helpers (method_6 through method_16 in the 5.4.8 decompile) that pushed
		/// path points a few units off the straight line using low-level
		/// NavMeshQuery.FindDistanceToWall/Raycast/FindPath(multi-poly)/ClosestPointOnPoly
		/// calls — none of which exist on this port's NavMeshQuery (only
		/// FindNearestPolygon, QueryPolygons and Raycast were ported; see NavMeshQuery.cs).
		/// FindPath here is a single NavigationApi.CalculatePathEx call with no
		/// post-processing hook, so setting this property has no observable effect. Left
		/// settable (not removed) because it is public HB API surface.
		/// </summary>
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

		/// <summary>
		/// Rewired: HB rebuilt Mesh/MeshQuery from MeshProvider.IsTiled/GetTile and called
		/// MeshQuery.Init/SetTileLoaderFunction (5.4.8 decompile lines 625-682). Those
		/// members do not exist on this port's NavMesh/NavMeshQuery (see their class
		/// summaries: both are stateless wrappers holding only a MapId, with no unmanaged
		/// resource of their own — Navigation.dll owns the actual mesh data, loaded
		/// separately via NavigationApi.LoadMaps()). So this only tracks the map-name list
		/// for HB API compatibility (MeshNavigator.cs compares against it with
		/// SequenceEqual) and refreshes the numeric map id from StyxWoW.Me.MapId — see the
		/// class summary's "Map identity" note — before (re)creating plain Mesh/MeshQuery
		/// instances bound to that id.
		/// </summary>
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
					}
					else
					{
						this.Mesh = null;
						this.MeshQuery = null;
					}
				}
			}
		}

		/// <summary>
		/// Rewired: HB's tile decompression (MeshProvider.GetTile + Mesh.AddTile, 5.4.8
		/// decompile's method_4) has no native equivalent to call into — Navigation.dll
		/// loads .mmtile data itself (MoveMap.cpp) and this port's NavMesh exposes no
		/// AddTile/GetTileAt (see NavMesh.cs: only GetPolyArea was ported). This instead
		/// calls NavigationApi.EnsureTiles for the world point at the tile's center (see
		/// TileCenter) with ring 0, then confirms the load via NavigationApi.IsTileLoaded
		/// (converted to Navigation.dll's own tile numbering via NativeTileIndex) since
		/// EnsureTiles itself returns void and gives no success/failure signal.
		/// </summary>
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

		/// <summary>
		/// Rewired: HB's UnloadTile (5.4.8 decompile lines 744-777) walked the four Detour
		/// sub-tiles of the given WoW tile through Mesh.GetTileRefAt/RemoveTile — members
		/// this port's NavMesh does not expose (only GetPolyArea; see NavMesh.cs). This
		/// instead calls NavigationApi.UnloadTile with the tile converted to Navigation.
		/// dll's own numbering (see NativeTileIndex) and drops the managed usage-time entry.
		/// </summary>
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

		/// <summary>
		/// Rewired: HB's UnloadAllTiles (5.4.8 decompile lines 728-742) enumerated
		/// Mesh.GetAllTiles()/RemoveTile — not exposed by this port's NavMesh (see
		/// NavMesh.cs). This instead walks the managed tile-usage tracking dictionary
		/// (the same one LoadTile/UnloadTile/GarbageCollectTiles maintain) and calls
		/// NavigationApi.UnloadTile for each.
		/// </summary>
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

		/// <summary>
		/// Rewired: HB's GarbageCollectTiles (5.4.8 decompile lines 404-438) swept its own
		/// TileIdentifier-&gt;Stopwatch dictionary and called the (now-rewired) UnloadTile
		/// for every entry older than GarbageCollectTime. Same algorithm here, over the
		/// same kind of managed usage-time dictionary, calling this port's
		/// NavigationApi-backed UnloadTile. Native side note: Navigation.cpp separately
		/// runs its own time-based tile GC (TILE_GC_TIMEOUT_MS = 60000, i.e. 1 minute,
		/// matching this class's own GarbageCollectTime default) automatically inside
		/// every CalculatePathEx call — this method is the managed-side counterpart for
		/// tiles this navigator loaded directly via LoadTile.
		/// </summary>
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

		/// <summary>
		/// Deliberate deviation from Honorbuddy 5.4.8 (decompile lines 888-906,
		/// smethod_0/smethod_1): HB excluded the opposing faction by OR-ing
		/// AbilityFlags.Horde/Alliance into QueryFilter.ExcludeFlags. That cannot work
		/// here: this extractor stamps a constant walkable-ability flag of 0x0001 on every
		/// walkable polygon (see the porting brief), so no AbilityFlags.Horde/Alliance bit
		/// is ever set on a poly for ExcludeFlags to match. Faction is carried by AREA TYPE
		/// instead: Tripper.MeshMisc.AreaType.Horde=15, .Alliance=16, .HordePortal=10,
		/// .AlliancePortal=11 match the extractor's NAV_HORDE/NAV_ALLIANCE/
		/// NAV_HORDE_PORTAL/NAV_ALLIANCE_PORTAL area ids value for value. So this sets a
		/// prohibitive area cost (ProhibitiveFactionAreaCost = 1000, an order of magnitude
		/// past the "Blocked" area's own 100 default cost — see BuildDefaultQueryFilter —
		/// so it is strongly avoided but, being a cost rather than a hard exclusion, still
		/// finite: a route through enemy territory can still be found if it is the only
		/// one, same as HB's own Blocked/Blackspot areas were never literally impassable)
		/// on the OPPOSING faction's area types, leaving the player's own faction's areas
		/// at the normal default cost (1.66). Precomputed once per faction in the
		/// constructor (_hordeFilter/_allianceFilter), mirroring HB's own
		/// dictionary_0["Horde"]/["Alliance"] cache.
		/// </summary>
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

		/// <summary>
		/// Rewired: HB's FindPath (5.4.8 decompile lines 441-618) drove Detour directly —
		/// two FindNearestPolygon calls, InitSlicedFindPath, a manual UpdateSlicedFindPath
		/// loop with OnPathFindProgress-based abort, FinalizeSlicedFindPath, a partial-path
		/// ClosestPointOnPoly snap, FindStraightPath, then RandomizePaths/
		/// MovePathAwayFromEdges post-processing — against NavMeshQuery members
		/// (InitSlicedFindPath/UpdateSlicedFindPath/FinalizeSlicedFindPath/
		/// FindStraightPath/ClosestPointOnPoly/FindDistanceToWall/FindPath(multi-poly))
		/// that this port's NavMeshQuery does not expose (only FindNearestPolygon,
		/// QueryPolygons and Raycast were ported; see NavMeshQuery.cs). Navigation.dll does
		/// the entire find-nearest-poly + sliced-find + finalize + partial-path snap +
		/// straight-path pipeline in one call (Navigation.cpp's FindPathEx), reached here
		/// through NavigationApi.CalculatePathEx.
		///
		/// No NavHelper/GraphicalHelper conversion is applied: HB's own FindPath converted
		/// its inputs with NavHelper.ToNav (decompile lines 447-448) and its output points
		/// back with NavHelper.ToWow (line 565) specifically because it talked to
		/// NavMeshQuery, which speaks Detour space. CalculatePathEx instead takes and
		/// returns raw WoW-space points directly (see NavMeshQuery.cs's "Coordinate space"
		/// note and NativeMethods.CalculatePathEx/NativeMethods.XYZ_C's implicit WoWPoint
		/// operator) — converting here would double-convert.
		///
		/// One abort/progress hook is lost as a result: OnPathFindProgress can no longer
		/// fire mid-search (there is no iterative loop left to call it from), so
		/// PathFindResult.Aborted is always false and the event, while still exposed for
		/// API-shape fidelity, is never raised by this method.
		/// </summary>
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
				WoWPoint startPoint = start;
				WoWPoint endPoint = end;
				NavPathResult navResult = NavigationApi.CalculatePathEx(this._mapId, startPoint, endPoint, true);
				stopwatch.Stop();
				result.Elapsed = stopwatch.Elapsed;

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
					points[i] = navResult.Points[i];
				}
				result.Points = points;

				StraightPathFlags[] flags = new StraightPathFlags[navResult.StraightPathFlags.Length];
				for (int i = 0; i < flags.Length; i++)
				{
					flags[i] = (StraightPathFlags)navResult.StraightPathFlags[i];
				}
				result.Flags = flags;

				Tripper.MeshMisc.AreaType[] polyTypes = new Tripper.MeshMisc.AreaType[navResult.PolyTypes.Length];
				for (int i = 0; i < polyTypes.Length; i++)
				{
					polyTypes[i] = (Tripper.MeshMisc.AreaType)navResult.PolyTypes[i];
				}
				result.PolyTypes = polyTypes;

				Tripper.MeshMisc.AbilityFlags[] abilityFlags = new Tripper.MeshMisc.AbilityFlags[navResult.AbilityFlags.Length];
				for (int i = 0; i < abilityFlags.Length; i++)
				{
					abilityFlags[i] = (Tripper.MeshMisc.AbilityFlags)navResult.AbilityFlags[i];
				}
				result.AbilityFlags = abilityFlags;

				PolygonReference[] polygons = new PolygonReference[navResult.PolyRefs.Length];
				for (int i = 0; i < polygons.Length; i++)
				{
					polygons[i] = new PolygonReference(navResult.PolyRefs[i]);
				}
				result.Polygons = polygons;

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

		/// <summary>
		/// Matches HB's GetNewDefaultQueryFilter + SetDefaultQueryFilterCosts (5.4.8
		/// decompile lines 849-876). Numeric constants preserved exactly.
		/// </summary>
		private static WowQueryFilter BuildDefaultQueryFilter()
		{
			WowQueryFilter filter = new WowQueryFilter(Tripper.MeshMisc.AbilityFlags.All, Tripper.MeshMisc.AbilityFlags.Unwalkable | Tripper.MeshMisc.AbilityFlags.Transport);
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
			return filter;
		}

		/// <summary>See SetFactionQueryFilter's XML doc for the area-cost deviation this implements.</summary>
		private const float ProhibitiveFactionAreaCost = 1000f;

		private static WowQueryFilter BuildFactionQueryFilter(bool isHorde)
		{
			WowQueryFilter filter = BuildDefaultQueryFilter();
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

		/// <summary>
		/// Maps Navigation.dll's NavPathFindStep (Navigation C++/Navigation/NavStatus.h:
		/// FIND_START_POLY=0, FIND_END_POLY=1, INIT_PATHFIND=2, UPDATE_PATHFIND=3,
		/// FINALIZE_PATHFIND=4, FIND_STRAIGHT_PATH=5, NONE=-1) onto this port's
		/// Tripper.Navigation.PathFindStep (None=0, FindStartPoly=1, FindEndPoly=2,
		/// InitPathFind=3, UpdatePathFind=4, FinalizePathFind=5,
		/// SnapPartialPathToEnd=6, FindStraightPath=7). The two enums do NOT share numeric
		/// values (PathFindStep.None is 0, not -1; every other member is offset by one; and
		/// SnapPartialPathToEnd has no native counterpart at all since the partial-path
		/// snap now happens inside Navigation.dll itself), so this is an explicit lookup
		/// rather than a cast.
		/// </summary>
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

		/// <summary>
		/// Derives a WoW-space point at the center of the ADT tile <paramref name="ti"/>
		/// identifies, to feed NavigationApi.EnsureTiles (which, like CalculatePathEx,
		/// takes raw un-permuted WoW coordinates — see NativeMethods.XYZ_C's implicit
		/// WoWPoint operator). This is the algebraic inverse of the unmodified
		/// Tripper.MeshMisc.TileIdentifier.GetByPosition(x, y): X = 32 - Ceiling(y /
		/// 533.3333), Y = 32 - Ceiling(x / 533.3333). Using the tile's center (0.5 tiles in
		/// from either edge) keeps the point safely away from both edges, so the
		/// Ceiling-vs-truncate difference between that formula and Navigation.dll's own
		/// WorldToTile ("Navigation C++/Navigation/Navigation.cpp":
		/// tileX = (int)((32*533.33333f - worldX) / 533.33333f), same for tileY/worldY)
		/// cannot push it into the wrong tile.
		/// </summary>
		private static WoWPoint TileCenter(Tripper.MeshMisc.TileIdentifier ti)
		{
			const float TileSize = 533.3333f;
			float worldX = (32f - ti.Y - 0.5f) * TileSize;
			float worldY = (32f - ti.X - 0.5f) * TileSize;
			return new WoWPoint(worldX, worldY, 0f);
		}

		/// <summary>
		/// Converts a TileIdentifier (HB's WoW-ADT tile numbering, X from world y and Y
		/// from world x — see TileCenter) to the (x, y) indices Navigation.dll's
		/// IsTileLoaded_C/UnloadTile_C expect. Navigation::WorldToTile computes tileX from
		/// worldX and tileY from worldY directly, with no axis swap (see TileCenter's XML
		/// doc for the exact formula); substituting TileCenter's worldX/worldY back into
		/// that formula shows nativeTileX == ti.Y and nativeTileY == ti.X exactly.
		/// </summary>
		private static void NativeTileIndex(Tripper.MeshMisc.TileIdentifier ti, out int nativeX, out int nativeY)
		{
			nativeX = ti.Y;
			nativeY = ti.X;
		}

		private readonly Dictionary<Tripper.MeshMisc.TileIdentifier, Stopwatch> _tileUsage = new Dictionary<Tripper.MeshMisc.TileIdentifier, Stopwatch>();

		private readonly WowQueryFilter _defaultFilter;
		private readonly WowQueryFilter _hordeFilter;
		private readonly WowQueryFilter _allianceFilter;
		private WowQueryFilter _queryFilter;

		/// <summary>
		/// See the class summary's "Map identity" note: refreshed from StyxWoW.Me.MapId
		/// every time ChangeMap actually changes the map list, and pushed into Mesh.MapId /
		/// MeshQuery.MapId.
		/// </summary>
		private uint _mapId;
	}
}
