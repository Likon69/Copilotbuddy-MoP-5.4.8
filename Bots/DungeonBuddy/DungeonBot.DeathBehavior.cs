using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bots.Grind;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Routines;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Action = Styx.TreeSharp.Action;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        private static readonly Stopwatch CorpseReleaseTimer = new Stopwatch();

        private static readonly WaitTimer _wipeRecoveryTimer = new WaitTimer(TimeSpan.FromSeconds(20));

        private static DateTime _lastCorpseRunTime;

        private static bool _wasDead;

        private static bool _wipeRecoveryPending;

        private readonly WaitTimer _corpseRunTimer = new WaitTimer(TimeSpan.FromMinutes(2));

        private Composite _createDeathBehavior;

        private static bool LeaderIsAliveAndInDungeon
        {
            get
            {
                if (DungeonManager.SelectedDungeon == null)
                    return false;

                return (from member in GroupMember.GroupMembers
                        let player = member.Player
                        let inDungeon = member.MapId == DungeonManager.SelectedDungeon.LfgDungeon.MapId
                        where member.IsOnline && member.IsAlive && inDungeon && member.IsLeader()
                        select member).Any();
            }
        }

        private static bool HealerIsAliveAndInDungeon
        {
            get
            {
                if (DungeonManager.SelectedDungeon == null)
                    return false;

                return (from member in GroupMember.GroupMembers
                        let player = member.Player
                        let inDungeon = member.MapId == DungeonManager.SelectedDungeon.LfgDungeon.MapId
                        where member.IsOnline && (member.IsAlive || !_wipeRecoveryTimer.IsFinished) && inDungeon && member.IsHealer
                        select member).Any();
            }
        }

        private static bool IsEncounterInProgress
        {
            get { return Lua.GetReturnVal<bool>("return IsEncounterInProgress()", 0); }
        }

        private bool UseDefaultDeathBehavior
        {
            get
            {
                if (!Routine.NeedDeath)
                    return Routine.GetType().GetProperty("DeathBehavior", BindingFlags.Instance | BindingFlags.Public).DeclaringType == typeof(CombatRoutine);

                return false;
            }
        }

        private static bool CanResurrect(WoWPlayer player)
        {
            switch (player.Class)
            {
                case WoWClass.Paladin:
                case WoWClass.Priest:
                case WoWClass.Shaman:
                case WoWClass.Monk:
                case WoWClass.Druid:
                    return true;
                default:
                    return false;
            }
        }

        private Composite CreateDeathBehavior()
        {
            return _createDeathBehavior ?? (_createDeathBehavior = new PrioritySelector(
                new Decorator(
                    ctx =>
                        StyxWoW.Me.Class == WoWClass.Priest && StyxWoW.Me.Shapeshift == ShapeshiftForm.SpiritOfRedemption &&
                        !StyxWoW.Me.PartyMembers.Any(p => !p.IsMe && p.IsAlive && p.DistanceSqr <= 40 * 40),
                    new ActionRunCoroutine(ctx => CancelSpiritOfRedemption("Nobody around to heal"))),
                new Decorator(
                    ctx =>
                        {
                            var isDead = Me.IsDead && Me.Level > 0;
                            if (!_wasDead && isDead)
                            {
                                CorpseReleaseTimer.Restart();
                                if (StyxWoW.Me.GroupInfo.IsInParty)
                                    _wipeRecoveryTimer.Reset();
                                _wasDead = true;
                            }
                            else if (_wasDead && !isDead)
                            {
                                _wasDead = false;
                            }
                            return isDead;
                        },
                    new PrioritySelector(
                        new Action(
                            ctx =>
                                {
                                    BossManager.BossTimer.Reset();
                                    Navigator.NavigationProvider.StuckHandler.Reset();
                                    Avoidance.Helpers.ClearAvoidPath();
                                    return RunStatus.Failure;
                                }),
                        new PrioritySelector(
                            ctx => IsEncounterInProgress,
                            new Decorator<bool>(
                                encounterInProgress => !encounterInProgress && _wipeRecoveryPending,
                                new Sequence(
                                    new Action(ctx => _wipeRecoveryPending = false),
                                    new Action(ctx => _wipeRecoveryTimer.Reset()))),
                            new Decorator<bool>(
                                encounterInProgress => encounterInProgress && CorpseReleaseTimer.IsRunning,
                                new Action(ctx => CorpseReleaseTimer.Reset())),
                            new Decorator<bool>(
                                encounterInProgress => !encounterInProgress && !CorpseReleaseTimer.IsRunning,
                                new Action(ctx => CorpseReleaseTimer.Start()))),
                        new Decorator(ctx => !UseDefaultDeathBehavior, Routine.DeathBehavior),
                        new Decorator(
                            ctx => UseDefaultDeathBehavior && Lua.GetReturnVal<string>("return HasSoulstone()", 0) != null,
                            new Sequence(
                                new Action(ctx => Logger.Write("Ankh or soulstone is available")),
                                new Action(ctx => Lua.DoString("UseSoulstone()")),
                                new Action(ctx => CorpseReleaseTimer.Reset()))),
                        new Decorator(
                            ctx => ShouldRelease(),
                            new Sequence(
                                new Action(ctx => Logger.Write("Releasing corpse")),
                                new Action(ctx => Lua.DoString("RepopMe()")),
                                new Action(ctx => _corpseRunTimer.Reset()),
                                new Action(ctx => CorpseReleaseTimer.Reset()),
                                new WaitContinue(10, ctx => Me.IsGhost && !Me.CurrentMap.IsDungeon, new ActionAlwaysSucceed()))),
                        new ActionAlwaysSucceed())),
                new Decorator(
                    ctx => Me.IsGhost,
                    new PrioritySelector(
                        new ActionRunCoroutine(ctx => CreateGhostCorpseRunBehavior()),
                        LevelBot.CreateDeathBehavior()))));
        }

        private Composite CreateSpiritRezBehavior()
        {
            WoWUnit spiritHealer = null;
            return new PrioritySelector(
                ctx => spiritHealer = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.IsSpiritHealer),
                new Decorator(
                    ctx => Me.IsGhost && spiritHealer != null,
                    new PrioritySelector(
                        new Decorator(
                            ctx => spiritHealer.WithinInteractRange,
                            new Sequence(
                                new Action(ctx => spiritHealer.Interact()),
                                new Sleep(2000),
                                new Action(ctx => Lua.DoString("AcceptXPLoss()")))),
                        new Action(ctx => Navigator.MoveTo(spiritHealer.Location)))));
        }

        private async Task<bool> CreateGhostCorpseRunBehavior()
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
            {
                return false;
            }

            WoWPoint location = activeMover.Location;
            _lastCorpseRunTime = DateTime.Now;
            if (DungeonManager.SelectedDungeon != null && DungeonManager.SelectedDungeon.LfgDungeon.SubType == LfgDungeons.LfgType.Scenario)
            {
                WoWUnit spiritGuide = ObjectManager.GetObjectsOfType<WoWUnit>()
                    .Where(unit => unit.IsSpiritGuide)
                    .OrderBy(unit => unit.DistanceSqr)
                    .FirstOrDefault();
                if (spiritGuide == null)
                {
                    Logger.WriteWarning("Could not find a spirit guide while in the {0} (Id: {1}) scenario", DungeonManager.SelectedDungeon.Name, DungeonManager.SelectedDungeon.DungeonId);
                    return false;
                }

                if (spiritGuide.DistanceSqr > 8.0)
                {
                    MoveResult moveResult = await CommonCoroutines.MoveTo(spiritGuide.Location, spiritGuide.SafeName);
                    if (moveResult.IsSuccessful())
                    {
                        return true;
                    }

                    Logger.WriteWarning("Failed to move from: {0} to spirit guide at: {1}", location, spiritGuide.Location);
                    return false;
                }

                TreeRoot.StatusText = "Waiting for area rez";
                return true;
            }

            if (!await Coroutine.Wait(10000, () => Me.CorpseMapId != -1))
            {
                Logger.WriteWarning("Invalid corpse map ID");
                return false;
            }

            if (Me.CorpseMapId == Me.MapId || DungeonManager.CorpseDungeon == null)
            {
                return false;
            }

            if (DungeonManager.CorpseDungeon.IsFlyingCorpseRun)
            {
                if (await FlyCorpseRunBreadCrumbs(DungeonManager.CorpseDungeon.CorpseRunBreadCrumb))
                {
                    return true;
                }

                Flightor.MoveTo(DungeonManager.CorpseDungeon.Entrance);
                return true;
            }

            if (location.DistanceSqr(DungeonManager.CorpseDungeon.Entrance) > 64f)
            {
                MoveResult moveResult = await CommonCoroutines.MoveTo(DungeonManager.CorpseDungeon.Entrance);
                if (moveResult.IsSuccessful())
                {
                    return true;
                }

                Logger.WriteError("Unable to navigate to the {0} entrance at: {1} from: {2}", DungeonManager.CorpseDungeon.Name, DungeonManager.CorpseDungeon.Entrance, location);
                return false;
            }

            Navigator.PlayerMover.MoveTowards(WoWMathHelper.CalculatePointFrom(location, DungeonManager.CorpseDungeon.Entrance, -4f));
            return true;
        }

        private async Task CancelSpiritOfRedemption(string reason)
        {
            Logger.Write("Canceling Spirit of Redemption. Reason: {0}", reason);

            var aura = StyxWoW.Me.GetAllAuras().FirstOrDefault(a => a.Cancellable && a.Name == "Spirit of Redemption");
            if (aura != null)
            {
                await Coroutine.Sleep(new Random().Next(500, 1500));
                aura.TryCancelAura();
            }
            else
            {
                Logger.Write("Unable to cancel Spirit of Redemption");
            }

            await Coroutine.Wait(15000, () => StyxWoW.Me.Shapeshift != ShapeshiftForm.SpiritOfRedemption);
        }

        private static bool ShouldRelease()
        {
            if (Me.CurrentMap.IsScenario)
            {
                Logger.WriteDebug("I should release corpse since I'm in a scenario.");
                return true;
            }

            if (!_wipeRecoveryTimer.IsFinished)
            {
                TreeRoot.StatusText = "Waiting on wipe recovery";
                return false;
            }

            using (StyxWoW.Memory.AcquireFrame())
            {
                if (IsEncounterInProgress)
                {
                    TreeRoot.StatusText = "Waiting on encounter to end";
                    return false;
                }

                if (CorpseReleaseTimer.Elapsed >= TimeSpan.FromMinutes(5))
                {
                    Logger.WriteDebug("Corpse release timer has expired.");
                    TreeRoot.StatusText = "";
                    return true;
                }

                var waitForPartyToComeToMe = DungeonBuddySettings.Instance.PartyMode != PartyMode.Off && LeaderIsAliveAndInDungeon && HealerIsAliveAndInDungeon;

                foreach (var member in GroupMember.GroupMembers)
                {
                    var loc = member.Location;

                    var inDungeon = member.MapId == Me.MapId;
                    var canRez = (member.Player != null && CanResurrect(member.Player)) || member.IsHealer;

                    if (member.IsOnline && member.IsAlive && inDungeon && canRez && Navigator.CanNavigateFully(Me.Location, loc)
                        && (loc.DistanceSqr(Me.Location) < 60 * 60 || (waitForPartyToComeToMe && loc.DistanceSqr(Me.Location) < 200 * 200)))
                    {
                        TreeRoot.StatusText = "Waiting on rez";
                        return false;
                    }
                }

                Logger.WriteDebug("There's nobody around that can rez");
                TreeRoot.StatusText = "";
                return true;
            }
        }
    }
}
