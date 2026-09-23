using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace Bots.DungeonBuddy.Helpers
{
    internal static class Updater
    {
        private static readonly WebClient Client = new WebClient();

        internal static bool UpdateComplete { get; private set; }

        public static void Update()
        {
            if (!UpdateComplete)
            {
                Logger.Write("Checking for profile updates.");
                UpdateProfiles();
                Logger.Write("Checking for script updates.");
                UpdateScripts();
                Logger.Write("All files are up-to-date.");
                UpdateComplete = true;
            }
        }

        private static void UpdateProfiles()
        {
            UpdateFiles("http://www.buddyauth.com/dungeonbuddy/profiles/", "Default Profiles\\DungeonBuddy");
        }

        private static void UpdateScripts()
        {
            UpdateFiles("http://www.buddyauth.com/dungeonbuddy/scripts/", "Dungeon Scripts");
        }

        private static void UpdateFiles(string webPath, string localPath)
        {
        }

        private static string ComputeHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "";
            }

            using (FileStream inputStream = File.OpenRead(filePath))
            {
                byte[] hash = new MD5CryptoServiceProvider().ComputeHash(inputStream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
    }
}
