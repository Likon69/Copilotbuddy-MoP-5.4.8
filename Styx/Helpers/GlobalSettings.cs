using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Xml;
using Styx.Common;
using Styx.Localization;

namespace Styx.Helpers
{
    public class GlobalSettings : Settings, INotifyPropertyChanged
    {
        public static GlobalSettings Instance { get; private set; }

        public GlobalSettings()
            : base(GlobalSettingsPath, null)
        {
        }

        private static string GlobalSettingsPath
        {
            get { return Path.Combine(Utilities.AssemblyDirectory, "Settings\\GlobalSettings.xml"); }
        }

        private static bool TryDeleteSettingsFile()
        {
            while (true)
            {
                try
                {
                    File.Delete(GlobalSettingsPath);
                    return true;
                }
                catch (IOException)
                {
                    if (MessageBox.Show(Globalization.SettingsFileInUse, Globalization.Error, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                    {
                        return false;
                    }
                }
            }
        }

        internal static bool Initialize()
        {
            try
            {
                Instance = new GlobalSettings();
                return true;
            }
            catch (XmlException)
            {
                Logging.WriteToFileSync(LogLevel.Normal, "Settings file corrupted - asking user to create a new one.");
                if (MessageBox.Show(Globalization.GlobalSettingsFileCorrupted, Globalization.Warning, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                {
                    return false;
                }

                Logging.WriteToFileSync(LogLevel.Normal, "Deleting settings file");
                if (!TryDeleteSettingsFile())
                {
                    return false;
                }

                Instance = new GlobalSettings();
                return true;
            }
        }

        private string _uiCulture;
        [Setting]
        [DefaultValue("en-US")]
        public string UICulture
        {
            get { return _uiCulture; }
            set
            {
                _uiCulture = value;
                Changed("UICulture");
            }
        }

        private bool _useFrameLock;
        [Setting]
        [DefaultValue(true)]
        public bool UseFrameLock
        {
            get { return _useFrameLock; }
            set
            {
                _useFrameLock = value;
                Changed("UseFrameLock");
            }
        }

        private bool? _sendUsageInformation;
        [Setting]
        public bool? SendUsageInformation
        {
            get { return _sendUsageInformation; }
            set
            {
                _sendUsageInformation = value;
                Changed("SendUsageInformation");
            }
        }

        [DefaultValue("Routines")]
        [Setting]
        public string CombatRoutinesPath { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ReloadRoutinesOnFileChange { get; set; }

        [DefaultValue("Plugins")]
        [Setting]
        public string PluginsPath { get; set; }

        [DefaultValue(false)]
        [Setting]
        public bool ReloadPluginsOnFileChange { get; set; }

        [Setting]
        [DefaultValue("Bots")]
        public string BotsPath { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ReloadBotsOnFileChange { get; set; }

        [DefaultValue("Quest Behaviors")]
        [Setting]
        public string QuestBehaviorsPath { get; set; }

        [Setting]
        [DefaultValue("")]
        [Encrypted("AAECAwQFBgcICQoLDA0ODw==", "AAECAwQFBgcICQoLDA0ODw==")]
        internal string Username { get; set; }

        private string _meshesFolderPath;
        [Setting]
        [DefaultValue("")]
        public string MeshesFolderPath
        {
            get { return _meshesFolderPath; }
            set
            {
                _meshesFolderPath = value;
                Changed("MeshesFolderPath");
            }
        }

        private bool _killBetweenHotspots;
        [Setting]
        [DefaultValue(true)]
        public bool KillBetweenHotspots
        {
            get { return _killBetweenHotspots; }
            set
            {
                _killBetweenHotspots = value;
                Changed("KillBetweenHotspots");
            }
        }

        private bool _advancedSettingsMode;
        [Setting]
        [DefaultValue(true)]
        public bool AdvancedSettingsMode
        {
            get { return _advancedSettingsMode; }
            set
            {
                _advancedSettingsMode = value;
                Changed("AdvancedSettingsMode");
            }
        }

        private bool _logoutForInactivity;
        [DefaultValue(true)]
        [Setting(Explanation = "Whether or not we should log out after the bot has detected inactivity.")]
        public bool LogoutForInactivity
        {
            get { return _logoutForInactivity; }
            set
            {
                _logoutForInactivity = value;
                Changed("LogoutForInactivity");
            }
        }

        [Setting(Explanation = "Logs out after X minutes of inactivity.")]
        [DefaultValue(20)]
        public int LogoutInactivityTimer { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool LogoutInactivityUseForceQuit { get; set; }

        private bool _profileDebuggingMode;
        [DefaultValue(false)]
        [Setting]
        public bool ProfileDebuggingMode
        {
            get { return _profileDebuggingMode; }
            set
            {
                _profileDebuggingMode = value;
                Changed("ProfileDebuggingMode");
            }
        }

        private LogLevel _logLevel;
        [Setting]
        [DefaultValue(LogLevel.Normal)]
        public LogLevel LogLevel
        {
            get { return _logLevel; }
            set
            {
                _logLevel = value;
                Changed("LogLevel");
            }
        }

        private bool _seperatedLogFolders;
        [Setting]
        [DefaultValue(true)]
        public bool SeperatedLogFolders
        {
            get { return _seperatedLogFolders; }
            set
            {
                if (_seperatedLogFolders == value)
                {
                    return;
                }

                _seperatedLogFolders = value;
                if (!StyxWoW.WowExited)
                {
                    StyxWoW.SetSeparatedLogFolders(value);
                }

                Changed("SeperatedLogFolders");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
