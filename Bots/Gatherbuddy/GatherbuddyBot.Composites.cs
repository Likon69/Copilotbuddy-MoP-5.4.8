using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bots.GatherBuddy;
using Bots.GatherBuddy.Resources;
using Bots.Grind;
using CommonBehaviors.Actions;
using Styx;

using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Inventory;
using Styx.CommonBot.Profiles;
using Styx.Helpers;


using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;
using System.Collections.Generic;
using Styx.WoWInternals.World;

namespace Bots.Gatherbuddy
{
    public partial class GatherbuddyBot : BotBase
    {
        #region CreateGatherbuddyLogic

		private static bool ignoreCombat;
        private static bool ascended;
        private static WoWPoint movePoint;
        private static float FlyingSpeedSqr
        {
            get
            {
                var speed = StyxWoW.Me.MovementInfo.FlyingForwardSpeed + 10f;

                return speed*speed;
            }
        }

        private Composite CreateGatherbuddyLogic()
        {
            return new PrioritySelector(
                CreateDeathBehavior(),

                new Decorator(
                    ret => StyxWoW.Me.Combat &&
                           Flightor.MountHelper.Mounted &&
                           (LootTargeting.Instance.FirstObject == null || LootTargeting.Instance.FirstObject.Distance > 50),
                    new Action(delegate
                        {
                            ignoreCombat = true;
                            return RunStatus.Failure;
                        })),

                new Decorator(
                    ret => StyxWoW.Me.Combat || StyxWoW.Me.HasAura("Food") || StyxWoW.Me.HasAura("Drink"),
                    new Action(delegate
                        {
                            harvestAttempts = 0;
                            nodeTimer.Reset();
                            return RunStatus.Failure;
                        })),

                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget == null &&
                           StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.CurrentTarget != null && StyxWoW.Me.Pet.CurrentTarget.IsAlive,
                    new Action(ret => StyxWoW.Me.Pet.CurrentTarget.Target())),

                //We don't want to call combat and loot while we are flying to not dismount
                new Decorator(
                    ret => !Flightor.MountHelper.Mounted || !StyxWoW.Me.MovementInfo.IsFlying,
                    new PrioritySelector(
                        new Decorator(ret => !ignoreCombat,
                            new PrioritySelector(
                                new Decorator(
                                    ret => StyxWoW.Me.HasAura("Sea Legs") &&
                                           StyxWoW.Me.CurrentTarget != null &&
                                           StyxWoW.Me.CurrentTarget.IsAlive &&
                                           StyxWoW.Me.CurrentTarget.Aggro &&
                                           StyxWoW.Me.Class != WoWClass.Hunter &&
                                           StyxWoW.Me.CurrentTarget.Distance > 5,
                                    new Action(ret => Navigator.PlayerMover.MoveTowards(WoWMathHelper.CalculatePointFrom(StyxWoW.Me.Location, StyxWoW.Me.CurrentTarget.Location, 3)))),

                                LevelBot.CreateCombatBehavior())))),
                CreateVendorBehavior(),

                new Action(delegate
                    {
                        ignoreCombat = false;
                        return RunStatus.Failure;
                    }),

                new Decorator(
                    ret => GatherbuddySettings.Instance.BottingHours != 0 &&
                           DateTime.Now.Subtract(_startTime).TotalHours >= GatherbuddySettings.Instance.BottingHours,
                    new Action(ret =>
                        {
                            TimeSpan runningTime = DateTime.Now.Subtract(_startTime);
                            Log(GatherBuddyResources.ExitingWoWAndHBAfterGathering__0__nodes_in__1_h__2_m__3_s_, NodeCollectionCount.Values.Sum(), runningTime.Hours,
                                    runningTime.Minutes, runningTime.Seconds);

                            foreach (var i in NodeCollectionCount)
                            {
                                Log(i.Key + ": " + i.Value);
                            }

                            StyxWoW.Memory.Process.Kill();
                            Environment.Exit(12);
                            return RunStatus.Success;
                        })),

                new Decorator(
                    ret => StyxWoW.Me.HasAura("Resurrection Sickness") && GatherbuddySettings.Instance.WaitRezSickness,
                    new PrioritySelector(
                        new Decorator(
                            ret => !ascended,
                            new Sequence(
                                new Action(ret => Flightor.MountHelper.MountUp()),
                                new DecoratorContinue(
                                    ret => Flightor.MountHelper.Mounted,
                                    new Sequence(
                                        new ActionSetActivity(GatherBuddyResources.AscendingToWaitResSickness),
                                        new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend, TimeSpan.FromSeconds(3)))
                                        )),
                                new Action(ret => ascended = true))),
                        new Sequence(
                            new ActionSetActivity(GatherBuddyResources.WaitingForResurrectionSickness),
                            new ActionAlwaysSucceed()))),

                //Get out of mailbox area in vashj'ir
                new Decorator(
                    ret => StyxWoW.Me.Location.Distance(new WoWPoint(-6112.708, 4296.319, -348.3496)) < 70,
                    new Action(ret => WoWMovement.ClickToMove(new WoWPoint(-6075.617, 4183.45, -384.7297)))),

                //Get out of mailbox area in deepholm
                new Decorator(
                    ret => StyxWoW.Me.Location.Distance(new WoWPoint(1015.401, 511.04, -49.33381)) < 150,
                    new Action(ret => Navigator.MoveTo(new WoWPoint(809.4955, 494.7677, -68.3133)))),

                new Decorator(
                    ret => Profile.CurrentProfile.Current.Distance2DSqr(StyxWoW.Me.Location) <= FlyingSpeedSqr,
                        new Action(ret => Profile.CurrentProfile.DequeueHotspot())),

                new Decorator(
                    ret => LootTargeting.Instance.FirstObject != null,
                    new PrioritySelector(
                        new Decorator(
                            ret => Profile.CurrentProfile.Path.Count > 2 &&
                                    Profile.CurrentProfile.Path.ElementAt(1).Distance2D(Profile.CurrentProfile.Path.Peek()) >
                                    Profile.CurrentProfile.Path.ElementAt(1).Distance2D(LootTargeting.Instance.FirstObject.Location),
                            new Action(ret => Profile.CurrentProfile.DequeueHotspot())),

                        new Decorator(
                            ret => lastNode == null || lastNode != LootTargeting.Instance.FirstObject,
                            new Sequence(
                                new Action(ret => harvestAttempts = 0),
                                new Action(ret => nodeTimer.Reset()),
                                new Action(ret => nodeTimer.Start()),
                                new Action(ret => movePoint = WoWPoint.Zero),
                                new Action(ret => lastNode = LootTargeting.Instance.FirstObject))),

                        new Decorator(
                            ret => lastNode != null && harvestAttempts >= 3 &&
                                    LootTargeting.Instance.FirstObject == lastNode,
                            new Action(ret =>
                                {
                                    harvestAttempts = 0;
                                    movePoint = WoWPoint.Zero;
                                    BlacklistNodes.Add(lastNode.Location);
                                    Blacklist.Add(lastNode.Guid, TimeSpan.FromMinutes(15));
                                    Log(GatherBuddyResources.BlacklistingCurrentNodeAfter3FailedAttempts);
                                    nodeTimer.Reset();
                                    lastNode = null;
                                })),

                        new Decorator(
                            ret => nodeTimer.Elapsed.Seconds > GatherbuddySettings.Instance.BlacklistTimer,
                            new Action(ret =>
                                {
                                    movePoint = WoWPoint.Zero;
                                    Log(GatherBuddyResources.BlacklistingCurrentNodeAfter__0__seconds, GatherbuddySettings.Instance.BlacklistTimer);
                                    BlacklistNodes.Add(lastNode.Location);
                                    Blacklist.Add(lastNode.Guid, TimeSpan.FromMinutes(15));
                                    nodeTimer.Reset();
                                    lastNode = null;
                                })),

                        new PrioritySelector(
                            ctx =>
                                {
                                    if (LootTargeting.Instance.FirstObject.Distance2DSqr <= 5*5 && nodeTimer.Elapsed.Seconds >= 10)
                                    {
                                        return LootTargeting.Instance.FirstObject.Location;
                                    }

                                    if (movePoint != WoWPoint.Zero)
                                    {
                                        return movePoint;
                                    }

                                    var pointsAroundNode = new List<WoWPoint>();

                                    var nodeLocation = LootTargeting.Instance.FirstObject.Location;
                                    var neededFacing = WoWMathHelper.CalculateNeededFacing(StyxWoW.Me.Location,
                                                                                           nodeLocation);

                                    // Front
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing - (float)Math.PI, 3f));
                                    // Front-Right 
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing - (float)Math.PI / 4 * 3, 3f));
                                    // Right
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing - (float)Math.PI / 2, 3f));
                                    // Back-Right
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing - (float)Math.PI / 4, 3f));
                                    // Back of node
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing, 3f));
                                    // Back-Left
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing + (float)Math.PI / 4, 3f));
                                    // Left side of node
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing + (float)Math.PI / 2, 3f));
                                    // Front-Left
                                    pointsAroundNode.Add(nodeLocation.RayCast(neededFacing + (float)Math.PI / 4 * 3, 3f));

                                    WoWPoint[] pointsAtGround;
                                    bool[] hitResults;

                                    GameWorld.MassTraceLine(
                                        pointsAroundNode.Select(p => new WorldLine(p.Add(0, 0, 20), p.Add(0, 0, -20))).
                                            ToArray(), GameWorld.CGWorldFrameHitFlags.HitTestGroundAndStructures, out hitResults,
                                        out pointsAtGround);

                                    var bestLandingPoint = pointsAtGround.Where(p => nodeLocation.DistanceSqr(p) <= 4f * 4f).OrderByDescending(p => p.Z).FirstOrDefault();

                                    //var bestLandingPoint =
                                    //    pointsAtGround.Where(p => 
                                    //                        Navigator.CanNavigateFully(p, nodeLocation)).
                                    //                   OrderBy(p =>
                                    //                        Math.Abs(WoWMathHelper.NormalizeRadian(WoWMathHelper.CalculateNeededFacing(nodeLocation, p)) -
                                    //                                 WoWMathHelper.NormalizeRadian(WoWMathHelper.CalculateNeededFacing(lowestPoint, nodeLocation)))).
                                    //                   FirstOrDefault();

                                    movePoint = bestLandingPoint != WoWPoint.Zero ? bestLandingPoint : nodeLocation.Add(0,0,2f);

                                    return movePoint;
                                },
                            new Decorator(
                                ret => ((WoWPoint)ret).DistanceSqr(StyxWoW.Me.Location) >= 2.5f * 2.5f &&
                                       (StyxWoW.Me.IsFlying || !LootTargeting.Instance.FirstObject.WithinInteractRange),
                                new Sequence(
                                    new Action(ret => Log(GatherBuddyResources.ApproachingNodeNameAtLocation, LootTargeting.Instance.FirstObject.Name,
                                        LootTargeting.Instance.FirstObject.Location.X, LootTargeting.Instance.FirstObject.Location.Y, LootTargeting.Instance.FirstObject.Location.Z)),
                                    new ActionSetActivity(GatherBuddyResources.MovingToNode),
                                    new DecoratorContinue(
                                        ret => StyxWoW.Me.MovementInfo.IsDescending,
                                        new Action(ret => WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend))),
                                    new Action(ret => Flightor.MoveTo((WoWPoint)ret))
                                    ))),

                        new Decorator(
                            ret => StyxWoW.Me.MovementInfo.IsFlying,
                            new Sequence(
                                new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.Descend)),
                                new WaitContinue(
                                    1, ret => !StyxWoW.Me.MovementInfo.IsFlying,
                                    new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.Forward))),
                                new WaitContinue(
                                    1, ret => !StyxWoW.Me.MovementInfo.IsFlying,
                                    new ActionAlwaysSucceed()),
                                new DecoratorContinue(
                                    ret => StyxWoW.Me.MovementInfo.IsFlying || LootTargeting.Instance.FirstObject.DistanceSqr > 5 * 5,
                                    new Action(ret => Flightor.MountHelper.Dismount())),
                                new Action(ret => Navigator.PlayerMover.MoveStop()))),

                        new Decorator(
                            ret => !LootTargeting.Instance.FirstObject.WithinInteractRange,
                            new PrioritySelector(
                                ctx => LootTargeting.Instance.FirstObject.Location,
                                new Decorator(
                                    ret => !StyxWoW.Me.HasAura("Sea Legs") && Navigator.CanNavigateFully(StyxWoW.Me.Location, (WoWPoint)ret),
                                    new Action(ret => Navigator.MoveTo((WoWPoint)ret))),
                                new Action(ret => WoWMovement.ClickToMove((WoWPoint)ret)))),

                        new Sequence(
                            ctx => LootTargeting.Instance.FirstObject,
                            new WaitContinue(
                                5, ret => !StyxWoW.Me.IsFalling,
                                new Action(ret => WoWMovement.MoveStop())),
                            new WaitContinue(
                                1, ret => !StyxWoW.Me.IsMoving,
                                new Action(ret => WoWMovement.MoveStop())),
                            new Action(ret => WoWMovement.MoveStop()),
                            new ActionSetActivity(GatherBuddyResources.InteractingWithNode),
                            new Action(ret => harvestAttempts++),
                            new DecoratorContinue(
                                ret => ret is WoWGameObject && GatherbuddySettings.Instance.FaceNodes,
                                new Action(ret => StyxWoW.Me.SetFacing((WoWGameObject)ret))),
                            new Action(ret => StyxWoW.SleepForLagDuration()),
                            new Action(ret => ((WoWObject)ret).Interact()),
                // Wait for the loot frame...
                            new Wait(
                                5, ret => LootFrame.Instance.IsVisible || StyxWoW.Me.Combat || StyxWoW.Me.IsFlying ||
                                          (WoWObject)ret == null ||
                                          (WoWObject)ret != lastNode,
                                new Action(
                                    ret =>
                                    {
                                        nodeTimer.Reset();
                                        if (StyxWoW.Me.Combat)
                                            return;

                                        Blacklist.Add(((WoWObject)ret).Guid, TimeSpan.FromSeconds(2));
                                        var obj = ObjectManager.GetAnyObjectByGuid<WoWObject>(LootFrame.Instance.LootingObjectGuid);
                                        // This happens sometimes, when we're moving slightly too fast. Just ignore it if its null.
                                        if (obj == null)
                                            return;

                                        string name = obj.Name;

                                        if (obj is WoWGameObject)
                                        {
                                            if (!NodeCollectionCount.ContainsKey(name))
                                            {
                                                NodeCollectionCount.Add(name, 0);
                                            }

                                            NodeCollectionCount[name]++;
                                        }

                                        LootFrame.Instance.LootAll();
                                        StyxWoW.SleepForLagDuration();
                                        movePoint = WoWPoint.Zero;

                                        Log("{0} {1}.", (obj is WoWGameObject) ? GatherBuddyResources.Harvested : GatherBuddyResources.Looted, name);
                                    })),

                            new Decorator(ret => Vendors.ForceSell && GatherbuddySettings.Instance.UseGuildVault,
                                new Action(ret => CreateVendorBehavior()))
                            ))),

                new Decorator(
                    ret => StyxWoW.Me.MovementInfo.IsSwimming && !StyxWoW.Me.HasAura("Sea Legs"),
                    new Sequence(
                        new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend)),
                        new Wait(
                                20, ret => StyxWoW.Me.MovementInfo.JumpingOrShortFalling,
                                new Sequence(
                                    new Action(ret => WoWMovement.MoveStop()),
                                    new Wait(
                                        5, ret => !StyxWoW.Me.MovementInfo.JumpingOrShortFalling ||
                                                   StyxWoW.Me.Class == WoWClass.Druid,
                                        new Action(
                                            ret =>
                                            {
                                                Flightor.MountHelper.MountUp();
                                                Thread.Sleep(100);
                                                WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
                                                Thread.Sleep(200);
                                                WoWMovement.MoveStop();
                                            })))))),
                new Decorator(
                    ret => LootTargeting.Instance.FirstObject == null,
                    new Sequence(
                        new ActionSetActivity(GatherBuddyResources.MovingToNextHotspot),
                        new DecoratorContinue(
                            ret => StyxWoW.Me.MovementInfo.IsDescending,
                            new Action(ret => WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend))),
                        new Action(ret => Flightor.MoveTo(Profile.CurrentProfile.Current)),
                        // Make sure we don't get afk flagged when flying around.
                        new ActionIdle()
                        ))
                );
        }

        #endregion

        #region Death Behavior

        private Composite CreateDeathBehavior()
        {
			return
				new PrioritySelector(
					new Decorator(
						ret => StyxWoW.Me.IsDead,
						LevelBot.CreateDeathBehavior()),

					new Decorator(
						ret => StyxWoW.Me.IsGhost,
						new PrioritySelector(
							new Decorator(
								ret => GatherbuddySettings.Instance.UseSpiritHealer ||
                                       (!StyxWoW.Me.MovementInfo.CanFly && !Navigator.CanNavigateFully(StyxWoW.Me.Location, StyxWoW.Me.CorpsePoint) &&
                                       !StyxWoW.Me.HasAura("Sea Legs")),
								new Action(delegate
									{
										var spiritHealer = ObjectManager.GetObjectsOfType<WoWUnit>(false, false).
															FirstOrDefault(u => u.Entry == 6491);

										if (spiritHealer == null)
										{
											dLog("Couldn't find a spirit healer around");
											return RunStatus.Failure;
										}

										if (spiritHealer.Distance > 3)
										{
                                            if (StyxWoW.Me.HasAura("Sea Legs"))
                                                Flightor.MoveTo((spiritHealer.Location));
                                            else
											    Navigator.MoveTo(spiritHealer.Location);

											return RunStatus.Success;
										}
                                        
										spiritHealer.Interact();
										Thread.Sleep(1000);
										Lua.DoString("StaticPopup1Button1:Click()");
										Thread.Sleep(500);
										Lua.DoString("StaticPopup1Button1:Click()");
										Thread.Sleep(2000);
										return RunStatus.Success;
									})),

                            new Decorator(
                                ret => StyxWoW.Me.HasAura("Sea Legs") && StyxWoW.Me.CorpsePoint.Distance(StyxWoW.Me.Location) > 5,
                                new Sequence(
                                    new ActionSetActivity(GatherBuddyResources.CorpseRun),
                                    new Action(ret => Flightor.MoveTo(StyxWoW.Me.CorpsePoint)))),

							LevelBot.CreateDeathBehavior())));
        }

        #endregion

        #region Vendor Behavior

        private Composite CreateVendorBehavior()
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret =>
                            {
                                if (Vendors.ForceSell)
                                    return true;

                                if (!GatherbuddySettings.Instance.GatherHerbs && !GatherbuddySettings.Instance.GatherMinerals)
                                {
                                    if (StyxWoW.Me.FreeBagSlots >= ProfileManager.CurrentProfile.MinFreeBagSlots)
                                        return false;

                                    return true;
                                }

                                if ((!GatherbuddySettings.Instance.GatherHerbs || BagHelper.EmptyHerbSlots <= ProfileManager.CurrentProfile.MinFreeBagSlots) &&
                                    (!GatherbuddySettings.Instance.GatherMinerals || BagHelper.EmptyMineSlots <= ProfileManager.CurrentProfile.MinFreeBagSlots))
                                    return true;

                                return false;
                            },
                        new PrioritySelector(
                            new Decorator(
                                ret => !StyxWoW.Me.IsFlying,
                                new PrioritySelector(
                                    new Decorator(
                                        ret => GatherbuddySettings.Instance.UseGuildVault,
                                        CreateGuildVaultBehavior()),
                                    new Decorator(
                                        ret => GatherbuddySettings.Instance.HearthAndExit,
                                        CreateHearthAndExitBehavior()))),
							new Decorator(
								ret => GatherbuddySettings.Instance.MailToAlt,
								CreateMailBehavior()))),
					new Decorator(
						ret => (StyxWoW.Me.DurabilityPercent <= ProfileManager.CurrentProfile.MinDurability ||
                               Vendors.ForceSell) &&
							   ProfileManager.CurrentProfile.MinDurability > 0 &&
							   ProfileManager.CurrentProfile.VendorManager.GetClosestVendor(Vendor.VendorType.Repair) != null,
						CreateRepairBehavior())	   
				);
        }

        #endregion

		#region Repair Behavior

		private static List<WoWPoint> vashjirBreadcrumb = new List<WoWPoint>()
		{
			new WoWPoint(-5948.648, 4386.404, -199.5479),	
			new WoWPoint(-5892.502, 4363.512, -325.8399),
			new WoWPoint(-6012.673, 4250.345, -367.7086),
			new WoWPoint(-6108.896, 4276.694, -351.7908),
			new WoWPoint(-6111.735, 4298.362, -348.4731)
		};

        private static List<WoWPoint> deepholmBreadcrumbHorde = new List<WoWPoint>
        {
            new WoWPoint(808.2299, 484.603, -54.59374),
            new WoWPoint(992.6205, 522.054, -37.29947),
            new WoWPoint(1052.455, 487.4484, -36.99965),
            new WoWPoint(1090.473, 435.7533, -45.93764)
        };

        private static List<WoWPoint> deepholmBreadcrumbAlliance = new List<WoWPoint>
        {
            new WoWPoint(847.9105f, 486.2112f, -33.11148f),
            new WoWPoint(980.3354f, 518.0457f, -35.31759f),
            new WoWPoint(1021.139f, 553.0032f, -37.98148f),
            new WoWPoint(1052.84f, 638.6432f, -45.02334f)
        };

		private Composite CreateRepairBehavior()
		{
			return
				new PrioritySelector(
					new Action(delegate
						{
							repairVendor = ProfileManager.CurrentProfile.VendorManager.GetClosestVendor(Vendor.VendorType.Repair);
							sellVendor = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == repairVendor.Entry);
							return RunStatus.Failure;
						}),

					new Decorator(
						ret => repairVendor.Location.Distance(StyxWoW.Me.Location) < 3,
						new Sequence(
						new ActionSetActivity(GatherBuddyResources.RepairingItems),
						new Action(ret => vashjirBreadcrumb = new List<WoWPoint>()
							{
								new WoWPoint(-5948.648, 4386.404, -199.5479),	
								new WoWPoint(-5892.502, 4363.512, -325.8399),
								new WoWPoint(-6012.673, 4250.345, -367.7086),
								new WoWPoint(-6108.896, 4276.694, -351.7908),
								new WoWPoint(-6111.735, 4298.362, -348.4731)
							}),
                        new Action(ret => deepholmBreadcrumbHorde = new List<WoWPoint>
                            {
                                new WoWPoint(808.2299, 484.603, -54.59374),
                                new WoWPoint(992.6205, 522.054, -37.29947),
                                new WoWPoint(1052.455, 487.4484, -36.99965),
                                new WoWPoint(1090.473, 435.7533, -45.93764)                                            
                            }),
                        new Action(ret => deepholmBreadcrumbAlliance = new List<WoWPoint>
                            {
                                new WoWPoint(847.9105f, 486.2112f, -33.11148f),
                                new WoWPoint(980.3354f, 518.0457f, -35.31759f),
                                new WoWPoint(1021.139f, 553.0032f, -37.98148f),
                                new WoWPoint(1052.84f, 638.6432f, -45.02334f)
                            }),
						new Action(ret => WoWMovement.MoveStop()),
						new Action(ret => StyxWoW.SleepForLagDuration()),
						new Action(ret => sellVendor.Interact()),
						new Action(ret => Thread.Sleep(1000)),
                        new DecoratorContinue(
                            ret => !MerchantFrame.Instance.IsVisible && GossipFrame.Instance.IsVisible,
                            new Action(ret => GossipFrame.Instance.SelectGossipOption(1))),
						new Action(ret =>
						{
							if (GatherbuddySettings.Instance.RepairFromGuildBank &&	CanGuildBankRepair)
								Lua.DoString("RepairAllItems(1)");
							else
								Vendors.RepairAllItems();
						}),
						new Action(ret => Thread.Sleep(1000)),
						new Action(ret =>
							{
								sellVendor = null;
								Vendors.ForceSell = false;
							}))),

					new Decorator(
						ret => StyxWoW.Me.HasAura("Sea Legs"),
						new PrioritySelector(
							new Sequence(
								new ActionSetActivity(GatherBuddyResources.MovingToRepair),
								new PrioritySelector(
									new Decorator(
										ret => StyxWoW.Me.Location.Distance(vashjirBreadcrumb[0]) > 3,
										new Action(ret => Flightor.MoveTo(vashjirBreadcrumb[0]))),
									new Action(ret =>
										{
											vashjirBreadcrumb = new List<WoWPoint>() 
											{
												vashjirBreadcrumb[1],
												vashjirBreadcrumb[2],
												vashjirBreadcrumb[3],
												vashjirBreadcrumb[4],
												vashjirBreadcrumb[0]
								
											};
										})))
							)),

                    new Decorator(
                        ret => StyxWoW.Me.ZoneId == 5042 && StyxWoW.Me.IsHorde,
                        new PrioritySelector(
                            new Sequence(
                                new ActionSetActivity(GatherBuddyResources.MovingToRepair),
                                new PrioritySelector(
                                    new Decorator(
                                        ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbHorde[0]) > 3,
                                        new Action(ret => Flightor.MoveTo(deepholmBreadcrumbHorde[0]))),
                                    new Action(ret =>
                                    {
                                        deepholmBreadcrumbHorde = new List<WoWPoint>() 
											{
												deepholmBreadcrumbHorde[1],
												deepholmBreadcrumbHorde[2],
												deepholmBreadcrumbHorde[3],
												deepholmBreadcrumbHorde[0]
								
											};
                                    })))
                            )),

                    new Decorator(
                        ret => StyxWoW.Me.ZoneId == 5042 && StyxWoW.Me.IsAlliance,
                        new PrioritySelector(
                            new Sequence(
                                new ActionSetActivity(GatherBuddyResources.MovingToRepair),
                                new PrioritySelector(
                                    new Decorator(
                                        ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbAlliance[0]) > 3,
                                        new Action(ret => Flightor.MoveTo(deepholmBreadcrumbAlliance[0]))),
                                    new Action(ret =>
                                    {
                                        deepholmBreadcrumbAlliance = new List<WoWPoint>() 
											{
												deepholmBreadcrumbAlliance[1],
												deepholmBreadcrumbAlliance[2],
												deepholmBreadcrumbAlliance[3],
												deepholmBreadcrumbAlliance[0]
								
											};
                                    })))
                            )),

					new Decorator(
						ret => sellVendor != null && sellVendor.IsValid &&
							(!sellVendor.IsOutdoors || GameWorld.TraceLine(sellVendor.Location, sellVendor.Location.Add(0, 0, 30), GameWorld.CGWorldFrameHitFlags.HitTestGroundAndStructures)),
						new Action(ret =>
							{
								WoWUnit outsideUnit = ObjectManager.GetObjectsOfType<WoWUnit>().
														Where(u => u.IsValid && u.IsOutdoors && !GameWorld.TraceLine(u.Location, u.Location.Add(0, 0, 30), GameWorld.CGWorldFrameHitFlags.HitTestGroundAndStructures)).
														OrderBy(u => u.Location.Distance(repairVendor.Location)).
														FirstOrDefault();

								if (outsideUnit != null && outsideUnit.Distance > 3 && Flightor.MountHelper.Mounted)
								{
									Flightor.MoveTo(outsideUnit.Location);
									return;
								}

								if (Flightor.MountHelper.Mounted)
								{
									Flightor.MountHelper.Dismount();
									Thread.Sleep(100);
								}

								Navigator.MoveTo(repairVendor.Location);
							})),

					new Sequence(
						new ActionSetActivity(GatherBuddyResources.MovingToRepair),
						new Action(ret => Flightor.MoveTo(repairVendor.Location)))
					);
		}

		#endregion

		#region Mail Behavior

		private Composite CreateMailBehavior()
        {
            return
				new PrioritySelector(
					new Decorator(
						ret => ProfileManager.CurrentProfile.MailboxManager == null,
						new Action(ret => Log(GatherBuddyResources.MailBoxWarning1))),
						
					new Decorator(
                        ret => string.IsNullOrEmpty(CharacterSettings.Instance.MailRecipient),
						new Action(ret => Log(GatherBuddyResources.MailBoxWarning2))),

					new Action(delegate
						{
							itemsToDeposit = StyxWoW.Me.BagItems.FindAll(ShouldMail);
							return RunStatus.Failure;
						}),

                    new Decorator(
                        ret => ClosestMailbox.Location.Distance(StyxWoW.Me.Location) > 3f &&
                               StyxWoW.Me.ZoneId == 5042 && StyxWoW.Me.IsHorde,
                        new PrioritySelector(
                            new Sequence(
                                new ActionSetActivity(GatherBuddyResources.MovingToMailbox),
                                new PrioritySelector(
                                    new Decorator(
                                        ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbHorde[0]) > 3,
                                        new PrioritySelector(
                                            new Decorator(
                                                ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbHorde[0]) < 200 &&
                                                       GameWorld.IsInLineOfSight(StyxWoW.Me.GetTraceLinePos(), deepholmBreadcrumbHorde[0]),
                                                new Action(ret => WoWMovement.ClickToMove(deepholmBreadcrumbHorde[0]))),
                                            new Action(ret => Flightor.MoveTo(deepholmBreadcrumbHorde[0])))),
                                    new Action(ret =>
                                    {
                                        deepholmBreadcrumbHorde = new List<WoWPoint>() 
											{
												deepholmBreadcrumbHorde[1],
												deepholmBreadcrumbHorde[2],
												deepholmBreadcrumbHorde[3],
												deepholmBreadcrumbHorde[0]
											};
                                    })))
                            )),

                    new Decorator(
                        ret => ClosestMailbox.Location.Distance(StyxWoW.Me.Location) > 3f &&
                               StyxWoW.Me.ZoneId == 5042 && StyxWoW.Me.IsAlliance,
                        new PrioritySelector(
                            new Sequence(
                                new ActionSetActivity(GatherBuddyResources.MovingToMailbox),
                                new PrioritySelector(
                                    new Decorator(
                                        ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbAlliance[0]) > 3,
                                        new PrioritySelector(
                                            new Decorator(
                                                ret => StyxWoW.Me.Location.Distance(deepholmBreadcrumbAlliance[0]) < 200 &&
                                                       GameWorld.IsInLineOfSight(StyxWoW.Me.GetTraceLinePos(), deepholmBreadcrumbAlliance[0]),
                                                new Action(ret => WoWMovement.ClickToMove(deepholmBreadcrumbAlliance[0]))),
                                            new Action(ret => Flightor.MoveTo(deepholmBreadcrumbAlliance[0])))),
                                    new Action(ret =>
                                    {
                                        deepholmBreadcrumbAlliance = new List<WoWPoint>() 
											{
												deepholmBreadcrumbAlliance[1],
												deepholmBreadcrumbAlliance[2],
												deepholmBreadcrumbAlliance[3],
												deepholmBreadcrumbAlliance[0]
											};
                                    })))
                            )),

					new Decorator(
						ret => ClosestMailbox.Location.Distance(StyxWoW.Me.Location) > 3f &&
							   StyxWoW.Me.HasAura("Sea Legs"),
						new PrioritySelector(
							new Sequence(
								new ActionSetActivity(GatherBuddyResources.MovingToMailbox),
								new PrioritySelector(
									new Decorator(
										ret => StyxWoW.Me.Location.Distance(vashjirBreadcrumb[0]) > 3,
										new PrioritySelector(
											new Decorator(
												ret => StyxWoW.Me.Location.Distance(vashjirBreadcrumb[0]) < 200 &&
													   GameWorld.IsInLineOfSight(StyxWoW.Me.GetTraceLinePos(), vashjirBreadcrumb[0]),
												new Action(ret => WoWMovement.ClickToMove(vashjirBreadcrumb[0]))),
											new Action(ret => Flightor.MoveTo(vashjirBreadcrumb[0])))),
									new Action(ret =>
										{
											vashjirBreadcrumb = new List<WoWPoint>() 
											{
												vashjirBreadcrumb[1],
												vashjirBreadcrumb[2],
												vashjirBreadcrumb[3],
												vashjirBreadcrumb[4],
												vashjirBreadcrumb[0]
											};
										})))
							)),

					new Decorator(
						ret => itemsToDeposit.Count > 0,
						new PrioritySelector(
							new Decorator(
								ret => ClosestMailbox.Location.Distance(StyxWoW.Me.Location) > 3f,
								new Sequence(
									new ActionSetActivity(GatherBuddyResources.MovingToMailbox),
									new Action(ret => Flightor.MoveTo(ClosestMailbox.Location)))),
							new Sequence(
								new ActionSetActivity("Mailing items"),
								new Action(ret => vashjirBreadcrumb = new List<WoWPoint>()
								{
									new WoWPoint(-5948.648, 4386.404, -199.5479),	
									new WoWPoint(-5892.502, 4363.512, -325.8399),
									new WoWPoint(-6012.673, 4250.345, -367.7086),
									new WoWPoint(-6108.896, 4276.694, -351.7908),
									new WoWPoint(-6111.735, 4298.362, -348.4731)
								}),
                                new Action(ret => deepholmBreadcrumbHorde = new List<WoWPoint>
                                {
                                    new WoWPoint(808.2299, 484.603, -54.59374),
                                    new WoWPoint(992.6205, 522.054, -37.29947),
                                    new WoWPoint(1052.455, 487.4484, -36.99965),
                                    new WoWPoint(1090.473, 435.7533, -45.93764)                                            
                                }),
                                new Action(ret => deepholmBreadcrumbAlliance = new List<WoWPoint>
                                {
                                    new WoWPoint(847.9105f, 486.2112f, -33.11148f),
                                    new WoWPoint(980.3354f, 518.0457f, -35.31759f),
                                    new WoWPoint(1021.139f, 553.0032f, -37.98148f),
                                    new WoWPoint(1052.84f, 638.6432f, -45.02334f)                                   
                                }),
								//new Action(ret => Vendors.ForceSell = true),
								new Action(ret => WoWMovement.MoveStop()),
								new Action(ret => ObjectManager.GetObjectsOfType<WoWGameObject>(false, false).
													OrderBy(o => o.Distance).
													FirstOrDefault(o => o.SubType == WoWGameObjectType.Mailbox).Interact()),
								new Action(ret => StyxWoW.SleepForLagDuration()),
								new Action(ret =>
									{
										var mailFrame = MailFrame.Instance;

										if (mailFrame != null)
										{
                                            mailFrame.SendMailWithManyAttachments(CharacterSettings.Instance.MailRecipient, 0, itemsToDeposit.ToArray());
											itemsToDeposit.Clear();

                                            if (!StyxWoW.Me.HasAura("Sea Legs") || !ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Any(u => u.IsRepairMerchant))
										        Vendors.ForceSell = false;
										}
									})))),
                    
					new Decorator(
						ret => StyxWoW.Me.HasAura("Sea Legs") && 
                               ObjectManager.GetObjectsOfType<WoWUnit>(false, false).Any(u => u.IsRepairMerchant),
						new PrioritySelector(
							new Decorator(
								ret => sellVendor == null,
								new Action(ret =>
									{
										sellVendor = ObjectManager.GetObjectsOfType<WoWUnit>(false, false).
														OrderBy(u => u.Distance).
														FirstOrDefault(u => u.IsValid && u.IsRepairMerchant);

										if (sellVendor == null)
										{
											Vendors.ForceSell = false;
										}
									})),
							new Decorator(
								ret => sellVendor.Distance > 3,
								new Sequence(
									new ActionSetActivity(GatherBuddyResources.MovingToRepairVendor),
									new Action(ret => Navigator.MoveTo(sellVendor.Location)))),
							new Sequence(
								new ActionSetActivity(GatherBuddyResources.RepairingItems),
								new Action(ret => WoWMovement.MoveStop()),
								new Action(ret => StyxWoW.SleepForLagDuration()),
								new Action(ret => sellVendor.Interact()),
								new Action(ret => Thread.Sleep(1000)),
								new Action(ret =>
									{
										if (GatherbuddySettings.Instance.RepairFromGuildBank && CanGuildBankRepair)
											Lua.DoString("RepairAllItems(1)");
										else
											Vendors.RepairAllItems();
									}),
								new Action(ret => Thread.Sleep(1000)),
								new Action(ret =>
									{
										sellVendor = null;
										Vendors.ForceSell = false;
									})))),
                            new Action(ret => Vendors.ForceSell = false));
        }

        #endregion

        #region GuildVault Behavior

		private static Dictionary<uint, List<WoWPoint>> guildVaults = new Dictionary<uint, List<WoWPoint>>
		{
			{
				1, //Kalimdor
				new List<WoWPoint>
				{
					new WoWPoint(1525.875, -4357.25, 20.54739),
					new WoWPoint(-1257.304, 61.9744, 127.814),
					new WoWPoint(9940.057, 2512.175, 1317.814),
					new WoWPoint(-919.7414, -3726.816, 9.326906),
					new WoWPoint(-7198.918, -3832.021, 8.721869),
					new WoWPoint(6776.943, -4670.174, 723.824)
				}
			},

			{
				0, //EK
				new List<WoWPoint>
				{
					new WoWPoint(1596.407, 227.1625, -54.29356),
					new WoWPoint(-1256.054, 64.11891, 127.7665),
					new WoWPoint(-8927.632, 615.8686, 100.2248),
					new WoWPoint(-4903.585, -996.6494, 503.9406),
					new WoWPoint(-14418.86, 520.1663, 5.012219)
				}
			},

			{
				530, //Outland
				new List<WoWPoint>
				{
					new WoWPoint(9805.942, -7486.653, 14.35392),
					new WoWPoint(-3909.765, -11551.3, -150.0554),
					new WoWPoint(-1989.722, 5322.523, -7.070733),
					new WoWPoint(2935.042, 3649.285, 132.5779)
				}
			},

			{
				571, //Northrend
				new List<WoWPoint>
				{
					new WoWPoint(5633.903, 670.1196, 651.9932),
                    new WoWPoint(5973.477, 631.1976, 650.6271)
				}
			},

			{
			    870, // Pandaria
                new List<WoWPoint>
                {
                    new WoWPoint(1696.267, 992.2201,486.9832),
                    new WoWPoint(724.3276, 266.1123, 519.6915)
                }
			}
		};

        private Composite CreateGuildVaultBehavior()
        {
            return
                new PrioritySelector(
					new Action(delegate
						{
							guildVault = ObjectManager.GetObjectsOfType<WoWGameObject>().
									Where(o => o.SubType == WoWGameObjectType.GuildBank && (o.Entry != 187299 || StyxWoW.Me.ZoneId != 4395)). // Extra check for Dalaran underground guild vault
									OrderBy(o => o.DistanceSqr).
									FirstOrDefault();
							return RunStatus.Failure;
						}),

                    new Decorator(
                        ret => guildVault == null,
						new PrioritySelector(
							new Decorator(
								ret => StyxWoW.Me.BagItems.Any(i => i.Entry == 6948) && StyxWoW.Me.BagItems.First(i => i.Entry == 6948).Cooldown == 0,
								new Sequence(
									new ActionSetActivity(GatherBuddyResources.GuildVaultRunUsingHearthstone),
									new Action(
										ret =>
										{
											WoWMovement.MoveStop();
											Thread.Sleep(200);
											StyxWoW.Me.BagItems.First(i => i.Entry == 6948).Interact();
											StyxWoW.SleepForLagDuration();
										}),
									new Wait(15, ret => StyxWoW.Me.BaseAddress == (IntPtr) 0 || 
														StyxWoW.Me.Combat ||
														StyxWoW.Me.IsMoving,
										new Action(ret => Thread.Sleep(200)))
									)),
							new Action(ret =>
							    {
							        uint continentID = StyxWoW.Me.MapId;
									if (guildVaults.ContainsKey(continentID))
									{
										WoWPoint closestGV = guildVaults[continentID].OrderBy(g => g.DistanceSqr(StyxWoW.Me.Location)).FirstOrDefault();
										Navigator.MoveTo(closestGV);
									}
								}))),

                    new Decorator(
                        ret => StyxWoW.Me.IsOutdoors && takeOffPoint == WoWPoint.Empty,
                        new Action(ret => takeOffPoint = StyxWoW.Me.Location)),

                    new Decorator(
                        ret => guildVault.Distance2D > 4f,
                        new Sequence(
                            new ActionSetActivity(GatherBuddyResources.MovingToGuildVault),
							new Action(ret => Navigator.MoveTo(guildVault.Location))
                            )),

                    new Sequence(
                        new Action(ret => WoWMovement.MoveStop()),
                        new Action(ret => StyxWoW.SleepForLagDuration()),
                        new ActionSetActivity(GatherBuddyResources.DepositingItemsToGuildVault),
						new Action(ret => guildVault.Interact()),
                        new Action(ret => StyxWoW.SleepForLagDuration()),
                        new Action(ret =>
                            {
                                uint foodId = 0;

                                if (Consumable.GetBestFood(false) != null)
                                {
                                    foodId = Consumable.GetBestFood(false).Entry;
                                }
                                uint drinkId = 0;

                                if (Consumable.GetBestDrink(false) != null)
                                {
                                    drinkId = Consumable.GetBestDrink(false).Entry;
                                }

                                itemsToDeposit = StyxWoW.Me.BagItems.
                                FindAll(i => !ProfileManager.CurrentProfile.ProtectedItems.HashSet1.Contains(i.Entry) &&
                                             !i.IsSoulbound && i.Entry != foodId && i.Entry != drinkId);


                                numGuildBankTabs = Lua.GetReturnVal<int>("return GetNumGuildBankTabs()", 0);
                            }),
                        new Action(delegate
                            {
                                if (curGuildBankTab > numGuildBankTabs)
                                {
                                    Log(GatherBuddyResources.ThereIsNotEnoughSpaceInGuildVaultToDepositAllItemsStoppingBot);
                                    TreeRoot.Stop(GatherBuddyResources.ThereIsNotEnoughSpaceInGuildVaultToDepositAllItemsStoppingBot);
                                    return RunStatus.Success;
                                }

                                Lua.DoString(String.Format("GuildBankTab{0}Button:Click()", curGuildBankTab));
                                Thread.Sleep(250);

                                int emptySlots = Lua.GetReturnVal<int>(
                                        String.Format("local t = 0; " +
                                                    "for i=1,MAX_GUILDBANK_SLOTS_PER_TAB do " +
                                                        "texture, count, locked = GetGuildBankItemInfo({0}, i); " +
                                                        "if (count == 0) then " +
                                                            "t = t + 1; " +
                                                        "end " +
                                                    "end " +
                                                    "return t; ", curGuildBankTab), 0);

								for (int i = 0; i < emptySlots; i++)
								{
									if (itemsToDeposit.Count == i)
										break;

									WoWItem item = itemsToDeposit[i];
									Lua.DoString(String.Format("UseContainerItem({0},{1})", item.BagIndex + 1, item.BagSlot + 1));
									Thread.Sleep(500);
								}

                                int depositedItemsCount = emptySlots > itemsToDeposit.Count ? itemsToDeposit.Count : emptySlots;

                                itemsToDeposit.RemoveRange(0, depositedItemsCount);

                                if (emptySlots > 0)
                                {
                                    Log(GatherBuddyResources.Deposited__0__item_1__to_guild_vault_tab_2_, depositedItemsCount, depositedItemsCount > 1 ? "s" : "", curGuildBankTab);
                                }

                                if (itemsToDeposit.Count > 5)
                                {
                                    curGuildBankTab++;
                                    Thread.Sleep(1000);
                                    return RunStatus.Running;
                                }

                                Log(GatherBuddyResources.SuccessfullyDepositedAllItems);
                                curGuildBankTab = 1;
								guildVault = null;
                                itemsToDeposit.Clear();
                                Vendors.ForceSell = false;
                                return RunStatus.Success;
                            }))
                );
        }

        #endregion

        #region Hearth and Exit Behavior

        private Composite CreateHearthAndExitBehavior()
        {
            return
                new PrioritySelector(
                    new Sequence(
                            new ActionSetActivity(GatherBuddyResources.HearthAndExit),
                            new Action(
                                ret =>
                                    {
                                        var hearthstone = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == 6948);
                                        if (hearthstone != null)
                                        {
                                            hearthstone.UseContainerItem();
                                            StyxWoW.SleepForLagDuration();
                                            StyxWoW.SleepForLagDuration();
                                            StyxWoW.SleepForLagDuration();
                                        }
                                }),
                            new Wait(
                                15, ret => !StyxWoW.Me.IsCasting,
                                new Action(delegate
                                    {
                                        if (StyxWoW.Me.Combat)
                                            return RunStatus.Failure;

                                        Thread.Sleep(5000);
                                        Log(GatherBuddyResources.ExitingWoWAndHBHeartAndExitBehavior);
                                        StyxWoW.Memory.Process.Kill();
                                        Environment.Exit(12);
                                        return RunStatus.Success;
                                    }))
                            )
                    );
        }

        #endregion
    }
}
