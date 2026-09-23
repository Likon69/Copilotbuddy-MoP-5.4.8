using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Bots.DungeonBuddy;

/// <summary> Dungeon, contrains all required information about a specific dungeon. </summary>
public abstract class Dungeon
{
	private readonly List<AvoidInfo> _avoidInfos = new List<AvoidInfo>();

	private bool _hasLoggedComplete;

	/// <summary> The name of this dungeon. </summary>
	/// <value>The name.</value>
	public virtual string Name
	{
		get
		{
			if (LfgDungeon.SubType == LfgDungeons.LfgType.Dungeon && LfgDungeon.DifficultyId == 2)
				return LfgDungeon.Name + " Heroic";
			return LfgDungeon.Name;
		}
	}

	/// <summary> The mapid of this dungeon. </summary>
	/// <value>The map identifier.</value>
	public abstract uint DungeonId { get; }

	/// <summary> True if it's required to fly to get back to the dungeon entrance when doing a corpse run. </summary>
	/// <value><c>true</c> if it's required to fly to get back to the dungeon entrance, <c>false</c> otherwise.</value>
	public virtual bool IsFlyingCorpseRun { get { return false; } }

	private LfgDungeons _lfgDungeon;
	/// <summary>The map of this dungeon.</summary>
	/// <value>The map.</value>
	public LfgDungeons LfgDungeon { get { return _lfgDungeon ?? (_lfgDungeon = LfgDungeons.FromId(DungeonId)); } }

	internal LfgCategory LfgCategory
	{
		get
		{
			switch (LfgDungeon.SubType)
			{
				case LfgDungeons.LfgType.Scenario:
					return LfgCategory.Scenario;
				case LfgDungeons.LfgType.Dungeon:
					return LfgCategory.LookingForDungeon;
				case LfgDungeons.LfgType.Raid:
					return LfgDungeon.LfgDifficulty == LfgDungeons.Difficulty.RaidFinder
							   ? LfgCategory.RaidFinder
							   : LfgCategory.LookingForRaid;
			}
			return LfgCategory.None;
		}
	}

	/// <summary> The entrance of this dungeon as a <see cref="WoWPoint"/>. </summary>
	/// <value>The entrance.</value>
	public virtual WoWPoint Entrance
	{
		get
		{
			var entrance2D = LfgDungeon.Map.GhostEntranceLocation;
			float z;
			if (Navigator.FindHeight(entrance2D.X, entrance2D.Y, out z))
				return new WoWPoint(entrance2D.X, entrance2D.Y, z);

			return WoWPoint.Zero;
		}
	}

	/// <summary>The exit of this dungeon as a <see cref="WoWPoint"/>.</summary>
	/// <value>The exit.</value>
	public virtual WoWPoint ExitLocation
	{
		get { return WoWPoint.Zero; }
	}

	public virtual bool CanExitNow => true;

	/// <summary>
	/// Needs to be overriden if the corpse run to the dungeon needs a breadcrumb
	/// </summary>
	public virtual CircularQueue<WoWPoint> CorpseRunBreadCrumb
	{
		get { return new CircularQueue<WoWPoint>(); }
	}

	/// <summary>Gets a value indicating whether this instance is complete. By default this returns true when all bosses are dead when 'Kill Optional bosses' setting is true or all non-optional bosses are dead if 'Kill Optional bosses' is false </summary>
	/// <value>
	/// 	<c>true</c> if this instance is complete; otherwise, <c>false</c>.
	/// </value>
	public virtual bool IsComplete
	{
		get
		{
			if (LfgDungeon.SubType == LfgDungeons.LfgType.Scenario)
			{
				return ScriptHelpers.CurrentScenarioInfo.IsComplete;
			}
			if (BossManager.CurrentBoss == null && ProfileManager.CurrentProfile != null)
			{
				LogCompleteOnce("CurrentBoss is null");
				return true;
			}
			if (LfgDungeon.SubType == LfgDungeons.LfgType.Raid && DungeonBuddySettings.Instance.RaidType == RaidType.AutoQueueBest && LfgQueue.HasCompletedLfr(LfgDungeon.Id))
			{
				LogCompleteOnce("All bosses in LFR have been looted.");
				return true;
			}
			return false;
		}
	}

	/// <summary>Dungeon specific unit weighting.</summary>
	/// <param name="units">The units.</param>
	public virtual void WeighTargetsFilter(List<Targeting.TargetPriority> units)
	{
	}

	/// <summary>Dungeon specific unit target inclusions.</summary>
	/// <param name="incomingunits">The incomingunits.</param>
	/// <param name="outgoingunits">The outgoingunits.</param>
	public virtual void IncludeTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	{
	}

	/// <summary>Dungeon specific unit target removal.</summary>
	/// <param name="units">The incomingunits.</param>
	public virtual void RemoveTargetsFilter(List<WoWObject> units)
	{
	}

	public virtual void WeighHealTargetsFilter(List<Targeting.TargetPriority> units)
	{
	}

	public virtual void IncludeHealTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	{
	}

	public virtual void RemoveHealTargetsFilter(List<WoWObject> units)
	{
	}

	public virtual void WeighLootTargetsFilter(List<Targeting.TargetPriority> units)
	{
	}

	public virtual void IncludeLootTargetsFilter(List<WoWObject> incomingunits, HashSet<WoWObject> outgoingunits)
	{
	}

	public virtual void RemoveLootTargetsFilter(List<WoWObject> units)
	{
	}

	internal void Enter()
	{
		Targeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
		Targeting.Instance.WeighTargetsFilter += WeighTargetsFilter;
		Targeting.Instance.RemoveTargetsFilter += RemoveTargetsFilter;
		HealTargeting.Instance.IncludeTargetsFilter += IncludeHealTargetsFilter;
		HealTargeting.Instance.WeighTargetsFilter += WeighHealTargetsFilter;
		HealTargeting.Instance.RemoveTargetsFilter += RemoveHealTargetsFilter;
		LootTargeting.Instance.IncludeTargetsFilter += IncludeLootTargetsFilter;
		LootTargeting.Instance.WeighTargetsFilter += WeighLootTargetsFilter;
		LootTargeting.Instance.RemoveTargetsFilter += RemoveLootTargetsFilter;
		// check if we're farming dungeon and if it has an exit location.
		bool farmingDungeons = LfgDungeon.SubType == LfgDungeons.LfgType.Dungeon && DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm;
		bool farmingRaids = LfgDungeon.SubType == LfgDungeons.LfgType.Raid && DungeonBuddySettings.Instance.RaidType == RaidType.Farm;
		if ((farmingDungeons || farmingRaids) && ExitLocation == WoWPoint.Zero)
		{
			TreeRoot.Stop("The current dungeon script has no exit location. Please report to the developer.");
			return;
		}
		_hasLoggedComplete = false;
		OnEnter();
	}

	internal void Exit()
	{
		Targeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
		Targeting.Instance.WeighTargetsFilter -= WeighTargetsFilter;
		Targeting.Instance.RemoveTargetsFilter -= RemoveTargetsFilter;
		HealTargeting.Instance.IncludeTargetsFilter -= IncludeHealTargetsFilter;
		HealTargeting.Instance.WeighTargetsFilter -= WeighHealTargetsFilter;
		HealTargeting.Instance.RemoveTargetsFilter -= RemoveHealTargetsFilter;
		LootTargeting.Instance.IncludeTargetsFilter -= IncludeLootTargetsFilter;
		LootTargeting.Instance.WeighTargetsFilter -= WeighLootTargetsFilter;
		LootTargeting.Instance.RemoveTargetsFilter -= RemoveLootTargetsFilter;
		ClearAvoids();
		ScriptHelpers.RestoreMovement();
		OnExit();
	}

	internal void AddAvoid(AvoidInfo avoid)
	{
		_avoidInfos.Add(avoid);
		Avoidance.AvoidanceManager.AddAvoid(avoid);
	}

	internal void AddAvoidRange(IEnumerable<AvoidInfo> avoids)
	{
		var list = avoids.ToList();
		Avoidance.AvoidanceManager.AddAvoidRange(list);
		_avoidInfos.AddRange(list);
	}

	internal void RemoveAvoid(AvoidInfo avoid)
	{
		Avoidance.AvoidanceManager.RemoveAllAvoids(a => a == avoid);
		_avoidInfos.Remove(avoid);
	}

	internal void ClearAvoids()
	{
		Avoidance.AvoidanceManager.RemoveAllAvoids(a => _avoidInfos.Contains(a));
		_avoidInfos.Clear();
	}

	/// <summary>Called when entering a dungeon.</summary>
	public virtual void OnEnter() { }

	/// <summary>Called when exiting a dungeon.</summary>
	public virtual void OnExit() { }

	/// <summary>Allows custom dungeon navigation such as handling elevators, using portals etc. Return MoveResult.Fail or base if not controlling movement in override.</summary>
	/// <param name="location">The location bot wants to move to.</param>
	/// <returns>The move result.</returns>
	[Obsolete("Use HandleMovement override instead")]
	public virtual MoveResult MoveTo(WoWPoint location) { return MoveResult.Failed; }

	/// <summary>Allows custom dungeon navigation such as handling elevators, using portals etc. Return null if not controlling movement in override.</summary>
	/// <param name="location">The location bot wants to move to.</param>
	public virtual Task<bool> HandleMovement(WoWPoint location)
	{
		return null;
	}

	/// <summary>
	/// Determines if <c>to</c> can be fully navigated <c>from</c> location.
	/// This allows you to include areas that not linked via mesh but you are handling navigation via the HandleMovement override.
	/// Only use if you know what you're doing.
	/// Returning base is required in override
	/// </summary>
	public virtual bool CanNavigateFully(WoWPoint from, WoWPoint to)
	{
		return DungeonBot.NavigationProvider.CanNavigateFullyInternal(from, to);
	}

	#region AvoidObject
	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, float radius, params uint[] unitIds)
	{
		AddAvoidObject(canRun, null, 40f, obj => radius, unitIds);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWObject, float> radiusSelector, params uint[] unitIds)
	{
		AddAvoidObject(canRun, null, 40f, radiusSelector, unitIds);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, float radius, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, null, 40f, obj => radius, objectSelector, locationSelector, ignoreIfBlocking);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWObject, float> radiusSelector, Predicate<WoWObject> objectSelector, Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, null, 40f, radiusSelector, objectSelector, locationSelector, ignoreIfBlocking);
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, float radius, params uint[] unitIds)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, obj => radius, o => unitIds.Contains(o.Entry));
	}

	protected void AddAvoidObject(CanRunDecoratorDelegate canRun, Func<WoWPoint> leashPointSelector, float leashRadius, Func<WoWObject, float> radiusSelector, params uint[] unitIds)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, radiusSelector, o => unitIds.Contains(o.Entry));
	}

	protected void AddAvoidObject(
		CanRunDecoratorDelegate canRun,
		Func<WoWPoint> leashPointSelector,
		float leashRadius,
		float radius,
		Predicate<WoWObject> objectSelector,
		Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidObject(canRun, leashPointSelector, leashRadius, obj => radius, objectSelector, locationSelector, ignoreIfBlocking);
	}

	/// <summary>
	///   Runs away from a unit or object if within range.
	/// </summary>
	/// <param name="canRun"> The condition to run. </param>
	/// <param name="leashPointSelector"> The leash center point selector. Unit location if null </param>
	/// <param name="radiusSelector"> The distance to run </param>
	/// <param name="leashRadius"> The max distance to run from leash. </param>
	/// <param name="objectSelector"> The object selector. </param>
	/// <param name="locationSelector"> </param>
	/// <param name="ignoreIfBlocking"></param>
	/// <returns> </returns>
	protected void AddAvoidObject(
		CanRunDecoratorDelegate canRun,
		Func<WoWPoint> leashPointSelector,
		float leashRadius,
		Func<WoWObject, float> radiusSelector,
		Predicate<WoWObject> objectSelector,
		Func<WoWObject, WoWPoint> locationSelector = null, bool ignoreIfBlocking = false)
	{
		var avoidInfo = new AvoidObjectInfo(canRun, objectSelector, radiusSelector, leashPointSelector, leashRadius, locationSelector, ignoreIfBlocking);
		_avoidInfos.Add(avoidInfo);
		Avoidance.AvoidanceManager.AddAvoid(avoidInfo);
	}

	#endregion

	#region AvoidLocation
	protected void AddAvoidLocation(CanRunDecoratorDelegate canRun, float radius, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, null, 40, obj => radius, locationSelector, collectionSelector, ignoreIfBlocking);
	}

	/// <summary>
	///   Run away from location.
	/// </summary>
	/// <param name="canRun"> The condition. </param>
	/// <param name="radiusSelector"> The max distance to run. </param>
	/// <param name="locationSelector"> The location selector. </param>
	/// <param name="collectionSelector"> Optional collection of objects that are passed as args to LocationSelector </param>
	protected void AddAvoidLocation(
		CanRunDecoratorDelegate canRun, Func<object, float> radiusSelector, Func<object, WoWPoint> locationSelector, Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, null, 40, radiusSelector, locationSelector, collectionSelector, ignoreIfBlocking);
	}


	protected void AddAvoidLocation(
		CanRunDecoratorDelegate canRun,
		Func<WoWPoint> leashPointSelector,
		float leashRadius,
		float radius,
		Func<object, WoWPoint> locationSelector,
		Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		AddAvoidLocation(canRun, leashPointSelector, leashRadius, obj => radius, locationSelector, collectionSelector, ignoreIfBlocking);
	}
	/// <summary>
	///   Run away from location.
	/// </summary>
	/// <param name="canRun"> The condition. </param>
	/// <param name="radiusSelector"> The max distance to run. </param>
	/// <param name="locationSelector"> The location selector. </param>
	/// <param name="collectionSelector"> Optional collection of objects that are passed as args to LocationSelector </param>
	protected void AddAvoidLocation(
		CanRunDecoratorDelegate canRun,
		Func<WoWPoint> leashPointSelector,
		float leashRadius,
		Func<object, float> radiusSelector,
		Func<object, WoWPoint> locationSelector,
		Func<IEnumerable<object>> collectionSelector = null, bool ignoreIfBlocking = false)
	{
		var avoidInfo = new AvoidLocationInfo(canRun, locationSelector, radiusSelector, leashPointSelector, leashRadius, collectionSelector, ignoreIfBlocking);
		_avoidInfos.Add(avoidInfo);
		Avoidance.AvoidanceManager.AddAvoid(avoidInfo);
	}
	#endregion

	private void LogCompleteOnce(string reason)
	{
		if (!_hasLoggedComplete)
		{
			Logger.WriteDebug("Dungeon is complete. Reason: {0}", reason);
			_hasLoggedComplete = true;
		}
	}
}
