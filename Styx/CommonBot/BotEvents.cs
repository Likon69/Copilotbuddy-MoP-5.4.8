using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public static class BotEvents
    {
        #region Fields

        private static readonly List<Action> _eventCheckers = new List<Action>();

        #endregion

        #region Constructors

        static BotEvents()
        {
            LoadEventCheckers(typeof(BotEvents));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Recursively loads all event checkers from a type (if it's a class), including nested classes.
        /// </summary>
        /// <param name="t"></param>
        private static void LoadEventCheckers(Type t)
        {
            if (!t.IsClass)
                return;

            var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
            foreach (var method in methods)
            {
                var checkerAttributes = method.GetCustomAttributes(typeof(EventCheckerAttribute), false);
                if (checkerAttributes == null || checkerAttributes.Length == 0)
                    continue;

                if (method.GetParameters().Length != 0 || method.ReturnType != typeof(void))
                {
                    Logging.WriteDiagnostic("[BotEvents] Invalid method signature for {0}. Skipping", method);
                    continue;
                }

                _eventCheckers.Add((Action)Delegate.CreateDelegate(typeof(Action), method));
            }

            var nestedClasses = t.GetNestedTypes();
            foreach (var nestedClass in nestedClasses)
                LoadEventCheckers(nestedClass);
        }

        public static void PulseEvents()
        {
            foreach (Action eventChecker in _eventCheckers)
            {
                try
                {
                    eventChecker();
                }
                catch (Exception ex)
                {
                    Logging.WriteDiagnostic("[BotEvents] EventChecker threw exception");
                    Logging.WriteDiagnostic(ex.ToString());
                }
            }
        }

        private static void InvokeEvent(Delegate d, string eventName, params object[] args)
        {
            InvokeEvent(d, ex =>
            {
                Logging.WriteDiagnostic("Handler for {0} threw exception!", eventName);
                Logging.WriteDiagnostic(ex.ToString());
            }, args);
        }

        private static void InvokeEvent(Delegate d, Action<Exception> exceptionHandler, params object[] args)
        {
            if (d == null)
                return;

            foreach (Delegate d2 in d.GetInvocationList())
            {
                try
                {
                    d2.DynamicInvoke(args);
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                        exceptionHandler(ex);
                }
            }
        }

        private static bool FireStartStopRequested(OnBotStartStopRequestedDelegate handler, string eventName)
        {
            if (handler == null)
                return true;

            var args = new BotStartStopRequestEventArgs();
            foreach (OnBotStartStopRequestedDelegate subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(args);
                }
                catch (Exception ex)
                {
                    Logging.WriteDiagnostic(Colors.Red, "An exception was thrown by a subcriber to {0}!", eventName);
                    Logging.WriteDiagnostic(Colors.Red, ex.ToString());
                    continue;
                }

                if (args.Cancel)
                    return false;
            }

            return true;
        }

        #endregion

        #region Bot Start

        public class BotStartStopRequestEventArgs : EventArgs
        {
            public bool Cancel { get; set; }
        }

        public delegate void OnBotStartStopRequestedDelegate(BotStartStopRequestEventArgs args);

        public static event OnBotStartStopRequestedDelegate OnBotStartRequested;

        public delegate void OnBotStartDelegate(EventArgs args);

        private sealed class LegacyOnBotStartSubscription
        {
            public LegacyOnBotStartSubscription(OnBotStartDelegate handler)
            {
                Handler = handler;
                RequestHandler = OnStartRequested;
            }

            private void OnStartRequested(BotStartStopRequestEventArgs args)
            {
                try
                {
                    Handler(args);
                }
                catch (HonorbuddyUnableToStartException ex)
                {
                    Logging.WriteQuiet(Colors.Red, ex.Message);
                    args.Cancel = true;
                }
            }

            public OnBotStartDelegate Handler;
            public OnBotStartStopRequestedDelegate RequestHandler;
        }

        private static readonly List<LegacyOnBotStartSubscription> _legacyOnBotStartSubscriptions = new List<LegacyOnBotStartSubscription>();

        /// <summary>
        /// Fired right before the bot is started.
        /// </summary>
        [Obsolete("Use OnBotStartRequested instead and use the event args to cancel the start request, if that is required.")]
        public static event OnBotStartDelegate OnBotStart
        {
            add
            {
                lock (_legacyOnBotStartSubscriptions)
                {
                    var subscription = new LegacyOnBotStartSubscription(value);
                    OnBotStartRequested += subscription.RequestHandler;
                    _legacyOnBotStartSubscriptions.Add(subscription);
                }
            }
            remove
            {
                lock (_legacyOnBotStartSubscriptions)
                {
                    int index = _legacyOnBotStartSubscriptions.FindIndex(subscription => subscription.Handler == value);
                    if (index != -1)
                    {
                        OnBotStartRequested -= _legacyOnBotStartSubscriptions[index].RequestHandler;
                        _legacyOnBotStartSubscriptions.RemoveAt(index);
                    }
                }
            }
        }

        /// <summary>
        /// Fired right after the bot is started.
        /// </summary>
        public static event OnBotStartDelegate OnBotStarted;

        internal static bool FireBotStartRequested()
        {
            return FireStartStopRequested(OnBotStartRequested, "OnBotStartRequested");
        }

        internal static void FireBotStarted()
        {
            InvokeEvent(OnBotStarted, "OnBotStarted", EventArgs.Empty);
        }

        #endregion

        #region Bot Stop

        public delegate void OnBotStopDelegate(EventArgs args);

        public static event OnBotStartStopRequestedDelegate OnBotStopRequested;

        /// <summary>
        /// Fired right before the bot has been stopped.
        /// </summary>
        [Obsolete("Use OnBotStopped instead. This will be removed soon.")]
        public static event OnBotStopDelegate OnBotStop
        {
            add
            {
                OnBotStopped += value;
            }
            remove
            {
                OnBotStopped -= value;
            }
        }

        /// <summary>
        /// Fired right after the bot has been stopped.
        /// </summary>
        public static event OnBotStopDelegate OnBotStopped;

        internal static bool FireBotStopRequested()
        {
            return FireStartStopRequested(OnBotStopRequested, "OnBotStopRequested");
        }

        internal static void FireBotStopped(TimeSpan runTime)
        {
            InvokeEvent(OnBotStopped, "OnBotStopped", new BotStoppedEventArgs(runTime));
        }

        public class BotStoppedEventArgs : EventArgs
        {
            internal BotStoppedEventArgs(TimeSpan runTime)
            {
                RunTime = runTime;
            }

            public TimeSpan RunTime { get; private set; }
        }

        #endregion

        #region Bot Changed

        public class BotChangedEventArgs : EventArgs
        {
            internal BotChangedEventArgs(BotBase oldBot, BotBase newBot)
            {
                OldBot = oldBot;
                NewBot = newBot;
            }

            public BotBase OldBot { get; private set; }
            public BotBase NewBot { get; private set; }
        }

        public delegate void OnBotChangedDelegate(BotChangedEventArgs args);

        /// <summary>
        /// Fired when the current bot is changed with the usage of TreeRoot.SetCurrent.
        /// </summary>
        public static event OnBotChangedDelegate OnBotChanged;

        internal static void FireBotChanged(BotChangedEventArgs args)
        {
            InvokeEvent(OnBotChanged, "OnBotChanged", args);
        }

        #endregion

        #region Pulse

        internal class PulseEventArgs : EventArgs
        {
            public int TickCount;
        }

        internal delegate void PulseDelegate(PulseEventArgs args);
        internal static event PulseDelegate OnPulse;

        internal static void FirePulse(int tickCount)
        {
            InvokeEvent(OnPulse, "OnPulse", new PulseEventArgs { TickCount = tickCount });
        }

        #endregion

        #region Nested classes

        public static class Profile
        {
            #region New Profile Loaded

            public class NewProfileLoadedEventArgs : EventArgs
            {
                public Profiles.Profile OldProfile;
                public Profiles.Profile NewProfile;
            }

            public delegate void NewProfileLoadedDelegate(NewProfileLoadedEventArgs args);

            /// <summary>
            /// Fired when a new inner profile is loaded. That means, without the actually .xml profile changing, but a new subprofile in the current profile is loaded.
            /// </summary>
            public static event NewProfileLoadedDelegate OnNewProfileLoaded;

            /// <summary>
            /// Fired when a new outer profile is loaded. That means when the actual .xml file changes.
            /// </summary>
            public static event NewProfileLoadedDelegate OnNewOuterProfileLoaded;

            internal static void FireNewProfileLoaded(Profiles.Profile oldProf, Profiles.Profile newProf)
            {
                InvokeEvent(OnNewProfileLoaded, "OnNewProfileLoaded", new NewProfileLoadedEventArgs { NewProfile = newProf, OldProfile = oldProf });
            }

            internal static void FireNewOuterProfileLoaded(Profiles.Profile oldProf, Profiles.Profile newProf)
            {
                InvokeEvent(OnNewOuterProfileLoaded, "OnNewOuterProfileLoaded", new NewProfileLoadedEventArgs { NewProfile = newProf, OldProfile = oldProf });
            }

            #endregion
        }

        public static class Player
        {
            #region Map Changed

            public class MapChangedEventArgs : EventArgs
            {
                /// <summary>
                /// The new map name. This is not a friendly map name, but is used by Tripper to denote which meshes to use.
                /// </summary>
                public string NewMapName;
            }

            public delegate void MapChangedDelegate(MapChangedEventArgs args);

            /// <summary>
            /// This event is fired when the map changes.
            /// </summary>
            public static event MapChangedDelegate OnMapChanged;

            private static uint? _oldMapId;

            [EventChecker]
            private static void CheckMapChanged()
            {
                if (StyxWoW.Me == null || !StyxWoW.IsInGame)
                    return;

                uint currentMapId = StyxWoW.Me.MapId;
                if (_oldMapId.HasValue && _oldMapId.Value == currentMapId)
                    return;

                string currentMapName = StyxWoW.Me.MapName;

                InvokeEvent(OnMapChanged, "OnMapChanged", new MapChangedEventArgs { NewMapName = currentMapName });

                _oldMapId = currentMapId;
            }

            #endregion

            #region Level Up

            public class LevelUpEventArgs : EventArgs
            {
                public int NewLevel;
                public int OldLevel;
            }

            public delegate void LevelUpDelegate(LevelUpEventArgs args);
            public static event LevelUpDelegate OnLevelUp;

            private static int _lastLevel;

            [EventChecker]
            internal static void CheckLevelUp()
            {
                if (StyxWoW.Me == null)
                    return;

                if (_lastLevel == 0)
                {
                    _lastLevel = StyxWoW.Me.Level;
                    return;
                }

                int myLevel = StyxWoW.Me.Level;
                if (myLevel <= _lastLevel)
                    return;

                InvokeEvent(OnLevelUp, "OnLevelUp", new LevelUpEventArgs { OldLevel = _lastLevel, NewLevel = myLevel });

                _lastLevel = myLevel;
            }

            #endregion

            #region Mob Killed

            public class MobKilledEventArgs : EventArgs
            {
                public WoWUnit KilledMob;
                public DateTime KillTime;
            }

            public delegate void MobKilledDelegate(MobKilledEventArgs args);
            public static event MobKilledDelegate OnMobKilled;

            private static readonly Dictionary<ulong, Stopwatch> _mobsKilled = new Dictionary<ulong, Stopwatch>();

            [EventChecker]
            internal static void CheckMobKilled()
            {
                IEnumerable<WoWUnit> units = ObjectManager.GetObjectsOfType<WoWUnit>(false, false);
                foreach (WoWUnit unit in units)
                {
                    ulong guid = unit.Guid;

                    Stopwatch stopwatch;
                    if (_mobsKilled.TryGetValue(guid, out stopwatch))
                    {
                        if (stopwatch.Elapsed.TotalMinutes <= 5.0)
                            continue;

                        _mobsKilled.Remove(guid);
                    }

                    if (unit.KilledByMe)
                    {
                        _mobsKilled[guid] = Stopwatch.StartNew();

                        InvokeEvent(OnMobKilled, "OnMobKilled", new MobKilledEventArgs { KilledMob = unit, KillTime = DateTime.Now });
                    }
                }
            }

            #endregion

            #region Looted Mob

            public class MobLootedEventArgs : EventArgs
            {
                public WoWUnit LootedMob;
            }

            public delegate void MobLootedDelegate(MobLootedEventArgs args);
            public static event MobLootedDelegate OnMobLooted;

            internal static void FireMobLooted(WoWUnit mob)
            {
                if (OnMobLooted == null)
                    return;

                InvokeEvent(OnMobLooted, "OnMobLooted", new MobLootedEventArgs { LootedMob = mob });
            }

            #endregion

            #region Object Harvested

            public class ObjectHarvestedEventArgs : EventArgs
            {
                public WoWGameObject HarvestedObject;
            }

            public delegate void ObjectHarvestedDelegate(ObjectHarvestedEventArgs args);
            public static event ObjectHarvestedDelegate OnObjectHarvested;

            internal static void FireObjectHarvested(WoWGameObject gameObject)
            {
                if (OnObjectHarvested == null)
                    return;

                InvokeEvent(OnObjectHarvested, "OnObjectHarvested", new ObjectHarvestedEventArgs { HarvestedObject = gameObject });
            }

            #endregion

            #region Player Died

            public delegate void PlayerDiedDelegate();

            /// <summary>
            /// This event fires when the local player dies
            /// </summary>
            public static event PlayerDiedDelegate OnPlayerDied;

            private static bool _playerDeadLastState;

            [EventChecker]
            private static void CheckPlayerDied()
            {
                if (StyxWoW.Me == null)
                    return;

                bool curState = StyxWoW.Me.IsDead;
                if (curState && !_playerDeadLastState)
                {
                    if (StyxWoW.Me.IsInInstance)
                    {
                        WoWDb.Row row = StyxWoW.Db[ClientDb.Map].GetRow(StyxWoW.Me.MapId);
                        float x = row.GetField<float>(13U);
                        float y = row.GetField<float>(14U);
                        LocalPlayer.InstanceDeathLocation = new WoWPoint(x, y, 0f);
                    }
                    else
                    {
                        LocalPlayer.InstanceDeathLocation = WoWPoint.Empty;
                    }

                    InvokeEvent(OnPlayerDied, "OnPlayerDied");
                }

                _playerDeadLastState = curState;
            }

            #endregion
        }

        public static class Battleground
        {
            static Battleground()
            {
                OnBotStarted += Battleground_OnBotStarted;
            }

            private static void Battleground_OnBotStarted(EventArgs args)
            {
                _lastBg = BattlegroundType.None;
            }

            #region Enter BG

            public delegate void BattlegroundEnterDelegate(BattlegroundType type);

            /// <summary>
            /// Event occurs as the localplayer enters a battleground, sender contains the current battlegroundtype
            /// </summary>
            public static event BattlegroundEnterDelegate OnBattlegroundEntered;

            public static void FireBattlegroundEntered()
            {
                InvokeEvent(OnBattlegroundEntered, "OnBattlegroundEntered", Battlegrounds.Current);
            }

            public static void FireBattlegroundLeft()
            {
                InvokeEvent(OnBattlegroundLeft, "OnBattlegroundLeft", EventArgs.Empty);
            }

            private static BattlegroundType _lastBg;

            [EventChecker]
            internal static void CheckBattleground()
            {
                BattlegroundType currentBg = Battlegrounds.Current;
                if (currentBg == _lastBg)
                    return;

                if (currentBg == BattlegroundType.None)
                {
                    FireBattlegroundLeft();
                }
                else
                {
                    FireBattlegroundEntered();
                }

                _lastBg = currentBg;
            }

            #endregion

            #region Left BG

            public delegate void BattlegroundLeftDelegate(EventArgs args);
            public static event BattlegroundLeftDelegate OnBattlegroundLeft;

            #endregion
        }

        public static class Questing
        {
            public delegate void QuestAcceptedDelegate(Quest quest);

            /// <summary>
            /// Event occurs as the localplayer accepts a new quest from a questgiver (gameobject/npc/item)
            /// </summary>
            public static event QuestAcceptedDelegate OnQuestAccepted;

            public static void FireQuestAccepted(Quest quest)
            {
                InvokeEvent(OnQuestAccepted, "OnQuestAccepted", quest);
            }
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class EventCheckerAttribute : Attribute
    {
    }
}
