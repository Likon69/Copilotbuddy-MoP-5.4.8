using System;
using System.ComponentModel;
using System.IO;
using Styx.CommonBot;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Profiles;

namespace Styx.Helpers
{
    public class CharacterSettings : Settings, INotifyPropertyChanged
    {
        public static CharacterSettings Instance { get; private set; }

        public bool Initialized;

        public CharacterSettings()
            : base(Path.Combine(Path.Combine(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name), "CharacterSettings.xml"), null)
        {
        }

        internal static void Initialize()
        {
            Instance = new CharacterSettings();
        }

        private bool _selectTalents;
        [DefaultValue(true)]
        [Setting]
        public bool SelectTalents
        {
            get { return _selectTalents; }
            set
            {
                if (_selectTalents == value)
                {
                    return;
                }
                _selectTalents = value;
                Changed("SelectTalents");
            }
        }

        private string _classProfileName;
        [Setting]
        [DefaultValue("")]
        public string ClassProfileName
        {
            get { return _classProfileName; }
            internal set
            {
                if (_classProfileName == value)
                {
                    return;
                }
                _classProfileName = value;
                Changed("ClassProfileName");
            }
        }

        private bool _autoEquip;
        [Setting]
        [DefaultValue(true)]
        public bool AutoEquip
        {
            get { return _autoEquip; }
            set
            {
                if (_autoEquip == value)
                {
                    return;
                }
                _autoEquip = value;
                Changed("AutoEquip");
            }
        }

        private bool _autoEquipBags;
        [Setting]
        [DefaultValue(true)]
        public bool AutoEquipBags
        {
            get { return _autoEquipBags; }
            set
            {
                if (_autoEquipBags == value)
                {
                    return;
                }
                _autoEquipBags = value;
                Changed("AutoEquipBags");
            }
        }

        private bool _autoEquipWeapons;
        [DefaultValue(true)]
        [Setting]
        public bool AutoEquipWeapons
        {
            get { return _autoEquipWeapons; }
            set
            {
                if (_autoEquipWeapons == value)
                {
                    return;
                }
                _autoEquipWeapons = value;
                Changed("AutoEquipWeapons");
            }
        }

        private bool _autoEquipArmor;
        [DefaultValue(true)]
        [Setting]
        public bool AutoEquipArmor
        {
            get { return _autoEquipArmor; }
            set
            {
                if (_autoEquipArmor == value)
                {
                    return;
                }
                _autoEquipArmor = value;
                Changed("AutoEquipArmor");
            }
        }

        private bool _autoEquipBoEEpics;
        [Setting]
        [DefaultValue(false)]
        public bool AutoEquipBoEEpics
        {
            get { return _autoEquipBoEEpics; }
            set
            {
                if (_autoEquipBoEEpics == value)
                {
                    return;
                }
                _autoEquipBoEEpics = value;
                Changed("AutoEquipBoEEpics");
            }
        }

        private WeaponStyle _weaponStyle;
        [DefaultValue(WeaponStyle.Automatic)]
        [Setting]
        public WeaponStyle WeaponStyle
        {
            get { return _weaponStyle; }
            set
            {
                if (_weaponStyle == value)
                {
                    return;
                }
                _weaponStyle = value;
                Changed("WeaponStyle");
            }
        }

        private bool _rollOnItems;
        [Setting]
        [DefaultValue(true)]
        public bool RollOnItems
        {
            get { return _rollOnItems; }
            set
            {
                if (_rollOnItems == value)
                {
                    return;
                }
                _rollOnItems = value;
                Changed("RollOnItems");
            }
        }

        private bool _rollDisenchantWhenGreed;
        [DefaultValue(true)]
        [Setting]
        public bool RollDisenchantWhenGreed
        {
            get { return _rollDisenchantWhenGreed; }
            set
            {
                if (_rollDisenchantWhenGreed == value)
                {
                    return;
                }
                _rollDisenchantWhenGreed = value;
                Changed("RollDisenchantWhenGreed");
            }
        }

        private WoWItemQuality _maxBoEDisenchantQuality;
        [Setting]
        [DefaultValue(WoWItemQuality.Uncommon)]
        public WoWItemQuality MaxBoEDisenchantQuality
        {
            get { return _maxBoEDisenchantQuality; }
            set
            {
                if (_maxBoEDisenchantQuality == value)
                {
                    return;
                }
                _maxBoEDisenchantQuality = value;
                Changed("MaxBoEDisenchantQuality");
            }
        }

        private byte _ticksPerSecond;
        [Setting]
        [DefaultValue(15)]
        public byte TicksPerSecond
        {
            get { return _ticksPerSecond; }
            set
            {
                if (_ticksPerSecond != value)
                {
                    Changed("TicksPerSecond");
                }
                _ticksPerSecond = value;
            }
        }

        private int _foodAmount;
        [Setting]
        [DefaultValue(0)]
        public int FoodAmount
        {
            get { return _foodAmount; }
            set
            {
                _foodAmount = value;
                Changed("FoodAmount");
            }
        }

        private int _drinkAmount;
        [DefaultValue(0)]
        [Setting]
        public int DrinkAmount
        {
            get { return _drinkAmount; }
            set
            {
                _drinkAmount = value;
                Changed("DrinkAmount");
            }
        }

        [Setting]
        public string[] EnabledPlugins { get; set; }

        [Setting]
        public string[] RecentProfiles { get; set; }

        private int _selectedBotIndex;
        [Setting(Explanation = "The last selected index used by the Combobox in the form.")]
        [Obsolete("Use SelectedBotName instead")]
        [DefaultValue(-1)]
        public int SelectedBotIndex
        {
            get { return _selectedBotIndex; }
            set
            {
                _selectedBotIndex = value;
                Changed("SelectedBotIndex");
            }
        }

        private string _selectedBotName;
        [Setting]
        [DefaultValue("")]
        public string SelectedBotName
        {
            get { return _selectedBotName; }
            set
            {
                if (_selectedBotName == value)
                {
                    return;
                }
                _selectedBotName = value;
                Changed("SelectedBotName");
            }
        }

        private bool _useFlightPaths;
        [DefaultValue(true)]
        [Setting]
        public bool UseFlightPaths
        {
            get { return _useFlightPaths; }
            set
            {
                _useFlightPaths = value;
                Changed("UseFlightPaths");
            }
        }

        private bool _findMountAutomatically;
        [Setting]
        [DefaultValue(true)]
        public bool FindMountAutomatically
        {
            get { return _findMountAutomatically; }
            set
            {
                _findMountAutomatically = value;
                Changed("FindMountAutomatically");
            }
        }

        private bool _useRandomMount;
        [DefaultValue(false)]
        [Setting]
        public bool UseRandomMount
        {
            get { return _useRandomMount; }
            set
            {
                _useRandomMount = value;
                Changed("UseRandomMount");
            }
        }

        private string _mailRecipient;
        [Setting]
        [DefaultValue("")]
        public string MailRecipient
        {
            get { return _mailRecipient; }
            set
            {
                _mailRecipient = value;
                Changed("MailRecipient");
            }
        }

        private string _foodName;
        [DefaultValue("")]
        [Setting]
        public string FoodName
        {
            get { return _foodName; }
            set
            {
                _foodName = value;
                Changed("FoodName");
            }
        }

        private string _drinkName;
        [Setting]
        [DefaultValue("")]
        public string DrinkName
        {
            get { return _drinkName; }
            set
            {
                _drinkName = value;
                Changed("DrinkName");
            }
        }

        private string _mountName;
        [Setting]
        [DefaultValue("")]
        public string MountName
        {
            get { return _mountName; }
            set
            {
                _mountName = value;
                Changed("MountName");
            }
        }

        private string _flyingMountName;
        [DefaultValue("")]
        [Setting]
        public string FlyingMountName
        {
            get { return _flyingMountName; }
            set
            {
                _flyingMountName = value;
                Changed("FlyingMountName");
            }
        }

        private bool _lootMobs;
        [DefaultValue(true)]
        [Setting]
        public bool LootMobs
        {
            get { return _lootMobs; }
            set
            {
                _lootMobs = value;
                Changed("LootMobs");
            }
        }

        private bool _skinMobs;
        [DefaultValue(false)]
        [Setting]
        public bool SkinMobs
        {
            get { return _skinMobs; }
            set
            {
                _skinMobs = value;
                Changed("SkinMobs");
            }
        }

        private bool _ninjaSkin;
        [DefaultValue(false)]
        [Setting]
        public bool NinjaSkin
        {
            get { return _ninjaSkin; }
            set
            {
                _ninjaSkin = value;
                Changed("NinjaSkin");
            }
        }

        private bool _lootChests;
        [Setting]
        [DefaultValue(true)]
        public bool LootChests
        {
            get { return _lootChests; }
            set
            {
                _lootChests = value;
                Changed("LootChests");
            }
        }

        private bool _harvestMinerals;
        [Setting]
        [DefaultValue(false)]
        public bool HarvestMinerals
        {
            get { return _harvestMinerals; }
            set
            {
                _harvestMinerals = value;
                Changed("HarvestMinerals");
            }
        }

        private bool _harvestHerbs;
        [Setting]
        [DefaultValue(false)]
        public bool HarvestHerbs
        {
            get { return _harvestHerbs; }
            set
            {
                _harvestHerbs = value;
                Changed("HarvestHerbs");
            }
        }

        [Setting]
        public bool UseMount
        {
            get { return Mount.UseMount; }
            set
            {
                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile != null)
                {
                    currentProfile.UseMount = value;
                }
            }
        }

        [Setting]
        public int PullDistance
        {
            get { return (int)Targeting.CollectionRange; }
            set
            {
                Profile currentProfile = ProfileManager.CurrentProfile;
                if (currentProfile != null)
                {
                    currentProfile.TargetingDistance = value;
                }
            }
        }

        private int _lootRadius;
        [DefaultValue(45)]
        [Setting]
        public int LootRadius
        {
            get { return _lootRadius; }
            set
            {
                _lootRadius = value;
                Changed("LootRadius");
            }
        }

        [Obsolete("This is no longer a setting. VendorManager automatically finds vendors from database if none of the vendors in the profile is eligible.")]
        public bool FindVendorsAutomatically
        {
            get { return true; }
            set
            {
            }
        }

        private string _lastUsedPath;
        [Setting]
        [DefaultValue("")]
        public string LastUsedPath
        {
            get { return _lastUsedPath; }
            set
            {
                _lastUsedPath = value;
                Changed("LastUsedPath");
            }
        }

        private int _mountDistance;
        [Setting]
        [DefaultValue(60)]
        public int MountDistance
        {
            get { return _mountDistance; }
            set
            {
                _mountDistance = value;
                Changed("MountDistance");
            }
        }

        private bool _ressAtSpiritHealers;
        [Setting]
        [DefaultValue(true)]
        public bool RessAtSpiritHealers
        {
            get { return _ressAtSpiritHealers; }
            set
            {
                _ressAtSpiritHealers = value;
                Changed("RessAtSpiritHealers");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }

            if (Instance != null && StyxWoW.Me != null && Initialized)
                Instance.Save();
        }
    }
}
