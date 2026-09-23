using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Profiles.Handlers;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;

namespace Bots.DungeonBuddy;

public static class BossManager
{
	public static event System.Action<Boss> OnBossKill;
	public static readonly Stopwatch BossTimer = new Stopwatch();

	private static Profile _profile;

	public static void Attach(Profile profile)
	{
		if (profile == null)
			throw new NullReferenceException("profile can not be null");
		_profile = profile;
		foreach (var boss in profile.BossEncounters)
		{
			boss.PathBreadCrumbs.OnEndOfQueue += PathBreadCrumbs_OnEndOfQueue;
		}
	}

	public static void Detach()
	{
		if (_profile != null)
		{
			foreach (var boss in _profile.BossEncounters)
			{
				boss.PathBreadCrumbs.OnEndOfQueue -= PathBreadCrumbs_OnEndOfQueue;
			}
		}
		_profile = null;
	}

	// If we find no boss at the boss location then mark the boss as dead.
	static void PathBreadCrumbs_OnEndOfQueue(object sender, EventArgs e)
	{
		var boss = _profile.BossEncounters.FirstOrDefault(b => b.PathBreadCrumbs == sender);
		if (boss != null && boss.IsAlive)
		{
			var unit = boss.ToWoWUnit();
			if (unit == null || unit.IsDead)
			{
				OnKill(boss, unit == null ? "Boss not found" : "Boss was found dead");
			}
		}
	}

	/// <summary>
	/// This looks for a dead boss in objectManager that is marked as alive in the BossEncounters list and marks it as dead.
	/// </summary>
	/// <returns></returns>
	public static Composite CreateCheckForDeadBossBehavior()
	{
		return new PrioritySelector(ctx => (from unit in ObjectManager.GetObjectsOfType<WoWUnit>()
											where _profile != null && unit.IsValid && unit.IsDead
											let boss = _profile.BossEncounters.FirstOrDefault(p => p.IsAlive && p.Entry == unit.Entry)
											where boss != null
											select boss).FirstOrDefault(),

											new Decorator(ctx => ctx != null,
												new Action(ctx => OnKill((Boss)ctx, "Boss was found dead"))));
	}

	/// <summary>
	/// Returns the Boss that is at the top of the list to kill or null if no bosses are left to kill
	/// </summary>
	static public Boss CurrentBoss
	{
		get
		{
			// If boss.Location and Path isn't set than boss is considered a random spawn
			return _profile != null ? (from boss in _profile.BossEncounters
									   where boss.IsAlive && boss.IsRandomSpawnBoss() == false
									   && ((boss.Optional && DungeonBuddySettings.Instance.KillOptionalBosses) || !boss.Optional)
									   && ((StyxWoW.Me.IsHorde && boss.Faction == BossAvailableToFaction.Horde)
									   || (StyxWoW.Me.IsAlliance && boss.Faction == BossAvailableToFaction.Alliance)
									   || boss.Faction == BossAvailableToFaction.Both)
									   orderby boss.KillOrder
									   select boss).FirstOrDefault() : null;
		}
	}

	public static List<Boss> BossEncounters { get { return _profile != null ? _profile.BossEncounters : new List<Boss>(); } }

	internal static void OnKill(Boss boss, string reason = null)
	{
		// kill all bosses that are not marked as dead and have a lower killOrder then boss that triggered this event.
		foreach (var otherBoss in _profile.BossEncounters.Where(b => b.IsAlive && b.KillOrder < boss.KillOrder))
			otherBoss.MarkAsDead(string.Format("lower kill order then {0}", boss.Name));

		//Display Boss statistics

		string bossTimeString = "";

		if (BossTimer.Elapsed.Hours > 0)
		{ bossTimeString = "{0:D2}h:{1:D2}m:{2:D2}s"; }
		else if (BossTimer.Elapsed.Minutes > 0)
		{ bossTimeString = "{1:D2}m:{2:D2}s"; }
		else
		{ bossTimeString = "{2:D2}s"; }

		var currentBoss = CurrentBoss;

		if (currentBoss != null && BossTimer.ElapsedMilliseconds != 0)
			Logger.Write("" + currentBoss.Name + " - Was killed in : " + string.Format(bossTimeString, BossTimer.Elapsed.Hours, BossTimer.Elapsed.Minutes, BossTimer.Elapsed.Seconds));

		BossTimer.Reset();

		//End boss statistics
		boss.MarkAsDead(reason);
		OnBossKill?.Invoke(boss);
	}

	/// <summary>
	/// Resets each boss encounter entry
	/// </summary>
	public static void Reset()
	{
		if (_profile != null)
		{
			foreach (var boss in _profile.BossEncounters)
			{
				boss.Reset();
			}
		}
	}
}
