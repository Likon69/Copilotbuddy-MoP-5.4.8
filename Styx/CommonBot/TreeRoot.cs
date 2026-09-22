using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Xml;
using Styx.Common;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Action = Styx.TreeSharp.Action;

namespace Styx.CommonBot
{
    /// <summary>
    /// Class that will tick/initialize/start/dispose the behavior tree
    /// </summary>
    public static class TreeRoot
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT placement);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT placement);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private struct WINDOWPLACEMENT
        {
            public int Length;
            public int Flags;
            public int ShowCmd;
            public Point MinPosition;
            public Point MaxPosition;
            public RECT NormalPosition;
        }

        private static WINDOWPLACEMENT QueryWindowPlacement(IntPtr hWnd)
        {
            var placement = default(WINDOWPLACEMENT);
            placement.Length = Marshal.SizeOf<WINDOWPLACEMENT>(placement);
            GetWindowPlacement(hWnd, ref placement);
            return placement;
        }

        private static Thread _preventMinimizeThread;
        private static WINDOWPLACEMENT? _lastWindowPlacement;

        private static void PreventMinimizedWow()
        {
            IntPtr mainWindowHandle = StyxWoW.Memory.Process.MainWindowHandle;
            WINDOWPLACEMENT placement = QueryWindowPlacement(mainWindowHandle);
            bool isMinimized = placement.ShowCmd == 2 || placement.ShowCmd == 6 || placement.ShowCmd == 7;
            if (IsRunning && isMinimized)
            {
                Logging.Write(Colors.Red, StyxResources.WoW_cannot_be_minimized_while_running_Honorbuddy_);
                if (_lastWindowPlacement != null)
                {
                    WINDOWPLACEMENT value = _lastWindowPlacement.Value;
                    value.ShowCmd = 9;
                    SetWindowPlacement(mainWindowHandle, ref value);
                }
                else
                {
                    ShowWindow(mainWindowHandle, 9);
                }
                Thread.Sleep(500);
            }
            if (!isMinimized)
            {
                _lastWindowPlacement = placement;
            }
        }

        private static void WatchWowWindow()
        {
            while (!StyxWoW.Memory.Process.HasExited)
            {
                PreventMinimizedWow();
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Occurs when Honorbuddy is requested to shut down.
        /// </summary>
        public static event EventHandler<ShutdownRequestedEventArgs> OnShutdownRequested;

        public static void Shutdown(int exitCode = 0, bool closeWow = false)
        {
            OnShutdownRequested?.Invoke(null, new ShutdownRequestedEventArgs(exitCode, closeWow));
        }

        public static bool CurrentThreadIsBotThread
        {
            get { return Thread.CurrentThread == _workerThread; }
        }

        private static byte? _ticksPerSecondOverride;

        /// <summary>
        /// Gets or sets the frequency of how often the bot is "Ticked". Default: 15
        /// </summary>
        /// <value>
        /// The ticks per second.
        /// </value>
        public static byte TicksPerSecond
        {
            get
            {
                if (_ticksPerSecondOverride != null)
                {
                    return _ticksPerSecondOverride.Value;
                }
                return CharacterSettings.Instance.TicksPerSecond;
            }
            set { _ticksPerSecondOverride = value; }
        }

        public static void ResetTicksPerSecond()
        {
            _ticksPerSecondOverride = null;
        }

        /// <summary>
        /// Gets the current <see cref="BotBase"/>.
        /// </summary>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static BotBase Current
        {
            get { return BotManager.Current; }
        }

        private static Thread _workerThread;
        private static volatile bool _isStopped;

        /// <summary>
        /// Gets a value indicating whether the <see cref="TreeRoot"/> is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the <see cref="TreeRoot"/> is running; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static bool IsRunning
        {
            get { return _workerThread != null && _workerThread.IsAlive && !_isStopped; }
        }

        internal static bool IsRunningUnpaused
        {
            get { return IsRunning && !IsPaused; }
        }

        public static bool IsPaused { get; internal set; }

        private static bool _minimizeWatcherStarted;

        internal static void Initialize()
        {
            BotEvents.OnBotStartRequested += BotEvents_OnBotStartRequested;
            BotEvents.OnBotStarted += BotEvents_OnBotStarted;
            StyxWoW.Memory.ProcessExited += WoWProcess_Exited;
            _preventMinimizeThread = new Thread(WatchWowWindow)
            {
                IsBackground = true,
                Name = "No minimizing WoW"
            };
            _preventMinimizeThread.Start();
            _minimizeWatcherStarted = true;
        }

        private static void BotEvents_OnBotStartRequested(BotEvents.BotStartStopRequestEventArgs args)
        {
            if (RoutineManager.Current != null)
            {
                return;
            }
            Logging.WriteQuiet(Colors.Red, StyxResources.Can_t_start___No_combat_class_is_loaded_);
            args.Cancel = true;
        }

        private static void BotEvents_OnBotStarted(EventArgs args)
        {
            Lua.DoString("SetCVar('autoSelfCast', '1')", "WoW.lua");
            Lua.DoString("SetCVar('autoInteract', '1')", "WoW.lua");
            Lua.DoString("SetCVar('interactOnLeftClick', '0')", "WoW.lua");
        }

        internal static bool _ignoreProcessExited;

        private static void WoWProcess_Exited(object sender, EventArgs e)
        {
            if (_ignoreProcessExited)
            {
                return;
            }
            Logging.Write(StyxResources.Wow_exited__Honorbuddy_stopped_);
            Stop(StyxResources.Wow_exited__Honorbuddy_stopped_);
            Logging.Write("Honorbuddy terminating in 20 seconds.");
            StyxWoW.Sleep(20000);
            Shutdown(0, false);
        }

        private static Composite _mainRoot;
        private static bool _wasOnTaxi;

        private static Composite MainRoot
        {
            get
            {
                if (_mainRoot == null)
                {
                    _mainRoot = new PrioritySelector(
                        new HookExecutor(
                            "InGame_Check",
                            "IsInGame check location. Warning: nothing is pulsed before this. Only hook this location for relogging purposes.",
                            new Decorator(ret => !StyxWoW.IsInGame, new Action(ret => Logging.Write(StyxResources.NotInGame)))),
                        new Action(ret => RunStatus.Failure),
                        new HookExecutor(
                            "Taxi_Check",
                            "Taxi check",
                            new PrioritySelector(
                                new Decorator(ret => StyxWoW.Me.OnTaxi,
                                    new Sequence(
                                        new Action(ret => { _wasOnTaxi = true; }),
                                        new Action(ret => { StatusText = StyxResources.Waiting_on_taxi___; }),
                                        new Action(ret => StyxWoW.ResetAfk()))),
                                new Decorator(ret => _wasOnTaxi,
                                    new Sequence(
                                        new Action(ret => { _wasOnTaxi = false; }),
                                        new Action(ret => BotPoi.Clear("Reached final destination with taxi")))))));
                }
                return _mainRoot;
            }
        }

        private static bool RunSafely(System.Action action, string actionName, bool stopBotOnFailure)
        {
            bool succeeded;
            try
            {
                action();
                succeeded = true;
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic("Exception was thrown in {0}", actionName);
                Logging.WriteDiagnostic(ex.ToString());
                if (stopBotOnFailure)
                {
                    Stop("Exception thrown");
                }
                succeeded = false;
            }
            return succeeded;
        }

        private static bool _wasPaused;

        private static void PerformTick()
        {
            if (IsPaused)
            {
                if (!_wasPaused)
                {
                    Logging.WriteQuiet(Colors.Lime, "Bot paused");
                    RunSafely(Current.OnPaused, "BotBase.OnPaused", true);
                    _wasPaused = true;
                }
                return;
            }

            if (_wasPaused)
            {
                Logging.WriteQuiet(Colors.Lime, "Bot resumed");
                if (!RunSafely(Current.OnResumed, "BotBase.OnResumed", true))
                {
                    return;
                }
                _wasPaused = false;
            }

            if (StyxWoW.IsInGame)
            {
                Pulsator.Pulse(Current.PulseFlags);
                BotEvents.FirePulse(Environment.TickCount);
            }

            if (MainRoot.LastStatus != RunStatus.Running)
            {
                MainRoot.Start(null);
            }

            if (!RunSafely(() => MainRoot.Tick(null), "MainRoot.Tick", true))
            {
                MainRoot.Stop(null);
                return;
            }

            RunStatus? lastStatus = MainRoot.LastStatus;
            if (lastStatus != RunStatus.Running)
            {
                MainRoot.Stop(null);
            }

            if (lastStatus != RunStatus.Failure)
            {
                return;
            }

            Current.Pulse();

            if (Current.Root.LastStatus != RunStatus.Running)
            {
                Current.Root.Start(null);
            }

            if (!RunSafely(() => Current.Root.Tick(null), "BotBase.Root.Tick", false))
            {
                BotPoi.Clear("Exception in Root.Tick");
                Current.Root.Stop(null);
                return;
            }

            if (Current.Root.LastStatus != RunStatus.Running)
            {
                Current.Root.Stop(null);
            }
        }

        private static bool ShouldLoadProfile(out string profilePath)
        {
            profilePath = CharacterSettings.Instance.LastUsedPath;
            if (string.IsNullOrWhiteSpace(profilePath))
            {
                return false;
            }
            if (!profilePath.StartsWith("store://") && !File.Exists(profilePath))
            {
                return false;
            }
            if (ProfileManager.CurrentOuterProfile == null)
            {
                return true;
            }
            profilePath = ProfileManager.CurrentOuterProfile.Path;
            bool hasChanged;
            try
            {
                hasChanged = ProfileManager.CurrentOuterProfile.HasChanged();
            }
            catch (Exception)
            {
                hasChanged = false;
            }
            return hasChanged;
        }

        #region Methods

        private static readonly object _startStopLock = new object();
        private static volatile bool _startSucceeded;
        private static readonly AutoResetEvent _startedSignal = new AutoResetEvent(false);

        /// <summary>
        /// Initializes and starts the current bot.
        /// </summary>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static void Start()
        {
            if (IsRunning)
            {
                return;
            }
            if (Current == null)
            {
                return;
            }
            lock (_startStopLock)
            {
                if (!IsRunning)
                {
                    if (_isStopped)
                    {
                        if (_workerThread.IsAlive)
                        {
                            _workerThread.Join();
                        }
                        _isStopped = false;
                    }
                    ObjectManager.Update();
                    if (StyxWoW.IsInGame && !(StyxWoW.Me == null))
                    {
                        BotPoi.Clear("Bot starting");
                        if (RoutineManager.Current == null)
                        {
                            RoutineManager.SetCurrentCombatRoutine();
                        }
                        string profilePath;
                        if (Current.RequiresProfile && ShouldLoadProfile(out profilePath))
                        {
                            try
                            {
                                ProfileManager.LoadNew(profilePath, true);
                            }
                            catch (XmlException ex)
                            {
                                Logging.Write(Colors.IndianRed, "Cannot start the bot because the profile cannot be loaded.");
                                Logging.Write(Colors.IndianRed, ex.Message);
                                return;
                            }
                        }
                        if (Current.RequiresProfile && ProfileManager.CurrentOuterProfile == null)
                        {
                            Logging.Write(Colors.IndianRed, "Please load a profile before starting the bot.");
                        }
                        else
                        {
                            Current.DoInitialize();
                            if (BotEvents.FireBotStartRequested())
                            {
                                _isStopped = false;
                                IsPaused = false;
                                _wasPaused = false;
                                _workerThread = new Thread(RunBotThread)
                                {
                                    IsBackground = true,
                                    Name = "BehaviorTree - Thread",
                                    CurrentUICulture = new CultureInfo(CultureInfo.InvariantCulture.Name)
                                    {
                                        NumberFormat =
                                        {
                                            CurrencyDecimalSeparator = ".",
                                            NumberDecimalSeparator = ".",
                                            NaNSymbol = "NaN"
                                        }
                                    },
                                    CurrentCulture = new CultureInfo(CultureInfo.InvariantCulture.Name)
                                    {
                                        NumberFormat =
                                        {
                                            CurrencyDecimalSeparator = ".",
                                            NumberDecimalSeparator = ".",
                                            NaNSymbol = "NaN"
                                        }
                                    }
                                };
                                _workerThread.Start();
                                _startedSignal.WaitOne();
                                if (!_startSucceeded)
                                {
                                    _workerThread.Join();
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Write(LogLevel.Quiet, Colors.Red, "The bot cannot be started while not ingame");
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to stop the current running bot.
        /// </summary>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static void Stop(string reason = null)
        {
            if (!IsRunning)
            {
                return;
            }
            lock (_startStopLock)
            {
                if (IsRunning)
                {
                    Logging.WriteQuiet("Bot stopping! Reason: " + (reason ?? "None Given"));
                    if (BotEvents.FireBotStopRequested())
                    {
                        _isStopped = true;
                    }
                }
            }
        }

        public static bool WaitForStop(int timeout = -1)
        {
            if (Thread.CurrentThread == _workerThread)
            {
                throw new InvalidOperationException("This function cannot be called from the bot thread as it would result in a dead lock");
            }
            if (timeout != -1 && timeout < 0)
            {
                throw new ArgumentOutOfRangeException("timeout", "Timeout cannot be negative");
            }
            return _workerThread == null || _workerThread.Join(timeout);
        }

        /// <summary>
        /// Function used by the thread to tick the current bot
        /// </summary>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        private static void RunBotThread()
        {
            bool started = false;
            try
            {
                Current.Start();
                started = true;
            }
            catch (HonorbuddyUnableToStartException ex)
            {
                Logging.WriteQuiet(Colors.Red, ex.Message);
            }
            catch (Exception ex2)
            {
                Logging.WriteDiagnostic("Exception was thrown in BotBase.Start. Bot cannot start.");
                Logging.WriteDiagnostic(ex2.ToString());
            }
            if (!started)
            {
                _startSucceeded = false;
                _startedSignal.Set();
                return;
            }
            BotEvents.FireBotStarted();
            _startSucceeded = true;
            _startedSignal.Set();
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (!_isStopped)
            {
                try
                {
                    TimeSpan elapsed = stopwatch.Elapsed;
                    Tick();
                    TimeSpan tickDuration = stopwatch.Elapsed - elapsed;
                    int sleepTime = (int)Math.Ceiling(1000.0 / TicksPerSecond - tickDuration.TotalMilliseconds);
                    if (sleepTime > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }
                }
                catch (Exception ex3)
                {
                    if (!(ex3 is ThreadAbortException))
                    {
                        Logging.WriteException(ex3);
                    }
                }
            }
            RunSafely(Current.Stop, "BotBase.Stop", false);
            Current.Root.Stop(null);
            Navigator.Clear();
            BotPoi.Clear("Bot stopping");
            BotEvents.FireBotStopped(stopwatch.Elapsed);
        }

        private static void Tick()
        {
            if (StyxWoW.WowExited)
            {
                return;
            }
            if (GlobalSettings.Instance.UseFrameLock)
            {
                using (StyxWoW.Memory.AcquireFrame(true))
                {
                    using (StyxWoW.Memory.TemporaryCacheState(true))
                    {
                        StyxWoW.Memory.ClearCache();
                        PerformTick();
                    }
                }
            }
            else
            {
                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    PerformTick();
                }
            }
            if (!StyxWoW.WowExited && StyxWoW.Memory.Executor.IsExecutingContinuously)
            {
                lock (StyxWoW.Memory.Executor.AssemblyLock)
                {
                    if (StyxWoW.Memory.Executor.IsExecutingContinuously)
                    {
                        Logging.WriteDiagnostic(Colors.Red, "Frame lock was forcibly released after tick completed");
                        StyxWoW.Memory.Executor.EndExecute();
                    }
                }
            }
        }

        #endregion

        private static string _statusText;

        /// <summary>
        /// Provides the status bar text for the main form and useful loggin info if needed
        /// </summary>
        [Localizable(false)]
        public static string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText == value)
                {
                    return;
                }
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Logging.WriteDiagnostic(StyxResources.Activity___0_, value);
                }
                string oldStatusText = _statusText;
                _statusText = value;
                OnStatusTextChanged?.Invoke(null, new StatusTextChangedEventArgs(oldStatusText, value));
            }
        }

        /// <summary>
        /// Occurs when [the status text changes].
        /// </summary>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static event EventHandler<StatusTextChangedEventArgs> OnStatusTextChanged;

        private static string _goalText;

        /// <summary>
        /// Gets or sets the goal text.
        /// </summary>
        /// <value>
        /// The goal text.
        /// </value>
        /// <remarks>Created by Contributor004 2012-02-04</remarks>
        public static string GoalText
        {
            get { return _goalText; }
            set
            {
                if (!string.IsNullOrEmpty(value) && _goalText != value)
                {
                    Logging.WriteDiagnostic("Goal: {0}", value);
                }
                _goalText = value;
            }
        }
    }
}
