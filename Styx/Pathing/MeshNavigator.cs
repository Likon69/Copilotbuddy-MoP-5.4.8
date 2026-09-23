using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Tripper.Navigation;
using Tripper.RecastManaged.Detour;
using Tripper.Tools.Math;

namespace Styx.Pathing
{
	public class MeshNavigator : NavigationProvider
	{
		public MeshNavigator()
		{
			this.PathPrecision = 2f;
			this.StuckHandler = new NewStuckHandler(this);
		}

		private static void Nav_OnNavigatorLogMessage(string message)
		{
			Styx.Common.Logging.WriteDiagnostic(Colors.LightBlue, message);
		}

		public override void OnSetAsCurrent()
		{
			if (this._isSetAsCurrent)
			{
				throw new InvalidOperationException("This MeshNavigator instance is already in use");
			}
			base.OnSetAsCurrent();
			this.Nav = new Tripper.Navigation.WowNavigator();
			this.Nav.OnPathFindProgress += this.Nav_OnPathFindProgress;
			Styx.CommonBot.BotEvents.OnBotStarted += this.BotEvents_OnBotStarted;
			Styx.CommonBot.BotEvents.OnBotStopped += this.BotEvents_OnBotStopped;
			Styx.CommonBot.BotEvents.OnPulse += this.BotEvents_OnPulse;
			Styx.CommonBot.Mount.OnMountUp += this.Mount_OnMountUp;
			this.Nav.OnNavigatorLogMessage += MeshNavigator.Nav_OnNavigatorLogMessage;
			this._isSetAsCurrent = true;
		}

		public override void OnRemoveAsCurrent()
		{
			if (!this._isSetAsCurrent)
			{
				throw new InvalidOperationException("This MeshNavigator instance is not in use");
			}
			this.Nav.OnNavigatorLogMessage -= MeshNavigator.Nav_OnNavigatorLogMessage;
			Styx.CommonBot.Mount.OnMountUp -= this.Mount_OnMountUp;
			Styx.CommonBot.BotEvents.OnPulse -= this.BotEvents_OnPulse;
			Styx.CommonBot.BotEvents.OnBotStopped -= this.BotEvents_OnBotStopped;
			Styx.CommonBot.BotEvents.OnBotStarted -= this.BotEvents_OnBotStarted;
			this.Nav.OnPathFindProgress -= this.Nav_OnPathFindProgress;
			this.Nav.Dispose();
			base.OnRemoveAsCurrent();
			this._isSetAsCurrent = false;
		}

		private void BotEvents_OnPulse(Styx.CommonBot.BotEvents.PulseEventArgs args)
		{
			this._factionAreaType = (StyxWoW.Me.IsHorde ? Tripper.MeshMisc.AreaType.Horde : Tripper.MeshMisc.AreaType.Alliance);
			this.UpdateMaps();
		}

		private void BotEvents_OnBotStopped(EventArgs args)
		{
			this.Clear();
		}

		private void BotEvents_OnBotStarted(EventArgs args)
		{
			this.Nav.SetFactionQueryFilter(StyxWoW.Me.IsHorde);
			Styx.Common.Logging.WriteDiagnostic("Set query filter to {0}", new object[] { StyxWoW.Me.IsHorde ? "Horde" : "Alliance" });
		}

		public void UpdateMaps()
		{
			IEnumerable<Styx.WoWInternals.World.JbnMap> maps = StyxWoW.WorldScene.WorldMap.GetMaps();
			IEnumerable<Styx.WoWInternals.World.JbnMap> enumerable = maps.Where(m => m.IsActive);
			List<string> list = enumerable.Select(m => m.MapName).ToList<string>();
			list.Reverse();
			if (this.Nav.Maps != null && list.SequenceEqual(this.Nav.Maps))
			{
				return;
			}
			this.Clear();
			try
			{
				this.Nav.ChangeMap(list);
				Styx.Common.Logging.WriteDiagnostic("Changed map(s) to {0}", new object[] { string.Join(", ", list) });
			}
			catch (ArgumentException ex)
			{
				Styx.Common.Logging.WriteDiagnostic("Could not change maps to {0}! {1}", new object[]
				{
					string.Join(", ", list),
					ex.Message
				});
			}
		}

		public Tripper.Navigation.WowNavigator Nav { get; private set; }

		public MeshMovePath CurrentMovePath { get; internal set; }

		private float PathPrecisionSqr
		{
			get
			{
				float pathPrecision = this.PathPrecision;
				return pathPrecision * pathPrecision;
			}
		}

		public override MoveResult MoveTo(WoWPoint location)
		{
			if (location == WoWPoint.Zero)
			{
				return MoveResult.Failed;
			}
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			if (activeMover == null)
			{
				return MoveResult.Failed;
			}
			WoWPoint location2 = activeMover.Location;
			if (this.IsAtLocation(location2, location))
			{
				Navigator.PlayerMover.MoveTowards(location);
				this.Clear();
				return MoveResult.ReachedDestination;
			}
			if (Styx.CommonBot.Mount.ShouldMount(location))
			{
				Styx.CommonBot.Mount.StateMount(() => location);
			}
			if (Styx.CommonBot.Mount.ShouldMount(location))
			{
				Styx.CommonBot.Mount.StateMount(new Styx.CommonBot.LocationRetriever(() => location));
			}
			if (this._doorTimer.IsFinished)
			{
				IEnumerable<Styx.WoWInternals.WoWObjects.WoWGameObject> enumerable = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWGameObject>(false, false);
				Styx.WoWInternals.WoWObjects.WoWGameObject woWGameObject = enumerable.FirstOrDefault(door =>
					door.SubType == WoWGameObjectType.Door && ((Styx.WoWInternals.WoWObjects.WoWDoor)door.SubObj).IsClosed && !door.Locked && door.WithinInteractRange && door.CanUse() && door.CanUseNow());
				if (woWGameObject != null)
				{
					Styx.Common.Logging.WriteDiagnostic("Opening Closed Door : {0} Locked : {1}", new object[] { woWGameObject.Name, woWGameObject.Locked });
					woWGameObject.Interact();
				}
				this._doorTimer.Reset();
			}
			bool flag = false;
			if (this.CurrentMovePath != null && this.CurrentMovePath.Path.End.DistanceSqr(location) <= this.PathPrecisionSqr)
			{
				if (this._pathValidationTimer.IsFinished && !this.IsStillOnPath(this.CurrentMovePath, location2))
				{
					Styx.WoWInternals.WoWMovement.MoveStop();
					Styx.Common.Logging.WriteDiagnostic("Generating new path because we are not on the old path anymore!");
					flag = true;
					this._pathValidationTimer.Reset();
				}
			}
			else
			{
				flag = true;
			}
			if (!flag)
			{
				return this.MovePath(this.CurrentMovePath);
			}
			WoWPoint woWPoint;
			WoWPoint woWPoint2;
			if (location2.DistanceSqr(location) > 160000f && Styx.CommonBot.FlightPaths.ShouldTakeFlightpath(location2, location, activeMover.MovementInfo.RunSpeed) && Styx.CommonBot.FlightPaths.SetFlightPathUsage(location2, location, out woWPoint, out woWPoint2))
			{
				return MoveResult.PathGenerated;
			}
			Tripper.Navigation.PathFindResult pathFindResult = this.FindPath(location2, location);
			if (!pathFindResult.Succeeded)
			{
				if (pathFindResult.Aborted)
				{
					Styx.Common.Logging.WriteDiagnostic(Colors.Red, "Path search from {0} to {1} was aborted due to combat (time used: {2})", new object[]
					{
						pathFindResult.Start,
						pathFindResult.End,
						MeshNavigator.GetTimeString(pathFindResult.Elapsed)
					});
				}
				else
				{
					Styx.Common.Logging.WriteDiagnostic(Colors.Red, "Could not generate path from {0} to {1} on map {2} (time used: {3}) @ {4}", new object[]
					{
						pathFindResult.Start,
						pathFindResult.End,
						this.LoadedMapNames,
						MeshNavigator.GetTimeString(pathFindResult.Elapsed),
						pathFindResult.FailStep
					});
				}
				return MoveResult.PathGenerationFailed;
			}
			if (pathFindResult.IsPartialPath)
			{
				Styx.Common.Logging.WriteDiagnostic(Colors.Orange, "Could not generate full path from {0} to {1} (time used: {2})", new object[]
				{
					pathFindResult.Start,
					pathFindResult.End,
					MeshNavigator.GetTimeString(pathFindResult.Elapsed)
				});
			}
			else if (pathFindResult.Elapsed.TotalMilliseconds > 50.0)
			{
				Styx.Common.Logging.WriteDiagnostic("Successfully generated path from {0} to {1} in {2}", new object[]
				{
					pathFindResult.Start,
					pathFindResult.End,
					MeshNavigator.GetTimeString(pathFindResult.Elapsed)
				});
			}
			this.CurrentMovePath = new MeshMovePath(pathFindResult);
			this.StuckHandler.Reset();
			this.AdjustPathStartIndex(this.CurrentMovePath, location2);
			this.MovePath(this.CurrentMovePath);
			return MoveResult.PathGenerated;
		}

		private string LoadedMapNames
		{
			get
			{
				if (this.Nav.Maps == null || !this.Nav.Maps.Any<string>())
				{
					return "Unknown";
				}
				string primaryMapName = this.Nav.PrimaryMapName;
				List<string> list = this.Nav.Maps.Where(m => m != primaryMapName).ToList<string>();
				if (!list.Any<string>())
				{
					return primaryMapName;
				}
				return string.Format("{0} (Phase: {1})", primaryMapName, string.Join(", ", list));
			}
		}

		private bool IsSegmentBlocked(PolygonReference startRef, Tripper.Tools.Math.Vector3 start, Tripper.Tools.Math.Vector3 end, out float t)
		{
			bool flag2;
			lock (this.Nav.MeshLock)
			{
				this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(Tripper.Navigation.NavHelper.ToWow(start)));
				this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(Tripper.Navigation.NavHelper.ToWow(end)));
				PolygonReference[] array;
				Tripper.Tools.Math.Vector3 vector;
				if (this.Nav.MeshQuery.Raycast(startRef, start, end, this.Nav.QueryFilter.InternalFilter, 512, out array, out t, out vector).Failed)
				{
					flag2 = true;
				}
				else
				{
					foreach (PolygonReference polygonReference in array)
					{
						byte b;
						if (this.Nav.Mesh.GetPolyArea(polygonReference, out b).Failed)
						{
							return true;
						}
						Tripper.MeshMisc.AreaType areaType = (Tripper.MeshMisc.AreaType)b;
						if (areaType != Tripper.MeshMisc.AreaType.Ground && areaType != Tripper.MeshMisc.AreaType.Water && areaType != Tripper.MeshMisc.AreaType.Lava && areaType != Tripper.MeshMisc.AreaType.Road && areaType != this._factionAreaType)
						{
							return true;
						}
					}
					flag2 = t < 1f;
				}
			}
			return flag2;
		}

		private void AdjustPathStartIndex(MeshMovePath path, Tripper.Tools.Math.Vector3 currentLocation)
		{
			lock (this.Nav.MeshLock)
			{
				this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(currentLocation));
				Tripper.Tools.Math.Vector3 vector = Tripper.Navigation.NavHelper.ToNav(currentLocation);
				PolygonReference polygonReference;
				if (!this.Nav.MeshQuery.FindNearestPolygon(vector, this.Nav.Extents, this.Nav.QueryFilter.InternalFilter, out vector, out polygonReference).Failed && polygonReference.Id != 0UL)
				{
					int num = 1;
					while (num < path.Path.Points.Length && (path.Path.Flags[num - 1] & StraightPathFlags.OffmeshConnection) == 0)
					{
						Tripper.Tools.Math.Vector3 vector2 = path.Path.Points[num];
						Tripper.Tools.Math.Vector3 vector3 = Tripper.Navigation.NavHelper.ToNav(vector2);
						float num2;
						if (this.IsSegmentBlocked(polygonReference, vector, vector3, out num2))
						{
							break;
						}
						num++;
					}
					int num3 = num - 1;
					num3 = Math.Min(num3, 1);
					if (path.Path.Flags[num3].HasFlag(StraightPathFlags.OffmeshConnection) && num3 + 1 < path.Path.Points.Length)
					{
						Tripper.MeshMisc.AreaType areaType = path.Path.PolyTypes[num3];
						if (areaType != Tripper.MeshMisc.AreaType.Elevator && areaType != Tripper.MeshMisc.AreaType.Portal && areaType != Tripper.MeshMisc.AreaType.DefendersPortal && areaType != Tripper.MeshMisc.AreaType.HordePortal && areaType != Tripper.MeshMisc.AreaType.AlliancePortal && areaType != Tripper.MeshMisc.AreaType.InteractUnit && areaType != Tripper.MeshMisc.AreaType.InteractObject)
						{
							Tripper.Tools.Math.Vector3 vector4 = path.Path.Points[num3];
							Tripper.Tools.Math.Vector3 vector5 = path.Path.Points[num3 + 1];
							Tripper.Tools.Math.Vector3 vector6 = MeshNavigator.ClosestPointOnSegment(currentLocation, vector4, vector5);
							if (this.IsAtLocation(currentLocation, vector6))
							{
								num3++;
							}
						}
					}
					if (num3 >= 2)
					{
						Styx.Common.Logging.WriteDiagnostic("Skipped {0} path nodes", new object[] { num3 });
					}
					path.Index = num3;
				}
			}
		}

		private static float PointToSegmentDistanceSqr(Tripper.Tools.Math.Vector3 segStart, Tripper.Tools.Math.Vector3 segEnd, Tripper.Tools.Math.Vector3 point)
		{
			segStart.Z = 0f;
			segEnd.Z = 0f;
			point.Z = 0f;
			if (segStart == segEnd)
			{
				return Tripper.Tools.Math.Vector3.DistanceSqr(ref segStart, ref point);
			}
			Tripper.Tools.Math.Vector3 vector = segEnd - segStart;
			float num = Tripper.Tools.Math.Vector3.Dot(vector, point - segStart);
			if (num < 0f)
			{
				return Tripper.Tools.Math.Vector3.DistanceSqr(ref point, ref segStart);
			}
			float num2 = Tripper.Tools.Math.Vector3.Dot(-vector, point - segEnd);
			if (num2 < 0f)
			{
				return Tripper.Tools.Math.Vector3.DistanceSqr(ref point, ref segEnd);
			}
			return Tripper.Tools.Math.Vector3.Cross(point - segStart, point - segEnd).MagnitudeSqr / (segEnd - segStart).MagnitudeSqr;
		}

		private bool IsStillOnPath(MeshMovePath path, Tripper.Tools.Math.Vector3 currentLocation)
		{
			Styx.WoWInternals.WoWObjects.WoWUnit woWUnit = Styx.WoWInternals.WoWMovement.ActiveMover ?? StyxWoW.Me;
			if (woWUnit.IsFalling)
			{
				return true;
			}
			if (path.Index <= 0 || path.Index >= path.Path.Points.Length)
			{
				return true;
			}
			if ((path.Path.Flags[path.Index - 1] & StraightPathFlags.OffmeshConnection) != 0)
			{
				return true;
			}
			this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(currentLocation));
			Tripper.Tools.Math.Vector3 vector = Tripper.Navigation.NavHelper.ToNav(currentLocation);
			PolygonReference polygonReference;
			if (this.Nav.MeshQuery.FindNearestPolygon(vector, this.Nav.Extents, this.Nav.QueryFilter.InternalFilter, out vector, out polygonReference).Failed || polygonReference.Id == 0UL)
			{
				return true;
			}
			Tripper.Tools.Math.Vector3 vector2 = path.Path.Points[path.Index];
			float num;
			if (!this.IsSegmentBlocked(polygonReference, vector, Tripper.Navigation.NavHelper.ToNav(vector2), out num))
			{
				return true;
			}
			if (Math.Abs(num) < 1E-05f)
			{
				return true;
			}
			Tripper.Tools.Math.Vector3 vector3 = currentLocation + (vector2 - currentLocation) * num;
			if (this.IsAtLocation(vector3, vector2))
			{
				return true;
			}
			Tripper.Tools.Math.Vector3 vector4 = path.Path.Points[path.Index - 1];
			float num2 = MeshNavigator.PointToSegmentDistanceSqr(vector4, vector2, currentLocation);
			return num2 < this.PathPrecisionSqr;
		}

		public Tripper.Navigation.PathFindResult FindPath(WoWPoint start, WoWPoint end)
		{
			this._isInCombat = false;
			Task<Tripper.Navigation.PathFindResult> task = Task<Tripper.Navigation.PathFindResult>.Factory.StartNew(() => this.Nav.FindPath(start, end));
			Tripper.Navigation.PathFindResult pathFindResult;
			try
			{
				if (task.Wait(10))
				{
					pathFindResult = task.Result;
				}
				else
				{
					using (StyxWoW.Memory.ReleaseFrame(true))
					{
						while (!task.Wait(1000 / (int)Styx.CommonBot.TreeRoot.TicksPerSecond))
						{
							try
							{
								using (StyxWoW.Memory.AcquireFrame())
								{
									Styx.WoWInternals.ObjectManager.Update();
									Styx.WoWInternals.WoWMovement.Pulse();
								}
								this._isInCombat = StyxWoW.Me.IsActuallyInCombat || !Styx.CommonBot.TreeRoot.IsRunning;
								StyxWoW.ResetAfk();
							}
							catch (Exception ex)
							{
								Styx.Common.Logging.WriteException(ex);
							}
						}
						pathFindResult = task.Result;
					}
				}
			}
			catch (AggregateException ex2)
			{
				throw new Exception("Failed to generate path", ex2.InnerException);
			}
			finally
			{
				task.Dispose();
			}
			return pathFindResult;
		}

		public override float PathPrecision { get; set; }

		public override bool Clear()
		{
			MeshNavigator._lastElevatorLocation = WoWPoint.Empty;
			this.CurrentMovePath = null;
			this.StuckHandler.Reset();
			return true;
		}

		public override WoWPoint[] GeneratePath(WoWPoint from, WoWPoint to)
		{
			Tripper.Navigation.PathFindResult pathFindResult = this.FindPath(from, to);
			if (!pathFindResult.Succeeded)
			{
				return new WoWPoint[0];
			}
			IEnumerable<Tripper.Tools.Math.Vector3> points = pathFindResult.Points;
			return points.Select(v => (WoWPoint)v).ToArray<WoWPoint>();
		}

		public override bool CanNavigateWithin(WoWPoint from, WoWPoint to, float distanceTolerancy)
		{
			Tripper.Navigation.PathFindResult pathFindResult = this.FindPath(from, to);
			return pathFindResult.Succeeded && pathFindResult.Points.Length > 0 && pathFindResult.Points[pathFindResult.Points.Length - 1].DistanceSqr(to) < distanceTolerancy * distanceTolerancy;
		}

		public override bool CanNavigateFully(WoWPoint from, WoWPoint to)
		{
			Tripper.Navigation.PathFindResult pathFindResult = this.FindPath(from, to);
			return pathFindResult.Succeeded && !pathFindResult.IsPartialPath;
		}

		public override float? PathDistance(WoWPoint from, WoWPoint to, float maxDistance = 3.40282347E+38f)
		{
			Tripper.Navigation.PathFindResult pathFindResult = this.FindPath(from, to);
			if (pathFindResult.Succeeded && !pathFindResult.IsPartialPath)
			{
				float num = from.Distance(pathFindResult.Points[0]);
				num += pathFindResult.Points[pathFindResult.Points.Length - 1].Distance(to);
				for (int i = 0; i < pathFindResult.Points.Length - 1; i++)
				{
					if (num > maxDistance)
					{
						return new float?(maxDistance);
					}
					num += pathFindResult.Points[i].Distance(pathFindResult.Points[i + 1]);
				}
				return new float?(num);
			}
			return null;
		}

		public override bool AtLocation(WoWPoint point1, WoWPoint point2)
		{
			return this.IsAtLocation(point1, point2);
		}

		private void Nav_OnPathFindProgress(object sender, Tripper.Navigation.PathFindProgressEventArgs e)
		{
			if (!this._isInCombat)
			{
				this._firstProgressRunTime = null;
				return;
			}
			if (this._firstProgressRunTime == null)
			{
				this._firstProgressRunTime = new TimeSpan?(e.RunTime);
				return;
			}
			e.Cancel = e.RunTime.Subtract(this._firstProgressRunTime.Value).TotalSeconds > 4.0;
		}

		public virtual MoveResult MovePath(MeshMovePath path)
		{
			if (path == null || path.Path == null || path.Index < 0 || path.Index >= path.Path.Points.Length)
			{
				return MoveResult.Failed;
			}
			this.CurrentHopAbilityFlags = ((path.Index > 0) ? path.Path.AbilityFlags[path.Index - 1] : Tripper.MeshMisc.AbilityFlags.None);
			if (path.Index > 0 && (path.Path.Flags[path.Index - 1] & StraightPathFlags.OffmeshConnection) != 0)
			{
				return this.MoveAlongOffMeshConnection(path);
			}
			return this.MoveTowardsCurrentPoint(path);
		}

		private void Mount_OnMountUp(object sender, Styx.CommonBot.MountUpEventArgs e)
		{
			MeshMovePath currentMovePath = this.CurrentMovePath;
			if (currentMovePath != null && currentMovePath.Index > 0 && currentMovePath.Path.PolyTypes[currentMovePath.Index - 1] == Tripper.MeshMisc.AreaType.Elevator)
			{
				e.Cancel = true;
			}
		}

		private MoveResult MoveAlongOffMeshConnection(MeshMovePath path)
		{
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			if (activeMover == null)
			{
				return MoveResult.Failed;
			}
			Tripper.Tools.Math.Vector3 vector = activeMover.Location;
			Tripper.MeshMisc.AreaType areaType = path.Path.PolyTypes[path.Index - 1];
			if (this.IsAtLocation(vector, path.Path.Points[path.Index]) && areaType != Tripper.MeshMisc.AreaType.Elevator)
			{
				path.Index++;
				return MoveResult.Moved;
			}
			switch (areaType)
			{
			case Tripper.MeshMisc.AreaType.Elevator:
				return this.MoveThroughElevator(vector, path);
			case Tripper.MeshMisc.AreaType.Portal:
			case Tripper.MeshMisc.AreaType.DefendersPortal:
			case Tripper.MeshMisc.AreaType.HordePortal:
			case Tripper.MeshMisc.AreaType.AlliancePortal:
				return this.MoveThroughPortal(vector, path);
			case Tripper.MeshMisc.AreaType.InteractUnit:
				return this.MoveToInteractUnit(vector, path);
			case Tripper.MeshMisc.AreaType.InteractObject:
				return this.MoveToInteractObject(vector, path);
			}
			return this.MoveOffMeshConnectionDefault(vector, path);
		}

		private MoveResult MoveOffMeshConnectionDefault(WoWPoint location, MeshMovePath path)
		{
			if ((ushort)(path.Path.AbilityFlags[path.Index - 1] & (Tripper.MeshMisc.AbilityFlags.Run | Tripper.MeshMisc.AbilityFlags.Jump)) == 0)
			{
				Styx.Common.Logging.WriteDiagnostic("Invalid offmesh connection encountered at {0}", new object[] { location });
				return MoveResult.Failed;
			}
			this.MoveTowardsCurrentPoint(path);
			return MoveResult.Moved;
		}

		private MoveResult MoveThroughElevator(WoWPoint location, MeshMovePath path)
		{
			this.StuckHandler.Reset();
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			IEnumerable<Styx.WoWInternals.WoWObjects.WoWGameObject> enumerable = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWGameObject>(false, false);
			IEnumerable<Styx.WoWInternals.WoWObjects.WoWGameObject> enumerable2 = enumerable.Where(o => o.SubType == WoWGameObjectType.Transport && o.Entry != 20657U && o.Entry != 20656U);
			Styx.WoWInternals.WoWObjects.WoWGameObject woWGameObject = enumerable2.OrderBy(o => o.Distance2DSqr).FirstOrDefault<Styx.WoWInternals.WoWObjects.WoWGameObject>();
			Tripper.Tools.Math.Vector3 vector = path.Path.Points[path.Index];
			Tripper.Tools.Math.Vector3 vector2 = path.Path.Points[path.Index - 1];
			if (woWGameObject == null)
			{
				Styx.Common.Logging.WriteDiagnostic("There is no elevator around. Something is wrong");
				return MoveResult.Failed;
			}
			if (location.DistanceSqr(vector) < 4f && !StyxWoW.Me.IsOnTransport && !StyxWoW.Me.IsFalling)
			{
				path.Index++;
				return MoveResult.Moved;
			}
			WoWPoint location2 = woWGameObject.Location;
			if (MeshNavigator._elevatorCheckTimer.IsFinished)
			{
				MeshNavigator._elevatorMoved = MeshNavigator._lastElevatorLocation.DistanceSqr(location2) > 0.0001f;
				MeshNavigator._lastElevatorLocation = location2;
				MeshNavigator._elevatorCheckTimer.Reset();
			}
			bool flag;
			if (activeMover.IsOnTransport)
			{
				if (Math.Abs(location.Z - vector.Z) <= 2f && !MeshNavigator._elevatorMoved)
				{
					Styx.Common.Logging.WriteDiagnostic("Moving out of elevator");
					Navigator.PlayerMover.MoveTowards(vector);
					return MoveResult.Moved;
				}
				Styx.Common.Logging.WriteDiagnostic("Waiting for elevator to reach end point");
				return MoveResult.Moved;
			}
			else if (!(flag = Math.Abs(vector2.Z - location.Z) > Math.Abs(vector.Z - location.Z)) && (Math.Abs(location2.Z - location.Z) > 2f || MeshNavigator._elevatorMoved))
			{
				if (location.DistanceSqr(vector2) > 4f)
				{
					Styx.Common.Logging.WriteDiagnostic("Woops, we are not in the waiting spot");
					Navigator.PlayerMover.MoveTowards(vector2);
					return MoveResult.Moved;
				}
				if (!activeMover.IsSafelyFacing(woWGameObject, 15f))
				{
					Navigator.PlayerMover.MoveStop();
					Styx.Common.Logging.WriteDiagnostic("Facing towards elevator");
					StyxWoW.Me.SetFacing(woWGameObject);
				}
				Styx.Common.Logging.WriteDiagnostic("Waiting for the elevator");
				return MoveResult.Moved;
			}
			else
			{
				if (!flag)
				{
					Styx.Common.Logging.WriteDiagnostic("Moving inside elevator");
					if (activeMover.Mounted)
					{
						Styx.CommonBot.Mount.Dismount("Moving inside transport.");
					}
					Navigator.PlayerMover.MoveTowards(woWGameObject.Location);
					return MoveResult.Moved;
				}
				return MoveResult.Moved;
			}
		}

		private MoveResult MoveToInteractObject(WoWPoint location, MeshMovePath path)
		{
			this.StuckHandler.Reset();
			if (!MeshNavigator._interactCooldownTimer.IsFinished)
			{
				return MoveResult.Moved;
			}
			IEnumerable<Styx.WoWInternals.WoWObjects.WoWGameObject> enumerable = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWGameObject>(false, false).OrderBy(o => o.Location.DistanceSqr(path.Path.Points[path.Index - 1]));
			Styx.WoWInternals.WoWObjects.WoWGameObject woWGameObject = enumerable.FirstOrDefault(o => o.CanUseNow() && !o.InUse);
			if (woWGameObject == null)
			{
				path.Index++;
				return MoveResult.Moved;
			}
			if (!woWGameObject.WithinInteractRange)
			{
				Navigator.PlayerMover.MoveTowards(woWGameObject.Location);
				return MoveResult.Moved;
			}
			Styx.CommonBot.Mount.Dismount("InteractObject in path dismounting");
			woWGameObject.Interact();
			MeshNavigator._interactCooldownTimer.Reset();
			return MoveResult.Moved;
		}

		private MoveResult MoveToInteractUnit(WoWPoint location, MeshMovePath path)
		{
			this.StuckHandler.Reset();
			if (!MeshNavigator._interactCooldownTimer.IsFinished)
			{
				return MoveResult.Moved;
			}
			IEnumerable<Styx.WoWInternals.WoWObjects.WoWUnit> enumerable = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWUnit>(false, false).OrderBy(u => u.Location.DistanceSqr(path.Path.Points[path.Index - 1]));
			Styx.WoWInternals.WoWObjects.WoWUnit woWUnit = enumerable.FirstOrDefault(u => u.ControllingPlayer == null);
			if (woWUnit == null)
			{
				Styx.Common.Logging.WriteDiagnostic("Could not find unit to interact with.");
				return MoveResult.Failed;
			}
			if (!woWUnit.WithinInteractRange)
			{
				Navigator.PlayerMover.MoveTowards(woWUnit.Location);
				return MoveResult.Moved;
			}
			Styx.CommonBot.Mount.Dismount("InteractUnit in path dismounting");
			woWUnit.Interact();
			MeshNavigator._interactCooldownTimer.Reset();
			return MoveResult.Moved;
		}

		private MoveResult MoveThroughPortal(WoWPoint location, MeshMovePath path)
		{
			this.StuckHandler.Reset();
			float num = float.MaxValue;
			Styx.WoWInternals.WoWObjects.WoWGameObject woWGameObject = null;
			foreach (Styx.WoWInternals.WoWObjects.WoWGameObject woWGameObject2 in Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWGameObject>(false, false))
			{
				if (woWGameObject2.SubType == WoWGameObjectType.Goober || woWGameObject2.SubType == WoWGameObjectType.SpellCaster)
				{
					float num2 = woWGameObject2.Location.DistanceSqr(location);
					if (num2 < num)
					{
						num = num2;
						woWGameObject = woWGameObject2;
					}
				}
			}
			if (!(woWGameObject == null) && woWGameObject.WithinInteractRange)
			{
				Styx.Common.Logging.WriteDiagnostic("Interacting with:{0}", new object[] { woWGameObject.Name });
				woWGameObject.Interact();
				return MoveResult.Moved;
			}
			Styx.Common.Logging.WriteDiagnostic("Could not find portal to take.");
			return MoveResult.Failed;
		}

		public Tripper.MeshMisc.AbilityFlags CurrentHopAbilityFlags { get; private set; }

		private MoveResult MoveTowardsCurrentPoint(MeshMovePath path)
		{
			if (Navigator.NavigationProvider.StuckHandler.IsStuck())
			{
				Navigator.NavigationProvider.StuckHandler.Unstick();
				return MoveResult.UnstuckAttempt;
			}
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			if (activeMover == null)
			{
				return MoveResult.Failed;
			}
			Tripper.Tools.Math.Vector3 vector = activeMover.Location;
			while (this.IsAtLocation(vector, path.Path.Points[path.Index]))
			{
				if (++path.Index >= path.Path.Points.Length || (path.Path.Flags[path.Index - 1] & StraightPathFlags.OffmeshConnection) != 0)
				{
					return MoveResult.Moved;
				}
				this.StuckHandler.Reset();
				this.MoveCurrentPoint(path);
			}
			this.MoveCurrentPoint(path);
			return MoveResult.Moved;
		}

		private void MoveCurrentPoint(MeshMovePath path)
		{
			if (path.Index >= 0 && path.Index < path.Path.Points.Length)
			{
				Tripper.MeshMisc.AreaType areaType = path.Path.PolyTypes[path.Index];
				Tripper.Tools.Math.Vector3 vector = path.Path.Points[path.Index];
				if (areaType == Tripper.MeshMisc.AreaType.Lava || areaType == Tripper.MeshMisc.AreaType.Water)
				{
					vector.Z += 2f;
				}
				if (Navigator.PlayerMover is ClickToMoveMover && path.Index != 0 && path.Index != path.Path.Points.Length - 1)
				{
					this.AdjustMovePoint(ref vector);
				}
				Navigator.PlayerMover.MoveTowards(vector);
				return;
			}
		}

		private void AdjustMovePoint(ref Tripper.Tools.Math.Vector3 point)
		{
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			if (activeMover == null)
			{
				return;
			}
			Tripper.Tools.Math.Vector3 vector = activeMover.Location;
			Tripper.Tools.Math.Vector3 vector2 = point - vector;
			vector2.Normalize();
			Tripper.Tools.Math.Vector3 vector3 = point + vector2 * Navigator.PathPrecision;
			this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(vector));
			this.Nav.LoadTile(Tripper.MeshMisc.TileIdentifier.GetByPosition(vector3));
			Tripper.Tools.Math.Vector3 vector4;
			PolygonReference polygonReference;
			if (this.Nav.MeshQuery.FindNearestPolygon(Tripper.Navigation.NavHelper.ToNav(point), new Tripper.Tools.Math.Vector3(0.5f, 3f, 0.5f), this.Nav.QueryFilter.InternalFilter, out vector4, out polygonReference).Succeeded && polygonReference.Id != 0UL)
			{
				PolygonReference[] array;
				float num;
				Tripper.Tools.Math.Vector3 vector5;
				if (this.Nav.MeshQuery.Raycast(polygonReference, vector4, Tripper.Navigation.NavHelper.ToNav(vector3), this.Nav.QueryFilter.InternalFilter, 64, out array, out num, out vector5).Succeeded)
				{
					if (num == 3.40282347E+38f)
					{
						point = vector3;
						return;
					}
				}
				return;
			}
		}

		private bool IsAtLocation(Tripper.Tools.Math.Vector3 point1, Tripper.Tools.Math.Vector3 point2)
		{
			return MeshNavigator.Distance2DSqr(ref point1, ref point2) <= this.PathPrecisionSqr && Math.Abs(point1.Z - point2.Z) < 4.5f;
		}

		private static float Distance2DSqr(ref Tripper.Tools.Math.Vector3 v1, ref Tripper.Tools.Math.Vector3 v2)
		{
			float num = v1.X - v2.X;
			float num2 = v1.Y - v2.Y;
			return num * num + num2 * num2;
		}

		private static Tripper.Tools.Math.Vector3 ClosestPointOnSegment(Tripper.Tools.Math.Vector3 point, Tripper.Tools.Math.Vector3 segStart, Tripper.Tools.Math.Vector3 segEnd)
		{
			float num = segStart.DistanceSqr(segEnd);
			if ((double)num == 0.0)
			{
				return segStart;
			}
			WoWPoint woWPoint = point - segStart;
			WoWPoint woWPoint2 = segEnd - segStart;
			float num2 = Tripper.Tools.Math.Vector3.Dot(woWPoint, woWPoint2) / num;
			if (num2 < 0f)
			{
				return segStart;
			}
			if (num2 > 1f)
			{
				return segEnd;
			}
			return new WoWPoint(segStart.X + woWPoint2.X * num2, segStart.Y + woWPoint2.Y * num2, segStart.Z + woWPoint2.Z * num2);
		}

		private static string GetTimeString(TimeSpan timeSpan)
		{
			if (timeSpan.TotalDays >= 1.0)
			{
				return string.Format("{0} days, {1} hours, {2} minutes and {3} seconds", new object[]
				{
					timeSpan.Days,
					timeSpan.Hours,
					timeSpan.Minutes,
					Math.Round((double)((float)timeSpan.Seconds + (float)timeSpan.Milliseconds / 1000f), 3)
				});
			}
			if (timeSpan.TotalHours >= 1.0)
			{
				return string.Format("{0} hours, {1} minutes and {2} seconds", timeSpan.Hours, timeSpan.Minutes, Math.Round((double)((float)timeSpan.Seconds + (float)timeSpan.Milliseconds / 1000f), 3));
			}
			if (timeSpan.TotalMinutes >= 1.0)
			{
				return string.Format("{0} minutes and {1} seconds", timeSpan.Minutes, Math.Round((double)((float)timeSpan.Seconds + (float)timeSpan.Milliseconds / 1000f), 3));
			}
			if (timeSpan.TotalSeconds >= 1.0)
			{
				return string.Format("{0} seconds", Math.Round(timeSpan.TotalSeconds, 3));
			}
			return string.Format("{0} milliseconds", Math.Round(timeSpan.TotalMilliseconds, 0));
		}

		private Tripper.MeshMisc.AreaType _factionAreaType;

		private bool _isSetAsCurrent;

		private readonly Styx.Common.Helpers.WaitTimer _doorTimer = new Styx.Common.Helpers.WaitTimer(TimeSpan.FromSeconds(1.0));

		private Styx.Common.Helpers.WaitTimer _pathValidationTimer = Styx.Common.Helpers.WaitTimer.FiveSeconds;

		private volatile bool _isInCombat;

		private TimeSpan? _firstProgressRunTime;

		private static bool _elevatorMoved;

		private static WoWPoint _lastElevatorLocation = WoWPoint.Empty;

		private static readonly Styx.Common.Helpers.WaitTimer _elevatorCheckTimer = new Styx.Common.Helpers.WaitTimer(TimeSpan.FromMilliseconds(400.0));

		private static readonly Styx.Common.Helpers.WaitTimer _interactCooldownTimer = new Styx.Common.Helpers.WaitTimer(TimeSpan.FromMilliseconds(2000.0));
	}
}
