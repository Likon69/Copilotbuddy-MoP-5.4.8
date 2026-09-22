using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using Bots.Grind;
using Bots.Quest.Objectives;
using Bots.Quest.QuestOrder;
using Bots.Quest.Resources;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Database;
using Styx.CommonBot.ObjectDatabase;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Bots.Quest.Actions
{
	public class ForcedBehaviorExecutor : Composite
	{
		public ForcedBehaviorExecutor(Bots.Quest.QuestOrder.QuestOrder order)
		{
			if (order == null)
			{
				throw new ArgumentNullException("order");
			}
			Order = order;
		}

		public Bots.Quest.QuestOrder.QuestOrder Order { get; private set; }

		private static ForcedQuestPickUp CreatePickUp(PickUpNode node)
		{
			bool isExplicitLocation = false;
			PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById(node.QuestId);
			WoWPoint location;
			if (!StyxWoW.Me.QuestLog.ContainsQuest(node.QuestId) && !StyxWoW.Me.QuestLog.GetCompletedQuests().Contains(node.QuestId) && (quest == null || !quest.IsCompleted))
			{
				if (node.GiverLocation != WoWPoint.Zero)
				{
					location = node.GiverLocation;
					isExplicitLocation = true;
				}
				else if (node.GiverType.HasValue)
				{
					switch (node.GiverType.Value)
					{
						case QuestObjectType.GameObject:
							WoWGameObject gameObject = (from currentGameObject in ObjectManager.GetObjectsOfType<WoWGameObject>()
								where currentGameObject.Entry == node.GiverId
								orderby currentGameObject.DistanceSqr
								select currentGameObject).FirstOrDefault();
							if (gameObject == null)
							{
								Logging.Write(QuestBotResources.PerformQuestObjectiveOrdered_CreatePickUp_Can_not_pick_up_a_quest_from_a_gameobject_without_specifying_a_location__Please_check_your_profile_);
								return null;
							}
							location = gameObject.Location;
							break;
						case QuestObjectType.Npc:
							ulong playerGuid = StyxWoW.Me.Guid;
							WoWUnit unit = (from currentUnit in ObjectManager.GetObjectsOfType<WoWUnit>()
								where currentUnit.Entry == node.GiverId
								orderby currentUnit.CreatedByUnitGuid == playerGuid descending, currentUnit.DistanceSqr
								select currentUnit).FirstOrDefault();
							if (unit != null)
							{
								location = unit.Location;
								break;
							}
							NpcResult npc = Query.GetNearestNpcById(node.GiverId, StyxWoW.Me.Location);
							if (npc == null)
							{
								Logging.Write("Could not find quest giver NPC with ID {0} in database.", node.GiverId);
								return null;
							}
							location = npc.Location;
							break;
						case QuestObjectType.Item:
							WoWItem item = StyxWoW.Me.CarriedItems.FirstOrDefault(currentItem => currentItem.Entry == node.GiverId);
							if (item == null)
							{
								Logging.Write(Colors.Red, "Could not pickup quest from item with id: {0} - the item was not found!", node.GiverId);
								return null;
							}
							location = WoWPoint.Empty;
							break;
						default:
							return null;
					}
				}
				else
				{
					ulong playerGuid = StyxWoW.Me.Guid;
					WoWObject questGiver = (from currentObject in ObjectManager.GetObjectsOfType<WoWObject>(true)
						where currentObject.Entry == node.GiverId
						orderby currentObject is WoWUnit ? 1 : currentObject is WoWGameObject ? 2 : 3,
							currentObject is WoWUnit && currentObject.ToUnit().CreatedByUnitGuid == playerGuid descending,
							currentObject.DistanceSqr
						select currentObject).FirstOrDefault();
					if (questGiver != null)
					{
						location = questGiver.Location;
					}
					else
					{
						NpcResult npc = Query.GetNearestNpcById(node.GiverId, StyxWoW.Me.Location);
						if (npc == null)
						{
							Logging.Write("Could not find quest giver NPC with ID {0} in database.", node.GiverId);
							return null;
						}
						location = npc.Location;
					}
				}
			}
			else
			{
				location = WoWPoint.Zero;
			}
			return new ForcedQuestPickUp(node.QuestId, node.QuestName, node.GiverId, node.GiverName, location, node.GiverType, node.NavType, isExplicitLocation);
		}

		private static async Task<PlayerQuest> GetQuest(uint questId)
		{
			PlayerQuest quest = null;
			bool containsQuest = StyxWoW.Me.QuestLog.ContainsQuest(questId);
			if (containsQuest)
			{
				await Coroutine.Wait(5000, () => (quest = StyxWoW.Me.QuestLog.GetQuestById(questId)) != null);
			}
			return quest;
		}

		private static async Task<ForcedBehavior> CreateTurnIn(TurnInNode node)
		{
			PlayerQuest quest = await GetQuest(node.QuestId);
			if (quest == null)
			{
				Logging.Write("Can not turn in quest {0} (ID: {1}) because I don't have it in my quest log!", Utilities.GetObjectString(node.QuestName, "(null)"), node.QuestId);
				return null;
			}
			if (ProfileManager.CurrentProfile != null)
			{
				QuestInfo overriddenInfo = ProfileManager.CurrentProfile.FindQuest(node.QuestId);
				if (overriddenInfo != null)
				{
					TurnInObjectiveInfo info = overriddenInfo.FindTurnIn();
					if (info != null)
					{
						return new ForcedQuestTurnIn(node.QuestId, node.QuestName, node.TurnInId, node.TurnInName, info.Location, node.NavType);
					}
				}
			}
			if (quest.IsAutoComplete)
			{
				return new ForcedQuestTurnIn(node.QuestId, node.QuestName, 0, string.Empty, WoWPoint.Empty);
			}
			if (node.TurnInLocation != WoWPoint.Zero)
			{
				return new ForcedQuestTurnIn(node.QuestId, node.QuestName, node.TurnInId, node.TurnInName, node.TurnInLocation, node.NavType);
			}

			ulong playerGuid = StyxWoW.Me.Guid;
			WoWObject turnInObject = (from currentObject in ObjectManager.GetObjectsOfType<WoWObject>(true)
				where (currentObject is WoWUnit || currentObject is WoWGameObject) && currentObject.Entry == node.TurnInId
				orderby currentObject is WoWUnit ? 1 : currentObject is WoWGameObject ? 2 : 3,
					currentObject is WoWUnit && currentObject.ToUnit().CreatedByUnitGuid == playerGuid descending,
					currentObject.DistanceSqr
				select currentObject).FirstOrDefault();
			if (turnInObject != null)
			{
				return new ForcedQuestTurnIn(node.QuestId, node.QuestName, node.TurnInId, node.TurnInName, turnInObject.Location, node.NavType, false);
			}

			WoWQuestPOIInfo poiInfo = quest.GetQuestPOIInfo();
			WoWQuestStep? turnInStep = null;
			foreach (WoWQuestStep step in poiInfo.Steps.Steps)
			{
				if (step.PoiObjectiveIndex == -1)
				{
					turnInStep = step;
					break;
				}
			}
			NpcResult result = Query.GetNearestNpcById(node.TurnInId, StyxWoW.Me.Location);
			if (result != null && (!turnInStep.HasValue || result.Location.Distance2DSqr(new Vector3(turnInStep.Value.StepPosition, 0f)) <= 400f))
			{
				return new ForcedQuestTurnIn(node.QuestId, node.QuestName, node.TurnInId, node.TurnInName, result.Location, node.NavType, false);
			}
			if (!turnInStep.HasValue)
			{
				Logging.Write(QuestBotResources.PerformQuestObjectiveOrdered_CreateTurnIn_Could_not_find_turn_in_step__Please_specify_a_turn_in_override_);
				return null;
			}
			Vector3 turnInStepLocation = new Vector3(turnInStep.Value.StepPosition, 0f);
			if (!HeightHelper.FindMeshHeight(ref turnInStepLocation))
			{
				Logging.Write(QuestBotResources.PerformQuestObjectiveOrdered_CreateTurnIn_Could_not_find_a_height_to_turn_in_quest__0___Consider_overriding_this_quest_in_your_profile_, quest.Name);
				return null;
			}
			return new ForcedQuestTurnIn(node.QuestId, node.QuestName, node.TurnInId, node.TurnInName, turnInStepLocation, node.NavType, false);
		}

		private static async Task<QuestObjective> CreatePerformableObjective(ObjectiveNode node)
		{
			PlayerQuest quest = await GetQuest(node.QuestId);
			if (quest == null)
			{
				Logging.Write("Could not find quest with ID {0}!", node.QuestId);
				return null;
			}
			WoWQuestPOIInfo poiInfo = quest.GetQuestPOIInfo();
			List<Styx.WoWInternals.Quest.QuestObjective> objectives = quest.GetObjectives();
			Styx.WoWInternals.Quest.QuestObjective? objective = null;
			int objectiveIndex = 0;
			List<uint> refrenceList = new List<uint>();
			List<uint> poiList = new List<uint>();
			List<WoWQuestStep> questSteps = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
			if (questSteps.Count == 0)
			{
				questSteps = poiInfo.Steps.Steps.ToList();
			}
			for (int i = 0; i < poiInfo.Steps.Steps.Length; i++)
			{
				if (!refrenceList.Contains(poiInfo.Steps.Steps[i].PoiObjectiveRelation))
				{
					refrenceList.Add(poiInfo.Steps.Steps[i].PoiObjectiveRelation);
				}
			}
			int turninObjectiveID = poiInfo.Steps.Steps.Count();
			for (int i = 0; i < poiInfo.Steps.Steps.Length; i++)
			{
				if (poiInfo.Steps.Steps[i].PoiObjectiveIndex == -1)
				{
					turninObjectiveID = i;
				}
			}
			for (int i = 0; i < poiInfo.Steps.Steps.Length; i++)
			{
				if (!poiList.Contains(poiInfo.Steps.Steps[i].PoiID))
				{
					poiList.Add(poiInfo.Steps.Steps[i].PoiID);
				}
			}

			if (objectives.Count == questSteps.Count)
			{
				List<WoWQuestStep> sameSteps = new List<WoWQuestStep>();
				int stepCount = 0;
				for (int i = 0; i < objectives.Count; i++)
				{
					if (objectives[i].ID != node.ObjectiveId)
					{
						continue;
					}
					objective = objectives[i];
					if (i > turninObjectiveID)
					{
						stepCount++;
					}
					sameSteps.Add(questSteps[i - stepCount]);
					break;
				}
				if (!objective.HasValue)
				{
					objective = objectives[0];
					sameSteps = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
				}
				if (objectives.Count == 1)
				{
					objective = objectives[0];
					sameSteps = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
				}
				if (sameSteps.Count == 0)
				{
					sameSteps = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
				}
				return QuestManager.ConstructQuestObjective(objective.Value, quest, sameSteps, null, node.ForcedHotspot, node.NavType);
			}

			List<uint> sortedRefrenceList = refrenceList.OrderByDescending(reference => reference).ToList();
			List<WoWQuestStep> sameStepsMultiple = new List<WoWQuestStep>();
			int stepCountMultiple = 0;
			try
			{
				for (int i = 0; i < objectives.Count; i++)
				{
					if (objectives[i].ID != node.ObjectiveId)
					{
						continue;
					}
					if (i > turninObjectiveID)
					{
						stepCountMultiple++;
					}
					objective = objectives[i];
					objectiveIndex = (int)sortedRefrenceList[i - stepCountMultiple];
					sameStepsMultiple = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveRelation == objectiveIndex).ToList();
					break;
				}
				if (!objective.HasValue || objectives.Count < questSteps.Count || sameStepsMultiple.Count == 0)
				{
					if (!objective.HasValue)
					{
						objective = objectives[0];
					}
					sameStepsMultiple = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
				}
			}
			catch (Exception)
			{
				if (!objective.HasValue || objectives.Count < questSteps.Count || sameStepsMultiple.Count == 0)
				{
					if (!objective.HasValue)
					{
						objective = objectives[0];
					}
					sameStepsMultiple = poiInfo.Steps.Steps.Where(questStep => questStep.PoiObjectiveIndex != -1).ToList();
				}
			}
			if (sameStepsMultiple.Count == 0)
			{
				Logging.Write(QuestBotResources.PerformQuestObjectiveOrdered_CreatePerformableObjective_Could_not_find_objective_with_ID__0__in_quest__1_, node.ObjectiveId, quest.Name);
				TreeRoot.Stop("Could not create 'CurrentBehavior' in quest bot; an exception was thrown: Could not create Quest Hotspots. Place a Quest Override");
				return null;
			}
			return QuestManager.ConstructQuestObjective(objective.Value, quest, sameStepsMultiple, null, node.ForcedHotspot, node.NavType);
		}

		private static ForcedBehavior CreateToggleBehavior(OrderNode node)
		{
			ToggleBehaviorNode toggleBehavior = (ToggleBehaviorNode)node;
			BehaviorFlags behaviorFlags;
			if (!Enum.TryParse(toggleBehavior.BehaviorName, true, out behaviorFlags))
			{
				IXmlLineInfo element = toggleBehavior.Element;
				if (element != null && element.HasLineInfo())
				{
					TreeRoot.Stop(string.Format("Couldn't parse the behavior name \"{0}\" in \"{1}\" tag. Line: {2}. Valid values are: {3}. These values can be used together seperated by a comma (,)" , toggleBehavior.BehaviorName, toggleBehavior.Element.Name.LocalName, element.LineNumber, string.Join(", ", Enum.GetNames(typeof(BehaviorFlags)))));
				}
				else
				{
					TreeRoot.Stop(string.Format("Couldn't parse the behavior name \"{0}\" in \"{1}\" tag.", toggleBehavior.BehaviorName, toggleBehavior.Element.Name.LocalName));
				}
				return null;
			}
			return new ForcedSingleton(() => LevelBot.BehaviorFlags = toggleBehavior.Enabled ? LevelBot.BehaviorFlags | behaviorFlags : LevelBot.BehaviorFlags & ~behaviorFlags);
		}

		private async Task CreateCurrent(OrderNode node)
		{
			ForcedBehavior behavior;
			switch (node.Type)
			{
				case OrderNodeType.Checkpoint:
					behavior = new ForcedNothing();
					break;
				case OrderNodeType.If:
					behavior = new ForcedIf((IfNode)node);
					break;
				case OrderNodeType.While:
					behavior = new ForcedWhile((WhileNode)node);
					break;
				case OrderNodeType.PickUp:
					behavior = CreatePickUp((PickUpNode)node);
					break;
				case OrderNodeType.TurnIn:
					behavior = await CreateTurnIn((TurnInNode)node);
					break;
				case OrderNodeType.Objective:
					ObjectiveNode objectiveNode = (ObjectiveNode)node;
					if (StyxWoW.Me.QuestLog.GetCompletedQuests().Contains(objectiveNode.QuestId))
					{
						behavior = new ForcedSingleton(() => Logging.WriteDiagnostic("[QDBG] Quest ({0}) for objective {1} is completed.", objectiveNode.QuestId, objectiveNode.ObjectiveId));
						break;
					}
					QuestObjective objective = await CreatePerformableObjective(objectiveNode);
					if (objective != null)
					{
						behavior = new ForcedQuestObjective(objective);
						break;
					}
					Logging.Write(QuestBotResources.PerformQuestObjectiveOrdered_CreateCurrent_Could_not_create_a_performable_quest_objective_for_objective_with_ID__0__, objectiveNode.ObjectiveId);
					behavior = null;
					break;
				case OrderNodeType.SetGrindArea:
					SetGrindAreaNode setGrindArea = (SetGrindAreaNode)node;
					behavior = new ForcedSingleton(() => QuestState.Instance.CurrentGrindArea = setGrindArea.Area);
					break;
				case OrderNodeType.ClearGrindArea:
					behavior = new ForcedSingleton(() => StyxWoW.AreaManager.SetArea(null));
					break;
				case OrderNodeType.SetMailbox:
					SetMailboxNode setMailbox = (SetMailboxNode)node;
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.MailboxManager.ForcedMailboxes = setMailbox.Mailboxes);
					break;
				case OrderNodeType.ClearMailbox:
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.MailboxManager.ForcedMailboxes = null);
					break;
				case OrderNodeType.SetVendor:
					SetVendorNode setVendor = (SetVendorNode)node;
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.VendorManager.ForcedVendors = setVendor.Vendors);
					break;
				case OrderNodeType.ClearVendor:
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.VendorManager.ForcedVendors = null);
					break;
				case OrderNodeType.SetBlacklist:
					SetBlacklistNode setBlacklist = (SetBlacklistNode)node;
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.Blacklist = setBlacklist.Blacklist);
					break;
				case OrderNodeType.ClearBlacklist:
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.Blacklist = null);
					break;
				case OrderNodeType.SetAvoidMobs:
					SetAvoidMobsNode setAvoidMobs = (SetAvoidMobsNode)node;
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.AvoidMobs = setAvoidMobs.AvoidMobs);
					break;
				case OrderNodeType.ClearAvoidMobs:
					behavior = new ForcedSingleton(() => ProfileManager.CurrentProfile.AvoidMobs = null);
					break;
				case OrderNodeType.DisableRepair:
					behavior = new ForcedSingleton(() => Vendors.RepairDisabled = true);
					break;
				case OrderNodeType.EnableRepair:
					behavior = new ForcedSingleton(() => Vendors.RepairDisabled = false);
					break;
				case OrderNodeType.GrindTo:
					behavior = new ForcedGrindTo((GrindToNode)node);
					break;
				case OrderNodeType.AbandonQuest:
					AbandonQuestNode abandonQuest = (AbandonQuestNode)node;
					behavior = new ForcedSingleton(() => StyxWoW.Me.QuestLog.AbandonQuestById(abandonQuest.QuestId));
					break;
				case OrderNodeType.MoveTo:
					MoveToNode moveTo = (MoveToNode)node;
					behavior = new ForcedMoveTo(moveTo.Location, moveTo.LocationName, moveTo.Precision, moveTo.QuestId, moveTo.NavType);
					break;
				case OrderNodeType.UseItem:
					UseItemNode useItem = (UseItemNode)node;
					behavior = new ForcedUseItem(useItem.ItemRetriever, useItem.TargetRetriever, useItem.ForceUse, useItem.QuestId, useItem.Location, useItem.NavType);
					break;
				case OrderNodeType.Code:
					behavior = new ForcedCodeBehavior((CodeNode)node);
					break;
				case OrderNodeType.ToggleBehavior:
					behavior = CreateToggleBehavior(node);
					break;
				case OrderNodeType.SetTargetingDistance:
					SetTargetingDistanceNode setTargetingDistance = (SetTargetingDistanceNode)node;
					behavior = new ForcedSingleton(delegate
					{
						ProfileManager.CurrentProfile.TargetingDistance = setTargetingDistance.TargetingDistance;
						Logging.WriteDiagnostic("TargetingDistance setting of the profile has been changed to {0}", setTargetingDistance.TargetingDistance);
					});
					break;
				case OrderNodeType.SetUseMount:
					SetUseMountNode setUseMount = (SetUseMountNode)node;
					behavior = new ForcedSingleton(delegate
					{
						ProfileManager.CurrentProfile.UseMount = setUseMount.UseMount;
						Logging.WriteDiagnostic("UseMount setting of the profile has been changed to {0}", setUseMount.UseMount);
					});
					break;
				case OrderNodeType.SetLootMobs:
					SetLootMobsNode setLootMobs = (SetLootMobsNode)node;
					behavior = new ForcedSingleton(delegate
					{
						ProfileManager.CurrentProfile.LootMobs = setLootMobs.LootMobs;
						Logging.WriteDiagnostic("LootMobs setting of the profile has been changed to {0}", setLootMobs.LootMobs);
					});
					break;
				case OrderNodeType.SetLootRadius:
					SetLootRadiusNode setLootRadius = (SetLootRadiusNode)node;
					behavior = new ForcedSingleton(delegate
					{
						ProfileManager.CurrentProfile.LootRadius = setLootRadius.LootRadius;
						Logging.WriteDiagnostic("LootRadius setting of the profile has been changed to {0}", setLootRadius.LootRadius);
					});
					break;
				default:
					behavior = null;
					break;
			}
			Order.CurrentBehavior = behavior;
		}

		private IEnumerable<RunStatus> CreateCurrentBehavior()
		{
			Func<Task> taskProducer = () => CreateCurrent(Order.CurrentNode);
			using (Coroutine coroutine = new Coroutine(taskProducer))
			{
				while (true)
				{
					try
					{
						coroutine.Resume();
					}
					catch (Exception ex)
					{
						if (Order.CurrentNode.Element != null)
						{
							Logging.Write(Colors.Red, "Could not create current behavior in quest bot; exception was thrown, Element: {0}", Order.CurrentNode.Element);
						}
						else
						{
							Logging.Write(Colors.Red, "Could not create current behavior in quest bot; exception was thrown");
						}
						Logging.WriteException(ex);
						break;
					}
					if (coroutine.IsFinished)
					{
						break;
					}
					yield return RunStatus.Running;
				}
			}
			if (Order.CurrentBehavior == null)
			{
				TreeRoot.Stop(QuestBotResources.PerformQuestObjectiveOrdered_Execute_Could_not_create_current_in_quest_bot_);
			}
		}

		protected override IEnumerable<RunStatus> Execute(object context)
		{
			if (Order.CurrentNode == null)
			{
				yield return RunStatus.Failure;
				yield break;
			}
			if (Order.CurrentBehavior == null)
			{
				foreach (RunStatus status in CreateCurrentBehavior())
				{
					if (status == RunStatus.Running)
					{
						yield return status;
					}
				}
				if (Order.CurrentBehavior == null)
				{
					yield break;
				}
				TreeRoot.GoalText = "Nothing";
				Logging.WriteDiagnostic("[QDBG] Starting behavior {0}.", Order.CurrentBehavior);
				Order.CurrentBehavior.OnStart();
			}
			if (Order.CurrentBehavior.IsDone)
			{
				using (StyxWoW.Memory.ReleaseFrame(false))
				{
					do
					{
						Logging.WriteDiagnostic("[QDBG] Done with forced behavior {0}.", Order.CurrentBehavior);
						Order.CurrentBehavior.OnFinished();
						Order.CurrentBehavior.Dispose();
						Order.CurrentBehavior = null;
						Order.Advance();
						if (Order.Nodes.Count > 0)
						{
							foreach (RunStatus status in CreateCurrentBehavior())
							{
								if (status == RunStatus.Running)
								{
									yield return status;
								}
							}
							if (Order.CurrentBehavior != null)
							{
								TreeRoot.GoalText = "Nothing";
								Logging.WriteDiagnostic("[QDBG] Starting behavior {0}.", Order.CurrentBehavior);
								Order.CurrentBehavior.OnStart();
								continue;
							}
							yield break;
						}
						yield return RunStatus.Failure;
						yield break;
					}
					while (Order.CurrentBehavior.IsDone);
				}
			}
			ForcedBehavior currentBehavior = Order.CurrentBehavior;
			currentBehavior.OnTick();
			currentBehavior.Branch.Start(context);
			while (currentBehavior.Branch.Tick(context) == RunStatus.Running)
			{
				yield return RunStatus.Running;
			}
			currentBehavior.Branch.Stop(context);
			yield return currentBehavior.Branch.LastStatus ?? RunStatus.Failure;
		}
	}
}
