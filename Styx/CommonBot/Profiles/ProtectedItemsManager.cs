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
	public static class ProtectedItemsManager
	{
		private static readonly DualHashSet<uint, string> _protectedItems = new DualHashSet<uint, string>();
		/// <summary>
		/// Add and remove protected items from/to here at runtime.
		/// </summary>
		private static readonly DualHashSet<uint, string> _dynamicProtectedItems = new DualHashSet<uint, string>();


		private static readonly HashSet<string> _protectedItemsFileNames = new HashSet<string>
                                                 			{
                                                                "protected items",
                                                 				"protecteditems",
                                                 				"protitems",
                                                 			};

		static ProtectedItemsManager()
		{
			ReloadProtectedItems();
		}

		public static void ReloadProtectedItems()
		{
			_protectedItems.HashSet1.Clear();
			_protectedItems.HashSet2.Clear();
			string searchPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string[] candidates = Directory.GetFiles(searchPath, "*.xml", SearchOption.AllDirectories);

			foreach (string file in candidates)
			{
				if (!_protectedItemsFileNames.Contains(Path.GetFileNameWithoutExtension(file).ToLowerInvariant()))
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
				Logging.Write(StyxResources.Error_in_ProtectedItems_XML_file_at__0____1_, filePath, ex.Message);
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

				if (!_protectedItems.Contains(iEntry))
					_protectedItems.Add(iEntry);

				if (!_protectedItems.Contains(iName))
					_protectedItems.Add(iName);

			forcecontinue:
				;
			}
		}

		public static bool Contains(uint item)
		{
			return _protectedItems.Contains(item) || _dynamicProtectedItems.Contains(item) || ProfileContains(item);
		}

		public static bool Contains(string item)
		{
			item = item.ToLowerInvariant();
			return _protectedItems.Contains(item) || _dynamicProtectedItems.Contains(item) || ProfileContains(item);
		}

		private static bool ProfileContains(uint item)
		{
			return ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ProtectedItems.Contains(item);
		}

		private static bool ProfileContains(string item)
		{
			return ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ProtectedItems.Contains(item.ToLowerInvariant());
		}

		public static bool Add(uint item)
		{
			return _dynamicProtectedItems.Add(item);
		}

		public static bool Add(string item)
		{
			return _dynamicProtectedItems.Add(item.ToLowerInvariant());
		}

		public static bool Remove(uint item)
		{
			return _dynamicProtectedItems.Remove(item);
		}

		public static bool Remove(string item)
		{
			return _dynamicProtectedItems.Remove(item.ToLowerInvariant());
		}

		public static List<string> GetAllItemNames()
		{
			var items = new List<string>();
			foreach (string s in _protectedItems.HashSet2)
				items.Add(s);

			foreach (string s in _dynamicProtectedItems.HashSet2)
				items.Add(s);

			if (ProfileManager.CurrentProfile != null)
				foreach (string s in ProfileManager.CurrentProfile.ProtectedItems.HashSet2)
					items.Add(s);

			return items;
		}

		public static List<uint> GetAllItemIds()
		{
			var items = new List<uint>();
			foreach (uint id in _protectedItems.HashSet1)
				items.Add(id);

			foreach (uint id in _dynamicProtectedItems.HashSet1)
				items.Add(id);

			if (ProfileManager.CurrentProfile != null)
				foreach (uint id in ProfileManager.CurrentProfile.ProtectedItems.HashSet1)
					items.Add(id);

			return items;
		}

	}
}
