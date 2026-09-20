using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

using Styx.Common;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot
{
    public class BotManager
    {
        #region Ctor & Fields

        public static BotManager Instance = new BotManager();

        private readonly Dictionary<string, BotBase> _bots = new Dictionary<string, BotBase>();

        public static BotBase Current { get; private set; }

        #endregion

        #region Instance Methods

        /// <summary> Returns a dictionary of all the currently loaded bots. </summary>
        public Dictionary<string, BotBase> Bots { get { return GetBots(); } }

        /// <summary> Sets the current bot. Note: Setting the bot will stop the current bot if it's running. </summary>
        /// <param name="bot"></param>
        public void SetCurrent(BotBase bot)
        {
            if (Current != null && TreeRoot.IsRunning)
            {
                TreeRoot.Stop("[BotManager] Current bot is running, stopping so it can be changed.");
            }

            BotBase old = Current;
            Current = bot;

            BotEvents.FireBotChanged(new BotEvents.BotChangedEventArgs(old, bot));
        }

        /// <summary> Gets the routine directory. </summary>
        /// <remarks>Created 2012-06-03</remarks>
        public static string BotsDirectory
        {
            get
            {
                string path = GlobalSettings.Instance.BotsPath;
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(Utilities.AssemblyDirectory, path);
                return path;
            }
        }

        private AssemblyLoader<BotBase> _botLoader;

        /// <summary> Loads all custom bots. </summary>
        public void LoadBots()
        {
            if (!Directory.Exists(BotsDirectory))
                throw new DirectoryNotFoundException("Could not find bots directory! please make sure the path is correct.");

            if (_botLoader == null)
                _botLoader = new AssemblyLoader<BotBase>(BotsDirectory, GlobalSettings.Instance.ReloadBotsOnFileChange);

            _botLoader.Reload("Reloading Bots");
            foreach (BotBase bot in _botLoader.Instances)
            {
                Add(bot.Name, bot);
            }
        }

        /// <summary> Adds a bot to the bot collection. </summary>
        /// <param name="name">name of the bot</param>
        /// <param name="derievesFromBotBase">object that derieves from BotBase</param>
        public void Add(string name, BotBase derievesFromBotBase)
        {
            if (_bots != null && !_bots.ContainsKey(name))
            {
                _bots.Add(name, derievesFromBotBase);
                Logging.Write(StyxResources.NewBotAdded, name);
            }
        }

        /// <summary> Adds a bot to the bot collection. </summary>
        /// <param name="derievesFromBotBase">object that derieves from BotBase</param>
        public void Add(BotBase derievesFromBotBase)
        {
            if (_bots != null && !_bots.ContainsKey(derievesFromBotBase.Name))
            {
                Add(derievesFromBotBase.Name, derievesFromBotBase);
            }
        }

        /// <summary> Removes a bot from the collection. </summary>
        /// <param name="name">name of the bot</param>
        public bool Remove(string name)
        {
            if (Current != null && Current.Name == name)
            {
                TreeRoot.Stop("[BotManager] bot is being removed from the available bots list.");
            }

            if (_bots != null && !_bots.ContainsKey(name))
            {
                Logging.Write(Colors.Red, string.Format(StyxResources.BotIsNotPresent, name));
            }

            return _bots != null && _bots.Remove(name);
        }

        /// <summary> Returns a dictionary of all the currently loaded bots. </summary>
        public Dictionary<string, BotBase> GetBots()
        {
            return _bots;
        }

        /// <summary> Clears the list of bots. </summary>
        private void Clear()
        {
            _bots.Clear();
        }

        #endregion
    }
}
