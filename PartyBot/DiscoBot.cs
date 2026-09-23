using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Bots.DungeonBuddy;
using Bots.Grind;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using CommonBehaviors.Decorators;
using Levelbot.Resources;
using PartyBot.Forms;
using PartyBot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Localization;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace PartyBot;

public class DiscoBot : BotBase
{
	private static PerFrameCachedValue<GroupMember> _leaderCache;

	private static float? _followStopVariance;

	private static Random _random = new Random();

	private Composite _root;

	private bool _eventsAttached;

	private bool _shouldWait;

	private static Composite _levelBotDeathBehaviorCache;

	private bool _acceptGroupInvite;

	private bool _acceptDungeonInvite;

	private bool _acceptOfferContinue;

	private bool _performRoleCheck;

	private bool _acceptSharedQuest;

	private static LocalPlayer Me => StyxWoW.Me;

	private static GroupMember Leader => _leaderCache ?? (_leaderCache = new PerFrameCachedValue<GroupMember>(() => GroupMember.GroupMembers.FirstOrDefault((GroupMember groupMember) => groupMember.NameEquals(PartyBotSettings.Instance.LeaderName))));

	public override string Name => Globalization.PartyBot_Name;

	public override Composite Root => _root ?? (_root = new PrioritySelector(new Decorator((object ret) => _shouldWait, new ActionIdle()), new ActionRunCoroutine((object ret) => CreateDeathBehavior()), CreateCombatBehavior(), CreateEventBehavior(), CreateLootBehavior(), new Decorator((object ret) => !StyxWoW.Me.IsInInstance && !Battlegrounds.IsInsideBattleground, LevelBot.CreateVendorBehavior()), new ActionRunCoroutine((object ret) => CreatePartyBotBehavior())));

	public override PulseFlags PulseFlags => PulseFlags.All;

	public override Form ConfigurationForm => new FormConfig();

	private static bool OnFlyingMount
	{
		get
		{
			ShapeshiftForm shapeshift = Me.Shapeshift;
			if (shapeshift != ShapeshiftForm.EpicFlightForm && shapeshift != ShapeshiftForm.FlightForm)
			{
				Mount.MountWrapper currentMountWrapper = GetCurrentMountWrapper();
				if (currentMountWrapper != null)
				{
					return currentMountWrapper.IsFlyingMount;
				}
				return false;
			}
			return true;
		}
	}

	private static Composite LevelBotDeathBehavior => _levelBotDeathBehaviorCache ?? (_levelBotDeathBehaviorCache = LevelBot.CreateDeathBehavior());

	private static bool HasRezOffer => Lua.GetReturnVal<bool>("if ResurrectGetOfferer() ~= nil then return 1 end return nil", 0u);

	private static int CorpseRecoveryDelayMs => Lua.GetReturnVal<int>("return GetCorpseRecoveryDelay()", 0u) * 1000;

	public override void Start()
	{
		if (string.IsNullOrEmpty(PartyBotSettings.Instance.LeaderName))
		{
			throw new HonorbuddyUnableToStartException(PartyBotResources.DiscoBot_NoLeaderNameProvided);
		}
		if (ProfileManager.CurrentProfile == null)
		{
			ProfileManager.LoadEmpty();
		}
		AttachEvents();
	}

	public override void Stop()
	{
		DetachEvents();
	}

	private void WoWChat_Party(Chat.ChatLanguageSpecificEventArgs e)
	{
		if (!(Leader != null) || !Leader.NameEquals(e.Author))
		{
			return;
		}
		string[] separator = new string[1] { "!" };
		string[] commands = e.Message.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		foreach (string command in commands)
		{
			switch (command)
			{
			case "dance":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Dancing);
				Lua.DoString("DoEmote('Dance')");
				break;
			case "leavedungeon":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Leaving_Dungeon);
				Lua.DoString("LFGTeleport(1)");
				break;
			case "enterdungeon":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Entering_Dungeon);
				Lua.DoString("LFGTeleport(0)");
				break;
			case "clearpoi":
				BotPoi.Clear("Leader said so");
				break;
			case "leavebattleground":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Leaving_Battleground);
				Battlegrounds.LeaveBattlefield();
				break;
			case "forcetrain":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_train_);
				Vendors.ForceTrainer = true;
				break;
			case "forcesell":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_go_sell_);
				Vendors.ForceSell = true;
				break;
			case "forcerepair":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_repair_);
				Vendors.ForceRepair = true;
				break;
			case "forcemail":
				Logging.Write(PartyBotResources.DiscoBot_WoWChat_Party__PartyBot__Someone_told_me_to_mail_);
				Vendors.ForceMail = true;
				break;
			case "dismount":
				Mount.Dismount("Request from Leader");
				break;
			case "mountup":
				Mount.MountUp(() => WoWPoint.Zero);
				break;
			case "wait":
				_shouldWait = !_shouldWait;
				break;
			case "interact":
			{
				WoWUnit leaderTarget = (Leader.Player != null) ? Leader.Player.CurrentTarget : null;
				if (leaderTarget != null)
				{
					leaderTarget.Interact();
				}
				break;
			}
			}
		}
	}

	private void AttachEvents()
	{
		if (!_eventsAttached)
		{
			LootTargeting.Instance.IncludeTargetsFilter += LevelBot.LevelbotIncludeLootsFilter;
			Targeting.Instance.IncludeTargetsFilter += PartyBotIncludeTargetsFilter;
			Targeting.Instance.WeighTargetsFilter += PartyBotTargetWeight;
			Chat.Party += WoWChat_Party;
			Chat.PartyLeader += WoWChat_Party;
			Lua.Events.AttachEvent("LFG_PROPOSAL_SHOW", HandleProposalShow);
			Lua.Events.AttachEvent("LFG_OFFER_CONTINUE", HandleOfferContinue);
			Lua.Events.AttachEvent("LFG_ROLE_CHECK_SHOW", HandleRoleCheckShow);
			Lua.Events.AttachEvent("PARTY_INVITE_REQUEST", HandlePartyInviteRequest);
			Lua.Events.AttachEvent("QUEST_DETAIL", HandleQuestDetail);
			_eventsAttached = true;
		}
	}

	private void DetachEvents()
	{
		if (_eventsAttached)
		{
			LootTargeting.Instance.IncludeTargetsFilter -= LevelBot.LevelbotIncludeLootsFilter;
			Targeting.Instance.IncludeTargetsFilter -= PartyBotIncludeTargetsFilter;
			Targeting.Instance.WeighTargetsFilter -= PartyBotTargetWeight;
			Chat.Party -= WoWChat_Party;
			Chat.PartyLeader -= WoWChat_Party;
			Lua.Events.DetachEvent("LFG_PROPOSAL_SHOW", HandleProposalShow);
			Lua.Events.DetachEvent("LFG_OFFER_CONTINUE", HandleOfferContinue);
			Lua.Events.DetachEvent("LFG_ROLE_CHECK_SHOW", HandleRoleCheckShow);
			Lua.Events.AttachEvent("PARTY_INVITE_REQUEST", HandlePartyInviteRequest);
			Lua.Events.DetachEvent("QUEST_DETAIL", HandleQuestDetail);
			_eventsAttached = false;
		}
	}

	private static Mount.MountWrapper GetCurrentMountWrapper()
	{
		return (from aura in Me.PassiveAuras
			where aura.Value.Spell.SpellEffects.Any((SpellEffect spellEffect) => spellEffect.EffectType == WoWSpellEffectType.ApplyAura && spellEffect.AuraType == WoWApplyAuraType.Mounted)
			select Mount.MountWrapper.FromSpellId(aura.Value.SpellId)).FirstOrDefault((Mount.MountWrapper mountWrapper) => mountWrapper != null);
	}

	private static void PartyBotIncludeTargetsFilter(IEnumerable<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
	{
		bool isInParty = StyxWoW.Me.GroupInfo.IsInParty;
		bool isInRaid = StyxWoW.Me.GroupInfo.IsInRaid;
		List<ulong> partyMemberGuids = StyxWoW.Me.GroupInfo.PartyMemberGuids.ToList();
		List<ulong> raidMemberGuids = StyxWoW.Me.GroupInfo.RaidMemberGuids.ToList();
		List<WoWPlayer> otherPlayers = null;
		if (isInParty)
		{
			otherPlayers = (from partyMember in StyxWoW.Me.GroupInfo.PartyMembers
				select partyMember.ToPlayer() into player
				where player != null && !player.IsMe
				select player).ToList();
		}
		else if (isInRaid)
		{
			otherPlayers = (from raidMember in StyxWoW.Me.GroupInfo.RaidMembers
				select raidMember.ToPlayer() into player
				where player != null && !player.IsMe
				select player).ToList();
		}
		foreach (WoWObject incomingObject in incomingObjects)
		{
			if (!(incomingObject is WoWUnit))
			{
				continue;
			}
			WoWUnit unit = incomingObject.ToUnit();
			if (!unit.Combat)
			{
				continue;
			}
			if (isInParty && partyMemberGuids.Contains(unit.CurrentTargetGuid))
			{
				outgoingObjects.Add(unit);
			}
			if (isInRaid && raidMemberGuids.Contains(unit.CurrentTargetGuid))
			{
				outgoingObjects.Add(unit);
			}
			if (otherPlayers == null)
			{
				continue;
			}
			foreach (WoWPlayer otherPlayer in otherPlayers)
			{
				if (IsTankingOrHasAggro(otherPlayer, unit) && !outgoingObjects.Contains(unit))
				{
					outgoingObjects.Add(unit);
				}
			}
		}
	}

	private static bool IsTankingOrHasAggro(WoWPlayer player, WoWUnit unit)
	{
		UnitThreatInfo threatInfo = player.GetThreatInfoFor(unit);
		if (threatInfo == null)
		{
			return false;
		}
		bool isTanking = threatInfo.ThreatStatus >= ThreatStatus.NoobishTank;
		bool hasMinionAggro = player.Minions.Any((WoWUnit minion) => minion.Guid == unit.CurrentTargetGuid);
		if (!isTanking)
		{
			return hasMinionAggro;
		}
		return true;
	}

	private static void PartyBotTargetWeight(IEnumerable<Targeting.TargetPriority> targets)
	{
		WoWPlayer raidLeader = RaFHelper.Leader;
		WoWUnit raidLeaderTarget = (!(raidLeader != null) || !raidLeader.IsValid) ? null : raidLeader.CurrentTarget;
		foreach (Targeting.TargetPriority target in targets)
		{
			WoWUnit targetUnit = target.Object as WoWUnit;
			if (!(targetUnit == null) && raidLeaderTarget == targetUnit && targetUnit.GetThreatInfoFor(StyxWoW.Me).ThreatStatus <= ThreatStatus.NotTanking)
			{
				target.Score += 200.0;
			}
		}
	}

	private static Composite CreateCombatBehavior()
	{
		return new PrioritySelector(new Decorator((object ret) => Mount.ShouldDismount(BotPoi.Current.Location), new Mount.ActionLandAndDismount("Combat")), new PrioritySelector(new Decorator((object ret) => BotPoi.Current.Type != PoiType.Skin && StyxWoW.Me.HasPendingSpell("Skinning"), new Action(delegate
		{
			Lua.DoString("SpellStopTargeting()");
		})), new DecoratorIsPoiType(PoiType.Kill, new PrioritySelector(new Decorator((object ret) => BotPoi.Current.AsObject != null && BotPoi.Current.AsObject.ToUnit().IsDead, new Sequence(new Action(delegate
		{
			StyxWoW.Me.ClearTarget();
		}), new Action(delegate
		{
			BotPoi.Clear();
		}))))), new Decorator((object ret) => !StyxWoW.Me.Combat, new PrioritySelector(new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.RestBehavior != null, RoutineManager.Current.RestBehavior), new Decorator((object ret) => RoutineManager.Current.NeedRest, new Sequence(new ActionSetActivity(LevelbotResources.LevelBot_CreateCombatBehavior_Resting), new Action(delegate
		{
			RoutineManager.Current.Rest();
		})))), new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.PreCombatBuffBehavior != null, RoutineManager.Current.PreCombatBuffBehavior), new Decorator((object ret) => RoutineManager.Current.NeedPreCombatBuffs, new Sequence(new ActionSetActivity(LevelbotResources.LevelBot_CreateCombatBehavior_Applying_PreCombatBuffs), new Action(delegate
		{
			RoutineManager.Current.PreCombatBuff();
		})))), new DecoratorIsPoiType(PoiType.Kill, new PrioritySelector(new Decorator((object ret) => (!StyxWoW.Me.GotTarget || (StyxWoW.Me.GotTarget && StyxWoW.Me.CurrentTarget.IsDead)) && Targeting.Instance.FirstUnit != null, new Sequence(new Action(delegate
		{
			Logging.Write(PartyBotResources.DiscoBot_CreateCombatBehavior_Setting_target_to_pull);
		}), new Action(delegate
		{
			Targeting.Instance.FirstUnit.Target();
		}))), new Decorator((object ret) => BotPoi.Current.AsObject != null && BotPoi.Current.AsObject.ToUnit() != null && BotPoi.Current.AsObject.ToUnit().Distance > Targeting.PullDistance, new Sequence(new Action(delegate
		{
			Logging.Write(LevelbotResources.DiscoBot_CreateCombatBehavior_Moving_to_pull_target);
		}), new NavigationAction((object ret) => BotPoi.Current.Location))))), new DecoratorIsPoiType(PoiType.Kill, new PrioritySelector(new Decorator((object ret) => Targeting.Instance.TargetList.Count != 0, new Decorator((object ret) => BotPoi.Current.AsObject != Targeting.Instance.FirstUnit && BotPoi.Current.Type == PoiType.Kill, new Sequence(new ActionDebugString("Current POI is not the best pull target. Changing."), new ActionSetPoi(ignorePreviousPoi: true, (object ret) => new BotPoi(Targeting.Instance.FirstUnit, PoiType.Kill)), new Action(delegate
		{
			BotPoi.Current.AsObject.ToUnit().Target();
		})))), new Decorator(NeedPull, new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.PullBuffBehavior != null, RoutineManager.Current.PullBuffBehavior), new Decorator((object ret) => RoutineManager.Current.PullBehavior != null, new Sequence(new ActionSetActivity(PartyBotResources.DiscoBot_CreateCombatBehavior_Pulling), RoutineManager.Current.PullBehavior)))))))), new Decorator(delegate
		{
			if (Targeting.Instance.FirstUnit != null)
			{
				if (!StyxWoW.Me.Mounted && (StyxWoW.Me.Combat || StyxWoW.Me.PartyMembers.Any((WoWPlayer p) => p.Combat)))
				{
					return true;
				}
				if (StyxWoW.Me.GotAlivePet)
				{
					return StyxWoW.Me.Pet.Combat;
				}
				return false;
			}
			return false;
		}, new PrioritySelector(new Decorator((object ret) => StyxWoW.Me.Mounted, new Mount.ActionLandAndDismount("Combat")), new Decorator((object ret) => BotPoi.Current.AsObject != null && BotPoi.Current.AsObject.Distance > Targeting.PullDistance, new Sequence(new Action(delegate
		{
			TreeRoot.StatusText = PartyBotResources.DiscoBot_CreateCombatBehavior_Moving_to_target;
		}), new NavigationAction((object ret) => BotPoi.Current.AsObject.Location))), new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.HealBehavior != null, RoutineManager.Current.HealBehavior), new Decorator((object ret) => RoutineManager.Current.NeedHeal, new Sequence(new ActionSetActivity(LevelbotResources.LevelBot_CreateCombatBehavior_Healing), new Action(delegate
		{
			RoutineManager.Current.Heal();
		})))), new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.CombatBuffBehavior != null, RoutineManager.Current.CombatBuffBehavior), new Decorator((object ret) => RoutineManager.Current.NeedCombatBuffs, new Sequence(new ActionSetActivity(LevelbotResources.LevelBot_CreateCombatBehavior_Applying_Combat_Buffs), new Action(delegate
		{
			RoutineManager.Current.CombatBuff();
		})))), new PrioritySelector(new Decorator((object ret) => RoutineManager.Current.CombatBehavior != null, RoutineManager.Current.CombatBehavior), new Sequence(new ActionSetActivity(LevelbotResources.LevelBot_CreateCombatBehavior_Combat), new Action(delegate
		{
			RoutineManager.Current.Combat();
		})))))));
	}

	private static bool NeedPull(object context)
	{
		return StyxWoW.Me.CurrentTarget != null;
	}

	private static async Task<bool> CreateDeathBehavior()
	{
		if (!Me.IsAlive)
		{
			if (PartyBotSettings.Instance.WaitForRessInDungeons && Me.IsInInstance && Me.IsDead && Me.PartyMembers.Any((WoWPlayer partyMember) => CanRez(partyMember) && partyMember.IsAlive))
			{
				Logging.Write(PartyBotResources.DiscoBot_CreateDeathBehavior__PartyBot__Waiting_for_ress_);
				bool gotRez = await Coroutine.Wait(180000, () => HasRezOffer);
				if (gotRez)
				{
					await Coroutine.Sleep(1000 + CorpseRecoveryDelayMs);
					Lua.DoString("AcceptResurrect() StaticPopup_Hide('RESURRECT_NO_TIMER') StaticPopup_Hide('RESURRECT_NO_SICKNESS')");
					await Coroutine.Wait(4000, () => Me.IsAlive);
					return true;
				}
				Logging.Write(PartyBotResources.DiscoBot_CreateDeathBehavior__PartyBot__Waited_3_minutes_and_we_got_no_ress__Releasing_from_corpse_);
				Lua.DoString("RepopMe()");
				return true;
			}
			return await LevelBotDeathBehavior.ExecuteCoroutine();
		}
		return false;
	}

	private static bool CanRez(WoWPlayer player)
	{
		switch (player.Class)
		{
		default:
			return false;
		case WoWClass.Paladin:
		case WoWClass.Priest:
		case WoWClass.Shaman:
		case WoWClass.Monk:
		case WoWClass.Druid:
			return true;
		}
	}

	private Composite CreateLootBehavior()
	{
		return new PrioritySelector(new Decorator((object ret) => AllowedToLoot(), LevelBot.CreateLootBehavior()));
	}

	private bool AllowedToLoot()
	{
		if (!Battlegrounds.IsInsideBattleground && (!StyxWoW.Me.IsInInstance || PartyBotSettings.Instance.LootInDungeons))
		{
			return true;
		}
		return false;
	}

	private Composite CreateEventBehavior()
	{
		return new PrioritySelector(new Decorator((object ret) => _acceptGroupInvite, new Sequence(new WaitContinue(() => _random.Next(1, 6), null), new Action(delegate
		{
			Logging.Write(PartyBotResources.DiscoBot_CreateEventBehavior__PartyBot__Accepting_group_invite);
		}), new Action(delegate
		{
			Lua.DoString("local f=select(2,StaticPopup_Visible('PARTY_INVITE')) or select(2,StaticPopup_Visible('PARTY_INVITE_XREALM')) if f then StaticPopup_OnClick(f, 1) end");
		}), new Action(delegate
		{
			_acceptGroupInvite = false;
		}))), new Decorator((object ret) => _acceptDungeonInvite, new Sequence(new Action(delegate
		{
			Logging.Write(PartyBotResources.DiscoBot_CreateEventBehavior__PartyBot__Accepting_dungeon_invite);
		}), new Action(delegate
		{
			Lua.DoString("AcceptProposal()");
		}), new WaitContinue(30, (object ret) => StyxWoW.Me.IsInInstance, new Action(delegate
		{
			_acceptDungeonInvite = false;
		})))), new Decorator((object ret) => _acceptOfferContinue, new Sequence(new Action(delegate
		{
			Lua.DoString("StaticPopup1Button1:Click()");
		}), new WaitContinue(1, (object ret) => false, new ActionIdle()))), new Decorator((object ret) => _performRoleCheck, new Sequence(new Action(delegate
		{
			Logging.Write(PartyBotResources.DiscoBot_CreateEventBehavior__PartyBot__Role_Check_is_in_progress);
		}), new Action(delegate
		{
			Lua.DoString("LFDRoleCheckPopupAcceptButton:Click() StaticPopup1Button1:Click()");
		}), new Action(delegate
		{
			_performRoleCheck = false;
		}))), new Decorator((object ret) => _acceptSharedQuest, new Sequence(new Action(delegate
		{
			Logging.Write(PartyBotResources.DiscoBot_CreateEventBehavior__PartyBot__Accepting_shared_quest);
		}), new Action(delegate
		{
			Lua.DoString("AcceptQuest()");
		}), new Action(delegate
		{
			_acceptSharedQuest = false;
		}))));
	}

	private void HandlePartyInviteRequest(object sender, LuaEventArgs e)
	{
		if (e.Args.Length > 0)
		{
			string leaderName = PartyBotSettings.Instance.LeaderName.Replace(" ", "").Trim();
			string inviter = e.Args[0] as string;
			_acceptGroupInvite = !string.IsNullOrEmpty(inviter) && inviter.Equals(leaderName, StringComparison.OrdinalIgnoreCase);
		}
	}

	private void HandleProposalShow(object sender, LuaEventArgs e)
	{
		_acceptDungeonInvite = PartyBotSettings.Instance.AcceptDungeonInvites;
	}

	private void HandleOfferContinue(object sender, LuaEventArgs e)
	{
		_acceptOfferContinue = PartyBotSettings.Instance.AcceptDungeonInvites;
	}

	private void HandleRoleCheckShow(object sender, LuaEventArgs e)
	{
		_performRoleCheck = PartyBotSettings.Instance.AcceptDungeonInvites;
	}

	private void HandleQuestDetail(object sender, LuaEventArgs e)
	{
		_acceptSharedQuest = PartyBotSettings.Instance.AutoAcceptSharedQuests;
	}

	private static async Task<bool> CreatePartyBotBehavior()
	{
		if (BotPoi.Current.Type.IsOneOf(PoiType.Loot, PoiType.Harvest, PoiType.Skin, PoiType.Train, PoiType.Sell, PoiType.Kill))
		{
			return false;
		}
		if (Leader == null)
		{
			return true;
		}
		WoWUnit leaderTarget = (Leader.Player != null) ? Leader.Player.CurrentTarget : null;
		if (leaderTarget != null && leaderTarget.IsVendor)
		{
			BotPoi.Current = new BotPoi(leaderTarget, leaderTarget.IsRepairMerchant ? PoiType.Repair : PoiType.Sell);
			return true;
		}
		if (leaderTarget != null && Leader.Player.Combat && leaderTarget.Attackable && leaderTarget.IsAlive && leaderTarget.IsHostile && leaderTarget.Combat && Navigator.CanNavigateFully(Me.Location, leaderTarget.Location))
		{
			if (Me.CurrentTarget != leaderTarget)
			{
				leaderTarget.Target();
			}
			BotPoi.Current = new BotPoi(leaderTarget, PoiType.Kill);
			Logging.Write(PartyBotResources.DiscoBot_CreatePartyBotBehavior__PartyBot__Killing_something);
			return true;
		}
		return await FollowLeader();
	}

	private static async Task<bool> FollowLeader()
	{
		if (Leader == null)
		{
			return false;
		}
		if (Leader.MapId != Me.MapId)
		{
			Logging.Write(Colors.Red, PartyBotResources.DiscoBot_CreatePartyBotBehavior__PartyBot__LeaderOnDifferentMap);
			return false;
		}
		WoWUnit activeMover = WoWMovement.ActiveMover;
		if (activeMover == null)
		{
			return false;
		}
		Logging.Write(PartyBotResources.DiscoBot_CreatePartyBotBehavior__PartyBot__Following_Leader);
		WoWPoint moverLocation = activeMover.Location;
		int followDistanceSqr = PartyBotSettings.Instance.FollowDistance * PartyBotSettings.Instance.FollowDistance;
		float distanceToLeaderSqr = Leader.Location.DistanceSqr(moverLocation);
		bool withinFollowDistance = distanceToLeaderSqr < (float)followDistanceSqr;
		if (Leader.Player != null && Leader.Player.Mounted)
		{
			if (Leader.Player.MovementInfo.CanFly && Flightor.CanFly && Flightor.MountHelper.CanMount && (!Me.Mounted || !OnFlyingMount))
			{
				Flightor.MountHelper.MountUp();
				return true;
			}
			if (!Me.Mounted)
			{
				Mount.MountUp(() => Leader.Location);
				return true;
			}
		}
		if (withinFollowDistance)
		{
			if (Leader.Player != null && !Leader.Player.Mounted)
			{
				bool dismounted = await CommonCoroutines.Dismount("Leader is not mounted");
				if (dismounted)
				{
					return true;
				}
			}
		}
		else if (activeMover.IsSwimming && Leader.Player != null && Leader.Player.IsSwimming)
		{
			MoveResult moveResult = await CommonCoroutines.MoveTo(Leader.Location);
			if (!moveResult.IsSuccessful())
			{
				Navigator.PlayerMover.MoveTowards(Leader.Location);
			}
			return true;
		}
		else
		{
			Flightor.MoveTo(Leader.Location);
			return true;
		}
		if (Me.IsMoving)
		{
			if (!_followStopVariance.HasValue)
			{
				_followStopVariance = (float)_random.NextDouble();
			}
			float upperBound = (float)PartyBotSettings.Instance.FollowDistance - 5f;
			float range = upperBound - 5f;
			if (range > 0f)
			{
				float threshold = upperBound - range * _followStopVariance.Value;
				if (distanceToLeaderSqr < threshold * threshold)
				{
					await CommonCoroutines.StopMoving();
					_followStopVariance = null;
				}
			}
		}
		StyxWoW.ResetAfk();
		return true;
	}
}
