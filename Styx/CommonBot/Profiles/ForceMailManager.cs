using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class ForceMailManager
    {
        private static readonly DualHashSet<uint, string> _forceMail = new DualHashSet<uint, string>();
        /// <summary>
        /// Add and remove protected items from/to here at runtime.
        /// </summary>
        private static readonly DualHashSet<uint, string> _dynamicForceMailItems = new DualHashSet<uint, string>();


        private static readonly HashSet<string> _forceMailFileNames = new HashSet<string>
                                                 			{
                                                                "forcemail",
                                                                "forcedmail",
                                                 				"force mail",
                                                                "forced mail"
                                                 			};

        static ForceMailManager()
        {
            ReloadProtectedItems();
        }

        public static void ReloadProtectedItems()
        {
            _forceMail.HashSet1.Clear();
            _forceMail.HashSet2.Clear();
            string searchPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] candidates = Directory.GetFiles(searchPath, "*.xml", SearchOption.AllDirectories);

            foreach (string file in candidates)
            {
                if (!_forceMailFileNames.Contains(Path.GetFileNameWithoutExtension(file).ToLowerInvariant()))
                    continue;

                ParseFileForItems(file);
            }
        }

        private static void ParseFileForItems(string filePath)
        {
            if (Path.GetExtension(filePath).ToLower() != ".xml" || !File.Exists(filePath))
                return;

            XElement xml;
            try
            {
                xml = XElement.Load(filePath);
            }
            catch (XmlException ex)
            {
                Logging.Write(StyxResources.Error_in_ForceMail_XML_file_at__0____1_, filePath, ex.Message);
                return;
            }

            XElement[] elements = xml.Elements().ToArray();
            foreach (XElement item in elements)
            {
                if (item.Name.ToString().ToLowerInvariant() != "item")
                    continue;

                uint iEntry = 0;
                string iName = "";
                XAttribute[] attributes = item.Attributes().ToArray();
                foreach (XAttribute attrib in attributes)
                {
                    string lowerName = attrib.Name.ToString().ToLowerInvariant();
                    switch (lowerName)
                    {
                        case "id":
                        case "entry":
                            if (!uint.TryParse(attrib.Value, out iEntry))
                                goto forcecontinue;
                            break;
                        case "name":
                            iName = attrib.Value.ToLowerInvariant();
                            break;
                    }
                }

                if (iEntry == 0 && !string.IsNullOrEmpty(item.Value))
                    uint.TryParse(item.Value, out iEntry);

                if (iEntry == 0 && iName == "")
                    continue;

                if (!_forceMail.Contains(iEntry))
                {
                    _forceMail.Add(iEntry);
                }

                if (!string.IsNullOrEmpty(iName))
                {
                    if (!_forceMail.Contains(iName))
                        _forceMail.Add(iName);
                }

            forcecontinue:
                ;
            }
        }

        public static bool Contains(uint item)
        {
            return _forceMail.Contains(item) || _dynamicForceMailItems.Contains(item) || ProfileContains(item);
        }

        public static bool Contains(string item)
        {
            item = item.ToLowerInvariant();
            return _forceMail.Contains(item) || _dynamicForceMailItems.Contains(item) || ProfileContains(item);
        }

        private static bool ProfileContains(uint item)
        {
            return ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ForceMail.Contains(item);
        }

        private static bool ProfileContains(string item)
        {
            return ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ForceMail.Contains(item.ToLowerInvariant());
        }

        public static bool Add(uint item)
        {
            return _dynamicForceMailItems.Add(item);
        }

        public static bool Add(string item)
        {
            return _dynamicForceMailItems.Add(item.ToLowerInvariant());
        }

        public static bool Remove(uint item)
        {
            return _dynamicForceMailItems.Remove(item);
        }

        public static bool Remove(string item)
        {
            return _dynamicForceMailItems.Remove(item.ToLowerInvariant());
        }

        public static List<string> GetAllItemNames()
        {
            var items = _forceMail.HashSet2.ToList();
            items.AddRange(_dynamicForceMailItems.HashSet2);

            if (ProfileManager.CurrentProfile != null)
                items.AddRange(ProfileManager.CurrentProfile.ForceMail.HashSet2);

            return items;
        }

        public static List<uint> GetAllItemIds()
        {
            var items = _forceMail.HashSet1.ToList();
            items.AddRange(_dynamicForceMailItems.HashSet1);

            if (ProfileManager.CurrentProfile != null)
                items.AddRange(ProfileManager.CurrentProfile.ForceMail.HashSet1);

            return items;
        }
    }
}
