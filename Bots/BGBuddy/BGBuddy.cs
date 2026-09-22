using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bots.BGBuddy.Forms;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using Bots.Grind;
using CommonBehaviors.Actions;

using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Loaders;
using Styx.Localization;

using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using BGs = Styx.WoWInternals.Battlegrounds;
using Tripper.MeshMisc;
using MathEx = Tripper.Tools.Math.MathEx;

namespace Bots.BGBuddy
{
    public delegate void WorldStatesUpdateDelegate();

    public partial class BGBuddy : BotBase
    {
        #region BotBase Overrides

        public static BGBuddy Instance { get; private set; }
        public override string Name { get { return Globalization.BGBuddy_Name; } }
        private Composite _rootLogic;
        public override Composite Root { get { return _rootLogic; } }

        public override PulseFlags PulseFlags { get { return PulseFlags.All; } }
        public override System.Windows.Forms.Form ConfigurationForm
        {
            get { return new ConfigWindow(); }
        }
        public override void Initialize()
        {
            if (BGBuddySettings.Instance.FirstTime)
            {
                BGBuddySettings.Instance.Queue1 = BattlegroundType.RandomBattleground;
                BGBuddySettings.Instance.FirstTime = false;
                BGBuddySettings.Instance.Save();
            }
        }

        private static float CalculatePathPrecision()
        {
            float curSpeed = StyxWoW.Me.MovementInfo.CurrentSpeed;
            float pathPrecision = curSpeed * 0.15f;

            return MathEx.Clamp(pathPrecision, 1.5f, 10f);
        }

        private readonly WaitTimer _worldStateUpdateTimer = new WaitTimer(TimeSpan.FromSeconds(2));
        public override void Pulse()
        {
            Navigator.PathPrecision = CalculatePathPrecision();
            Battleground.StartOfGameTimer.Update();
            Battleground.FightTimer.Update();
            if (_worldStateUpdateTimer.IsFinished)
            {
                if (WorldStatesUpdated != null)
                {
                    foreach (var @delegate in WorldStatesUpdated.GetInvocationList())
                    {
                        try
                        {
                            @delegate.DynamicInvoke(null);
                        }
                        catch (Exception ex)
                        {
                            Logging.WriteException(ex);
                        }
                    }
                }
                _worldStateUpdateTimer.Reset();
            }
        }

        public override void Start()
        {
            if (StyxWoW.Me.Level < 10)
                throw new HonorbuddyUnableToStartException(BGBuddyResources.CanNotStartUnderLevel10);

            // Load all BG "scripts"
            Battlegrounds = new DllLoader<Battleground>(Assembly.GetExecutingAssembly());
            Instance = this;

            ProfileManager.LoadEmpty();

            if (BGBuddySettings.Instance.DisableMounting)
                ProfileManager.CurrentProfile.UseMount = false;

            if (Navigator.NavigationProvider is MeshNavigator nav)
            {
                nav.UpdateMaps();
                nav.Nav.RandomizePaths = true;
                nav.Nav.QueryFilter.SetAreaCost(AreaType.Water, 7.33f - (0x7D262173 ^ GetTokenHash()));
                nav.Nav.QueryFilter.SetAreaCost(AreaType.Misc8, 7.33f - (0x7D262173 ^ GetTokenHash()));
                nav.Nav.QueryFilter.SetAreaCost(AreaType.Fall, 1.0f - (0x7D262173 ^ GetTokenHash()));
            }
            BotEvents.Player.OnMapChanged += Player_OnMapChanged;

            Lua.Events.AttachEvent("PLAYER_SPECIALIZATION_CHANGED", PlayerSpecializationChanged);
            // Force an update.
            UpdateBattleground(null);

            // Set PVP Roles
            PlayerSpecializationChanged(null, null);
        }

        uint GetTokenHash()
        {
            var asm = Assembly.GetEntryAssembly();
            // Hidden way to do... asm.GetName().GetPublicKeyToken();

            var getNameBytes = new byte[] { 0x11, 0x33, 0x22, 0x18, 0x37, 0x3B, 0x33 };
            var getPublicKeyTokenBytes = new byte[] { 0x76, 0x54, 0x45, 0x61, 0x44, 0x53, 0x5D, 0x58, 0x52, 0x7A, 0x54, 0x48, 0x65, 0x5E, 0x5A, 0x54, 0x5F };

            var asmName = asm.GetType().GetMethod(Encoding.ASCII.GetString(getNameBytes.Select(b => (byte)(b ^ 0x56)).ToArray()), new Type[0]).Invoke(asm, null);
            var token = (byte[])asmName.GetType().GetMethod(Encoding.ASCII.GetString(getPublicKeyTokenBytes.Select(b => (byte)(b ^ 0x31)).ToArray())).Invoke(asmName, null);


            // 64bit FNV-1a
            var hash = 0xCBF29CE484222325;
            foreach (var b in token)
            {
                hash = hash ^ b;
                hash = hash * 0x100000001B3;
            }

            return (uint)(hash & uint.MaxValue);
        }

        public override void Stop()
        {
            if (Navigator.NavigationProvider is MeshNavigator nav)
            {
                nav.UpdateMaps();
                nav.Nav.RandomizePaths = false;
                nav.Nav.SetFactionQueryFilter(StyxWoW.Me.IsHorde);
            }
            BotEvents.Player.OnMapChanged -= Player_OnMapChanged;

            Lua.Events.DetachEvent("PLAYER_SPECIALIZATION_CHANGED", PlayerSpecializationChanged);
            // Force an update.
            UpdateBattleground(null);

            if (CharacterSettings.Instance.LastUsedPath != null && File.Exists(CharacterSettings.Instance.LastUsedPath))
                ProfileManager.LoadNew(CharacterSettings.Instance.LastUsedPath, false);
            else
                ProfileManager.CurrentProfile.UseMount = null;

            if (BattlegroundHistory.Count > 0)
            {
                Logger.Write(BGBuddyResources.BGBuddySessionReport);
                Logger.Write("-----------------------------------------");
                foreach (var history in BattlegroundHistory)
                {
                    var bgName = history.Key;
                    var wins = history.Value.ContainsKey(BGBuddyResources.Won) ? history.Value[BGBuddyResources.Won] : 0;
                    var loses = history.Value.ContainsKey(BGBuddyResources.Lost) ? history.Value[BGBuddyResources.Lost] : 0;
                    Logger.Write(BGBuddyResources.WinsLosses, bgName, wins,loses);
                }
            }
        }

        private void PlayerSpecializationChanged(object sender, LuaEventArgs args)
        {
            var isHealer = StyxWoW.Me.SpecType == SpecType.Healer;
            var isTank = StyxWoW.Me.SpecType == SpecType.Tank;

            Lua.DoString("SetPVPRoles({0}, {1}, {2});", isTank.ToString().ToLowerInvariant(), isHealer.ToString().ToLowerInvariant(), (!isTank && !isHealer).ToString().ToLowerInvariant());
        }

        public override bool IsPrimaryType
        {
            get { return false; }
        }

        public override bool RequirementsMet
        {
            get
            {
                // Obviously. Any time we're in a BG, we should be running the BG bot. Period.
                if (BGs.IsInsideBattleground)
                    return true;

                // Ignore if we have deserter for some reason
                if (StyxWoW.Me.HasAura("Deserter"))
                    return false;

                // Check for stuff we need to queue for
                int queueCount = 0;
                if (BGBuddySettings.Instance.Queue1 != BattlegroundType.None)
                    queueCount++;
                if (BGBuddySettings.Instance.Queue2 != BattlegroundType.None)
                    queueCount++;
                var queuedBgs = BGs.BattlegroundStatuses.Count(s => s == BattlegroundStatus.Queued);
                // Make sure we can queue up.
                if (queuedBgs < queueCount)
                    return true;

                // And lastly... if we have a queue that popped, make sure we can get in it!
                return BGs.WaitingForConfirmation;
            }
        }

        public override void AddMonitorMembers()
        {
            dynamic statusObject = base.StatusObject;
            statusObject.HonorGained = GameStats.HonorGained;
            statusObject.HonorPerHour = GameStats.HonorPerHour;
            statusObject.BGsCompleted = GameStats.BGsCompleted;
            statusObject.BGsWon = GameStats.BGsWon;
            statusObject.HonorPoints = StyxWoW.Me.HonorPoints;
        }

        #endregion

        void CreateRootLogic()
        {
            TreeHooks.Instance.ClearAll();
            _rootLogic = new PrioritySelector(
                CreateQueueBattlegroundBehavior(),
                new Decorator(
                    ret => BGs.IsInsideBattleground,
                    CreateInsideBattlegroundBehavior())
                );
        }

        internal static Battleground CurrentBattleground { get; set; }
        public List<Battleground> Battlegrounds { get; set; }
        public event WorldStatesUpdateDelegate WorldStatesUpdated;

        #region Battleground Updates

        private void Player_OnMapChanged(BotEvents.Player.MapChangedEventArgs args)
        {
            UpdateBattleground(null);
        }

        private void UpdateBattleground(EventArgs args)
        {
            if (StyxWoW.Me.CurrentMap.IsBattleground)
            {
                uint map = StyxWoW.Me.MapId;


                if (CurrentBattleground != null && CurrentBattleground.MapId == map)
                    return;

                CurrentBattleground = Battlegrounds.FirstOrDefault(b => b.MapId == map);
                if (CurrentBattleground != null)
                {
                    SetTargetingFilters();
                    Logger.Write(BGBuddyResources.Starting + CurrentBattleground.Name);
                    if (Navigator.NavigationProvider is MeshNavigator nav)
                        nav.UpdateMaps();
                    BotPoi.Clear("Starting new BG");
                    CurrentBattleground.Start();
                }
                else
                {
                    throw new HonorbuddyUnableToStartException(BGBuddyResources.NotSupported);
                }
            }
            else
            {
                if (CurrentBattleground != null)
                {
                    CurrentBattleground.Dispose();
                    CurrentBattleground = null;
                    RemoveTargetingFilters();
                }
            }

            CreateRootLogic();
        }

        #endregion

        private static readonly WaitTimer QueueTimer = new WaitTimer(TimeSpan.FromSeconds(2));
        private Composite CreateQueueBattlegroundBehavior()
        {
            return new Decorator(
                ret => !BGs.IsInsideBattleground,
                new PrioritySelector(
                    LevelBot.CreateDeathBehavior(),
                    LevelBot.CreateVendorBehavior(),

                    new Decorator(
                        ret => StyxWoW.Me.HasAura("Deserter"),
                        new Sequence(
                            new Action(ret => Logger.Write(BGBuddyResources.WaitingForDeserter)),
                            new WaitContinue(5, ret => false, new ActionAlwaysSucceed()),
                            new ActionIdle())),

                   new Action(ret => { ObjectManager.Update(); return RunStatus.Failure; }),
                   new Decorator(
                        ret => BGs.WaitingForConfirmation && !StyxWoW.Me.Combat,
                        new PrioritySelector(
                            ctx => new Random().Next(3,10),
                            // Wait 3-10s before accepting the port. Kthxbai
                            new Sequence(
                                new Action(ret => Logger.Write(BGBuddyResources.AcceptingJoin, ret)),
                                new Sleep(ret => TimeSpan.FromMilliseconds((int)ret * 1000)),
                                // Reset the timer again, since we slept more then it's waittime.
                                new Action(ret => QueueTimer.Reset()),
                                new Action(ret => BGs.AcceptBattlegroundConfirmation()),
                                new Sleep(10000)
                                ))),

                    LevelBot.CreateCombatBehavior(), // Always accept any BG queue regardless of Combat Status but dont queue if in combat


                    // Don't queue if we are not the party leader
                    new Decorator(
                        ret => StyxWoW.Me.GroupInfo.IsInParty && !StyxWoW.Me.IsGroupLeader,
                        new Sequence(
                            new Action(ret => Logger.Write(BGBuddyResources.WaitingGroupLeader)),
                            new ActionIdle()
                            )),
                    new Decorator(
                        ret => BGBuddySettings.Instance.Queue1 != BattlegroundType.None && !BGs.IsQueuedForBattleground(BGBuddySettings.Instance.Queue1),
                        new Sequence(
                            new Action(ret => Logger.Write(BGBuddyResources.QueueingUpFor, BGBuddySettings.Instance.Queue1)),
                            new Action(ret => BGs.JoinBattlefield(BGBuddySettings.Instance.Queue1, StyxWoW.Me.GroupInfo.IsInParty)))),
                    new Decorator(
                        ret => BGBuddySettings.Instance.Queue2 != BattlegroundType.None && !BGs.IsQueuedForBattleground(BGBuddySettings.Instance.Queue2),
                        new Sequence(
                            new Action(ret => Logger.Write(BGBuddyResources.QueueingUpFor, BGBuddySettings.Instance.Queue2)),
                            new Action(ret => BGs.JoinBattlefield(BGBuddySettings.Instance.Queue2, StyxWoW.Me.GroupInfo.IsInParty)))),
                    new ActionIdle()
                    ));
        }

        private bool WonOrLost
        {
            get
            {
                return BGs.Winner == BattlefieldWinner.Alliance ? StyxWoW.Me.IsAlliance : StyxWoW.Me.IsHorde;
            }
        }


        public static Dictionary<string, Dictionary<string, int>> BattlegroundHistory = new Dictionary<string, Dictionary<string, int>>();

        private void UpdateBattlegroundHistory()
        {
            var bgName = CurrentBattleground.Name;
            var wonOrLost = WonOrLost ? BGBuddyResources.Won : BGBuddyResources.Lost;

            if (WonOrLost)
                GameStats.BGWon();
            else
                GameStats.BGLost();

            if (BattlegroundHistory.ContainsKey(bgName))
            {
                var bgHistory= BattlegroundHistory[bgName];
                if (bgHistory.ContainsKey(wonOrLost))
                {
                    bgHistory[wonOrLost]++;
                }
                else
                {
                    bgHistory.Add(wonOrLost, 1);
                }
            }
            else
            {
                var newDict = new Dictionary<string, int> {{wonOrLost, 1}};
                BattlegroundHistory.Add(bgName, newDict);
            }
        }

        private Composite CreateInsideBattlegroundBehavior()
        {

            var retcomp = new PrioritySelector(

                new Decorator(
                    ret => BGs.Finished,
                    new Sequence(
                        new Action(ret => Logger.Write(BGBuddyResources.BattlegroundEnded, WonOrLost ? BGBuddyResources.Won2: BGBuddyResources.Lost2)),
                        new Action(ret => UpdateBattlegroundHistory()),
                        new WaitContinue(3, ret => false, new ActionAlwaysSucceed()),
                        new Action(ret => Logger.Write(BGBuddyResources.LeavingBattleground)),
                        new Action(ret => Navigator.PlayerMover.MoveStop()),
                        new Action(ret => Navigator.Clear()),
                        new ActionClearPoi("Finished BG"),
                        new Action(ret => BGs.LeaveBattlefield()),
                        new Sleep(10000),
                        new Action(ret => QueueTimer.Reset()))),

                new Decorator(
                    ret => StyxWoW.Me.IsDead || StyxWoW.Me.IsGhost,
                    new PrioritySelector(
                        ctx => ObjectManager.ObjectList.FirstOrDefault(o => o is WoWUnit && ((WoWUnit)o).IsSpiritGuide),
                        // If there's no spirit guide (bg death fucker), then repop us
                        new Decorator(
                            ret => ret == null,
                            new PrioritySelector(
                                new Wait(() => new Random().Next(1, 3), ret => false, new ActionAlwaysFail()),
                                new Sequence(
                                    new Action(ret => BotPoi.Clear(BGBuddyResources.Died)),
                                    new Action(ret => Navigator.PlayerMover.MoveStop()),
                                    new Action(ret => Lua.DoString("RepopMe()")))
                                )
                            ),
                        new Decorator(
                            ret => ret != null,
                            // There's a spirit guide near. Just wait for it to res us.
                            new ActionAlwaysSucceed()
                            ))));

            // If we have a BG. Set the logic.
            if (CurrentBattleground != null)
            {
                retcomp.AddChild(CurrentBattleground.CreateCommonLogic);
                retcomp.AddChild(CurrentBattleground.Logic);
            }
            return retcomp;
        }

        public Composite CreateTakeConsumablesBehavior()
        {
            /*
              Refreshment Table => 207387
              Soulwell => 181621
             */
            return new Decorator(
                ret => ObjectManager.GetObjectsOfType<WoWGameObject>().Any(go =>go.Entry == 181621),
                new PrioritySelector(

                    // Healthstones...
                    new Decorator(
                        ret => !StyxWoW.Me.CarriedItems.Any(i => i.ItemSpells.Any(s => s.ActualSpell != null && s.ActualSpell.Name == "Healthstone")),
                        new PrioritySelector(
                            // Cache the soul well
                            ctx => ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(go => go.Entry == 181621),
                            new Decorator(
                                ret => ((WoWObject)ret).WithinInteractRange,
                                new Action(ret => ((WoWObject)ret).Interact())),
                            CreateMoveToLocationBehavior(ret => ((WoWObject)ret).Location, true, 2)

                            ))


                    ));
        }

        #region Shared Behaviors


        public delegate WoWPoint LocationRetriever(object context);

        public delegate float DynamicRangeRetriever(object context);
        /// <summary>
        ///   Creates a move to location behavior. Will return RunStatus.Success if it has reached the location, or stopped in range. Best used at the end of a rotation.
        /// </summary>
        /// <remarks>
        ///   Created 5/1/2011.
        /// </remarks>
        /// <param name = "location">The location.</param>
        /// <param name = "stopInRange">true to stop in range.</param>
        /// <param name = "range">The range.</param>
        /// <returns>.</returns>
        public static Composite CreateMoveToLocationBehavior(LocationRetriever location, bool stopInRange, float range)
        {
            // Do not fuck with this. It will ensure we stop in range if we're supposed to.
            // Otherwise it'll stick to the targets ass like flies on dog shit.
            // Specifying a range of, 2 or so, will ensure we're constantly running to the target. Specifying 0 will cause us to spin in circles around the target
            // or chase it down like mad. (PVP oriented behavior)
            return new PrioritySelector(
                new Decorator(
                    // Give it a little more than 1/2 a yard buffer to get it right. CTM is never 'exact' on where we land. So don't expect it to be.
                    ret => stopInRange && StyxWoW.Me.Location.Distance(location(ret)) + 0.6f < range,
                    new PrioritySelector(
                        CreateEnsureMovementStoppedBehavior(),
                        // In short; if we're not moving, just 'succeed' here, so we break the tree.
                        new Action(ret => RunStatus.Success)
                        )
                    ),
                new Action(ret => Navigator.MoveTo(location(ret)))
                );
        }
        /// <summary>
        ///   Creates the ensure movement stopped behavior. Will return RunStatus.Success if it has stopped any movement, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/1/2011.
        /// </remarks>
        /// <returns>.</returns>
        public static Composite CreateEnsureMovementStoppedBehavior()
        {
            return new Decorator(
                ret => StyxWoW.Me.IsMoving,
                new Action(ret => Navigator.PlayerMover.MoveStop()));
        }

        #endregion
    }
}
