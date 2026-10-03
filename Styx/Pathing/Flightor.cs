using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Pathing.FlightorNavigation;
using Styx.Resources;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

namespace Styx.Pathing
{
	public static class Flightor
	{
		static Flightor()
		{
			Styx.CommonBot.BotEvents.OnBotStopped += Flightor.OnBotStopped;
		}

		private static void OnBotStopped(EventArgs eventArgs)
		{
			Flightor.Clear();
		}

		public static bool CanFly
		{
			get
			{
				if (!Flightor._flyableMapIds.Contains(StyxWoW.Me.MapId))
				{
					return false;
				}
				Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
				return !(activeMover == null) && (activeMover.MovementInfo.CanFly || (!StyxWoW.Me.IsGhost && activeMover.IsMe && !StyxWoW.Me.InVehicle && !StyxWoW.Me.IsOnTransport && (Styx.CommonBot.SpellManager.HasSpell("Flight Master's License") || (StyxWoW.Me.MapId != 0U && StyxWoW.Me.MapId != 1U)) && (Styx.CommonBot.SpellManager.HasSpell("Expert Riding") || Styx.CommonBot.SpellManager.HasSpell("Artisan Riding") || Styx.CommonBot.SpellManager.HasSpell("Master Riding") || StyxWoW.Me.MapId != 530U) && (Styx.CommonBot.SpellManager.HasSpell("Cold Weather Flying") || StyxWoW.Me.MapId != 571U) && (Styx.CommonBot.SpellManager.HasSpell("Wisdom of the Four Winds") || Styx.CommonBot.SpellManager.HasSpell("Cloud Serpent Riding") || StyxWoW.Me.MapId != 870U) && !StyxWoW.Me.HasAura("Temporal Anomaly") && (Styx.CommonBot.Mount.FlyingMounts.Count != 0 || (StyxWoW.Me.Class == WoWClass.Druid && (Styx.CommonBot.SpellManager.HasSpell("Flight Form") || Styx.CommonBot.SpellManager.HasSpell("Swift Flight Form")))) && (StyxWoW.Me.Level >= 60 || StyxWoW.Me.Class == WoWClass.Druid) && (StyxWoW.Me.Level >= 58 || StyxWoW.Me.Class != WoWClass.Druid)));
			}
		}

		private static void CancelMountUp(object sender, Styx.CommonBot.MountUpEventArgs e)
		{
			e.Cancel = true;
		}

		private static void MoveToLaunchSpot()
		{
			try
			{
				Styx.CommonBot.Mount.OnMountUp += Flightor.CancelMountUp;
				Navigator.MoveTo(Flightor._launchSpot);
			}
			finally
			{
				Styx.CommonBot.Mount.OnMountUp -= Flightor.CancelMountUp;
			}
		}

		private static Styx.WoWInternals.WoWObjects.WoWObject FindTakeoffPlatform(float height)
		{
			float num = Navigator.PathPrecision;
			num *= num;
			WoWPoint location = StyxWoW.Me.Location;
			IEnumerable<Styx.WoWInternals.WoWObjects.WoWObject> candidates = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWObject>(true, false)
				.Where(o => o is Styx.WoWInternals.WoWObjects.WoWUnit || o is Styx.WoWInternals.WoWObjects.WoWGameObject);
			foreach (Styx.WoWInternals.WoWObjects.WoWObject woWObject in candidates.OrderBy(o => o.DistanceSqr))
			{
				if (woWObject.DistanceSqr >= (double)num && !Styx.CommonBot.Blacklist.Contains(woWObject, Styx.CommonBot.BlacklistFlags.Interact) && !Styx.CommonBot.Mount.IsNearCantMountSpot(woWObject.Location) && woWObject.IsOutdoors && Navigator.CanNavigateWithin(location, woWObject.Location, Navigator.PathPrecision) && Styx.WoWInternals.World.GameWorld.IsInLineOfSight(woWObject.Location.Add(0f, 0f, 2f), woWObject.Location.Add(0f, 0f, height)))
				{
					return woWObject;
				}
			}
			return null;
		}

		public static void MoveTo(WoWPoint destination, bool checkIndoors)
		{
			Flightor.MoveTo(destination, 40f, checkIndoors);
		}

		public static void MoveTo(WoWPoint destination, float minHeight = 40f, bool checkIndoors = false)
		{
			bool hasSeaLegs = StyxWoW.Me.HasAura("Sea Legs");
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			bool isVashjir = StyxWoW.Me.MapId == 646U;
			if (activeMover == null)
			{
				return;
			}
			if (!Flightor.CanFly)
			{
				Navigator.MoveTo(destination);
				return;
			}
			if (StyxWoW.Me.Combat && !Flightor.MountHelper.Mounted && !hasSeaLegs)
			{
				Navigator.MoveTo(destination);
				return;
			}
			if (!Flightor.MountHelper.Mounted && !hasSeaLegs && destination.Distance(StyxWoW.Me.Location) < Styx.CommonBot.Mount.MountDistance && Navigator.CanNavigateWithin(StyxWoW.Me.Location, destination, Navigator.PathPrecision) && !activeMover.IsSwimming)
			{
				float? num = Navigator.PathDistance(StyxWoW.Me.Location, destination, Styx.CommonBot.Mount.MountDistance);
				if (num != null && num.Value < Styx.CommonBot.Mount.MountDistance)
				{
					Navigator.MoveTo(destination);
					return;
				}
			}
			WoWPoint myLoc = StyxWoW.Me.Location;
			WoWPoint upLoc = myLoc.Add(0f, 0f, StyxWoW.Me.BoundingHeight);
			bool isOutdoors = StyxWoW.Me.IsOutdoors;
			if (checkIndoors && !hasSeaLegs)
			{
				if (Flightor._indoorLandingDestination != WoWPoint.Empty && Flightor._indoorLandingDestination.DistanceSqr(destination) > 900f)
				{
					Flightor._indoorLandingSpot = WoWPoint.Empty;
					Flightor._dismountedForLanding = false;
					Flightor._indoorLandingDestination = WoWPoint.Empty;
					return;
				}
				if (Flightor._indoorLandingSpot != WoWPoint.Empty)
				{
					if (!Flightor._dismountedForLanding)
					{
						if (Flightor._indoorLandingSpot.DistanceSqr(myLoc) < 16f)
						{
							IEnumerable<float> enumerable = (Navigator.FindHeights(StyxWoW.Me.X, StyxWoW.Me.Y) ?? new List<float>()).Where(h => h <= StyxWoW.Me.Z);
							float num2 = enumerable.OrderByDescending(h => h).FirstOrDefault();
							WoWPoint woWPoint2 = new WoWPoint(StyxWoW.Me.X, StyxWoW.Me.Y, num2);
							if (Math.Abs(num2 - Flightor._indoorLandingSpot.Z) < 3f && Navigator.CanNavigateWithin(Flightor._indoorLandingSpot, woWPoint2, 5f))
							{
								Flightor._dismountedForLanding = true;
								Styx.CommonBot.Mount.Dismount("Dismounting for landing spot.");
							}
						}
						Flightor.MoveTo(Flightor._indoorLandingSpot, 40f, false);
						return;
					}
					Navigator.MoveTo(destination);
					return;
				}
				else
				{
					float num3 = (isVashjir ? 250f : 150f);
					if (destination.Distance2DSqr(myLoc) <= num3 * num3 && !Styx.WoWInternals.World.GameWorld.IsInLineOfSight(destination.Add(0f, 0f, 2f), destination.Add(0f, 0f, 200f)))
					{
						if ((!isOutdoors || !Styx.WoWInternals.World.GameWorld.IsInLineOfSight(upLoc, myLoc.Add(0f, 0f, 200f))) && !activeMover.IsFlying && Navigator.CanNavigateWithin(myLoc, destination, Navigator.PathPrecision))
						{
							Navigator.MoveTo(destination);
							return;
						}
						List<WoWPoint> list = new List<WoWPoint>();
						bool[] array;
						using (StyxWoW.Memory.AcquireFrame())
						{
							int num4 = 10;
							while ((float)num4 < num3)
							{
								for (int i = 0; i < 360; i += 90)
								{
									WoWPoint testPoint = Styx.Helpers.WoWMathHelper.GetPointAt(destination, (float)num4, Styx.Helpers.WoWMathHelper.DegreesToRadians((float)i), 0f);
									float num5 = (Navigator.FindHeights(testPoint.X, testPoint.Y) ?? new List<float>())
										.OrderBy(h => Math.Abs(h - testPoint.Z))
										.FirstOrDefault(h =>
										{
											WoWPoint woWPoint = new WoWPoint(testPoint.X, testPoint.Y, h);
											return Styx.WoWInternals.World.GameWorld.TraceLine(woWPoint.Add(0f, 0f, 2f), woWPoint.Add(0f, 0f, -2f), Styx.WoWInternals.World.TraceLineHitFlags.WmoCollision | Styx.WoWInternals.World.TraceLineHitFlags.Terrain);
										});
									if (num5 != 0f)
									{
										list.Add(new WoWPoint(testPoint.X, testPoint.Y, num5));
									}
								}
								num4 += 10;
							}
							IEnumerable<WoWPoint> enumerable3 = list;
							Styx.WoWInternals.World.GameWorld.MassTraceLine(enumerable3.Select(p => new Styx.WoWInternals.World.WorldLine(p.Add(0f, 0f, 2f), p.Add(0f, 0f, 200f))).ToArray(), Styx.WoWInternals.World.TraceLineHitFlags.Collision, out array);
						}
						for (int j = 0; j < array.Length; j++)
						{
							if (!array[j])
							{
								WoWPoint woWPoint3 = list[j];
								if (Navigator.CanNavigateWithin(woWPoint3, destination, Navigator.PathPrecision))
								{
									Flightor._indoorLandingSpot = woWPoint3;
									Styx.Common.Logging.WriteDiagnostic("Destination might be indoors. Landing at a safe location: {0} from: {1}", new object[]
									{
										Flightor._indoorLandingSpot,
										myLoc
									});
									Flightor._indoorLandingDestination = destination;
									return;
								}
							}
						}
						float x = myLoc.X;
						float y = myLoc.Y;
						IEnumerable<float> enumerable4 = (Navigator.FindHeights(myLoc.X, myLoc.Y) ?? new List<float>()).Where(h => h <= myLoc.Z);
						WoWPoint woWPoint4 = new WoWPoint(x, y, enumerable4.OrderBy(h => myLoc.Z - h).FirstOrDefault());
						if (Navigator.CanNavigateWithin(woWPoint4, destination, Navigator.PathPrecision))
						{
							Flightor._indoorLandingSpot = woWPoint4;
							Styx.Common.Logging.WriteDiagnostic("Destination might be indoors. Landing at a safe location: {0} from: {1}", new object[]
							{
								Flightor._indoorLandingSpot,
								myLoc
							});
							Flightor._indoorLandingDestination = destination;
							return;
						}
					}
				}
			}
			if (Flightor._launchDestination != WoWPoint.Empty && Flightor._launchDestination.DistanceSqr(destination) > 900f)
			{
				Flightor._launchSpot = WoWPoint.Empty;
				Flightor._launchDestination = WoWPoint.Empty;
			}
			checkIndoors = checkIndoors || Flightor._indoorLandingSpot != WoWPoint.Empty;
			if (isOutdoors && !Styx.CommonBot.Mount.IsNearCantMountSpot(myLoc) && Styx.WoWInternals.World.GameWorld.IsInLineOfSight(upLoc, myLoc.Add(0f, 0f, (checkIndoors || isVashjir) ? 200f : 10f)))
			{
				Flightor._launchSpot = WoWPoint.Empty;
				Flightor._launchDestination = WoWPoint.Empty;
			}
			if (Flightor._launchSpot != WoWPoint.Empty)
			{
				if (Flightor._launchSpot.DistanceSqr(myLoc) < 16f)
				{
					Flightor._launchSpot = WoWPoint.Empty;
					Flightor._launchDestination = WoWPoint.Empty;
				}
				else
				{
					if (Navigator.CanNavigateWithin(myLoc, Flightor._launchSpot, Navigator.PathPrecision))
					{
						Flightor.MoveToLaunchSpot();
						return;
					}
					Flightor._launchSpot = WoWPoint.Empty;
					Flightor._launchDestination = WoWPoint.Empty;
				}
			}
			if (!activeMover.IsMoving && !activeMover.IsFlying && (!isOutdoors || Styx.CommonBot.Mount.IsNearCantMountSpot(myLoc) || (!hasSeaLegs && !Styx.WoWInternals.World.GameWorld.IsInLineOfSight(upLoc, myLoc.Add(0f, 0f, (checkIndoors || isVashjir) ? 200f : 10f)))))
			{
				Styx.WoWInternals.WoWObjects.WoWObject woWObject4 = Flightor.FindTakeoffPlatform((checkIndoors || isVashjir) ? 200f : 10f);
				if (woWObject4 != null)
				{
					Styx.Common.Logging.WriteDiagnostic("We can't start flying from current location. Moving to a better position to take off.");
					Flightor._launchSpot = woWObject4.Location;
					Flightor._launchDestination = destination;
					Flightor.MoveToLaunchSpot();
					return;
				}
				float num6 = (isVashjir ? 250f : 150f);
				List<WoWPoint> list2 = new List<WoWPoint>();
				bool[] array2;
				using (StyxWoW.Memory.AcquireFrame())
				{
					int num7 = 10;
					while ((float)num7 < num6)
					{
						for (int k = 0; k < 360; k += 90)
						{
							WoWPoint testPoint2 = Styx.Helpers.WoWMathHelper.GetPointAt(myLoc, (float)num7, Styx.Helpers.WoWMathHelper.DegreesToRadians((float)k), 0f);
							float num8 = (Navigator.FindHeights(testPoint2.X, testPoint2.Y) ?? new List<float>())
								.OrderBy(h => Math.Abs(h - testPoint2.Z))
								.FirstOrDefault(h =>
								{
									WoWPoint woWPoint = new WoWPoint(testPoint2.X, testPoint2.Y, h);
									return Styx.WoWInternals.World.GameWorld.TraceLine(woWPoint.Add(0f, 0f, 2f), woWPoint.Add(0f, 0f, -2f), Styx.WoWInternals.World.TraceLineHitFlags.WmoCollision | Styx.WoWInternals.World.TraceLineHitFlags.Terrain);
								});
							if (num8 != 0f)
							{
								list2.Add(new WoWPoint(testPoint2.X, testPoint2.Y, num8));
							}
						}
						num7 += 10;
					}
					IEnumerable<WoWPoint> enumerable6 = list2;
					Styx.WoWInternals.World.GameWorld.MassTraceLine(enumerable6.Select(p => new Styx.WoWInternals.World.WorldLine(p.Add(0f, 0f, 2f), p.Add(0f, 0f, 200f))).ToArray(), Styx.WoWInternals.World.TraceLineHitFlags.Collision, out array2);
				}
				for (int l = 0; l < array2.Length; l++)
				{
					if (!array2[l])
					{
						WoWPoint woWPoint5 = list2[l];
						if (!Styx.CommonBot.Mount.IsNearCantMountSpot(woWPoint5) && Navigator.CanNavigateWithin(myLoc, woWPoint5, Navigator.PathPrecision))
						{
							Styx.Common.Logging.WriteDiagnostic("We can't start flying from current location. Moving to a better position to take off.");
							Flightor._launchSpot = woWPoint5;
							Flightor._launchDestination = destination;
							Flightor.MoveToLaunchSpot();
							return;
						}
					}
				}
			}
			if (!Flightor.MountHelper.Mounted)
			{
				if (!Flightor.MountHelper.CanMountHere(false))
				{
					Navigator.MoveTo(destination);
					return;
				}
				if (activeMover.IsSwimming && !hasSeaLegs && !Styx.WoWInternals.World.GameWorld.TraceLine(upLoc, myLoc, Styx.WoWInternals.World.TraceLineHitFlags.LiquidWaterWalkable))
				{
					WoWPoint woWPoint6 = Flightor.GetPointAt(StyxWoW.Me.Location, 10f, Styx.Helpers.WoWMathHelper.CalculateNeededFacing(StyxWoW.Me.Location, destination), Styx.Helpers.WoWMathHelper.DegreesToRadians(60f));
					Navigator.PlayerMover.MoveTowards(woWPoint6);
					return;
				}
				if (!hasSeaLegs && activeMover.IsSwimming && StyxWoW.Me.Class == WoWClass.Druid)
				{
					Styx.WoWInternals.WoWSpell flyingMount = Flightor.MountHelper.FlyingMount;
					if (flyingMount.Id == 33943 || flyingMount.Id == 40120)
					{
						Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
						StyxWoW.Sleep(50);
						Flightor.MountHelper.MountUp(true);
						StyxWoW.Sleep(50);
						Flightor.MountHelper.MountUp(true);
						StyxWoW.Sleep(50);
						Flightor.MountHelper.MountUp(true);
						Styx.WoWInternals.WoWMovement.MoveStop();
						return;
					}
				}
				if (activeMover.IsSwimming && !hasSeaLegs)
				{
					Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
					StyxWoW.Sleep(100);
					Styx.WoWInternals.WoWMovement.MoveStop();
					StyxWoW.Sleep(1000);
				}
				Flightor.MountHelper.MountUp();
				return;
			}
			else
			{
				Flightor.callCount++;
				if (Flightor.AntiStuck)
				{
					Flightor.callCount = 0;
				}
				if (Flightor.callCount % 2 != 0)
				{
					return;
				}
				Flightor.callCount = 0;
				if (Flightor.MountHelper.Mounted && ((!hasSeaLegs && !activeMover.IsFlying) || (hasSeaLegs && !activeMover.IsSwimming)))
				{
					Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.Forward | Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
					StyxWoW.Sleep(100);
					Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.Forward | Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
				}
				Styx.WoWInternals.WoWMovement.ClickToMoveInfoStruct clickToMoveInfo = Styx.WoWInternals.WoWMovement.ClickToMoveInfo;
				if (activeMover.IsMoving && Flightor._lastDestination == destination && clickToMoveInfo.IsClickMoving && clickToMoveInfo.ClickPos.DistanceSqr(myLoc) > 900f)
				{
					return;
				}
				if (Flightor._lastDestination != destination)
				{
					Flightor._currentPath = null;
					Flightor._lastDestination = destination;
				}
				if (Flightor._currentPath == null)
				{
					Styx.Common.Vector2 vector = new Styx.Common.Vector2(myLoc.X, myLoc.Y);
					Styx.Common.Vector2 vector2 = new Styx.Common.Vector2(destination.X, destination.Y);
					Flightor._currentPath = Flightor.GetPath(StyxWoW.Me.MapId, vector, vector2);
				}
				Styx.Common.Vector2 vector3 = Flightor._currentPath.Hops.Peek();
				if (Flightor._currentPath.Hops.Count > 1 && StyxWoW.Me.Location.Distance2DSqr(new WoWPoint(vector3.X, vector3.Y, 0f)) <= 900f)
				{
					vector3 = Flightor._currentPath.Hops.Dequeue();
				}
				WoWPoint woWPoint7;
				if (Flightor._currentPath.Hops.Count == 1)
				{
					woWPoint7 = Flightor.GetClickPoint(destination, minHeight);
				}
				else
				{
					woWPoint7 = Flightor.GetClickPoint(new WoWPoint(vector3.X, vector3.Y, (destination.DistanceSqr(myLoc) < 40000f) ? destination.Z : myLoc.Z), minHeight);
				}
				if (woWPoint7 != WoWPoint.Empty)
				{
					if (!activeMover.IsMoving || clickToMoveInfo.ClickPos != woWPoint7 || !clickToMoveInfo.IsClickMoving)
					{
						Navigator.PlayerMover.MoveTowards(woWPoint7);
					}
					if (Flightor.MountHelper.Mounted && ((!hasSeaLegs && !activeMover.IsFlying) || (hasSeaLegs && !activeMover.IsSwimming)))
					{
						StyxWoW.Sleep(100);
						Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.Forward | Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
						StyxWoW.Sleep(100);
						Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.Forward | Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
						Navigator.PlayerMover.MoveTowards(woWPoint7);
						return;
					}
				}
				else
				{
					Flightor.DoAntiStuck();
				}
				return;
			}
		}

		private static WoWPoint GetClickPoint(WoWPoint destination, float minHeight)
		{
			WoWPoint traceLinePos = StyxWoW.Me.GetTraceLinePos();
			WoWPoint location = StyxWoW.Me.Location;
			if (destination.Z != 0f && traceLinePos.DistanceSqr(destination) < 40000f && Styx.WoWInternals.World.GameWorld.IsInLineOfSight(traceLinePos, destination.Add(0f, 0f, 2f)))
			{
				return destination;
			}
			float num = Styx.Helpers.WoWMathHelper.CalculateNeededFacing(location, destination);
			float num2 = 60f;
			float num3 = 200f;
			float num4 = 0f;
			float num5 = location.Distance2D(destination);
			if (num5 < 100f && destination.Z != 0f)
			{
				float num6 = location.Distance(destination);
				num2 = num6 - 1.5f;
				float num7 = (float)Math.Asin((double)Math.Min(1f, Math.Abs(destination.Z - location.Z) / num6));
				num4 = ((traceLinePos.Z > destination.Z) ? (-num7) : num7);
			}
			else if (!StyxWoW.Me.HasAura("Sea Legs"))
			{
				if (Styx.WoWInternals.World.GameWorld.TraceLine(traceLinePos, traceLinePos.Add(0f, 0f, -minHeight), Styx.WoWInternals.World.TraceLineHitFlags.Terrain | Styx.WoWInternals.World.TraceLineHitFlags.LiquidWaterWalkable))
				{
					num4 = Styx.Helpers.WoWMathHelper.DegreesToRadians(20f);
				}
				else if (StyxWoW.Me.ZoneId != 3540U && num5 > 300f && !Styx.WoWInternals.World.GameWorld.TraceLine(traceLinePos, traceLinePos.Add(0f, 0f, -num3), Styx.WoWInternals.World.TraceLineHitFlags.WmoCollision | Styx.WoWInternals.World.TraceLineHitFlags.Terrain | Styx.WoWInternals.World.TraceLineHitFlags.LiquidWaterWalkable) && (StyxWoW.Me.MapId != 530U || !Styx.WoWInternals.World.GameWorld.TraceLine(traceLinePos, Flightor.GetPointAt(traceLinePos, 300f, StyxWoW.Me.Rotation, 0f), Styx.WoWInternals.World.TraceLineHitFlags.WmoCollision | Styx.WoWInternals.World.TraceLineHitFlags.Terrain)))
				{
					num4 = Styx.Helpers.WoWMathHelper.DegreesToRadians(-60f);
				}
			}
			if (StyxWoW.Me.ZoneId == 3540U)
			{
				num4 = Styx.Helpers.WoWMathHelper.DegreesToRadians(30f);
			}
			WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, num2, num, num4);
			if (!Styx.WoWInternals.World.GameWorld.TraceLine(traceLinePos, woWPoint, Styx.WoWInternals.World.TraceLineHitFlags.Collision))
			{
				return woWPoint;
			}
			List<Styx.WoWInternals.World.WorldLine> list = Flightor.TraceLines(num, num4, num2);
			bool[] array;
			Styx.WoWInternals.World.GameWorld.MassTraceLine(list.ToArray(), Styx.WoWInternals.World.TraceLineHitFlags.Collision, out array);
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i])
				{
					return list[i].End;
				}
			}
			list = Flightor.TraceLines(num, num4, num2 / 3f);
			Styx.WoWInternals.World.GameWorld.MassTraceLine(list.ToArray(), Styx.WoWInternals.World.TraceLineHitFlags.Collision, out array);
			for (int j = 0; j < array.Length; j++)
			{
				if (!array[j])
				{
					return list[j].End;
				}
			}
			return WoWPoint.Empty;
		}

		private static List<Styx.WoWInternals.World.WorldLine> TraceLines(float neededFacing, float pitch, float dist)
		{
			WoWPoint traceLinePos = StyxWoW.Me.GetTraceLinePos();
			List<Styx.WoWInternals.World.WorldLine> list = new List<Styx.WoWInternals.World.WorldLine>();
			int num = 15;
			for (int i = 1; i <= 3; i++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing, pitch + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(i * num)));
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			for (int j = 1; j <= 3; j++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(j * -(float)num)), pitch);
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
				woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(j * num)), pitch);
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			for (int k = 4; k <= 6; k++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing, pitch + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(k * num)));
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			for (int l = 4; l <= 8; l++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(l * -(float)num)), pitch);
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
				woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(l * num)), pitch);
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			for (int m = 7; m <= 9; m++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing, pitch + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(m * num)));
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			for (int n = 1; n <= 4; n++)
			{
				WoWPoint woWPoint = Flightor.GetPointAt(traceLinePos, dist, neededFacing, pitch + Styx.Helpers.WoWMathHelper.DegreesToRadians((float)(n * -(float)num)));
				list.Add(new Styx.WoWInternals.World.WorldLine(traceLinePos, woWPoint));
			}
			return list;
		}

		private static WoWPoint GetPointAt(WoWPoint from, float distance, float rotationRadians, float pitchRadians)
		{
			float num = (float)(Math.Cos((double)pitchRadians) * Math.Cos((double)rotationRadians) * (double)distance);
			float num2 = (float)(Math.Cos((double)pitchRadians) * Math.Sin((double)rotationRadians) * (double)distance);
			float num3 = (float)(Math.Sin((double)pitchRadians) * (double)distance);
			return from + new WoWPoint(num, num2, num3);
		}

		public static void Clear()
		{
			Flightor._launchSpot = WoWPoint.Empty;
			Flightor._launchDestination = WoWPoint.Empty;
			Flightor._indoorLandingSpot = WoWPoint.Empty;
			Flightor._dismountedForLanding = false;
			Flightor._indoorLandingDestination = WoWPoint.Empty;
			Flightor._currentPath = null;
			Flightor._polyNav = null;
		}

		public static bool AntiStuck
		{
			get
			{
				Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
				if (activeMover == null || activeMover.Stunned || activeMover.Fleeing)
				{
					return false;
				}
				DateTime now = DateTime.Now;
				if (now.Subtract(Flightor._lastStuckCheckTime).TotalMilliseconds > 500.0)
				{
					Flightor._lastStuckCheckLocation = WoWPoint.Empty;
					Flightor._lastStuckCheckTime = now;
					return false;
				}
				Flightor._lastStuckCheckTime = now;
				if (!Flightor._stuckWaitTimer.IsFinished)
				{
					return false;
				}
				WoWPoint location = activeMover.Location;
				if (Flightor._lastStuckCheckLocation.DistanceSqr(location) < 9f)
				{
					Styx.Common.Logging.Write(Colors.Red, "We are stuck! ({0})", new object[] { location });
					Flightor.DoAntiStuck();
					return true;
				}
				if (activeMover.MovementInfo.TimeMoved == 0U)
				{
					Flightor._lastStuckCheckLocation = location;
					Flightor._stuckWaitTimer.Reset();
					return false;
				}
				Flightor._lastStuckCheckLocation = WoWPoint.Empty;
				return false;
			}
		}

		private static bool TriedStrafeLeft { get; set; }

		private static bool TriedStrafeRight { get; set; }

		private static bool TriedAscend { get; set; }

		private static WoWPoint LastStuckLocation { get; set; }

		public static void DoAntiStuck()
		{
			Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
			if (activeMover == null)
			{
				return;
			}
			WoWPoint location = activeMover.Location;
			if (Flightor.LastStuckLocation != WoWPoint.Empty && Flightor.LastStuckLocation.Distance2DSqr(location) > 100f)
			{
				Flightor.TriedStrafeLeft = false;
				Flightor.TriedStrafeRight = false;
				Flightor.TriedAscend = false;
			}
			Flightor.LastStuckLocation = location;
			if (activeMover.IsMoving)
			{
				Styx.WoWInternals.WoWMovement.MoveStop();
				StyxWoW.Sleep(100);
			}
			if (!Flightor.TriedAscend)
			{
				Styx.Common.Logging.WriteDiagnostic("[Stuck] Trying to ascend.");
				Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
				StyxWoW.Sleep(200);
				Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.JumpAscend);
				StyxWoW.Sleep(100);
				Flightor.TriedAscend = true;
				return;
			}
			if (!Flightor.TriedStrafeLeft)
			{
				Styx.Common.Logging.WriteDiagnostic("[Stuck] Trying strafing left.");
				Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.StrafeLeft);
				StyxWoW.Sleep(300);
				Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.StrafeLeft);
				StyxWoW.Sleep(100);
				Flightor.TriedStrafeLeft = true;
				return;
			}
			if (!Flightor.TriedStrafeRight)
			{
				Styx.Common.Logging.WriteDiagnostic("[Stuck] Trying strafing right.");
				Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.StrafeRight);
				StyxWoW.Sleep(300);
				Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.StrafeRight);
				StyxWoW.Sleep(100);
				Flightor.TriedStrafeRight = true;
				return;
			}
			Styx.Common.Logging.WriteDiagnostic("[Stuck] Trying to backup.");
			Styx.WoWInternals.WoWMovement.Move(Styx.WoWInternals.WoWMovement.MovementDirection.Backwards);
			StyxWoW.Sleep(500);
			Styx.WoWInternals.WoWMovement.MoveStop(Styx.WoWInternals.WoWMovement.MovementDirection.Backwards);
			StyxWoW.Sleep(100);
			Flightor.TriedStrafeLeft = false;
			Flightor.TriedStrafeRight = false;
			Flightor.TriedAscend = false;
		}

		private static Flightor.Path GetPath(uint mapId, Styx.Common.Vector2 start, Styx.Common.Vector2 end)
		{
			if (Flightor._polyNav == null || Flightor._lastMapId != mapId || Flightor._charGuid != StyxWoW.Me.Guid)
			{
				Flightor._lastMapId = mapId;
				Flightor._charGuid = StyxWoW.Me.Guid;
				Styx.Pathing.FlightorNavigation.BlackspotManager.AddDefaultBlackspots();
				if (Styx.Pathing.FlightorNavigation.Areas.ContinentAreas.ContainsKey(Flightor._lastMapId))
				{
					Flightor._polyNav = new Styx.Pathing.FlightorNavigation.PolyNav(Styx.Pathing.FlightorNavigation.Areas.ContinentAreas[Flightor._lastMapId], Styx.Pathing.FlightorNavigation.BlackspotManager.Blackspots);
				}
			}
			Queue<Styx.Common.Vector2> queue = new Queue<Styx.Common.Vector2>();
			if (Flightor._polyNav != null)
			{
				queue = new Queue<Styx.Common.Vector2>(Flightor._polyNav.FindPath(start, end));
			}
			if (queue.Count == 0)
			{
				queue.Enqueue(end);
			}
			else if (queue.Count > 1)
			{
				queue.Dequeue();
			}
			return new Flightor.Path
			{
				End = end,
				Hops = queue,
				MapId = Flightor._lastMapId,
				Start = start
			};
		}

		private static int callCount;

		private static WoWPoint _lastDestination;

		private static readonly List<uint> _flyableMapIds = new List<uint> { 0U, 1U, 530U, 571U, 646U, 870U, 609U };

		private static Flightor.Path _currentPath;

		private static bool _dismountedForLanding;

		private static WoWPoint _indoorLandingSpot = WoWPoint.Empty;

		private static WoWPoint _indoorLandingDestination = WoWPoint.Empty;

		private static WoWPoint _launchSpot = WoWPoint.Empty;

		private static WoWPoint _launchDestination = WoWPoint.Empty;

		private static readonly Styx.Common.Helpers.WaitTimer _stuckWaitTimer = new Styx.Common.Helpers.WaitTimer(TimeSpan.FromMilliseconds(500.0));

		private static WoWPoint _lastStuckCheckLocation = WoWPoint.Empty;

		private static DateTime _lastStuckCheckTime = DateTime.MinValue;

		private static Styx.Pathing.FlightorNavigation.PolyNav _polyNav;

		private static uint _lastMapId;

		private static ulong _charGuid;

		private class Path
		{
			public uint MapId;

			public Styx.Common.Vector2 Start;

			public Styx.Common.Vector2 End;

			public Queue<Styx.Common.Vector2> Hops = new Queue<Styx.Common.Vector2>();
		}

		public static class MountHelper
		{
			internal static Styx.WoWInternals.WoWSpell FlyingMount
			{
				get
				{
					Styx.CommonBot.Mount.SetMountInfo();
					if (StyxWoW.Me.HasAura("Sea Legs"))
					{
						if (!StyxWoW.Me.IsOutdoors)
						{
							return null;
						}
						IEnumerable<Styx.WoWInternals.WoWObjects.WoWUnit> enumerable = Styx.WoWInternals.ObjectManager.GetObjectsOfType<Styx.WoWInternals.WoWObjects.WoWUnit>(false, false);
						if (enumerable.Any(u => u.Entry == 39878U && u.InLineOfSight))
						{
							return null;
						}
						WoWPoint location = StyxWoW.Me.Location;
						WoWPoint woWPoint = location.Add(0f, 0f, StyxWoW.Me.BoundingHeight);
						if (!Styx.WoWInternals.World.GameWorld.TraceLine(woWPoint, location, Styx.WoWInternals.World.TraceLineHitFlags.LiquidWaterWalkable))
						{
							Styx.WoWInternals.WoWSpell woWSpell = null;
							if (Styx.CommonBot.SpellManager.HasSpell("Aquatic Form"))
							{
								woWSpell = Styx.CommonBot.SpellManager.Spells["Aquatic Form"];
							}
							else
							{
								IEnumerable<Styx.CommonBot.Mount.MountWrapper> underwaterMounts = Styx.CommonBot.Mount.UnderwaterMounts;
								if (underwaterMounts.Any(m => m.CreatureSpellId == 75207))
								{
									woWSpell = Styx.WoWInternals.WoWSpell.FromId(75207);
								}
								else if (Styx.CommonBot.Mount.UnderwaterMounts.Count > 0)
								{
									woWSpell = Styx.CommonBot.Mount.UnderwaterMounts[Flightor.MountHelper._random.Next(0, Styx.CommonBot.Mount.UnderwaterMounts.Count)].CreatureSpell;
								}
							}
							return woWSpell;
						}
					}
					if (string.IsNullOrEmpty(Styx.Helpers.CharacterSettings.Instance.FlyingMountName))
					{
						if (Styx.CommonBot.Mount.FlyingMounts.Count != 0)
						{
							return Styx.CommonBot.Mount.FlyingMounts[Flightor.MountHelper._random.Next(0, Styx.CommonBot.Mount.FlyingMounts.Count)].CreatureSpell;
						}
						return null;
					}
					else
					{
						if (Styx.CommonBot.SpellManager.HasSpell("Swift Flight Form"))
						{
							Styx.WoWInternals.WoWSpell woWSpell2 = Styx.CommonBot.SpellManager.Spells["Swift Flight Form"];
							if (Styx.Helpers.CharacterSettings.Instance.FlyingMountName == woWSpell2.Name || woWSpell2.Name.ToLowerInvariant().Contains(Styx.Helpers.CharacterSettings.Instance.FlyingMountName) || Styx.Helpers.CharacterSettings.Instance.FlyingMountName == woWSpell2.Id.ToString(CultureInfo.InvariantCulture))
							{
								return woWSpell2;
							}
						}
						if (Styx.CommonBot.SpellManager.HasSpell("Flight Form"))
						{
							Styx.WoWInternals.WoWSpell woWSpell3 = Styx.CommonBot.SpellManager.Spells["Flight Form"];
							if (Styx.Helpers.CharacterSettings.Instance.FlyingMountName == woWSpell3.Name || woWSpell3.Name.ToLowerInvariant().Contains(Styx.Helpers.CharacterSettings.Instance.FlyingMountName) || Styx.Helpers.CharacterSettings.Instance.FlyingMountName == woWSpell3.Id.ToString(CultureInfo.InvariantCulture))
							{
								return woWSpell3;
							}
						}
						IEnumerable<Styx.CommonBot.Mount.MountWrapper> flyingMounts = Styx.CommonBot.Mount.FlyingMounts;
						Styx.CommonBot.Mount.MountWrapper mountWrapper = flyingMounts.FirstOrDefault(m => m.Name == Styx.Helpers.CharacterSettings.Instance.FlyingMountName || m.CreatureSpellId.ToString(CultureInfo.InvariantCulture) == Styx.Helpers.CharacterSettings.Instance.FlyingMountName || m.Name.ToLowerInvariant().Contains(Styx.Helpers.CharacterSettings.Instance.FlyingMountName.ToLowerInvariant())) ?? Styx.CommonBot.Mount.FlyingMounts.FirstOrDefault<Styx.CommonBot.Mount.MountWrapper>();
						if (!(mountWrapper != null))
						{
							return null;
						}
						return mountWrapper.CreatureSpell;
					}
				}
			}

			internal static bool CanMountHere(bool ignoreIndoorsCheck = false)
			{
				if (!Flightor.CanFly)
				{
					return false;
				}
				if (!ignoreIndoorsCheck && !StyxWoW.Me.IsOutdoors)
				{
					return false;
				}
				if (Styx.CommonBot.Mount.IsNearCantMountSpot(StyxWoW.Me.Location))
				{
					return false;
				}
				float boundingHeight = StyxWoW.Me.BoundingHeight;
				WoWPoint woWPoint = StyxWoW.Me.Location.Add(0f, 0f, boundingHeight);
				WoWPoint woWPoint2 = woWPoint.Add(0f, 0f, boundingHeight);
				bool flag = !Styx.WoWInternals.World.GameWorld.IsInLineOfSight(woWPoint, woWPoint2);
				return !StyxWoW.Me.Combat && !flag;
			}

			public static bool CanMount
			{
				get
				{
					return Flightor.MountHelper.CanMountHere(false);
				}
			}

			public static bool Mounted
			{
				get
				{
					if (!StyxWoW.Me.HasAura("Sea Legs"))
					{
						Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
						return activeMover != null && activeMover.MovementInfo.CanFly;
					}
					if (StyxWoW.Me.HasAura("Aquatic Form"))
					{
						return true;
					}
					IEnumerable<Styx.CommonBot.Mount.MountWrapper> underwaterMounts = Styx.CommonBot.Mount.UnderwaterMounts;
					return underwaterMounts.Any(m => StyxWoW.Me.Auras.Values.Any(a => a.SpellId == m.CreatureSpellId)) || StyxWoW.Me.IsGhost;
				}
			}

			public static void MountUp()
			{
				Flightor.MountHelper.MountUp(false);
			}

			internal static void MountUp(bool noDelay)
			{
				if (!Flightor.MountHelper.CanMount)
				{
					return;
				}
				if (Flightor.MountHelper.Mounted)
				{
					return;
				}
				Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
				if (activeMover == null)
				{
					return;
				}
				if (activeMover.IsMoving)
				{
					Navigator.PlayerMover.MoveStop();
					if (!noDelay)
					{
						StyxWoW.SleepForLagDuration();
					}
				}
				Styx.WoWInternals.WoWSpell flyingMount = Flightor.MountHelper.FlyingMount;
				if (Styx.CommonBot.SpellManager.HasSpell(flyingMount))
				{
					Styx.CommonBot.SpellManager.Cast(flyingMount);
				}
				else
				{
					Styx.WoWInternals.Lua.DoString(string.Format("for companion=1, GetNumCompanions(\"MOUNT\") do local _, _, id = GetCompanionInfo(\"MOUNT\", companion) id = tostring(id) if id == \"{0}\" then CallCompanion(\"MOUNT\", companion) break end end", flyingMount.Id), "WoW.lua");
				}
				Styx.Common.Logging.Write(Styx.Resources.StyxResources.Mounting___0_, new object[] { flyingMount.Name });
				if (!noDelay)
				{
					StyxWoW.SleepForLagDuration();
					StyxWoW.Sleep((int)(flyingMount.CastTime + 100U));
					StyxWoW.SleepForLagDuration();
				}
				StyxWoW.SleepForLagDuration();
				if (!Flightor.MountHelper.Mounted)
				{
					Flightor.MountHelper._failedMountAttempts += 1U;
					if (Flightor.MountHelper._failedMountAttempts >= 3U)
					{
						Styx.CommonBot.Mount.AddCantMountSpot(StyxWoW.Me.Location);
						Flightor.MountHelper._failedMountAttempts = 0U;
						return;
					}
				}
				else
				{
					Flightor.MountHelper._failedMountAttempts = 0U;
				}
			}

			public static void Dismount()
			{
				if (!Flightor.MountHelper.Mounted)
				{
					return;
				}
				Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
				if (activeMover == null)
				{
					return;
				}
				if (activeMover.IsMoving)
				{
					Styx.WoWInternals.WoWMovement.MoveStop();
					StyxWoW.SleepForLagDuration();
				}
				if (!StyxWoW.Me.HasAura("Swift Flight Form") && !StyxWoW.Me.HasAura("Flight Form") && !StyxWoW.Me.HasAura("Aquatic Form"))
				{
					Styx.WoWInternals.Lua.DoString("Dismount()", "WoW.lua");
					return;
				}
				Styx.WoWInternals.Lua.DoString("CancelShapeshiftForm()", "WoW.lua");
				StyxWoW.SleepForLagDuration();
			}

			private static readonly Random _random = new Random();

			private static uint _failedMountAttempts;

			public class DisMount : Styx.TreeSharp.Action
			{
				protected override Styx.TreeSharp.RunStatus Run(object context)
				{
					if (!Flightor.MountHelper.Mounted)
					{
						return Styx.TreeSharp.RunStatus.Failure;
					}
					Styx.WoWInternals.WoWObjects.WoWUnit activeMover = Styx.WoWInternals.WoWMovement.ActiveMover;
					if (activeMover == null)
					{
						return Styx.TreeSharp.RunStatus.Failure;
					}
					if (activeMover.IsMoving)
					{
						Styx.WoWInternals.WoWMovement.MoveStop();
						StyxWoW.SleepForLagDuration();
					}
					if (!StyxWoW.Me.HasAura("Swift Flight Form") && !StyxWoW.Me.HasAura("Flight Form") && !StyxWoW.Me.HasAura("Aquatic Form"))
					{
						Styx.WoWInternals.Lua.DoString("Dismount()", "WoW.lua");
						return Styx.TreeSharp.RunStatus.Success;
					}
					Styx.WoWInternals.Lua.DoString("CancelShapeshiftForm()", "WoW.lua");
					StyxWoW.Sleep(250);
					return Styx.TreeSharp.RunStatus.Success;
				}
			}
		}
	}
}
