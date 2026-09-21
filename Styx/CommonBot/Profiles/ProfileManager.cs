using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;

namespace Styx.CommonBot.Profiles
{
    public static class ProfileManager
    {
        private static Profile _currentOuterProfile;
        private static Profile _currentProfile;

        private static readonly Profile EmptyProfile;

        static ProfileManager()
        {
            BotEvents.Player.OnLevelUp += Player_LevelUp;
            BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
            XmlLocation = string.Empty;

            EmptyProfile = new Profile
            {
                SubProfiles = new List<Profile>(new[]
                {
                    new Profile
                    {
                        Name = "Empty Profile",
                        MinLevel = 0,
                        MaxLevel = 999999
                    }
                })
            };
        }

        private static void Profile_OnNewProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            Profile newProfile = args.NewProfile;
            if (args.NewProfile != null)
            {
                string msg = string.Format(StyxResources.Changing_current_profile_to__0_, !string.IsNullOrEmpty(newProfile.Name) ? newProfile.Name : string.Format("level {0} - {1}", newProfile.MinLevel, newProfile.MaxLevel));
                Logging.Write(msg);
                return;
            }
            Logging.Write(StyxResources.We_either_outleveled_the_currently_loaded_profile__or_something_has_gone_terribly_wrong__Can_t_find_a_new__sub_profile_to_use_);
        }

        public static string XmlLocation { get; private set; }

        public static Profile CurrentOuterProfile
        {
            get
            {
                return _currentOuterProfile;
            }
            private set
            {
                Profile profile = _currentOuterProfile;
                _currentOuterProfile = value;
                if (profile != _currentOuterProfile && _currentOuterProfile != null)
                {
                    BotEvents.Profile.FireNewOuterProfileLoaded(profile, _currentOuterProfile);
                }
            }
        }

        public static Profile CurrentProfile
        {
            get
            {
                if (_currentProfile == null)
                {
                    RefreshBestProfile();
                }
                return _currentProfile;
            }
            set
            {
                Profile profile = _currentProfile;
                _currentProfile = value;
                if (profile != _currentProfile && _currentProfile != null)
                {
                    BotEvents.Profile.FireNewProfileLoaded(profile, _currentProfile);
                }
            }
        }

        private static void Player_LevelUp(BotEvents.Player.LevelUpEventArgs args)
        {
            if (!Battlegrounds.IsInsideBattleground)
            {
                Logging.Write(StyxResources.We_leveled_up__Checking_if_we_need_to_switch_profiles_);
                RefreshBestProfile();
            }
        }

        public static void RefreshBestProfile()
        {
            CurrentProfile = GetBestProfile(StyxWoW.Me.Level);
        }

        private static Profile GetBestProfile(int level)
        {
            if (CurrentOuterProfile == null)
            {
                return null;
            }
            List<Profile> scopeSortedProfiles = CurrentOuterProfile.GetScopeSortedProfiles();
            for (int i = 0; i < scopeSortedProfiles.Count; i++)
            {
                Profile profile = scopeSortedProfiles[i];
                if (profile.ContinentId == -1 || profile.ContinentId == (int)StyxWoW.Me.MapId)
                {
                    if (!BotManager.Current.RequiresProfileScope)
                    {
                        return scopeSortedProfiles[i];
                    }
                    if (level >= profile.MinLevel && level < profile.MaxLevel)
                    {
                        return scopeSortedProfiles[i];
                    }
                }
            }
            return null;
        }

        public static void LoadNew(Stream stream)
        {
            StreamReader streamReader = new StreamReader(stream);
            XElement xelement = XElement.Parse(streamReader.ReadToEnd());
            XmlLocation = null;
            StyxWoW.AreaManager.SetArea(null);
            MeshNavigator meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator != null)
            {
                meshNavigator.UpdateMaps();
            }
            CurrentOuterProfile = new Profile(xelement, null);
            RefreshBestProfile();
        }

        public static void LoadNew(string path, bool rememberMe = true)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            Logging.WriteDiagnostic("Loading profile from " + path);
            Profile profile;
            if (!File.Exists(path))
            {
                throw new ProfileNotFoundException(path);
            }
            profile = new Profile(path, null);
            XmlLocation = path;
            if (rememberMe)
            {
                CharacterSettings.Instance.LastUsedPath = path;
            }
            StyxWoW.AreaManager.SetArea(null);
            MeshNavigator meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator != null)
            {
                meshNavigator.UpdateMaps();
            }
            CurrentOuterProfile = profile;
            RefreshBestProfile();
            if (CharacterSettings.Instance.RecentProfiles != null)
            {
                IEnumerable<string> recentProfiles = CharacterSettings.Instance.RecentProfiles;
                string text = recentProfiles.FirstOrDefault(rp => rp.Contains(path));
                if (text != null)
                {
                    List<string> list = CharacterSettings.Instance.RecentProfiles.ToList();
                    list.Remove(text);
                    CharacterSettings.Instance.RecentProfiles = list.ToArray();
                }
            }
            else
            {
                CharacterSettings.Instance.RecentProfiles = new string[0];
            }
            List<string> list2 = CharacterSettings.Instance.RecentProfiles.ToList();
            string text2 = !string.IsNullOrWhiteSpace(CurrentOuterProfile.Name) ? CurrentOuterProfile.Name : Path.GetFileNameWithoutExtension(path);
            list2.Insert(0, text2 + "@@!@@" + path);
            if (list2.Count > 5)
            {
                list2 = list2.Take(5).ToList();
            }
            CharacterSettings.Instance.RecentProfiles = list2.ToArray();
            CharacterSettings.Instance.Save();
        }

        public static void LoadEmpty()
        {
            StyxWoW.AreaManager.SetArea(null);
            MeshNavigator meshNavigator = Navigator.NavigationProvider as MeshNavigator;
            if (meshNavigator != null)
            {
                meshNavigator.UpdateMaps();
            }
            CurrentOuterProfile = EmptyProfile;
            RefreshBestProfile();
        }
    }
}
