using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Styx.Common;
using Styx.Helpers;
using Styx.Localization;

namespace Styx.CommonBot.CharacterManagement
{
    public static class CharacterManager
    {
        private static bool _initialized;
        private static readonly List<ClassProfile> _classProfiles = new List<ClassProfile>();

        internal static void Pulse()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("CharacterManager has not been inited and cannot be pulsed");
            }

            if (StyxWoW.IsInGame && StyxWoW.Me != null)
            {
                AutoEquip.Pulse();
                Talented.Pulse();
            }
        }

        private static ClassProfile SelectDefaultClassProfile()
        {
            WoWClass wowClass = StyxWoW.Me.Class;
            if (!string.IsNullOrEmpty(CharacterSettings.Instance.ClassProfileName))
            {
                ClassProfile classProfile = _classProfiles.FirstOrDefault(cp => cp.Name == CharacterSettings.Instance.ClassProfileName);
                if (classProfile != null && classProfile.Class == wowClass)
                {
                    return classProfile;
                }
            }

            WoWSpec wowSpec = StyxWoW.Me.Specialization;
            if (wowSpec != WoWSpec.None)
            {
                ClassProfile classProfile2 = _classProfiles.FirstOrDefault(cp => cp.GetSpec() == wowSpec);
                if (classProfile2 != null)
                {
                    return classProfile2;
                }
            }

            var defaultSpecsByClass = new Dictionary<WoWClass, WoWSpec[]>
            {
                { WoWClass.Warrior, new[] { WoWSpec.WarriorProtection, WoWSpec.WarriorArms, WoWSpec.WarriorFury } },
                { WoWClass.Paladin, new[] { WoWSpec.PaladinRetribution, WoWSpec.PaladinProtection, WoWSpec.PaladinHoly } },
                { WoWClass.Hunter, new[] { WoWSpec.HunterBeastMastery, WoWSpec.HunterSurvival, WoWSpec.HunterMarksmanship } },
                { WoWClass.Rogue, new[] { WoWSpec.RogueCombat, WoWSpec.RogueAssassination, WoWSpec.RogueSubtlety } },
                { WoWClass.Priest, new[] { WoWSpec.PriestShadow, WoWSpec.PriestDiscipline, WoWSpec.PriestHoly } },
                { WoWClass.DeathKnight, new[] { WoWSpec.DeathKnightBlood, WoWSpec.DeathKnightUnholy, WoWSpec.DeathKnightFrost } },
                { WoWClass.Shaman, new[] { WoWSpec.ShamanEnhancement, WoWSpec.ShamanElemental, WoWSpec.ShamanRestoration } },
                { WoWClass.Mage, new[] { WoWSpec.MageFrost, WoWSpec.MageFire, WoWSpec.MageArcane } },
                { WoWClass.Warlock, new[] { WoWSpec.WarlockDemonology, WoWSpec.WarlockDestruction, WoWSpec.WarlockAffliction } },
                { WoWClass.Druid, new[] { WoWSpec.DruidFeral, WoWSpec.DruidBalance, WoWSpec.DruidGuardian, WoWSpec.DruidRestoration } },
                { WoWClass.Monk, new[] { WoWSpec.MonkWindwalker, WoWSpec.MonkBrewmaster, WoWSpec.MonkMistweaver } }
            };

            WoWSpec[] specs;
            if (!defaultSpecsByClass.TryGetValue(wowClass, out specs))
            {
                Logging.WriteDiagnostic(Colors.Red, "Cannot get default class specs for class {0}", wowClass);
                return _classProfiles.FirstOrDefault(cp => cp.Class == wowClass);
            }

            foreach (WoWSpec spec in specs)
            {
                ClassProfile classProfile3 = _classProfiles.FirstOrDefault(cp => cp.GetSpec() == spec);
                if (classProfile3 != null)
                {
                    return classProfile3;
                }
            }

            return null;
        }

        internal static bool Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException("CharacterManager has already been inited, and cannot be inited again");
            }

            try
            {
                LoadClassProfiles();
            }
            catch (ClassProfileLoadException ex)
            {
                Logging.Write(Colors.Red, "Class profiles could not be loaded!");
                Logging.Write(Colors.Red, ex.Message);
                return false;
            }

            ClassProfile classProfile = SelectDefaultClassProfile();

            try
            {
                SetClassProfile(classProfile);
            }
            catch (ArgumentException ex)
            {
                Logging.Write(Colors.Red, "Could not set class profile at init time!");
                Logging.Write(Colors.Red, ex.Message);
                return false;
            }

            BotEvents.OnBotStartRequested += CharacterManager_OnBotStartRequested;
            AutoEquip.Initialize();
            Talented.Initialize();
            _initialized = true;
            return true;
        }

        private static void CharacterManager_OnBotStartRequested(BotEvents.BotStartStopRequestEventArgs args)
        {
            if ((BotManager.Current.PulseFlags & PulseFlags.CharacterManager) == 0)
            {
                return;
            }

            if (!CharacterSettings.Instance.AutoEquip && !CharacterSettings.Instance.SelectTalents)
            {
                return;
            }

            if (CurrentClassProfile == null)
            {
                Logging.Write(LogLevel.Quiet, Colors.Red, Globalization.NoClassProfileLoaded);
                Logging.WriteToFileSync(LogLevel.Diagnostic, "No class profile is loaded. Please uncheck Auto Equip and Select Talents or load a class profile before starting the bot.");
                args.Cancel = true;
                return;
            }

            WoWSpec specialization = StyxWoW.Me.Specialization;
            WoWSpec spec = CurrentClassProfile.GetSpec();
            if (specialization != WoWSpec.None && specialization != spec)
            {
                Logging.Write(LogLevel.Quiet, Colors.Red, Globalization.WrongClassProfileLoaded,
                    ClassProfileLocalization.GetClassName(CurrentClassProfile.Class),
                    ClassProfileLocalization.GetSpecName(spec),
                    ClassProfileLocalization.GetSpecName(specialization),
                    ClassProfileLocalization.GetClassName(StyxWoW.Me.Class));
                Logging.WriteToFileSync(LogLevel.Diagnostic, "The class profile currently loaded is {0}, but the toon is {1} - can't start.", spec, specialization);
                args.Cancel = true;
            }
        }

        internal static void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            BotEvents.OnBotStartRequested -= CharacterManager_OnBotStartRequested;
            AutoEquip.Shutdown();
            Talented.Shutdown();
            _initialized = false;
        }

        public static ReadOnlyCollection<ClassProfile> ClassProfiles
        {
            get
            {
                return _classProfiles.AsReadOnly();
            }
        }

        public static ClassProfile CurrentClassProfile { get; private set; }

        public static AutoEquipper AutoEquip { get; private set; } = new AutoEquipper();

        public static TalentSelector Talented { get; private set; } = new TalentSelector();

        public static void SetClassProfile(ClassProfile classProfile)
        {
            if (classProfile != null && StyxWoW.Me.Class != classProfile.Class)
            {
                throw new ArgumentException("The class profile does not belong to the current class", "classProfile");
            }

            if (CurrentClassProfile == classProfile)
            {
                return;
            }

            CurrentClassProfile = classProfile;
            if (classProfile != null)
            {
                Logging.WriteDiagnostic(Colors.LawnGreen, "Class profile set to {0} ({1})", classProfile.GetSpec(), classProfile.Name);
            }
            else
            {
                Logging.WriteDiagnostic(Colors.LawnGreen, "Class profile set to null");
            }

            CharacterSettings.Instance.ClassProfileName = classProfile != null ? classProfile.Name : "";
        }

        private static string GetClassProfilesDirectory()
        {
            return Path.Combine(Utilities.AssemblyDirectory, "Data", "Class Profiles");
        }

        private static void LoadClassProfiles()
        {
            _classProfiles.Clear();
            List<ClassProfile> loadedProfiles = new List<ClassProfile>();
            string classProfilesDirectory = GetClassProfilesDirectory();
            foreach (string classProfilePath in Directory.GetFiles(classProfilesDirectory, "*.xml"))
            {
                ClassProfile classProfile = ClassProfile.Load(classProfilePath);
                loadedProfiles.Add(classProfile);
            }

            _classProfiles.AddRange(loadedProfiles);
        }
    }
}
