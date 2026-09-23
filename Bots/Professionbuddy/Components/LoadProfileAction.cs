using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.PropertyGridUtilities;
using Bots.Professionbuddy.PropertyGridUtilities.Editors;
using Styx.CommonBot.Profiles;
using Bots.Professionbuddy.Localization;

namespace Bots.Professionbuddy.Components
{
	[PBXmlElement("LoadProfile", new []{"LoadProfileAction"})]
	public sealed class LoadProfileAction : PBAction
	{
		private volatile bool _loadingProfile;

		public LoadProfileAction()
		{
			Properties["Path"] = new MetaProp(
				"Path",
				typeof (string),
				new EditorAttribute(
					typeof (FileLocationEditor),
					typeof (UITypeEditor)),
				new DisplayNameAttribute(Strings.Action_Common_Path));

			Properties["ProfileType"] = new MetaProp(
				"ProfileType",
				typeof (LoadProfileType),
				new DisplayNameAttribute(Strings.Action_LoadProfileAction_ProfileType));

			Properties["IsLocalOrFromStore"] = new MetaProp(
				"IsLocalOrFromStore",
				typeof (bool),
				new DisplayNameAttribute(Strings.Action_LoadProfileAction_IsLocal));

			Path = "";
			ProfileType = LoadProfileType.Honorbuddy;
			IsLocalOrFromStore = true;
		}

		[PBXmlAttribute]
		public LoadProfileType ProfileType
		{
			get { return Properties.GetValue<LoadProfileType>("ProfileType"); }
			set { Properties["ProfileType"].Value = value; }
		}

		[PBXmlAttribute]
		public string Path
		{
			get { return Properties.GetValue<string>("Path"); }
			set { Properties["Path"].Value = value; }
		}

		[PBXmlAttribute("IsLocalOrFromStore", new[] { "IsLocal" })]
		public bool IsLocalOrFromStore
		{
			get { return Properties.GetValue<bool>("IsLocalOrFromStore"); }
			set { Properties["IsLocalOrFromStore"].Value = value; }
		}

		public override string Name
		{
			get { return Strings.Action_LoadProfileAction_Name; }
		}

		public override string Title
		{
			get { return string.Format("{0}: {1}", Name, Path); }
		}

		public override string Help
		{
			get { return Strings.Action_LoadProfileAction_Help; }
		}

		private bool IsStoreProfile
		{
			get
			{
				return !string.IsNullOrEmpty(ProfileManager.XmlLocation) &&
					ProfileManager.XmlLocation.StartsWith("store://");
			}
		}

		protected async override Task Run()
		{
			if (_loadingProfile)
				return;

			var absPath = GetAbsolutePath();

			bool emptyProfile = string.IsNullOrEmpty(Path);

			if (IsLocalOrFromStore)
			{
				// check if profile is already loaded.
				if (!string.IsNullOrEmpty(ProfileManager.XmlLocation)
					&& ProfileManager.XmlLocation.Equals(absPath, StringComparison.CurrentCultureIgnoreCase))
				{
					IsDone = true;
					return;
				}
				// check if profile exists
				if (!emptyProfile && !IsStoreProfile && !File.Exists(absPath))
				{
					PBLog.Warn("{0}: {1}", Strings.Error_UnableToFindProfile, Path);
					IsDone = true;
					return;
				}
			}

			PBLog.Debug(
				"Loading Profile: {0}, previous profile was {1}",
				emptyProfile ? "(Empty Profile)" : Path,
				ProfileManager.XmlLocation ?? "[No Profile]");

			var path = emptyProfile || !IsLocalOrFromStore ? Path : absPath;

			Util.ExecuteActionWhileBotIsStopped(
				() =>
				{
					try
					{
						if (string.IsNullOrEmpty(path))
						{
							ProfileManager.LoadEmpty();
						}
						else if (!IsLocalOrFromStore)
						{
							var req = WebRequest.Create(path);
							req.Proxy = null;
							using (WebResponse res = req.GetResponse())
							{
								using (var stream = res.GetResponseStream())
								{
									ProfileManager.LoadNew(stream);
								}
							}
						}
						else
						{
							ProfileManager.LoadNew(path);
						}
						IsDone = true;
						PBLog.Debug("Successfully loaded profile:{0}", emptyProfile ? "(Empty Profile)" : Path);
					}
					catch (Exception ex)
					{
						// if ex is not null there was a problem loading profile.
						PBLog.Fatal("Failed to load profile.\n{0}", ex);
					}
				},
				"Loading a new profile");
			_loadingProfile = true;
		}

		public override IPBComponent DeepCopy()
		{
			return new LoadProfileAction {Path = Path, ProfileType = ProfileType, IsLocalOrFromStore = IsLocalOrFromStore};
		}

		public override void Reset()
		{
			_loadingProfile = false;
			base.Reset();
		}

		public string GetAbsolutePath()
		{
			if (!IsLocalOrFromStore)
				return Path;

			if (string.IsNullOrEmpty(ProfessionbuddyBot.Instance.CurrentProfile.XmlPath))
				return string.Empty;

			string dir = System.IO.Path.GetDirectoryName(ProfessionbuddyBot.Instance.CurrentProfile.XmlPath);
			return System.IO.Path.Combine(dir, Path).Replace("store:\\", "store:///").Replace("\\", "/");
		}
	}
}
