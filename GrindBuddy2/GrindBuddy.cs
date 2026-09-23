using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bots.Grind;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Levelbot.Actions.Combat;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GrindBuddy2;

public class GrindBuddy : BotBase
{
	private Composite _root;

	private static ulong _lastLootGuid;

	private static int _lootAttemptCounter;

	private static ulong _lootAttemptLastGuid;

	private static PoiType _lootAttemptLastType;

	private static ICombatRoutine Routine => RoutineManager.Current;

	public override string Name => "[BETA] GrindBuddy";

	public override Composite Root => _root;

	public override PulseFlags PulseFlags => PulseFlags.All;

	public override bool RequiresProfile => true;

	public override bool RequiresProfileScope => true;

	protected static bool PoiIsLootable => BotPoi.Current.Type switch
	{
		PoiType.Loot => BotPoi.Current.AsObject.ToUnit().CanLoot, 
		PoiType.Skin => BotPoi.Current.AsObject.ToUnit().CanSkin, 
		PoiType.Harvest => BotPoi.Current.AsObject.ToGameObject().CanLoot, 
		_ => false, 
	};

	private static bool CleanPoi
	{
		get
		{
			if (BotPoi.Current.Type != PoiType.None)
			{
				return BotPoi.Current.Type == PoiType.Hotspot;
			}
			return true;
		}
	}

	private static WoWObject FirstLoot => LootTargeting.Instance.FirstObject;

	private static WoWUnit FirstTarget => Targeting.Instance.FirstUnit;

	private static Profile CurrentProfile => ProfileManager.CurrentProfile;

	public static List<WoWUnit> GrindUnits => (from ret in ObjectManager.GetObjectsOfType<WoWUnit>()
		where (ProfileManager.CurrentProfile.Factions.Contains(ret.FactionId) || StyxWoW.AreaManager.CurrentGrindArea.Factions.Contains((int)ret.FactionId) || StyxWoW.AreaManager.CurrentGrindArea.MobIDs.Contains((int)ret.Entry)) && ret.IsAlive && !ret.IsCritter && !ret.TaggedByOther && !Blacklist.Contains(ret.Guid, BlacklistFlags.Combat | BlacklistFlags.Pull) && (ret.OwnedByUnit == null || (ret.OwnedByUnit != null && !ret.OwnedByUnit.IsPlayer))
		orderby ret.Location.Distance(StyxWoW.Me.Location)
		select ret).ToList();

	public static WoWUnit SkinUnit => (from ret in ObjectManager.GetObjectsOfType<WoWUnit>()
		where ret.SkinType == WoWCreatureSkinType.Leather && !Blacklist.Contains(ret, BlacklistFlags.Node) && ret.Skinnable && ret.CanSkin && !IsOutsideLootRange(ret)
		orderby ret.Location.Distance(StyxWoW.Me.Location)
		select ret).FirstOrDefault();

	public static List<WoWUnit> AroundUnits => (from ret in ObjectManager.GetObjectsOfType<WoWUnit>()
		where ret.IsAlive && !ret.TaggedByOther && !Blacklist.Contains(ret.Guid, BlacklistFlags.Combat) && Math.Abs(StyxWoW.Me.Level - ret.Level) <= 3 && ret.Distance < 15.0 && (ret.OwnedByUnit == null || (ret.OwnedByUnit != null && !ret.OwnedByUnit.IsPlayer)) && ret.IsHostile
		orderby ret.Location.Distance(StyxWoW.Me.Location)
		select ret).ToList();

	public static Composite CreateCombatBehavior()
	{
		return new PrioritySelector(new Decorator((object ret) => !StyxWoW.Me.Combat, new PrioritySelector(Routine.RestBehavior, Routine.PreCombatBuffBehavior, new Decorator((object ret) => BotPoi.Current.Type == PoiType.Kill, new PrioritySelector(new Decorator((object ret) => !StyxWoW.Me.GotTarget, new Styx.TreeSharp.Action(delegate
		{
			FirstTarget.Target();
		})), new PrioritySelector(new Decorator((object ret) => Routine.MoveToTargetBehavior != null, Routine.MoveToTargetBehavior), new Decorator((object ret) => (double)BotPoi.Current.Location.Distance(StyxWoW.Me.Location) > Targeting.PullDistance, new ActionMoveToPoi())), Routine.PullBuffBehavior, Routine.PullBehavior)))), new Decorator((object ret) => StyxWoW.Me.Combat && BotPoi.Current.Type == PoiType.Kill, new PrioritySelector(Routine.HealBehavior, Routine.CombatBuffBehavior, Routine.CombatBehavior)));
	}

	public override void Start()
	{
		SetDefaultHooks();
		_root = CreateGrindBotBrain();
		Targeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
		LootTargeting.Instance.IncludeTargetsFilter += IncludeLootsFilter;
	}

	public override void Stop()
	{
		Targeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
		LootTargeting.Instance.IncludeTargetsFilter -= IncludeLootsFilter;
	}

	public static void SetDefaultHooks()
	{
		TreeHooks.Instance.ClearAll();
		TreeHooks.Instance.AddHook("GrindBuddy_Combat", CreateCombatBehavior());
	}

	private static void LootAllItems()
	{
		if (!LootTargeting.LootFrameIsOpen)
		{
			return;
		}
		using (StyxWoW.Memory.AcquireFrame())
		{
			_lastLootGuid = LootFrame.Instance.LootingObjectGuid;
			List<WoWItem> carriedItems = StyxWoW.Me.CarriedItems;
			for (int i = 0; i < LootFrame.Instance.LootItems; i++)
			{
				uint itemId = LootFrame.Instance.GetItemId(i);
				foreach (WoWItem item in carriedItems)
				{
					if (item.Entry == itemId)
					{
						ItemInfo itemInfo = item.ItemInfo;
						if (itemInfo != null && (itemInfo.UniqueCount == 1 || itemInfo.BeginQuestId != 0))
						{
							Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot, TimeSpan.FromHours(3.0), "Looting completed");
							break;
						}
					}
				}
				LootFrame.Instance.Loot(i);
				Lua.DoString("ConfirmLootSlot({0})", i + 1);
			}
			Lua.DoString("CloseLoot();");
		}
	}

	public static Composite CreateLootBehavior()
	{
		return new DecoratorIsPoiType(new PoiType[3]
		{
			PoiType.Loot,
			PoiType.Harvest,
			PoiType.Skin
		}, new Sequence(new DecoratorContinue((object ret) => BotPoi.Current.Location.Distance(StyxWoW.Me.Location) > 4f, new ActionMoveToPoi()), new DecoratorContinue((object ret) => StyxWoW.Me.IsMoving, new Styx.TreeSharp.Action(delegate
		{
			Navigator.PlayerMover.MoveStop();
		})), new Styx.TreeSharp.Action(delegate
		{
			BotPoi.Current.AsObject.Interact();
		}), new Wait(4, (object ret) => LootTargeting.LootFrameIsOpen || !PoiIsLootable, new Styx.TreeSharp.Action(delegate
		{
			LootAllItems();
		})), new DecoratorContinue((object ret) => NeedToBlacklistCurrentLootable(), new Sequence(new Styx.TreeSharp.Action(delegate
		{
			Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot, TimeSpan.FromDays(10.0), "Tried to loot unsuccessfully too many times.");
		}), new ActionClearPoi("Tried to loot too many times. Blacklisting!")))));
	}

	private static bool NeedToBlacklistCurrentLootable()
	{
		if (_lootAttemptLastType != BotPoi.Current.Type)
		{
			_lootAttemptCounter = 0;
			_lootAttemptLastGuid = 0uL;
			_lootAttemptLastType = BotPoi.Current.Type;
			return false;
		}
		if (_lootAttemptLastGuid != BotPoi.Current.Guid)
		{
			_lootAttemptCounter = 0;
			_lootAttemptLastGuid = 0uL;
			_lootAttemptLastType = BotPoi.Current.Type;
			return false;
		}
		_lootAttemptCounter++;
		if (_lootAttemptCounter >= 3)
		{
			return true;
		}
		return false;
	}

	private static PoiType GetPoiTypeForLootable(WoWObject obj)
	{
		if (obj is WoWGameObject)
		{
			return PoiType.Harvest;
		}
		if (obj is WoWUnit && (obj as WoWUnit).CanSkin)
		{
			return PoiType.Skin;
		}
		return PoiType.Loot;
	}

	public static Composite CreateGrindBotBrain()
	{
		return new PrioritySelector(new Decorator((object ret) => BotPoi.Current.Type == PoiType.Kill && BotPoi.Current.AsObject.ToUnit().IsDead, new Sequence(new ActionClearPoi("Dead target with POI type as Kill"), new Styx.TreeSharp.Action(delegate
		{
			StyxWoW.Me.ClearTarget();
		}), new DecoratorContinue((object ret) => Targeting.GetAggroOnMeWithin(StyxWoW.Me.Location, 30f) == 0 && CharacterSettings.Instance.LootMobs, new Styx.TreeSharp.Action(delegate
		{
			Thread.Sleep(300);
			WaitTimer waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(1500.0));
			waitTimer.Reset();
			while (!waitTimer.IsFinished && FirstLoot == null)
			{
				Thread.Sleep(100);
			}
		})))), new DecoratorIsPoiType(new PoiType[3]
		{
			PoiType.Loot,
			PoiType.Harvest,
			PoiType.Skin
		}, new Decorator((object ret) => ((SkinUnit == null && LootTargeting.SkinMobs) || !LootTargeting.SkinMobs) && (FirstLoot == null || FirstLoot.Guid != BotPoi.Current.Guid || !PoiIsLootable), new ActionClearPoi("Finished looting!"))), new DecoratorIsPoiType(new PoiType[3]
		{
			PoiType.Loot,
			PoiType.Harvest,
			PoiType.Skin
		}, new Decorator((object ret) => !Navigator.CanNavigateFully(StyxWoW.Me.Location, BotPoi.Current.Location), new Sequence(new Styx.TreeSharp.Action(delegate
		{
			Blacklist.Add(BotPoi.Current.Guid, BlacklistFlags.Loot, TimeSpan.FromDays(10.0), "Unable to reach lootable target.");
		}), new ActionClearPoi("Can't reach lootable!")))), new DecoratorIsPoiType(new PoiType[3]
		{
			PoiType.Loot,
			PoiType.Harvest,
			PoiType.Skin
		}, new Decorator((object ret) => StyxWoW.Me.Combat, new ActionClearPoi("Got thrown in combat while looting!"))), LevelBot.CreateDeathBehavior(), LevelBot.CreateVendorBehavior(), LevelBot.CreateLootBehavior(), new Decorator((object ret) => StyxWoW.Me.CurrentTarget != null && StyxWoW.Me.CurrentTarget.IsDead, new Styx.TreeSharp.Action(delegate
		{
			StyxWoW.Me.ClearTarget();
		})), new Decorator((object ret) => CleanPoi, new PrioritySelector(new DecoratorIsNotPoiType(new PoiType[4]
		{
			PoiType.Kill,
			PoiType.Loot,
			PoiType.Skin,
			PoiType.Harvest
		}, new Decorator((object ret) => GrindUnits != null && GrindUnits.Count > 0, new PrioritySelector(new Decorator((object ret) => GrindUnits[0].Distance < Targeting.PullDistance, new Sequence(new Styx.TreeSharp.Action(delegate
		{
			GrindUnits[0].Target();
		}), new Wait(5, (object ret) => StyxWoW.Me.GotTarget, new ActionIdle()), new ActionSetPoi((object ret) => new BotPoi(StyxWoW.Me.CurrentTarget, PoiType.Kill)))), new Decorator((object ret) => GrindUnits[0].Distance >= Targeting.PullDistance, new Sequence(new Styx.TreeSharp.Action(delegate
		{
			GrindUnits[0].Target();
		}), new Wait(5, (object ret) => StyxWoW.Me.GotTarget, new ActionIdle()), new Decorator((object ret) => NeedToMoveToTarget(StyxWoW.Me.CurrentTarget), new Sequence(new Decorator((object ret) => Mount.ShouldMount(GrindUnits[0].Location), new Styx.TreeSharp.Action(delegate
		{
			Mount.StateMount(() => GrindUnits[0].Location);
		})), new ActionMoveToTarget()))))))))), new HookExecutor("GrindBuddy_BeforeCombat"), new HookExecutor("GrindBuddy_Combat"), new HookExecutor("GrindBuddy_AfterCombat"), new PrioritySelector(new Decorator((object ctx) => RoutineManager.Current.MoveToTargetBehavior != null, RoutineManager.Current.MoveToTargetBehavior), new Decorator((object ret) => NeedToMoveToTarget(StyxWoW.Me.CurrentTarget), new ActionMoveToTarget())), new Decorator((object ret) => AroundUnits.Count > 0, new ActionSetPoi((object ret) => new BotPoi(AroundUnits[0], PoiType.Kill))), new Decorator((object ret) => StyxWoW.Me.Combat && FirstTarget != null && ((StyxWoW.Me.Mounted && Math.Abs(StyxWoW.Me.Level - FirstTarget.Level) <= 3) || !StyxWoW.Me.Mounted), new ActionSetPoi((object ret) => new BotPoi(FirstTarget, PoiType.Kill))), new DecoratorIsNotPoiType(new PoiType[4]
		{
			PoiType.Kill,
			PoiType.Loot,
			PoiType.Skin,
			PoiType.Harvest
		}, new Decorator((object ret) => BotPoi.Current.Type != PoiType.Skin && SkinUnit != null && LootTargeting.SkinMobs && !Blacklist.Contains(SkinUnit, BlacklistFlags.Node), new ActionSetPoi((object ret) => new BotPoi(SkinUnit, PoiType.Skin)))), new DecoratorIsNotPoiType(PoiType.Kill, new Decorator((object ret) => NeedToMoveToHotspot(), new Styx.TreeSharp.Action(delegate
		{
			if (Mount.ShouldMount(StyxWoW.AreaManager.CurrentGrindArea.CurrentHotSpot))
			{
				Mount.StateMount(() => StyxWoW.AreaManager.CurrentGrindArea.CurrentHotSpot);
			}
			TreeRoot.StatusText = LevelbotResources.LevelBot_CreateRoamBehavior_Moving_to_hotspot;
			Navigator.MoveTo(StyxWoW.AreaManager.CurrentGrindArea.CurrentHotSpot);
		}))), new DecoratorIsNotPoiType(new PoiType[1] { PoiType.Kill }, new Decorator((object ret) => StyxWoW.Me.Location.Distance(BotPoi.Current.Location) > 4f, new ActionMoveToPoi())), new ActionAlwaysSucceed());
	}

	private static bool IsOutsideLootRange(WoWObject obj)
	{
		WoWPoint mySpot = StyxWoW.Me.Location;
		WoWPoint[] array = Navigator.GeneratePath(mySpot, obj.Location);
		if (array.Length > 0)
		{
			if (array.Any((WoWPoint spot) => mySpot.Distance(spot) > 80f))
			{
				return true;
			}
			if (array[array.Length - 1].Distance(obj.Location) > 5f)
			{
				Blacklist.Add(obj.Guid, BlacklistFlags.Loot, new TimeSpan(1, 1, 1), "Target is not within loot range.");
				return true;
			}
		}
		return false;
	}

	private static bool NeedToClearPoi(object context)
	{
		WoWUnit firstUnit = Targeting.Instance.FirstUnit;
		WoWUnit currentTarget = StyxWoW.Me.CurrentTarget;
		if ((BotPoi.Current.Type == PoiType.Kill && firstUnit != null && firstUnit.Distance < Targeting.PullDistance && currentTarget == null) || (currentTarget != null && firstUnit != null && currentTarget.Guid != firstUnit.Guid))
		{
			return true;
		}
		if (currentTarget != null)
		{
			return currentTarget.TaggedByOther;
		}
		return false;
	}

	private static bool NeedToMoveToHotspot()
	{
		if (StyxWoW.AreaManager.CurrentGrindArea != null)
		{
			return StyxWoW.AreaManager.CurrentGrindArea.HotspotChanged;
		}
		Logging.Write("It seems that there's no valid grind area for your current level and/or area. Please check your profile!");
		return false;
	}

	private static bool NeedToMoveToTarget(WoWUnit moveTarget = null)
	{
		if (moveTarget == null)
		{
			moveTarget = Targeting.Instance.FirstUnit;
		}
		if (moveTarget == null)
		{
			return false;
		}
		if (StyxWoW.Me.IsFlying)
		{
			if (moveTarget.DistanceSqr <= 3.0 && moveTarget.InLineOfSpellSight)
			{
				return false;
			}
		}
		else if (moveTarget.DistanceSqr < Targeting.PullDistance * Targeting.PullDistance && moveTarget.InLineOfSpellSight)
		{
			return false;
		}
		return true;
	}

	private void IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
	{
		Profile currentProfile = ProfileManager.CurrentProfile;
		if (currentProfile == null || StyxWoW.Me.Combat)
		{
			return;
		}
		HashSet<uint> hashSet = new HashSet<uint>();
		if (StyxWoW.AreaManager.CurrentGrindArea != null && StyxWoW.AreaManager.CurrentGrindArea.Factions.Count > 0)
		{
			foreach (int faction in StyxWoW.AreaManager.CurrentGrindArea.Factions)
			{
				hashSet.Add((uint)faction);
			}
		}
		else
		{
			foreach (uint faction2 in currentProfile.Factions)
			{
				hashSet.Add(faction2);
			}
		}
		foreach (WoWObject incomingUnit in incomingUnits)
		{
			if (incomingUnit is WoWUnit && !(incomingUnit is WoWPlayer))
			{
				WoWUnit woWUnit = incomingUnit.ToUnit();
				if (!currentProfile.AvoidMobs.Contains(woWUnit.Entry) && woWUnit.IsAlive && !IsTooNearBlackspot(currentProfile.Blackspots, woWUnit.Location) && (hashSet.Contains(woWUnit.FactionId) || (StyxWoW.AreaManager.CurrentGrindArea != null && StyxWoW.AreaManager.CurrentGrindArea.MobIDs.Contains((int)woWUnit.Entry))))
				{
					outgoingUnits.Add(incomingUnit);
				}
			}
		}
	}

	private void IncludeLootsFilter(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
	{
		for (int i = 0; i < incomingObjects.Count; i++)
		{
			if (incomingObjects[i] is WoWUnit)
			{
				WoWUnit woWUnit = (WoWUnit)incomingObjects[i];
				if (LootTargeting.LootMobs && woWUnit.Distance <= LootTargeting.LootRadius && woWUnit.IsDead && !Blacklist.Contains(woWUnit.Guid, BlacklistFlags.Loot | BlacklistFlags.Node) && ((woWUnit.KilledByMe && woWUnit.CanLoot) || (woWUnit.CanSkin && LootTargeting.SkinMobs && (CharacterSettings.Instance.NinjaSkin || woWUnit.KilledByMe))))
				{
					outgoingObjects.Add(woWUnit);
				}
			}
			else
			{
				if (!(incomingObjects[i] is WoWGameObject))
				{
					continue;
				}
				_ = StyxWoW.Me.Location;
				WoWGameObject woWGameObject = (WoWGameObject)incomingObjects[i];
				if (woWGameObject.Distance <= LootTargeting.LootRadius && ((woWGameObject.IsHerb && LootTargeting.HarvestHerbs) || (woWGameObject.IsMineral && LootTargeting.HarvestMinerals) || (woWGameObject.IsChest && LootTargeting.LootChests)) && woWGameObject.CanLoot && !Blacklist.Contains(woWGameObject.Guid, BlacklistFlags.Node))
				{
					if (IsTooNearBlackspot(ProfileManager.CurrentProfile.Blackspots, woWGameObject.Location))
					{
						Blacklist.Add(woWGameObject.Guid, BlacklistFlags.Node, TimeSpan.FromDays(3.0), "Target is too close to a blackspot.");
					}
					else
					{
						outgoingObjects.Add(woWGameObject);
					}
				}
			}
		}
	}

	private bool IsTooNearBlackspot(IEnumerable<Blackspot> blackspots, WoWPoint point)
	{
		return blackspots?.Any((Blackspot t) => point.Distance2DSqr(t.Location) <= t.Radius * t.Radius) ?? false;
	}
}
