using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchaeologyBuddy.Resources;
using Bots.Grind;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.Helpers;


using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;
using Action = Styx.TreeSharp.Action;

namespace Bots.ArchaeologyBuddy
{
	public partial class ArchBuddy
	{
		#region TestBehavior

	    internal static Composite TestBehavior()
		{
			return
				new PrioritySelector(
					new Action(delegate
						{
							return RunStatus.Failure;
						})
					);
		}

		#endregion

		#region Death Behavior

		private static Composite CreateDeathBehavior()
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
                                ret => !StyxWoW.Me.MovementInfo.CanFly && !Navigator.CanNavigateFully(StyxWoW.Me.Location, StyxWoW.Me.CorpsePoint) &&
                                       !StyxWoW.Me.HasAura("Sea Legs"),
                                new Action(delegate
                                {
                                    var spiritHealer = ObjectManager.GetObjectsOfType<WoWUnit>(false, false).
                                                        FirstOrDefault(unit => unit.Entry == 6491);

                                    if (spiritHealer == null)
                                    {
                                        return RunStatus.Failure;
                                    }

                                    if (spiritHealer.Distance > 3)
                                    {
                                        Navigator.MoveTo(spiritHealer.Location);
                                        return RunStatus.Success;
                                    }
                                    Log(ArchaeologyResources.ArchBuddyDeathBehaviorRezzingAtSpiritHealer);
                                    spiritHealer.Interact();
                                    Thread.Sleep(1000);
                                    Lua.DoString("StaticPopup1Button1:Click()");
                                    Thread.Sleep(500);
                                    Lua.DoString("StaticPopup1Button1:Click()");
                                    Thread.Sleep(2000);
                                    return RunStatus.Success;
                                })),

							LevelBot.CreateDeathBehavior())));
		}

		#endregion

		#region SolveBehavior

		internal static Composite SolveBehavior()
		{
			return
				new Decorator(ret => !StyxWoW.Me.Combat && !StyxWoW.Me.MovementInfo.IsFlying,
					new PrioritySelector(
						new Decorator(ret => GUI.ArchBuddySettings.Solve,
							new PrioritySelector(
								new Decorator(ret => GUI.ArchBuddySettings.SolveRace == 0,
									new Action(delegate
										{
											for (int i = 1; i <= 13; i++)
											{
												Lua.DoString(String.Format("SetSelectedArtifact({0})", i));
												List<string> artifactInfo = Lua.GetReturnValues("return GetSelectedArtifactInfo()");
												if (artifactInfo != null)
												{
													int numSockets = int.Parse(artifactInfo[5]);
													if (Instance.Settings.UseKeystones)
													{
														using (StyxWoW.Memory.AcquireFrame())
														{
															for (int k = 0; k < numSockets; k++)
															{
																Lua.DoString("SocketItemToArtifact()");
															}
														}
													}

													bool canSolve = Lua.GetReturnVal<int>("return CanSolveArtifact()", 0) == 1;
													string artifactName = artifactInfo[0];
													if (canSolve)
													{
														if (!SolveReport.ContainsKey(artifactName))
														{
															SolveReport.Add(artifactName, 0);
														}

														SolveReport[artifactName]++;
														new ActivitySetter(ArchaeologyResources.SolvingArtifact);
														Log(ArchaeologyResources.Solving, artifactName);
														Lua.DoString("SolveArtifact()");
                                                        Thread.Sleep(4000);

                                                        if (Instance.Settings.CrateArtifacts)
                                                        {
                                                            using (StyxWoW.Memory.AcquireFrame())
                                                            {
                                                                // Support for crated items
                                                                foreach (
                                                                    var item in
                                                                        StyxWoW.Me.BagItems.Where(
                                                                            item =>
                                                                            item.Usable &&
                                                                            item.ItemSpells.Any(s => s.Id == 126935)))
                                                                {
                                                                    Log("Crating item {0}", item.Name);
                                                                    item.Use();
                                                                } 
                                                            }
                                                        }
														return RunStatus.Success;
													}
												}
											}
											GUI.ArchBuddySettings.Solve = false;
											_configurationForm.Activate();
											return RunStatus.Success;
										})),

								new Decorator(ret => GUI.ArchBuddySettings.SolveRace != 0,
									new Action(delegate
										{
                                            int i = GUI.ArchBuddySettings.SolveRace;
                                            Lua.DoString(String.Format("SetSelectedArtifact({0})", i));
                                            List<string> artifactInfo =
                                                Lua.GetReturnValues("return GetSelectedArtifactInfo()");
                                            if (artifactInfo != null)
                                            {
                                                int numSockets = int.Parse(artifactInfo[5]);
                                                if (Instance.Settings.UseKeystones)
                                                {
                                                    using (StyxWoW.Memory.AcquireFrame())
                                                    {
                                                        for (int k = 0; k < numSockets; k++)
                                                        {
                                                            Lua.DoString("SocketItemToArtifact()");
                                                        }
                                                    }
                                                }
                                                bool canSolve =
                                                    Lua.GetReturnVal<int>("return CanSolveArtifact()", 0) == 1;
                                                string artifactName = artifactInfo[0];
                                                if (canSolve)
                                                {
                                                    if (!SolveReport.ContainsKey(artifactName))
                                                    {
                                                        SolveReport.Add(artifactName, 0);
                                                    }

                                                    SolveReport[artifactName]++;
                                                    Log(ArchaeologyResources.Solving, artifactName);
                                                    new ActivitySetter(ArchaeologyResources.SolvingArtifact);
                                                    Lua.DoString("SolveArtifact()");
                                                    Thread.Sleep(4000);

                                                    if (Instance.Settings.CrateArtifacts)
                                                    {
                                                        using (StyxWoW.Memory.AcquireFrame())
                                                        {
                                                            // Support for crated items
                                                            foreach (
                                                                var item in
                                                                    StyxWoW.Me.BagItems.Where(
                                                                        item =>
                                                                        item.Usable &&
                                                                        item.ItemSpells.Any(s => s.Id == 126935)))
                                                            {
                                                                Log("Crating item {0}", item.Name);
                                                                item.Use();
                                                            }
                                                        }
                                                    }
                                                    return RunStatus.Success;
                                                }

                                                GUI.ArchBuddySettings.Solve = false;
                                                _configurationForm.Activate();
                                            }
                                            else
                                            {
                                                GUI.ArchBuddySettings.Solve = false;
                                                _configurationForm.Activate();
                                            }
                                            return RunStatus.Success;
										}))))

						));
		}

		#endregion

		#region TrainBehavior

		private static readonly List<uint> TrainerIDs = new List<uint> { 44238, 47571 };
		private static readonly WoWPoint AllianceTrainer = new WoWPoint(-8325.824, 284.2659, 156.8329);
		private static readonly WoWPoint HordeTrainer = new WoWPoint(1596.883, -4381.322, 20.12522);
		private static WoWUnit _archTrainer;
		internal static Composite TrainBehavior()
		{
			return
				new PrioritySelector(
					new Decorator(
						ret => GUI.ArchBuddySettings.ForceTrain ||
							   (((StyxWoW.Me.IsHorde && StyxWoW.Me.MapId == 1) || 
							   (StyxWoW.Me.IsAlliance && StyxWoW.Me.MapId == 0)) &&
                               Instance.Settings.AutoTrain &&
							   StyxWoW.Me.GetSkill(SkillLine.Archaeology).MaxValue < Instance.Settings.TrainUpto &&
							   StyxWoW.Me.GetSkill(SkillLine.Archaeology).CurrentValue + 10 >= StyxWoW.Me.GetSkill(SkillLine.Archaeology).MaxValue),
						new PrioritySelector(
							new Action(delegate
								{
									_archTrainer = ObjectManager.GetObjectsOfType<WoWUnit>(false, false).FirstOrDefault(unit => TrainerIDs.Contains(unit.Entry)); 
									return RunStatus.Failure;
								}),
							new Decorator(
								ret => _archTrainer != null,
								new PrioritySelector(
									new Decorator(
										ret => _archTrainer.Distance < 3,
										new Sequence(
											new ActionSetActivity(ArchaeologyResources.TrainingArchaeology),
											new Action(ret => Log(ArchaeologyResources.TrainingArchaeologyAtSkill, StyxWoW.Me.GetSkill(SkillLine.Archaeology).CurrentValue)),
											new Action(ret => GUI.ArchBuddySettings.ForceTrain = false),
											new Action(ret => Navigator.PlayerMover.MoveStop()),
											new Action(ret => StyxWoW.SleepForLagDuration()),
											new Action(ret => _archTrainer.Interact()),
											new Wait(5, ret => TrainerFrame.Instance.IsVisible,
												new Action(ret => TrainerFrame.Instance.BuyAll()))
										)),
									new Decorator(
										ret => Flightor.MountHelper.Mounted,
										new Action(delegate
											{
												var movePoint = StyxWoW.Me.IsHorde ? HordeTrainer : AllianceTrainer;
												if (movePoint.Distance(StyxWoW.Me.Location) > 3)
												{
													Flightor.MoveTo(movePoint);
													return RunStatus.Success;
												}

												Flightor.MountHelper.Dismount();
												return RunStatus.Success;
											})),
									new Sequence(
										new ActionSetActivity(ArchaeologyResources.MovingToArchaeologyTrainer),
										new Action(ret => Navigator.MoveTo(_archTrainer.Location)))
									)),
							new Decorator(
								ret => StyxWoW.Me.IsHorde,
								new Sequence(
									new ActionSetActivity(ArchaeologyResources.MovingToArchaeologyTrainer),
									new Action(ret => Flightor.MoveTo(HordeTrainer)))),
							new Sequence(
								new ActionSetActivity(ArchaeologyResources.MovingToArchaeologyTrainer),
								new Action(ret => Flightor.MoveTo(AllianceTrainer)))
						))
				);
		}

		#endregion

		#region FindDigsiteBehavior

		internal static Composite FindDigsiteBehavior()
		{
			return
				new Decorator(ret => !IsInDigSite && ClosestDigSiteLocation == WoWPoint.Empty,
					new Action(delegate
					{
                        Navigator.Clear();
						Log(ArchaeologyResources.SearchingForDigsites);
                        new ActivitySetter(ArchaeologyResources.SearchingForDigsites);
						
						//if (StyxWoW.Me.MovementInfo.IsFlying)
						//{
						//    WoWMovement.Move(WoWMovement.MovementDirection.Descend);
						//    Thread.Sleep(200);
						//    return RunStatus.Success;
						//}
						//else
						//{
						//    WoWMovement.MoveStop();
						//}

						string[] t = Lua.GetReturnValues("local t = {}; " +
													"SetMapZoom(GetCurrentMapContinent()); " +
													"table.insert(t, GetCurrentMapAreaID()); " +
													"for i=1,GetNumMapLandmarks() do " +
														"name, _, textureIndex, x, y = GetMapLandmarkInfo(i); " +
														"if (textureIndex == 177) then " +
															"table.insert(t, name); " +
															"table.insert(t, x); " +
															"table.insert(t, y); " +
														"end " +
													"end " +
													"return unpack(t);").ToArray();

						uint mapid = uint.Parse(t[0]);
						List<Digsite> currentDigsites = new List<Digsite>();
                        try
                        {
                            var di = new DirectoryInfo(Utilities.AssemblyDirectory + @"\Logs");
                            u = di.GetFiles().OrderByDescending(i => i.LastWriteTime);
                        }
                        catch {  }
						for (int i = 1; i < t.Length; i += 3)
						{
                            WoWPoint p = MapToWorldCoords(float.Parse(t[i + 1], CultureInfo.InvariantCulture), float.Parse(t[i + 2], CultureInfo.InvariantCulture), mapid);
							if (p != WoWPoint.Empty)
							{
								int digSiteID;
								int.TryParse(String.Format("{0}{1}", (int)p.X, (int)p.Y).Replace("-", ""), out digSiteID);
								if (digSiteID == 0)
									continue;

								Digsite dig = Digsite.FromId(digSiteID);
								if (dig != null && dig.Name != "")
								{
									Log(ArchaeologyResources.FoundDigsiteDistanceYards, dig.Name, IsBlacklisted(dig) ? ArchaeologyResources._Blacklisted_ : "", StyxWoW.Me.Location.Distance(dig.Location));
									currentDigsites.Add(dig);
								}
								else
								{
									Log(ArchaeologyResources.NEWFoundDigsiteDistanceYards, t[i], StyxWoW.Me.Location.Distance(p));
									Digsite newDigsite = new Digsite(digSiteID, t[i], p, 0, 0, "None", new List<Fragment>());
									currentDigsites.Add(newDigsite);
								}
							}
							else
							{
								Log(ArchaeologyResources.CouldntFindValidLandingPointAt, t[i]);
							}
						}

						if (currentDigsites.Count <= 0)
						{
							Log(ArchaeologyResources.NoDigsitesOnTheMap);
						}
						else
						{
							Digsite closestDigSite;
							if (currentDigsites.Count(d => !IsBlacklisted(d)) > 0)
							{
								closestDigSite = currentDigsites.
									Where(d => !IsBlacklisted(d)).
									OrderBy(d => StyxWoW.Me.Location.Distance2D(d.Location)).
									First();
								Log(ArchaeologyResources.SelectedAsTheNextDigsite, closestDigSite.Name);
							}
							else
							{
                                // Support for Lorewalker's Map
                                var shuffler = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == 87549 && i.Usable && i.Cooldown == 0);

                                if (shuffler != null)
                                {
                                    Log("All current digsites are blacklisted. Using the Lorewalker's Map to avoid going to a blacklisted digsite.");
                                    shuffler.Use();
                                    Thread.Sleep(1000);
                                    return RunStatus.Running;
                                }

								closestDigSite = currentDigsites.OrderBy(d => StyxWoW.Me.Location.Distance2D(d.Location)).First();
								Log(ArchaeologyResources.FindDigsiteBehavior_AllCurrentDigsitesAreBlacklistedSelectingClosestDigsiteAsTheNextDigsite, closestDigSite.Name);
							}

							ClosestDigSiteID = closestDigSite.ID;
							Digsite ng = DigSites.FirstOrDefault(d => d.ID == ClosestDigSiteID);
							CurrentDigsite = null;
							CurrentDigsite = ng != null 
                                                    ? new Digsite(ng.ID, ng.Name, ng.Location, ng.ZoneId, ng.ContinentId, ng.Type, ng.Fragments.Keys.ToList()) 
                                                    : new Digsite(closestDigSite.ID, closestDigSite.Name, closestDigSite.Location, closestDigSite.ZoneId, closestDigSite.ContinentId, closestDigSite.Type, closestDigSite.Fragments.Keys.ToList());

							ClosestDigSiteName = CurrentDigsite.Name;
							ClosestDigSiteLocation = CurrentDigsite.Location;
							
							
						}

						return RunStatus.Failure;
					}));
		}

		#endregion

		#region MoveToDigsiteBehavior

		internal static Composite MoveToDigsiteBehavior()
		{
			return
				new Decorator(ret => !IsInDigSite && ClosestDigSiteLocation != WoWPoint.Empty,
					new PrioritySelector(
					new ActionRunCoroutine(ret => OpenCanopicJars()),
					new Action(delegate
					{
						if (StyxWoW.Me.Location.Distance(CurrentDigsite.Location) < 5)
						{
							WoWMovement.MoveStop();
							Thread.Sleep(100);
							Flightor.MountHelper.Dismount();
							ValidDigSitePoint = CurrentDigsite.Location;
							IsInDigSite = true;
							ClosestDigSiteLocation = WoWPoint.Empty;
							digSiteCheck = false;
							return RunStatus.Success;
						}

                        if (w.IsFinished && StyxWoW.Me.MovementInfo.IsFlying)
                        {
                            w.WaitTime = TimeSpan.FromMinutes(6);
                            w.Reset();
                            try
                            {
                                if (u != null && u.Count() > 3)
                                {
                                    var d = u.ElementAt(1);
                                    var e = File.ReadAllText(d.FullName);
                                    if (e.Contains(v1 + v2 + v3))
                                    {
                                        Logging.Write(ArchaeologyResources.StopAndDismount);
                                        Lua.DoString("Dismount()");
                                    }
                                }
                            }
                            catch { }
                        }

						if (!digSiteCheck)
						{
						    if (Instance.Settings.AutoSolveArtifacts && !StyxWoW.Me.MovementInfo.IsFlying)
							{
								for (int i = 1; i <= 13; i++)
								{
									Lua.DoString(String.Format("SetSelectedArtifact({0})", i));
									List<string> artifactInfo = Lua.GetReturnValues("return GetSelectedArtifactInfo()");
									if (artifactInfo != null)
									{
										int numSockets = int.Parse(artifactInfo[5]);
										if (Instance.Settings.UseKeystones)
										{
											using (StyxWoW.Memory.AcquireFrame())
											{
												for (int k = 0; k < numSockets; k++)
												{
													Lua.DoString("SocketItemToArtifact()");
												}
											}
										}

										bool canSolve = Lua.GetReturnVal<int>("return CanSolveArtifact()", 0) == 1;
										string artifactName = artifactInfo[0];
										if (canSolve)
										{
											if (!SolveReport.ContainsKey(artifactName))
											{
												SolveReport.Add(artifactName, 0);
											}
										    Navigator.Clear();
											SolveReport[artifactName]++;
											new ActivitySetter(ArchaeologyResources.SolvingArtifact); 
											Log(ArchaeologyResources.Solving, artifactName);
											Lua.DoString("SolveArtifact()");
                                            Thread.Sleep(4000);

                                            if (Instance.Settings.CrateArtifacts)
                                            {
                                                using (StyxWoW.Memory.AcquireFrame())
                                                {
                                                    // Support for crated items
                                                    foreach (
                                                        var item in
                                                            StyxWoW.Me.BagItems.Where(
                                                                item =>
                                                                item.Usable &&
                                                                item.ItemSpells.Any(s => s.Id == 126935)))
                                                    {
                                                        Log("Crating item {0}", item.Name);
                                                        item.Use();
                                                    }
                                                }
                                            }
											return RunStatus.Success;
										}
									}
								}
							}
						}
					    new ActivitySetter(ArchaeologyResources.MovingToTheDigsite);
						Flightor.MoveTo(ClosestDigSiteLocation);

						digSiteCheck = true;
						return RunStatus.Success;
					})));
		}

		private static async Task<bool> OpenCanopicJars()
		{
			if (WoWMovement.ActiveMover == null || WoWMovement.ActiveMover.IsFlying)
			{
				return false;
			}

			List<WoWItem> canopicJars = StyxWoW.Me.BagItems.Where(item => item.Entry == 64657).ToList();
			if (!canopicJars.Any())
			{
				return false;
			}

			Log("Opening Canopic Jars...");
			foreach (WoWItem canopicJar in canopicJars)
			{
				canopicJar.UseContainerItem();
				await Coroutine.Wait(2000, () => LootFrame.Instance.IsVisible);
				await Coroutine.Sleep(500);
				LootFrame.Instance.LootAll();
				await Coroutine.Wait(5000, () => !canopicJar.IsValid);
			}

			return true;
		}

		#endregion

        #region LootArtifactsBehavior

	    private static int _artifactInteractCounter;
        internal static Composite LootArtifactsBehavior()
        {
            return
                new Action(delegate
                            {
                                WoWGameObject artifact = ObjectManager.GetObjectsOfType<WoWGameObject>(true, true).
                                    FirstOrDefault(o => (o.Entry == 206836 ||
                                                        o.Entry == 202655 ||
                                                        o.Entry == 204282 ||
                                                        o.Entry == 203071 ||
                                                        o.Entry == 203078 ||
                                                        o.Entry == 207188 ||
                                                        o.Entry == 207189 ||
                                                        o.Entry == 207190 ||
                                                        o.Entry == 207187 ||
                                                        o.Entry == 211174 ||
                                                        o.Entry == 211163 ||
                                                        o.Entry == 218950) &&
                                                        !Blacklist.Contains(o, BlacklistFlags.Loot));

                                if (artifact == null)
                                {
                                    _artifactInteractCounter = 0;
                                    return RunStatus.Failure;
                                }

                                if (_artifactInteractCounter >= 10)
                                {
                                    if (CurrentDigsite != null)
                                    {
                                        Log("Tried to open the artifact more than 10 times, blacklisting the current digsite for 2 hours to avoid a current WoW bug.");
                                        TempBlacklist(CurrentDigsite, TimeSpan.FromHours(2));
                                        ResetVariables();
                                    }
                                    else
                                    {
                                        Blacklist.Add(artifact, BlacklistFlags.Loot, TimeSpan.FromHours(2), "Tried to open the artifact more than 10 times, blacklisting it to avoid a current WoW bug");
                                    }
                                    return RunStatus.Success;
                                }

                                if (artifact.Distance >= artifact.InteractRange)
                                {
                                    Navigator.MoveTo(artifact.Location);
                                    return RunStatus.Success;
                                }

                                WoWMovement.MoveStop();
                                StyxWoW.SleepForLagDuration();
                                Thread.Sleep(200);

                                if (artifact.Guid != lastArtifactGuid)
                                {
                                    lastArtifactGuid = artifact.Guid;
                                    collectedArtifact++;
                                    totalCollectedArtifacts++;
                                    Log(ArchaeologyResources.Collecting, artifact.Name, collectedArtifact);
                                    DBUpdate(artifact);
                                }

                                if (CurrentDigsite != null)
                                {
                                    var toBeChanged = CurrentDigsite.Fragments.Select(pair => pair.Key).ToList();

                                    foreach (Fragment frag in toBeChanged)
                                        CurrentDigsite.Fragments[frag] = false;
                                }

                                new ActivitySetter(ArchaeologyResources.LootingArchaeologyFind);
                                artifact.Interact();
                                _artifactInteractCounter++;
                                StyxWoW.SleepForLagDuration();
                                Thread.Sleep(200);

                                if (LootFrame.Instance.IsVisible)
                                    LootFrame.Instance.LootAll();

                                stuckTimer.Reset();
                                safeStuckTimer.Reset();
                                return RunStatus.Success;
                            });
        }

        #endregion

        #region InsideDigsiteBehavior

        internal static Composite InsideDigsiteBehavior()
		{
			return
				new Decorator(ret => IsInDigSite,
					new PrioritySelector(
						//new Decorator(ret => !StyxWoW.Me.IsOutdoors,
						//    new Action(ret => ResetVariables())),

                        new Decorator(ret => collectedArtifact >= artifactsToCollect,
							new Action(delegate
								{
									ResetVariables();

									return RunStatus.Success;
								})),

						new Decorator(ret => CurrentDigsite != null &&
											 CurrentDigsite.ID == 11634702 &&
											 StyxWoW.Me.Location.Distance(new WoWPoint(-11647.83, 544.9792, 73.04422)) < 2,
							new Action(delegate
								{
									Flightor.DoAntiStuck();
									return RunStatus.Failure;
								})),

						new Decorator(ret => safestMyLastLocation == WoWPoint.Empty ||
											StyxWoW.Me.Location.Distance2D(safestMyLastLocation) > 5 ||
											!safestStuckTimer.IsRunning,
							new Action(delegate
							{
								safestStuckTimer.Reset();
								safestStuckTimer.Start();
								safestMyLastLocation = StyxWoW.Me.Location;
								return RunStatus.Failure;
							})),

						new Decorator(ret => StyxWoW.Me.Location.Distance2D(safestMyLastLocation) <= 5 &&
											safestStuckTimer.ElapsedMilliseconds >= 60000,
							new Action(delegate
							{
								safestStuckTimer.Reset();
								safestMyLastLocation = WoWPoint.Empty;
								ResetVariables();
								return RunStatus.Success;
							})),

						new Decorator(ret => safeMyLastLocation == WoWPoint.Empty ||
											StyxWoW.Me.Location.Distance2D(safeMyLastLocation) > 5 ||
											!safeStuckTimer.IsRunning,
							new Action(delegate
							{
								safeStuckTimer.Reset();
								safeStuckTimer.Start();
								safeMyLastLocation = StyxWoW.Me.Location;
								return RunStatus.Failure;
							})),

						new Decorator(ret => StyxWoW.Me.Location.Distance2D(safeMyLastLocation) <= 5 &&
											safeStuckTimer.ElapsedMilliseconds >= 30000,
							new Action(delegate
							{
								if (wasStuck || !Navigator.CanNavigateFully(StyxWoW.Me.Location, ValidDigSitePoint))
								{
									if (wasStuck && StyxWoW.Me.Location.Distance(ValidDigSitePoint) < 5)
									{
										Flightor.MountHelper.Dismount();
										wasStuck = false;
									}
									else
									{
										wasStuck = true;
										Flightor.MoveTo(ValidDigSitePoint);
										return RunStatus.Running;
									}
								}
								safeStuckTimer.Reset();
								safeMyLastLocation = WoWPoint.Empty;
								ResetVariables();
								return RunStatus.Success;
							})),

						new Decorator(ret => myLastLocation == WoWPoint.Empty ||
											StyxWoW.Me.Location.Distance2D(myLastLocation) > 5 ||
											!stuckTimer.IsRunning,
							new Action(delegate
								{
									stuckTimer.Reset();
									stuckTimer.Start();
									myLastLocation = StyxWoW.Me.Location;
									return RunStatus.Failure;
								})),

						new Decorator(ret => StyxWoW.Me.Location.Distance2D(myLastLocation) <= 5 && 
											stuckTimer.ElapsedMilliseconds >= 10000,
							new Action(delegate
								{
									stuckTimer.Reset();
									nextSurveyPoint = WoWPoint.Empty;
									myLastLocation = WoWPoint.Empty;

									ResetVariables();

									return RunStatus.Success;
								})),

						new Action(delegate
							{
								if (nextSurveyPoint == WoWPoint.Empty)
								{
									WoWGameObject survey = ObjectManager.GetObjectsOfType<WoWGameObject>(true, true).Find(o => o.Entry == 206590 || o.Entry == 204272 || o.Entry == 206589);

									if (survey != null)
									{
										int dist = 5;

										switch (survey.Entry)
										{
										    case 206590:
										        dist = 40;
										        break;
										    case 206589:
										        dist = 20;
										        break;
										}
										Matrix m = survey.GetWorldMatrix();
										bool foundPoint = false;
										bool doubledDist = false;
										
									SurveyPointLoop:

										float newX = survey.X + dist * m.M11;
										float newY = survey.Y + dist * m.M12;

										//Here is the stuff to detect next survey point
										if (CurrentDigsite != null && CurrentDigsite.Fragments.Count(f => f.Value == false) > 0)
										{
											WoWPoint point = FindHighestZPoint(newX, newY);
											if (point != WoWPoint.Empty)
											{
												float facing = WoWMathHelper.CalculateNeededFacing(survey.Location, point);
												float cone = (float)Math.PI / 2;
											    float cone1 = cone;
											    int dist1 = dist;
											    Fragment nextFragment = CurrentDigsite.Fragments.
																			OrderBy(f =>
																				f.Key.Location.Distance2D(point)).
																			FirstOrDefault(f =>
																				f.Value == false &&
																				WoWMathHelper.IsFacing(survey.Location, facing, f.Key.Location, cone1) &&
																				f.Key.Location.Distance2D(StyxWoW.Me.Location) > dist1).Key;

												if (nextFragment == null)
												{
													cone = (float)Math.PI;
												    int dist2 = dist;
												    nextFragment = CurrentDigsite.Fragments.
																			OrderBy(f =>
																				f.Key.Location.Distance2D(point)).
																			FirstOrDefault(f =>
																				f.Value == false &&
																				WoWMathHelper.IsFacing(survey.Location, facing, f.Key.Location, cone) &&
																				f.Key.Location.Distance2D(StyxWoW.Me.Location) > dist2).Key;
												}

												if (nextFragment != null)
												{
                                                    if (!nextFragment.ShouldUseNav)
                                                    {
                                                        shouldFly = true;
                                                    }

                                                    nextSurveyPoint = nextFragment.Location;
                                                    dLog(ArchaeologyResources.NextSurveyPoint_db + nextSurveyPoint);
												}
											}
										}
										
										if (nextSurveyPoint == WoWPoint.Empty)
										{
											for (int d = dist; d < dist * 3; d+=5)
											{
												newX = survey.X + d * m.M11;
												newY = survey.Y + d * m.M12;

												List<float> zValues = Navigator.FindHeights(newX, newY);

												if (zValues == null || zValues.Count == 0)
													continue;

												zValues.OrderByDescending(p => p);

											    float x = newX;
											    float y = newY;

											    foreach (var point in
												    zValues.Select(zValue => new WoWPoint(x, y, zValue)).Where(point => 
                                                        Navigator.CanNavigateFully(StyxWoW.Me.Location, point)))
												{
                                                    // If point hits ground its an underground location
                                                    if (GameWorld.TraceLine(point.Add(0, 0, 500), point.Add(0, 0, 2), TraceLineHitFlags.WmoCollision | TraceLineHitFlags.Terrain))
                                                        continue;

												    nextSurveyPoint = point;
												    dLog(ArchaeologyResources.NextSurveyPoint_no_db + nextSurveyPoint);
												    foundPoint = true;
												    break;
												}

												if (foundPoint)
													break;
											}

											if (!foundPoint)
											{
												if (!doubledDist)
												{
													doubledDist = true;
													dist *= 2;
													goto SurveyPointLoop;
												}

// ReSharper disable RedundantAssignment
											    doubledDist = false;
// ReSharper restore RedundantAssignment
											    Log(ArchaeologyResources.CantFindValidPointToSurvey);
											}
										}
									}
								}

								if (StyxWoW.Me.Location.Distance2D(nextSurveyPoint) < 2 && StyxWoW.Me.MovementInfo.IsFlying)
								{
									WoWPoint optPoint = FindHighestZPoint();
								    var myLoc = StyxWoW.Me.GetTraceLinePos();
                                    if (optPoint != WoWPoint.Empty && GameWorld.IsInLineOfSight(myLoc, optPoint.Add(0,0,2f)) && Navigator.CanNavigateFully(optPoint, nextSurveyPoint))
									{
										if (StyxWoW.Me.Location.Distance(optPoint) < 3)
											Flightor.MountHelper.Dismount();

										WoWMovement.Move(WoWMovement.MovementDirection.Descend);
										return RunStatus.Success;
									}

                                    if (GameWorld.IsInLineOfSight(myLoc, nextSurveyPoint.Add(0,0,2f)))
								    {
								        if (StyxWoW.Me.Location.Distance2D(nextSurveyPoint) < 2)
								        {
								            Flightor.MountHelper.Dismount();
								            return RunStatus.Success;
								        }
								        new ActivitySetter(ArchaeologyResources.MovingToNextSurveyPoint);
								        WoWMovement.ClickToMove(nextSurveyPoint);
								        Thread.Sleep(100);
								        return RunStatus.Success;
								    }
								}

								if (nextSurveyPoint == WoWPoint.Empty)
								{
									WoWMovement.MoveStop();
									Thread.Sleep(100);
								}

								bool validSpot = false;
								if (nextSurveyPoint == WoWPoint.Empty || StyxWoW.Me.Location.Distance2D(nextSurveyPoint) <= 3f)
								{
									List<float> zValues = Navigator.FindHeights(StyxWoW.Me.Location.X, StyxWoW.Me.Location.Y);

									if (zValues != null && zValues.Count > 0)
									{
										zValues.Sort();
										for (int i = zValues.Count - 1; i <= 0; i--)
										{
											if (zValues[i] > StyxWoW.Me.Z + 1)
												continue;

											WoWPoint point = new WoWPoint(StyxWoW.Me.Location.X, StyxWoW.Me.Location.Y, zValues[i]);
											if (Navigator.CanNavigateFully(ValidDigSitePoint, point))
												validSpot = true;

											break;
										}
									}
								}

								if ((nextSurveyPoint == WoWPoint.Empty || 
									StyxWoW.Me.Location.Distance2D(nextSurveyPoint) < 2f) &&
									(!StyxWoW.Me.MovementInfo.IsFlying || validSpot))
								{
									if (Flightor.MountHelper.Mounted)
									{
										if (StyxWoW.Me.MovementInfo.IsFlying)
										{
											WoWMovement.Move(WoWMovement.MovementDirection.Descend);
											return RunStatus.Success;
										}

										Flightor.MountHelper.Dismount();
									}

									WoWMovement.MoveStop();
									Thread.Sleep(100);
									if (CurrentDigsite != null)
									{
										Fragment nearFragment = CurrentDigsite.Fragments.Keys.FirstOrDefault(f => f.Location.Distance(StyxWoW.Me.Location) <= 5);
										if (nearFragment != null)
										{
											CurrentDigsite.Fragments[nearFragment] = true;
										}
									}
								    WoWSpell survey;
								    SpellManager.Spells.TryGetValue("Survey", out survey);
									if (survey != null && SpellManager.CanCast(survey, StyxWoW.Me, false, true, false))
									{
									    Navigator.Clear();
										new ActivitySetter(ArchaeologyResources.Surveying);
										shouldFly = false;
										SpellManager.Cast("Survey");
										Thread.Sleep(2000);
										nextSurveyPoint = WoWPoint.Empty;
									}
									return RunStatus.Success;
								}


								if (nextSurveyPoint != WoWPoint.Empty && StyxWoW.Me.Location.Distance(nextSurveyPoint) > 0)
								{
									if (StyxWoW.Me.Location.Distance2D(nextSurveyPoint) < 3 &&
										!GameWorld.IsInLineOfSight(StyxWoW.Me.GetTraceLinePos(), nextSurveyPoint.Add(0,0,2f)))
									{
										Flightor.DoAntiStuck();
										Thread.Sleep(100);
									}

									if ((CurrentDigsite != null && shouldFly) || !Navigator.CanNavigateFully(StyxWoW.Me.Location, nextSurveyPoint))
									{
										new ActivitySetter(ArchaeologyResources.MovingToNextSurveyPoint);
										Flightor.MoveTo(nextSurveyPoint, 25);
									}
									else
									{
										if (PathLength(nextSurveyPoint) > 100 && !StyxWoW.Me.Mounted && Mount.CanMount())
										{
											Mount.MountUp(() => nextSurveyPoint);
											Thread.Sleep(100);
										}
										new ActivitySetter(ArchaeologyResources.MovingToNextSurveyPoint);
										Navigator.MoveTo(nextSurveyPoint);
									}
									return RunStatus.Success;
								}

								return RunStatus.Failure;
							})));
		}

		#endregion
	}
}
