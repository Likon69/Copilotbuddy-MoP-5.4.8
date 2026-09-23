using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GreenMagic;
using MahApps.Metro.Controls;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Helpers;
using Styx.Localization;
using Styx.Plugins;
using Styx.WoWInternals;

namespace CopilotBuddy
{
    public partial class MainWindow : MetroWindow
    {
        // Ported from Honorbuddy 5.4.8 Honorbuddy.MainWindow. The Buddy Store / BuddyAuth /
        // Tripwire / auto-updater machinery HB 5.4.8 wraps around this logic (LoginWindow,
        // UpdateWindow, Buddy.Auth.*, Tripwire.Client, ns20/ns36 auth classes) has no
        // equivalent anywhere in this project (no such namespaces exist), so none of it is
        // reproduced here - CopilotBuddy attaches directly, the way HB 5.4.8's own
        // HandleSuccess()/InitializeOnAuthSuccess()/AfterInitOnAuthSuccess() behave once
        // authentication has already succeeded.

        public MainWindow()
        {
            InitializeComponent();

            StyxWoW.InitOffsets();

            SetStaticLabels();

            Logging.OnLogMessage += OnLogMessage;
            StyxLog.StartLogThread();

            ToggleButtons(false);

            if (!GlobalSettings.Initialize())
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }

            Logging.Write("CopilotBuddy v" + Assembly.GetEntryAssembly().GetName().Version + " started.");

            HandleSuccess();
        }

        private void SetStaticLabels()
        {
            // No ResxExtension markup extension exists in this project's Styx.Localization
            // (only Globalization.cs, the generated resource accessor, is present - the WotLK
            // reference project's Styx/Localization/ResxExtension.cs was never ported here).
            // Every one of these keys is confirmed present in Styx/Localization/Globalization.resx,
            // so the real localized string is used - just assigned in code instead of via
            // {loc:Resx ...} markup.
            tbLoadProfileText.Text = Globalization.LoadProfileButtonText;
            tbSettingsText.Text = Globalization.SettingsButtonText;
            tbEnhancedModeText.Text = Globalization.EnhancedMode;
            tbBotConfigText.Text = Globalization.BotConfigButtonText;
            tbClassConfigText.Text = Globalization.ClassConfigButtonText;
            tbPluginsText.Text = Globalization.PluginsButtonText;
            tbDevToolsText.Text = Globalization.DeveloperToolsButtonText;
            tbStartText.Text = Globalization.StartButtonText;
            tbStopText.Text = Globalization.StopButtonText;
            btnPause.Header = Globalization.PauseButtonText;
            tabLog.Header = Globalization.LogTabHeader;
            tabInfo.Header = Globalization.InfoTabHeader;
        }

        #region Attach / HandleSuccess (HB 5.4.8 MainWindow.HandleSuccess)

        private void HandleSuccess()
        {
            bool found64Bits;
            if (FindAndAttachToWoW(out found64Bits) && StyxWoW.Me != null && StyxWoW.Memory != null)
            {
                try
                {
                    LoadSettings();
                    SetFormLocation();
                    HotkeysManager.Initialize(StyxWoW.Memory.Process.MainWindowHandle);

                    Logging.Write("CopilotBuddy v{0} started!", Assembly.GetEntryAssembly().GetName().Version);
                    Logging.WriteDiagnostic("Using WoW with process ID {0}", StyxWoW.Memory.Process.Id);
                    Logging.WriteDiagnostic("Platform: {0}", Environment.OSVersion);
                    Logging.WriteDiagnostic("Executable Path: {0}", Assembly.GetEntryAssembly().Location);
                    Logging.Write("Character is a level {0} {1} {2}", StyxWoW.Me.Level, StyxWoW.Me.Race, StyxWoW.Me.Class);
                    Logging.Write("Current zone is {0}", StyxWoW.Me.RealZoneText);
                    _lastLevel = (int)StyxWoW.Me.Level;

                    Title = string.Format(Globalization.MainWindowTitleFormat,
                        Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location),
                        StyxWoW.Memory.Process.Id);

                    BotEvents.OnBotStarted += BotEvents_OnBotStarted;
                    BotEvents.OnBotStopped += BotEvents_OnBotStopped;
                    BotEvents.OnBotChanged += BotEvents_OnBotChanged;
                    BotEvents.Player.OnLevelUp += args => _lastLevel = args.NewLevel;

                    // Wires WoW-process-exit -> bot stop, and the "no combat routine" start guard.
                    // HB 5.4.8's own TreeRoot has no such method to call; this project's TreeRoot
                    // does (Styx/CommonBot/TreeRoot.cs), and nothing else calls it, so it must be
                    // invoked once after attach for that wiring to exist at all.
                    TreeRoot.Initialize();

                    InitializeOnAuthSuccess();
                }
                catch (Exception ex)
                {
                    Logging.WriteException(ex);
                }
                return;
            }

            MessageBoxResult result = MessageBoxResult.Cancel;
            if (_freeProcesses.Count == 0)
            {
                if (found64Bits)
                {
                    Logging.Write(Colors.OrangeRed, "We couldn't find a proper WoW to attach. There are 64bit WoW processes running and CopilotBuddy only works with the 32bit version.");
                    result = System.Windows.MessageBox.Show(Globalization.StartUpError64Bit, "CopilotBuddy Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                }
                else
                {
                    Logging.Write(Colors.OrangeRed, "We couldn't find a free WoW process to attach. Please make certain your toon is standing in game world and you are running the latest version of CopilotBuddy.");
                    result = System.Windows.MessageBox.Show(Globalization.StartUpErrorNotInGame, "CopilotBuddy Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                }
            }

            if (result == MessageBoxResult.Cancel)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }

            HandleSuccess();
        }

        private void InitializeOnAuthSuccess()
        {
            TreeRoot.StatusText = Globalization.Initializing;

            AddBotsToGui();

            TreeRoot.StatusText = Globalization.Initialization_complete;

            try
            {
                Logging.Write("Initializing Plugins...");
                var enabledPlugins = CharacterSettings.Instance.EnabledPlugins ?? new string[0];
                PluginManager.Initialize(enabledPlugins);
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.Red, "Failed to initialize plugins: {0}", ex.Message);
                Logging.WriteException(ex);
            }

            CharacterSettings.Instance.Initialized = true;
            ToggleButtons(true);
            AfterInitOnAuthSuccess();

            var infoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            infoTimer.Tick += (s, e) => UpdateInfoPanel();
            infoTimer.Start();
            _infoTimer = infoTimer;
        }

        /// <summary>
        /// Mirrors HB 5.4.8 AfterInitOnAuthSuccess(): consumes /loadprofile and /autostart
        /// (autostart here, matching the ORIGINAL HB 4.3.4/5.4.8 command line, not /botname -
        /// HB 5.4.8's own AfterInitOnAuthSuccess only checks AutoLoadProfile and AutoStart).
        /// </summary>
        private void AfterInitOnAuthSuccess()
        {
            TreeRoot.StatusText = Globalization.Honorbuddy_Startup_Complete;
            Logging.Write(Colors.Green, Globalization.Honorbuddy_Startup_Complete);

            string autoLoadProfile = AutoLoadProfile;
            if (!string.IsNullOrEmpty(autoLoadProfile) && File.Exists(autoLoadProfile))
            {
                LoadProfile(autoLoadProfile);
            }

            if (AutoStart)
            {
                StartStop(null);
            }
        }

        private static string AutoLoadProfile
        {
            get
            {
                string text = Environment.GetCommandLineArgs().FirstOrDefault(s => s.StartsWith("/loadprofile="));
                return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("/loadprofile=", string.Empty).Trim();
            }
        }

        private static bool AutoStart
        {
            get { return Environment.GetCommandLineArgs().Any(s => s == "autostart" || s == "/autostart"); }
        }

        #endregion

        #region Bot selector (HB 5.4.8 AddBotsToGui)

        private void AddBotsToGui()
        {
            cmbBotSelector.Items.Clear();
            try
            {
                BotManager.Instance.Add(new Bots.BGBuddy.BGBuddy());
                BotManager.Instance.Add(new Bots.Grind.LevelBot());
                BotManager.Instance.Add(new Bots.Professionbuddy.ProfessionbuddyBot());
                BotManager.Instance.Add(new Bots.Quest.QuestBot());
                BotManager.Instance.LoadBots();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }

            int selectedIndex = -1;
            int i = 0;
            foreach (var bot in BotManager.Instance.Bots.Values.OrderBy(b => b.Name))
            {
                cmbBotSelector.Items.Add(bot);
                if (string.Equals(CharacterSettings.Instance.SelectedBotName, bot.Name, StringComparison.OrdinalIgnoreCase))
                    selectedIndex = i;
                i++;
            }

            if (selectedIndex >= 0)
                cmbBotSelector.SelectedIndex = selectedIndex;
            else if (cmbBotSelector.Items.Count > 0)
                cmbBotSelector.SelectedIndex = 0;
        }

        private BotBase _previousSelection;

        private void cmbBotSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selectedItem = cmbBotSelector.SelectedItem;
            if (_previousSelection != null)
                _previousSelection.OnDeselected();

            var bot = selectedItem as BotBase;
            if (bot != null)
            {
                BotManager.Instance.SetCurrent(bot);
                bot.OnSelected();
                CharacterSettings.Instance.SelectedBotName = bot.Name;
                _previousSelection = bot;
            }
        }

        private void BotEvents_OnBotChanged(BotEvents.BotChangedEventArgs args)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(new Action<BotEvents.BotChangedEventArgs>(BotEvents_OnBotChanged), args);
                return;
            }

            if (!ReferenceEquals(cmbBotSelector.SelectedItem, args.NewBot))
                cmbBotSelector.SelectedItem = args.NewBot;
        }

        #endregion

        #region Start / Stop (HB 5.4.8 StartStop / Start)

        private void StartStop(string stopReason)
        {
            SaveSettings();

            if (StyxWoW.Memory == null || TreeRoot.Current == null)
                return;

            if (TreeRoot.IsRunning)
            {
                Logging.Write("Stopping the bot!");
                TreeRoot.Stop(stopReason);
                return;
            }

            Logging.Write("Starting the bot!");
            Logging.Write("Currently Using BotBase : {0}", BotManager.Current.Name);
            Logging.WriteDiagnostic("Character is a level {0} {1} {2}", StyxWoW.Me.Level, StyxWoW.Me.Race, StyxWoW.Me.Class);
            Logging.WriteDiagnostic("Current zone is {0}", StyxWoW.Me.RealZoneText);
            Start();
        }

        private void Start()
        {
            try
            {
                if (cmbBotSelector.SelectedItem is BotBase selected && TreeRoot.Current != selected)
                    BotManager.Instance.SetCurrent(selected);

                ThreadPool.QueueUserWorkItem(ctx =>
                {
                    Dispatcher.Invoke(() => btnStart.IsEnabled = false);
                    try
                    {
                        TreeRoot.Start();
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteException(ex);
                    }
                    Dispatcher.Invoke(() => btnStart.IsEnabled = true);
                });
            }
            catch (UserException ex)
            {
                Logging.Write(Colors.Red, ex.Message);
                Logging.WriteException(ex);
                TreeRoot.Stop(ex.Message);
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.Red, ex.Message);
                Logging.WriteException(ex);
                TreeRoot.Stop(ex.Message);
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartStop("User pressed the start button");
        }

        private void btnStop_OnClick(object sender, RoutedEventArgs e)
        {
            StartStop("User pressed the stop button");
        }

        private void btnStopArrow_Click(object sender, RoutedEventArgs e)
        {
            if (btnStopArrow.ContextMenu != null)
            {
                btnStopArrow.ContextMenu.PlacementTarget = btnStopArrow;
                btnStopArrow.ContextMenu.IsOpen = true;
            }
        }

        private void btnPause_OnClick(object sender, RoutedEventArgs e)
        {
            if (TreeRoot.IsPaused)
            {
                btnPause.Header = Globalization.PauseButtonText;
                TreeRoot.IsPaused = false;
                return;
            }
            btnPause.Header = Globalization.ResumeButtonText;
            TreeRoot.IsPaused = true;
        }

        private void BotEvents_OnBotStopped(EventArgs args)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<EventArgs>(BotEvents_OnBotStopped), args);
                return;
            }

            btnStart.Visibility = Visibility.Visible;
            btnStopGroup.Visibility = Visibility.Hidden;
            btnPause.Header = Globalization.PauseButtonText;
            btnLoadProfile.IsEnabled = true;
            cmbBotSelector.IsEnabled = true;
            btnSettings.IsEnabled = true;
            btnSettings.ToolTip = null;
            TreeRoot.StatusText = Globalization.Honorbuddy_Stopped;
        }

        private void BotEvents_OnBotStarted(EventArgs args)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new Action<EventArgs>(BotEvents_OnBotStarted), args);
                return;
            }

            btnStart.Visibility = Visibility.Hidden;
            btnStopGroup.Visibility = Visibility.Visible;
            btnLoadProfile.IsEnabled = false;
            cmbBotSelector.IsEnabled = false;
            btnSettings.IsEnabled = false;
            btnSettings.ToolTip = "You can not edit the settings while the bot is running.";
        }

        #endregion

        #region Info panel (HB 5.4.8 UpdateInfoPanel)

        private DispatcherTimer _infoTimer;
        private int _lastLevel;

        private void UpdateInfoPanel()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(Globalization.XP_HR___0_, GameStats.XPPerHour.ToString("F0")));
            sb.AppendLine(string.Format(Globalization.Kills___0____1__hr_, GameStats.MobsKilled, GameStats.MobsPerHour.ToString("F0")));
            sb.AppendLine(string.Format(Globalization.Deaths___0____1__hr_, GameStats.Deaths, GameStats.DeathsPerHour.ToString("F0")));
            sb.AppendLine(string.Format(Globalization.Loots___0____1__hr_, GameStats.Loots, GameStats.LootsPerHour.ToString("F0")));
            sb.AppendLine(string.Format(Globalization.Honor_Gained__0____1__HR_, GameStats.HonorGained, GameStats.HonorPerHour.ToString("F0")));
            sb.AppendLine(string.Format(Globalization.BGInfo, GameStats.BGsWon, GameStats.BGsLost, GameStats.BGsCompleted, GameStats.BGsPerHour.ToString("F0")));

            if (_lastLevel > 0 && _lastLevel < 90)
            {
                sb.AppendLine(string.Format(Globalization.TimeToLevel,
                    ((int)GameStats.TimeToLevel.TotalHours).ToString("F0"),
                    GameStats.TimeToLevel.Minutes.ToString("00"),
                    GameStats.TimeToLevel.Seconds.ToString("00")));
            }

            sb.AppendLine();
            if (!string.IsNullOrEmpty(TreeRoot.GoalText))
                sb.AppendLine(string.Format(Globalization.Goal___0_, TreeRoot.GoalText));

            tbInfoBlock.Text = sb.ToString();
            tbInfoBlock.TextWrapping = System.Windows.TextWrapping.Wrap;
        }

        #endregion

        #region Profile loading (HB 5.4.8 LoadProfileFromFileSystem)

        private void SetGuiForProfileLoad(bool value, bool async)
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                btnStart.IsEnabled = value;
                btnLoadProfile.IsEnabled = value;
                return;
            }
            if (async)
                Dispatcher.BeginInvoke(new Action<bool, bool>(SetGuiForProfileLoad), value, false);
            else
                Dispatcher.Invoke(new Action<bool, bool>(SetGuiForProfileLoad), value, false);
        }

        private void LoadProfile(string path)
        {
            SetGuiForProfileLoad(false, false);
            try
            {
                ProfileManager.LoadNew(path, true);
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.IndianRed, "Cannot load profile!");
                Logging.Write(Colors.IndianRed, ex.Message);
                Logging.WriteException(ex);
            }
            finally
            {
                SetGuiForProfileLoad(true, true);
            }
        }

        private void btnLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            TreeRoot.StatusText = Globalization.Loading_Profile___;
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = "Profile XML|*.xml",
                Title = Globalization.LoadAHonorbuddyProfile
            };

            if (ofd.ShowDialog() == true)
            {
                Task.Run(() => LoadProfile(ofd.FileName));
            }
        }

        #endregion

        #region Settings / Plugins / Class / Bot config windows

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void btnBotConfig_Click(object sender, RoutedEventArgs e)
        {
            if (BotManager.Current == null)
                return;

            try
            {
                var configurationForm = BotManager.Current.ConfigurationForm;
                if (configurationForm != null && !configurationForm.IsDisposed && !configurationForm.Disposing)
                    configurationForm.Show();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        private void btnClassConfig_Click(object sender, RoutedEventArgs e)
        {
            if (RoutineManager.Current == null)
                RoutineManager.SetCurrentCombatRoutine();

            if (RoutineManager.Current != null && RoutineManager.Current.WantButton)
                RoutineManager.Current.OnButtonPress();
        }

        private void btnPlugins_Click(object sender, RoutedEventArgs e)
        {
            var pluginsWindow = new PluginsWindow { Owner = this };
            pluginsWindow.Show();
        }

        /// <summary>
        /// HB 5.4.8 opens DevToolsWindow here. No DeveloperToolsWindow/DevToolsWindow class
        /// exists anywhere in this project, and creating one is out of this task's scope, so
        /// this reports the fact instead of silently doing nothing.
        /// </summary>
        private void btnDevTools_Click(object sender, RoutedEventArgs e)
        {
            Logging.Write(Colors.OrangeRed, "Developer Tools window is not available in this build.");
        }

        private void EnhancedMode_Unchecked(object sender, RoutedEventArgs e)
        {
            UISettings.Instance.EnhancedMode = false;
            btnDevTools.Visibility = Visibility.Hidden;
            btnClassConfig.Visibility = Visibility.Hidden;
            btnPlugins.Visibility = Visibility.Hidden;
            btnBotConfig.Visibility = Visibility.Hidden;
            UISettings.Instance.Save();
        }

        private void EnhancedMode_Checked(object sender, RoutedEventArgs e)
        {
            UISettings.Instance.EnhancedMode = true;
            btnDevTools.Visibility = Visibility.Visible;
            btnClassConfig.Visibility = Visibility.Visible;
            btnPlugins.Visibility = Visibility.Visible;
            btnBotConfig.Visibility = Visibility.Visible;
            UISettings.Instance.Save();
        }

        #endregion

        #region Window geometry (HB 5.4.8 SetFormLocation / SaveLocation)

        private void SetFormLocation()
        {
            if (!StyxWoW.IsInGame)
                return;

            if (UISettings.Instance.MainWindowWidth > 0)
                Width = UISettings.Instance.MainWindowWidth;
            if (UISettings.Instance.MainWindowHeight > 0)
                Height = UISettings.Instance.MainWindowHeight;
            Left = UISettings.Instance.MainWindowLocationX;
            Top = UISettings.Instance.MainWindowLocationY;
            WindowState = UISettings.Instance.MainWindowState;

            if (UISettings.Instance.EnhancedMode)
                EnhancedMode.IsChecked = true;
        }

        private void SaveLocation()
        {
            UISettings.Instance.MainWindowHeight = (int)Height;
            UISettings.Instance.MainWindowWidth = (int)Width;
            UISettings.Instance.MainWindowState = WindowState;
            if (Left == RestoreBounds.Left && Top == RestoreBounds.Top)
            {
                UISettings.Instance.MainWindowLocationX = (int)Left;
                UISettings.Instance.MainWindowLocationY = (int)Top;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // HB 5.4.8 minimizes to a tray NotifyIcon here. This project has no tray-icon
            // resource wired up (HbWasp.ico is HB's own embedded resource); minimizing to the
            // taskbar instead, rather than inventing a tray icon, is the honest behaviour.
        }

        #endregion

        #region Window closing (HB 5.4.8 Window_Closing)

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows.Cast<Window>().ToList())
            {
                if (!ReferenceEquals(window, this))
                    window.Close();
            }

            SaveLocation();
            SaveSettings();

            if (TreeRoot.IsRunning)
            {
                TreeRoot.Stop("Main window is closing");
                if (!TreeRoot.WaitForStop(5000))
                    Logging.Write("Window can't close because bot thread hung");
            }

            foreach (var pluginContainer in PluginManager.Plugins)
                pluginContainer.Enabled = false;

            StyxLog.StopLogThread();
            e.Cancel = false;
        }

        #endregion

        #region Settings load/save (HB 5.4.8 LoadSettings / SaveSettings)

        private void LoadSettings()
        {
            if (StyxWoW.Memory != null && StyxWoW.IsInGame && StyxWoW.Me != null)
            {
                CharacterSettings.Initialize();
                UISettings.Instance.Load();
                CharacterSettings.Instance.Load();
                LevelbotSettings.Instance.Load();
                PVPSettings.Instance.Load();
                BGBotSettings.Instance.Load();
                CombatAssistSettings.Instance.Load();
                return;
            }
            throw new Exception("Unable to load settings due to either not being in game, or me being null.");
        }

        private void SaveSettings()
        {
            try
            {
                UISettings.Instance.Save();
                if (StyxWoW.Memory != null && StyxWoW.IsInGame)
                {
                    CharacterSettings.Instance.Save();
                    GlobalSettings.Instance.Save();
                    LevelbotSettings.Instance.Save();
                    PVPSettings.Instance.Save();
                    BGBotSettings.Instance.Save();
                    CombatAssistSettings.Instance.Save();
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic("Error saving settings.");
                Logging.WriteException(ex);
            }
        }

        #endregion

        #region Toggle / log level (unchanged from the pre-existing stub - already correct)

        private void ToggleButtons(bool on)
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                btnLoadProfile.IsEnabled = on;
                btnStart.IsEnabled = on;
                btnSettings.IsEnabled = on;
                cmbBotSelector.IsEnabled = on;
                btnBotConfig.IsEnabled = on;
                btnClassConfig.IsEnabled = on;
                btnDevTools.IsEnabled = on;
                btnPlugins.IsEnabled = on;
                EnhancedMode.IsEnabled = on;
                return;
            }
            Dispatcher.BeginInvoke(new ToggleDelegate(ToggleButtons), on);
        }

        private delegate void ToggleDelegate(bool on);

        #endregion

        #region Attach to WoW (HB 5.4.8 FindAndAttachToWoW / SelectWoWProcess - unchanged logic)

        private void SelectWoWProcess()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new ThreadStart(SelectWoWProcess));
                return;
            }

            ProcessSelectorWindow processSelectorWindow = new ProcessSelectorWindow();
            if (processSelectorWindow.ShowDialog() == true)
            {
                _selectedWoWProc = processSelectorWindow.Entry.ProcessIdentifier;
                return;
            }

            _selectedWoWProc = 0;
        }

        private bool FindAndAttachToWoW(out bool found64Bits)
        {
            found64Bits = false;
            _selectedWoWProc = null;

            List<Process> processes = Process.GetProcessesByName("Wow").ToList();

            _freeProcesses.Clear();
            foreach (Process process in processes)
            {
                using (ExternalProcessMemory externalProcessMemory = new ExternalProcessMemory(process, false, false, false))
                {
                    bool wasCreated;
                    using (MutexHelper.CreateMutexForProcess(process.Id, out wasCreated))
                    {
                        if (!wasCreated)
                        {
                            continue;
                        }
                    }

                    if (externalProcessMemory.Read<byte>(true, StyxWoW.Offsets.Statics.IsInGame) == 0)
                    {
                        continue;
                    }
                }

                _freeProcesses.Add(process.Id, process.MainModule.FileVersionInfo.FilePrivatePart);
            }

            List<KeyValuePair<int, int>> loggedInProcesses = _freeProcesses.Where(d => d.Value == 18414).ToList();
            if (loggedInProcesses.Count == 1)
            {
                _selectedWoWProc = loggedInProcesses.First().Key;
            }

            if (loggedInProcesses.Count == 0)
            {
                if (Process.GetProcessesByName("Wow-64").Any())
                {
                    found64Bits = true;
                }

                return false;
            }

            if (_selectedWoWProc == null)
            {
                SelectWoWProcess();
                while (_selectedWoWProc == null)
                {
                    Thread.Sleep(100);
                }
            }

            if (_selectedWoWProc == 0)
            {
                return false;
            }

            try
            {
                using (ExternalProcessMemory externalProcessMemory = new ExternalProcessMemory(Process.GetProcessById(_selectedWoWProc.Value), false, false, false))
                {
                    GraphicsApi graphicsApi = StyxWoW.ReadGraphicsApi(externalProcessMemory, false);

                    if (externalProcessMemory.Read<byte>(true, StyxWoW.Offsets.Statics.IsInGame) == 0)
                    {
                        Logging.Write("Error attaching CopilotBuddy to WoW! - Process ID {0} is not logged in game!", _selectedWoWProc.Value);
                        return false;
                    }

                    if (graphicsApi != GraphicsApi.D3D9 && graphicsApi != GraphicsApi.D3D11)
                    {
                        Logging.Write("Error attaching CopilotBuddy to WoW! - Process ID {0} is not using DX9 or DX11!", _selectedWoWProc.Value);
                        return false;
                    }

                    StyxWoW.Initialize(externalProcessMemory.Process, graphicsApi);
                    Logging.Write("Attached to WoW with ID {0}", _selectedWoWProc.Value);

                    bool wasCreated;
                    _mut = MutexHelper.CreateMutexForProcess(_selectedWoWProc.Value, out wasCreated);
                }
            }
            catch (Exception ex)
            {
                Logging.Write("Error attaching CopilotBuddy to WoW! - {0}", ex);
                return false;
            }

            return true;
        }

        private LogLevel LogLevel
        {
            get { return GlobalSettings.Instance != null ? GlobalSettings.Instance.LogLevel : Logging.LoggingLevel; }
        }

        private void OnLogMessage(ReadOnlyCollection<Logging.LogMessage> messages)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
            {
                Dispatcher.BeginInvoke(new Logging.LogMessageDelegate(OnLogMessage), messages);
                return;
            }

            if (_para == null)
            {
                rtbLog.Document.Blocks.Clear();
                _para = new Paragraph();
                rtbLog.Document.Blocks.Add(_para);
            }

            if (_font == null)
            {
                _font = new FontFamily("Consolas");
            }

            if (_logCount >= 1000)
            {
                _para.Inlines.Clear();
                _logCount = 0;
            }

            Color color = messages[0].Color;
            StringBuilder stringBuilder = new StringBuilder();
            Action<StringBuilder, Color> appendRun = delegate(StringBuilder sb, Color col)
            {
                SolidColorBrush solidColorBrush = new SolidColorBrush(col);
                solidColorBrush.Freeze();
                _para.Inlines.Add(new Run(sb.ToString())
                {
                    Foreground = solidColorBrush,
                    FontFamily = _font
                });
            };

            foreach (Logging.LogMessage logMessage in messages)
            {
                if (logMessage.Level <= LogLevel)
                {
                    if (logMessage.Color != color)
                    {
                        appendRun(stringBuilder, color);
                        stringBuilder.Clear();
                        color = logMessage.Color;
                    }

                    stringBuilder.AppendLine(logMessage.Message);
                }
            }

            appendRun(stringBuilder, color);
            _logCount += messages.Count;

            if (scrollLog.ScrollableHeight == scrollLog.ContentVerticalOffset)
            {
                scrollLog.ScrollToEnd();
            }
        }

        private int? _selectedWoWProc;

        private readonly Dictionary<int, int> _freeProcesses = new Dictionary<int, int>();

        private Mutex _mut;

        private int _logCount;

        private Paragraph _para;

        private FontFamily _font;

        #endregion
    }
}
