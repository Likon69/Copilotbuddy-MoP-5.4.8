using System;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.TreeSharp;

namespace Styx.CommonBot
{
    public abstract class BotBase
    {
        public abstract string Name { get; }
        public abstract Composite Root { get; }
        public abstract PulseFlags PulseFlags { get; }
        public virtual Form ConfigurationForm { get { return null; } }

        public virtual void OnSelected()
        {
            Chat.Whisper += Chat_Whisper;
        }

        public virtual void OnDeselected()
        {
            Chat.Whisper -= Chat_Whisper;
        }

        /// <summary>Gets a value indicating whether this object is primary bot type. These will be used by default in mixed-mode.</summary>
        /// <value>true if this object is primary type, false if not.</value>
        public virtual bool IsPrimaryType { get { return true; } }

        /// <summary>Gets a value indicating whether the requirements met for this bot to be run. Secondary type bots MUST implement this
        /// 		 member, for the bot to be used when needed.</summary>
        /// <value>true if requirements met, false if not.</value>
        public virtual bool RequirementsMet { get { return false; } }

        protected JObject StatusObject
        {
            get
            {
                dynamic obj = new JObject();
                obj.BotType = 1;
                obj.IsRunning = TreeRoot.IsRunning;
                obj.Character = StyxWoW.Me.Name.Substring(0, StyxWoW.Me.Name.Length - 2) + "**";
                obj.Realm = StyxWoW.Me.RealmName;
                obj.Level = StyxWoW.Me.Level;
                obj.HealthPercentage = StyxWoW.Me.HealthPercent;
                obj.Zone = StyxWoW.Me.RealZoneText;
                obj.Poi = BotPoi.Current.ToString();
                obj.LastWhisper = _lastWhisper == string.Empty ? "None" : _lastWhisper;
                return obj;
            }
        }

        public virtual void AddMonitorMembers()
        {
        }

        private string _lastWhisper = string.Empty;

        private void Chat_Whisper(Chat.ChatWhisperEventArgs args)
        {
            _lastWhisper = args.Message;
        }

        public string StatusText
        {
            get
            {
                AddMonitorMembers();
                return JsonConvert.SerializeObject(StatusObject);
            }
        }

        public virtual bool RequiresProfileScope { get { return false; } }

        public virtual bool RequiresProfile { get { return false; } }

        public virtual void Pulse() { }

        public virtual void Initialize() { }

        public virtual void OnPaused() { }

        public virtual void OnResumed()
        {
            INavigationProvider navigationProvider = Navigator.NavigationProvider;
            if (navigationProvider == null)
            {
                return;
            }

            IStuckHandler stuckHandler = navigationProvider.StuckHandler;
            if (stuckHandler == null)
            {
                return;
            }

            stuckHandler.Reset();
        }

        public bool Initialized { get; private set; }
        public void DoInitialize()
        {
            if (!Initialized)
            {
                Initialize();
                Initialized = true;
            }
        }

        public virtual void Start() { }

        public virtual void Stop() { }

        public override string ToString()
        {
            try
            {
                return Name;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
