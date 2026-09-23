using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles.Handlers;
using Styx;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Pathing;

namespace Bots.DungeonBuddy.Profiles;

public static class ProfileManager
{
	private static readonly string ProfilePath = Path.Combine(Utilities.AssemblyDirectory, "Default Profiles\\DungeonBuddy\\");

	public static Profile CurrentProfile { get; private set; }

	public static event EventHandler<EventArgs> OnProfileSet;

	public static void Load(Profile profile)
	{
		// unload blackspots from previous profile
		if (CurrentProfile != null)
			Unload();

		CurrentProfile = profile;
		// load new blackspots
		if (CurrentProfile != null && CurrentProfile.Blackspots != null)
		{
			List<Styx.CommonBot.Profiles.Blackspot> blackspots = CurrentProfile.Blackspots;
			Styx.CommonBot.Profiles.ProfileManager.CurrentProfile.Blackspots.AddRange(blackspots);
			BlackspotManager.AddBlackspots(blackspots);
		}

		BossManager.Attach(profile);

		OnProfileSet?.Invoke(null, EventArgs.Empty);
	}

	public static void Unload()
	{
		if (CurrentProfile != null && CurrentProfile.Blackspots != null)
		{
			List<Styx.CommonBot.Profiles.Blackspot> blackspots = CurrentProfile.Blackspots;
			BlackspotManager.RemoveBlackspots(blackspots);
			Styx.CommonBot.Profiles.ProfileManager.CurrentProfile.Blackspots.RemoveAll(p => true);
		}
		BossManager.Detach();
		CurrentProfile = null;
	}

	/// <summary>
	///   Loads the Dungeon profile designed for mapId
	/// </summary>
	/// <param name="dungeonId"> </param>
	public static void LoadProfileForDungeon(uint dungeonId)
	{
		Profile profile = null;
		// See if profile is found in custom profile and scripts path.
		if (!string.IsNullOrWhiteSpace(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath) && Directory.Exists(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath))
		{
			profile = GetProfileForDungeon(DungeonBuddySettings.Instance.CustomProfileAndScriptsPath, dungeonId);
		}
		bool usingCustomProfile = profile != null;
		// Load default profile
		if (profile == null)
		{
			profile = GetProfileForDungeon(ProfilePath, dungeonId);
		}
		// load
		if (profile != null)
		{
			Load(profile);
			Logger.Write("Successfully loaded {0} profile for {1}", usingCustomProfile ? "custom" : "default", profile.Name.Trim('\n', ' ', '\r'));
		}
		else
		{
			Logger.Write(Colors.Red, "No profile found for dungeonId: {0}", dungeonId);
		}
	}

	private static Profile GetProfileForDungeon(string path, uint dungeonId)
	{
		var files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
		foreach (var file in files)
		{
			try
			{
				XElement xmlFile = XElement.Load(file);
				var dungeonIdElement = xmlFile.Element("DungeonId");

				if (dungeonIdElement != null && int.Parse(dungeonIdElement.Value) == dungeonId)
				{
					ErrorCollection errors;

					Profile profile;
					try
					{
						profile = Profile.Load(file, out errors);
					}
					catch (XmlException ex)
					{
						Logger.Write(Colors.Red, "There was an XML error in your profile!");
						Logger.Write(Colors.Red, ex.Message);
						return null;
					}

					foreach (Error error in errors)
						Logger.WriteError(error);

					if (!errors.HasErrors)
					{
						return profile;
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Write(Colors.Red, "{0}\n{1}", file, ex);
			}
		}
		return null;
	}

	internal static bool IsPointInPullBlackspot(WoWPoint point)
	{
		if (CurrentProfile == null || CurrentProfile.PullBlackspots == null)
			return false;
		return (from pullBlackspot in CurrentProfile.PullBlackspots
				let radiusSqr = pullBlackspot.Radius * pullBlackspot.Radius
				where
					point.DistanceSqr(pullBlackspot.Location) < radiusSqr &&
					(pullBlackspot.Height == 0.0 || (pullBlackspot.Height > 0f && pullBlackspot.Z + pullBlackspot.Height < point.Z && point.Z >= pullBlackspot.Z))
				select pullBlackspot).Any();
	}
}
