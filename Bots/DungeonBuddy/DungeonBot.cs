using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Behaviors;
using CommonBehaviors.Actions;
using System.Linq;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Localization;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

using Bots.DungeonBuddy.Forms;
using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Action = Styx.TreeSharp.Action;
using AvoidanceManager = Bots.DungeonBuddy.Avoidance.AvoidanceManager;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot : BotBase
    {
        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        internal static readonly DungeonNavigationProvider DungeonNavigator = new DungeonNavigationProvider();

        internal static DungeonNavigationProvider NavigationProvider { get { return DungeonNavigator; } }

        private bool _initializationStarted;

        internal static string HbPath = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);

        internal static uint CurrentLfgDungeonId { get; private set; }

        internal static DungeonBot Instance { get; private set; }

        public DungeonBot()
        {
            Instance = this;
        }

        #region Overrides of BotBase

        public override void OnSelected()
        {
            AttachEvents();
        }

        public override void OnDeselected()
        {
            DetachEvents();
        }

        public override string Name { get { return Globalization.DungeonBuddy_Name; } }

        private Composite _root { get; set; }

        public override Composite Root
        {
            get
            {
                return _root ?? (_root = new LockSelector(
                                             ctx => GlobalSettings.Instance.UseFrameLock,
                                             // Accepting dungeon invites, role checks, resurrects etc.
                                             CreateEventBehavior(),
                                             // load dungeon if farming it
                                             new Decorator(
                                                 ret =>
                                                 DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm &&
                                                 (DungeonManager.CurrentDungeon == null ||
                                                  DungeonManager.CurrentDungeon.DungeonId != DungeonBuddySettings.Instance.SelectedDungeonIds.FirstOrDefault()),
                                                 new Action(ret => LoadDungeonScriptAndProfile(DungeonBuddySettings.Instance.SelectedDungeonIds.FirstOrDefault()))),
                                             // load raid if farming it
                                             new Decorator(
                                                 ret =>
                                                 DungeonBuddySettings.Instance.RaidType == RaidType.Farm &&
                                                 (DungeonManager.CurrentDungeon == null ||
                                                  DungeonManager.CurrentDungeon.DungeonId != DungeonBuddySettings.Instance.SelectedRaidIds.FirstOrDefault()),
                                                 new Action(ret => LoadDungeonScriptAndProfile(DungeonBuddySettings.Instance.SelectedRaidIds.FirstOrDefault()))),

                                             // Queueing for the dungeon finder.
                                             CreateEnqueueBehavior(),
                                             CreateInsideDungeonBehavior(),
                                             CreateOutsideDungeonBehavior(),
                                             new ActionIdle()));
            }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All; }
        }

        public override bool IsPrimaryType
        {
            get { return false; }
        }

        public override bool RequirementsMet
        {
            get
            {
                if (!DungeonManager.Initialized)
                    return false;

                if (ShouldAcceptProposal)
                    return true;

                Map map = Me.CurrentMap;
                if (map.IsDungeon || map.IsScenario || map.IsRaid)
                    return true;

                if (IsFarming)
                    return true;

                if (ShouldVendor)
                    return true;

                if (DungeonManager.SelectedDungeon != null)
                    return true;

                if (Me.HasAura(71041))
                    return false;

                if (Me.HasAura("Dungeon Cooldown"))
                    return false;

                if (DungeonBuddySettings.Instance.PartyMode != PartyMode.Follower)
                {
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        if (LfgQueue.SupportedLfgCategories.Any(LfgQueue.NeedsQueueSync))
                            return true;
                    }
                }

                return false;
            }
        }

        public override void Start()
        {
            if (Me.Level < 15)
                throw new HonorbuddyUnableToStartException("DungeonBuddy can't run unless you are level 15 or higher.");

            if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Follower && string.IsNullOrWhiteSpace(DungeonBuddySettings.Instance.LeaderName))
                throw new HonorbuddyUnableToStartException("You must specify the name of leader in the 'Leader Name' field in Dungeonbuddy settings");

            if (DungeonBuddySettings.Instance.PartyMode == PartyMode.Leader &&
                (DungeonBuddySettings.Instance.PartyMembers == null || !DungeonBuddySettings.Instance.PartyMembers.Any()))
                throw new HonorbuddyUnableToStartException("You must add the names of your followers in the 'Party Members' field in Dungeonbuddy settings");

            if (DungeonBuddySettings.Instance.DungeonType == DungeonType.None &&
                DungeonBuddySettings.Instance.ScenarioType == ScenarioType.None &&
                DungeonBuddySettings.Instance.RaidType == RaidType.None &&
                DungeonBuddySettings.Instance.PartyMode != PartyMode.Follower)
                throw new HonorbuddyUnableToStartException("You have not selected a Dungeon/Scenario/Raid type.");

            DungeonManager.SelectedDungeon = null;

            ProfileManager.LoadEmpty();

            Navigator.NavigationProvider = DungeonNavigator;
            DungeonNavigator.UpdateMaps();

            Targeting.Instance = new DungeonTargeting();

            _inactivityTimer.Reset();
            _lastKnownLocation = Me.Location;

            if (Me.GroupInfo.IsInParty && Me.IsGroupLeader && DungeonBuddySettings.Instance.FreeForAllLoot)
                Lua.DoString("SetLootMethod('freeforall')");

            if (!GlobalSettings.Instance.UseFrameLock)
                Logger.Write(Colors.Red, "It is highly recommended that framelock is enabled in Honorbuddy settings while running Dungeonbuddy because of reduced reaction time and other problems the can occurr while disabled.");

            PrintDiagnosticsInfo(Colors.LawnGreen);
        }

        public override void Stop()
        {
            if (CharacterSettings.Instance.LastUsedPath != null && File.Exists(CharacterSettings.Instance.LastUsedPath))
                ProfileManager.LoadNew(CharacterSettings.Instance.LastUsedPath, false);

            HandleDungeonLeft();

            Targeting.Instance = new Targeting();

            Navigator.NavigationProvider = new MeshNavigator();
            ((MeshNavigator) Navigator.NavigationProvider).Nav.ChangeMap(new[] { Me.MapName });
        }

        public override void Initialize()
        {
            if (!_initializationStarted)
            {
                Task.Factory.StartNew(Updater.Update).ContinueWith(task => DungeonManager.Initialize());
                _initializationStarted = true;
            }
        }


        private FormConfig _gui;

        public override Form ConfigurationForm
        {
            get
            {
                if (!FormConfig.IsShown)
                    _gui = new FormConfig();
                else
                    _gui.Activate();
                return _gui;
            }
        }

        private readonly WaitTimer _inactivityTimer = new WaitTimer(TimeSpan.FromMinutes(15));
        private WoWPoint _lastKnownLocation;
        private int _slowPulseCnt;
        private DateTime _lastPulseTime;

        public override void Pulse()
        {
            if (!StyxWoW.IsInWorld || !DungeonManager.Initialized)
                return;

            ScriptHelpers.PulseMovementChecks();
            AvoidanceManager.Pulse();
            DynamicBlackspotManager.Pulse();

            WoWPoint location = Me.Location;
            if (location.DistanceSqr(_lastKnownLocation) > 0.25)
            {
                _lastKnownLocation = location;
                _inactivityTimer.Reset();
            }

            var currentTime = DateTime.Now;
            var pulseTime = currentTime - _lastPulseTime;
            if (_lastPulseTime != DateTime.MinValue && pulseTime >= TimeSpan.FromSeconds(2))
            {
                if (_slowPulseCnt >= 2)
                    Logger.WriteDebug("Warning: There was a {0}.{1} second delay after last pulse", pulseTime.TotalSeconds, pulseTime.Milliseconds);
                _slowPulseCnt++;
            }
            else if (_slowPulseCnt > 0)
                _slowPulseCnt = 0;
            _lastPulseTime = currentTime;
        }

        #endregion

        private void PrintDiagnosticsInfo(Color color)
        {
            switch (DungeonBuddySettings.Instance.PartyMode)
            {
                case PartyMode.Leader:
                    Logger.Write(
                        color,
                        "Party Mode is on and I'm the Leader. I will attempt to invite {0} others to party before continuing",
                        DungeonBuddySettings.Instance.PartyMembers.Length);
                    break;
                case PartyMode.Follower:
                    Logger.Write(color, "Party Mode is on and I'm a follower. I will be invited to group by leader");
                    if (string.IsNullOrWhiteSpace(DungeonBuddySettings.Instance.LeaderName))
                        Logger.Write(color, "I do not know who my leader is (Leader Name in settings is blank)");
                    break;
                case PartyMode.Off:
                    Logger.Write(color, "Party Mode is off");
                    break;
            }

            Logger.Write(color, "I will {0}kill optional bosses.", DungeonBuddySettings.Instance.KillOptionalBosses ? null : "not ");
            Logger.Write(color, "I will sell items of {0} quality and lower", DungeonBuddySettings.Instance.SellItemQuality);
            Logger.Write(
                color,
                "I will {0}mail Bind-On-Equip items{1}.",
                DungeonBuddySettings.Instance.MailBoeItems ? null : "not ",
                DungeonBuddySettings.Instance.MailBoeItems ? string.Format(" of {0} quality and higher", DungeonBuddySettings.Instance.MailItemQuality) : null);

            Logger.Write(color, "I will loot {0}", DungeonBuddySettings.Instance.LootMode == LootMode.Always ? "everything" : "bosses only");
            Logger.Write(color, "I have FrameLock turned {0}", GlobalSettings.Instance.UseFrameLock ? "on" : "off");
            Logger.Write(color, "My role is {0}", Me.IsTank() ? "tank" : Me.IsHealer() ? "healer" : Me.IsRange() ? "ranged dps" : "melee dps");

            // Dungeons
            Logger.Write(color, "Dungeon type is set to {0}", DungeonBuddySettings.Instance.DungeonType);
            if (DungeonBuddySettings.Instance.DungeonType == DungeonType.Specific)
            {
                if (DungeonBuddySettings.Instance.SelectedDungeonIds.Length > 0)
                {
                    Logger.Write(color, "I have selected the following dungeons");
                    foreach (var d in LfgDungeons.Dungeons.Where(d => DungeonBuddySettings.Instance.SelectedDungeonIds.Contains(d.Id)))
                        Logger.Write(color, "{0}", d.DifficultyId == 1 ? d.Name : d.Name + " Heroic");
                }
                else
                    Logger.Write(color, "I have not selected any dungeons");
            }
            if (DungeonBuddySettings.Instance.DungeonType == DungeonType.Farm)
            {
                var dungId = DungeonBuddySettings.Instance.SelectedDungeonIds.FirstOrDefault();
                if (dungId > 0)
                    Logger.Write(color, "I have selected to farm  the {0} dungeon", LfgDungeons.FromId(dungId).Name);
                else
                    Logger.Write(color, "I have not selected a dungeon to farm");
            }
            // Raids
            Logger.Write(color, "Raid type is set to {0}", DungeonBuddySettings.Instance.RaidType);
            if (DungeonBuddySettings.Instance.RaidType == RaidType.Specific || DungeonBuddySettings.Instance.RaidType == RaidType.AutoQueueBest)
            {
                if (DungeonBuddySettings.Instance.SelectedRaidIds.Length > 0 && DungeonBuddySettings.Instance.RaidType == RaidType.Specific)
                    Logger.Write(color, "1 have selected to queue for the {0} raid", LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRaidIds.FirstOrDefault()).Name);
                else if (DungeonBuddySettings.Instance.RaidType == RaidType.AutoQueueBest)
                    Logger.Write(color, "I will automatically queue for the best available raid that hasn't been completed this week.");
                else
                    Logger.Write(color, "I have not selected any raids");
            }

            if (DungeonBuddySettings.Instance.RaidType == RaidType.Farm)
            {
                if (DungeonBuddySettings.Instance.SelectedRaidIds.Length > 0)
                    Logger.Write(color, "I have selected to farm the {0} raid", LfgDungeons.FromId(DungeonBuddySettings.Instance.SelectedRaidIds.FirstOrDefault()).Name);
                else
                    Logger.Write(color, "I have not selected a raid to farm");
            }

            // Scenarios
            Logger.Write(color, "Scenario type is set to {0}", DungeonBuddySettings.Instance.ScenarioType);
            if (DungeonBuddySettings.Instance.ScenarioType == ScenarioType.Specific)
            {
                if (DungeonBuddySettings.Instance.SpecifiedScenariosIds.Length > 0)
                {
                    Logger.Write(color, "I have selected the following scenarios");
                    foreach (var d in LfgDungeons.Scenarios.Where(d => DungeonBuddySettings.Instance.SpecifiedScenariosIds.Contains(d.Id)))
                        Logger.Write(color, "{0}", d.Name);
                }
                else
                    Logger.Write(color, "I have not selected any scenarios");
            }
        }

        private static class BehaviorDebug
        {
            public static string GetActionName(Action action)
            {
                if (action != null)
                {
                    MethodInfo actionMethod = null;
                    if (action.SucceedRunner != null)
                        actionMethod = action.SucceedRunner.Method;
                    if (action.Runner != null)
                        actionMethod = action.Runner.Method;

                    if (actionMethod != null)
                        return string.Format("{0}.{1}", actionMethod.Module.Assembly.GetName().Name, actionMethod.Name);
                    var actionType = action.GetType();
                    return string.Format("{0}.{1}", actionType.Assembly.GetName().Name, actionType.Name);
                }
                return string.Empty;
            }

            public static Action GetLastBehaviorAction(Composite behavior)
            {
                var groupComposite = behavior as GroupComposite;
                if (groupComposite != null)
                {
                    foreach (
                        var child in
                            groupComposite.Children.Where(
                                child => child != null && child.LastStatus.HasValue && (child.LastStatus == RunStatus.Success || child.LastStatus == RunStatus.Running)))
                    {
                        if (child is GroupComposite)
                            return GetLastBehaviorAction(child);
                        if (child is Action)
                        {
                            var childType = child.GetType();
                            if (childType == typeof (HookExecutor))
                            {
                                var compositeFieldInfo =
                                    childType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(fi => fi.FieldType == typeof (Composite));
                                var compositeValue = compositeFieldInfo.GetValue(child) as Composite;
                                if (compositeValue == null)
                                    return null;
                                if (compositeValue is GroupComposite)
                                    return GetLastBehaviorAction(child);
                            }
                            // exclude actions from Custom Routine.
                            //if (RoutineManager.Current != null && RoutineManager.Current.GetType().Assembly == childType.Assembly)
                            //    return null;

                            if (childType.Assembly == Assembly.GetExecutingAssembly() && childType.IsNotPublic)
                                return null;
                            return child as Action;
                        }
                    }
                }
                return null;
            }
        }

        #region Nested type: LockSelector

        // Originally created by Contributor002. 
        /// <summary>
        ///     This behavior wraps the child behaviors in a 'FrameLock' which can provide a big performance improvement if the child behaviors makes multiple api calls that internally run off a frame in WoW in one CC pulse.
        /// </summary>
        private class LockSelector : PrioritySelector
        {
            private readonly CanRunDecoratorDelegate _useFrameLock;
            private Action _prevLastAction;

            public LockSelector(CanRunDecoratorDelegate useFramelock, params Composite[] children) : base(children)
            {
                _useFrameLock = useFramelock;
            }

            public override RunStatus Tick(object context)
            {
                if (!StyxWoW.IsInWorld)
                    return RunStatus.Success;
                RunStatus result;
                if (_useFrameLock(context))
                {
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        result = base.Tick(context);
                    }
                }
                else
                    result = base.Tick(context);

                var action = BehaviorDebug.GetLastBehaviorAction(this);
                if (action != null && action != _prevLastAction)
                {
                    Logger.WriteDebug("Executed action: {0}", BehaviorDebug.GetActionName(action));
                    _prevLastAction = action;
                }
                //BehaviorDebug
                return result;
            }
        }

        #endregion
    }
}
