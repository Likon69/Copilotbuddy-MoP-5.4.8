using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.CommonBot.AreaManagement;
using Styx.CommonBot.Profiles.Quest;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Resources;

namespace Styx.CommonBot.Profiles
{
    public class Profile : IEquatable<Profile>
    {
        private DualHashSet<uint, string> _avoidMobs = new DualHashSet<uint, string>();
        private readonly DualHashSet<string, uint> _blacklistedQuestGivers = new DualHashSet<string, uint>();
        private readonly DualHashSet<string, uint> _blacklistedQuests = new DualHashSet<string, uint>();
        private readonly List<Blackspot> _blackspots = new List<Blackspot>();
        private readonly Dictionary<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> _aerialBlackspots = new Dictionary<Tuple<uint, WoWFactionGroup>, List<Vector2[]>>();
        private readonly HashSet<uint> _factions = new HashSet<uint>();
        private readonly DualHashSet<uint, string> _forceMail = new DualHashSet<uint, string>();
        private static readonly Dictionary<uint, BlacklistFlags> _defaultBlacklist = new Dictionary<uint, BlacklistFlags>();
        private Dictionary<uint, BlacklistFlags> _blacklist = new Dictionary<uint, BlacklistFlags>(_defaultBlacklist);
        private HotspotManager _hotspotManager;
        private MailboxManager _mailboxManager;
        private int _protectedItemsDefaultCount;
        private OrderNodeCollection _questOrder = new OrderNodeCollection();
        private readonly List<QuestInfo> _quests = new List<QuestInfo>();
        private GrindArea _grindArea;
        private Guid _guid;
        private bool? _mailBlue;
        private bool? _mailGreen;
        private bool? _mailGrey;
        private bool? _mailPurple;
        private bool? _mailWhite;
        private int? _mapId;
        private int? _maxLevel;
        private float? _minDurability;
        private int? _minFreeBagSlots;
        private int? _minLevel;
        private int? _minMailLevel;
        private bool? _sellBlue;
        private bool? _sellGreen;
        private bool? _sellGrey;
        private bool? _sellPurple;
        private bool? _sellWhite;
        private bool? _targetElites;
        private int? _targetMaxLevel;
        private int? _targetMinLevel;
        private double? _targetingDistance;
        private bool? _useMount;
        private bool? _lootMobs;
        private double? _lootRadius;
        private VendorManager _vendorManager;
        private readonly byte[] _fileHash;

        #region hardcoded protected items

        private readonly DualHashSet<uint, string> _protectedItems = new DualHashSet<uint, string>
        {
            // archaeology
            new ValuePair<uint, string>(63127, "Highborne Scroll"),
            new ValuePair<uint, string>(63128, "Troll Tablet"),
            new ValuePair<uint, string>(64395, "Vrykul Rune Stick"),
            new ValuePair<uint, string>(52843, "Dwarf Rune Stone"),
            new ValuePair<uint, string>(64397, "Tol'vir Hieroglyphic"),
            new ValuePair<uint, string>(64396, "Nerubian Obelisk"),
            new ValuePair<uint, string>(64394, "Draenei Tome"),
            new ValuePair<uint, string>(64392, "Orc Blood Text"),
            new ValuePair<uint, string>(6948, "Hearthstone"),
            new ValuePair<uint, string>(2901, "Mining Pick"),
            new ValuePair<uint, string>(7005, "Skinning Knife"),
            new ValuePair<uint, string>(6256, "Fishing Pole"),
            // engineerng
            new ValuePair<uint, string>(5956, "Blacksmith Hammer"),
            new ValuePair<uint, string>(6219, "Arclight Spanner"),
            new ValuePair<uint, string>(23821, "Zapthrottle Mote Extractor"),
            // enchanting
            new ValuePair<uint, string>(52723, "Runed Elementium Rod"),
            new ValuePair<uint, string>(44452, "Runed Titanium Rod"),
            new ValuePair<uint, string>(22463, "Runed Eternium Rod"),
            new ValuePair<uint, string>(22462, "Runed Adamantite Rod"),
            new ValuePair<uint, string>(22461, "Runed Fel Iron Rod"),
            new ValuePair<uint, string>(16207, "Runed Arcanite Rod"),
            new ValuePair<uint, string>(11145, "Runed Truesilver Rod"),
            new ValuePair<uint, string>(11130, "Runed Golden Rod"),
            new ValuePair<uint, string>(6339, "Runed Silver Rod"),
            new ValuePair<uint, string>(6218, "Runed Copper Rod"),
            // poisons
            new ValuePair<uint, string>(6497, "Instant Poison"),
            new ValuePair<uint, string>(6949, "Instant Poison"),
            new ValuePair<uint, string>(6950, "Instant Poison"),
            new ValuePair<uint, string>(8926, "Instant Poison"),
            new ValuePair<uint, string>(8927, "Instant Poison"),
            new ValuePair<uint, string>(8928, "Instant Poison"),
            new ValuePair<uint, string>(21927, "Instant Poison"),
            new ValuePair<uint, string>(43230, "Instant Poison"),
            new ValuePair<uint, string>(43231, "Instant Poison"),
            new ValuePair<uint, string>(3775, "Crippling Poison"),
            new ValuePair<uint, string>(17031, "Rune of Teleportation"),
            new ValuePair<uint, string>(118, "Healing Potion"),
            new ValuePair<uint, string>(858, "Healing Potion"),
            new ValuePair<uint, string>(929, "Healing Potion"),
            new ValuePair<uint, string>(1710, "Healing Potion"),
            new ValuePair<uint, string>(3928, "Healing Potion"),
            new ValuePair<uint, string>(13446, "Healing Potion"),
            new ValuePair<uint, string>(17556, "Healing Potion"),
            new ValuePair<uint, string>(28551, "Healing Potion"),
            new ValuePair<uint, string>(33447, "Healing Potion"),
            new ValuePair<uint, string>(4496, "Pouch"),
            new ValuePair<uint, string>(5571, "Pouch"),
            new ValuePair<uint, string>(828, "Pouch"),
            new ValuePair<uint, string>(5572, "Pouch"),
            new ValuePair<uint, string>(805, "Pouch"),
            new ValuePair<uint, string>(1251, "Bandage"),
            new ValuePair<uint, string>(2581, "Bandage"),
            new ValuePair<uint, string>(3530, "Bandage"),
            new ValuePair<uint, string>(3531, "Bandage"),
            new ValuePair<uint, string>(6450, "Bandage"),
            new ValuePair<uint, string>(6451, "Bandage"),
            new ValuePair<uint, string>(8544, "Bandage"),
            new ValuePair<uint, string>(8545, "Bandage"),
            new ValuePair<uint, string>(14529, "Bandage"),
            new ValuePair<uint, string>(14530, "Bandage"),
            new ValuePair<uint, string>(21990, "Bandage"),
            new ValuePair<uint, string>(21991, "Bandage"),
            new ValuePair<uint, string>(34721, "Bandage"),
            new ValuePair<uint, string>(34722, "Bandage"),
            new ValuePair<uint, string>(38640, "Bandage"),
            new ValuePair<uint, string>(38643, "Bandage")
        };

        #endregion

        public Profile()
        {
            _vendorManager = new VendorManager();
        }

        public Profile(XElement xml, Profile parent)
        {
            XmlElement = xml;
            using (new ForcedCulture())
            {
                _guid = Guid.NewGuid();

                _protectedItemsDefaultCount = _protectedItems.Count; // <- VERY IMPORTANT!
                Parent = parent;
                SubProfiles = new List<Profile>();
                XElement[] xelements = xml.Elements().ToArray();

                for (int i = 0; i < xelements.Length; i++)
                {
                    try
                    {
                        XElement element = xelements[i];
                        switch (element.Name.ToString().ToLowerInvariant())
                        {
                            case "name":
                                Name = element.Value;
                                break;

                            case "mesh":
                                break; //note: lol  // note: stop laughing!!! :'(

                            case "continentid":
                                int mapId;
                                if (!int.TryParse(element.Value, out mapId))
                                {
                                    throw ProfileTagExpectedException.Create<int>(element);
                                }
                                _mapId = mapId;
                                break;

                            case "mindurability":
                                float minDurability;
                                if (!float.TryParse(element.Value, out minDurability))
                                {
                                    throw ProfileTagExpectedException.Create<float>(element);
                                }
                                _minDurability = minDurability;
                                break;

                            case "minfreebagslots":
                                int minFreeBagSlots;
                                if (!int.TryParse(element.Value, out minFreeBagSlots))
                                {
                                    throw ProfileTagExpectedException.Create<int>(element);
                                }
                                _minFreeBagSlots = minFreeBagSlots;
                                break;

                            case "targetingdistance":
                                double targetingDistance;
                                if (!double.TryParse(element.Value, out targetingDistance))
                                {
                                    throw ProfileTagExpectedException.Create<double>(element);
                                }
                                _targetingDistance = targetingDistance;
                                break;

                            case "usemount":
                                bool useMount;
                                if (!bool.TryParse(element.Value, out useMount))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element);
                                }
                                _useMount = useMount;
                                break;

                            case "lootmobs":
                                bool lootMobs;
                                if (!bool.TryParse(element.Value, out lootMobs))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element);
                                }
                                _lootMobs = lootMobs;
                                break;

                            case "minlevel":
                                int minLevel;
                                if (!int.TryParse(element.Value, out minLevel))
                                {
                                    throw ProfileTagExpectedException.Create<int>(element);
                                }
                                _minLevel = minLevel;
                                break;

                            case "maxlevel":
                                int maxLevel;
                                if (!int.TryParse(element.Value, out maxLevel))
                                {
                                    throw ProfileTagExpectedException.Create<int>(element);
                                }
                                _maxLevel = maxLevel;
                                break;

                            case "targetminlevel":
                                {
                                    int targetMinLevel;
                                    if (!int.TryParse(element.Value, out targetMinLevel))
                                    {
                                        throw ProfileTagExpectedException.Create<int>(element);
                                    }

                                    _targetMinLevel = targetMinLevel;
                                    if (_grindArea != null)
                                    {
                                        _grindArea.TargetMinLevel = _targetMinLevel.Value;
                                    }

                                    Logging.WriteDiagnostic(
                                        StyxResources.
                                            Warning___TargetMinLevel__has_been_moved_to__GrindArea___Please_reflect_this_change_in_your_profiles_);
                                    break;
                                }

                            case "targetmaxlevel":
                                {
                                    int targetMaxLevel;
                                    if (!int.TryParse(element.Value, out targetMaxLevel))
                                    {
                                        throw ProfileTagExpectedException.Create<int>(element);
                                    }

                                    _targetMaxLevel = targetMaxLevel;

                                    if (_grindArea != null)
                                    {
                                        _grindArea.TargetMinLevel = _targetMinLevel.Value;
                                    }

                                    Logging.WriteDiagnostic(
                                        StyxResources.
                                            Warning___TargetMaxLevel__has_been_moved_to__GrindArea___Please_reflect_this_change_in_your_profiles_);
                                    break;
                                }

                            case "minmaillevel":
                                int minMailLevel;
                                if (!int.TryParse(element.Value, out minMailLevel))
                                {
                                    throw ProfileTagExpectedException.Create<int>(element);
                                }
                                _minMailLevel = minMailLevel;
                                break;

                            case "sellgray":
                            case "sellgrey":
                                bool sellGrey;
                                if (!bool.TryParse(element.Value, out sellGrey))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _sellGrey = sellGrey;
                                break;
                            case "sellwhite":
                                bool sellWhite;
                                if (!bool.TryParse(element.Value, out sellWhite))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _sellWhite = sellWhite;
                                break;
                            case "sellgreen":
                                bool sellGreen;
                                if (!bool.TryParse(element.Value, out sellGreen))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _sellGreen = sellGreen;
                                break;
                            case "sellblue":
                                bool sellBlue;
                                if (!bool.TryParse(element.Value, out sellBlue))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _sellBlue = sellBlue;
                                break;
                            case "sellpurple":
                                bool sellPurple;
                                if (!bool.TryParse(element.Value, out sellPurple))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _sellPurple = sellPurple;
                                break;

                            case "mailgrey":
                                bool mailGrey;
                                if (!bool.TryParse(element.Value, out mailGrey))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _mailGrey = mailGrey;
                                break;
                            case "mailwhite":
                                bool mailWhite;
                                if (!bool.TryParse(element.Value, out mailWhite))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _mailWhite = mailWhite;
                                break;
                            case "mailgreen":
                                bool mailGreen;
                                if (!bool.TryParse(element.Value, out mailGreen))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _mailGreen = mailGreen;
                                break;
                            case "mailblue":
                                bool mailBlue;
                                if (!bool.TryParse(element.Value, out mailBlue))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _mailBlue = mailBlue;
                                break;
                            case "mailpurple":
                                bool mailPurple;
                                if (!bool.TryParse(element.Value, out mailPurple))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _mailPurple = mailPurple;
                                break;

                            case "targetelites":
                            case "elites":
                            case "elite":
                                bool targetElites;
                                if (!bool.TryParse(element.Value, out targetElites))
                                {
                                    throw ProfileTagExpectedException.Create<bool>(element, "True", "False");
                                }
                                _targetElites = targetElites;
                                break;

                            case "mailing":
                            case "mailboxes":
                            case "mails":
                                _mailboxManager = new MailboxManager(element);
                                break;

                            case "protecteditems":
                                List<XElement> protectedItems = element.Elements().ToList();
                                foreach (XElement protectedItem in protectedItems)
                                {
                                    try
                                    {
                                        if (protectedItem.Name.ToString().ToLowerInvariant() != "item")
                                        {
                                            throw new ProfileUnknownElementException(protectedItem, "Item");
                                        }

                                        uint itemId = 0;
                                        string itemName = "";

                                        XAttribute[] attributes = protectedItem.Attributes().ToArray();
                                        foreach (XAttribute attrib in attributes)
                                        {
                                            switch (attrib.Name.ToString().ToLowerInvariant())
                                            {
                                                case "name":
                                                    itemName = attrib.Value;
                                                    break;
                                                case "entry":
                                                case "id":
                                                    if (!uint.TryParse(attrib.Value, out itemId))
                                                    {
                                                        throw new ProfileAttributeExpectedException<uint>(attrib);
                                                    }
                                                    break;
                                                default:
                                                    throw new ProfileUnknownAttributeException(attrib, "Name", "Entry");
                                            }
                                        }

                                        if (itemId == 0 && !uint.TryParse(protectedItem.Value, out itemId))
                                        {
                                            throw new ProfileMissingAttributeException<uint>("Entry", protectedItem);
                                        }
                                        var vp = new ValuePair<uint, string>(itemId, itemName);
                                        _protectedItems.Add(vp);
                                    }
                                    catch (ProfileException ex)
                                    {
                                        Logging.Write(ex.Message);
                                    }
                                }
                                break;

                            case "forcemail":
                                List<XElement> forceMailItems = element.Elements().ToList();
                                foreach (XElement forceMailItem in forceMailItems)
                                {
                                    try
                                    {
                                        if (forceMailItem.Name.ToString().ToLowerInvariant() != "item")
                                        {
                                            throw new ProfileUnknownElementException(forceMailItem, "Item");
                                        }

                                        uint itemId = 0;
                                        string name = "";

                                        XAttribute[] attribs = forceMailItem.Attributes().ToArray();
                                        foreach (XAttribute attrib in attribs)
                                        {
                                            switch (attrib.Name.ToString().ToLowerInvariant())
                                            {
                                                case "name":
                                                    name = attrib.Value;
                                                    break;
                                                case "entry":
                                                case "id":
                                                    if (!uint.TryParse(attrib.Value, out itemId))
                                                    {
                                                        throw new ProfileAttributeExpectedException<uint>(attrib);
                                                    }
                                                    break;
                                            }
                                        }

                                        if (itemId == 0 && !uint.TryParse(forceMailItem.Value, out itemId))
                                        {
                                            throw new ProfileMissingAttributeException<uint>("Entry", forceMailItem);
                                        }

                                        // if its ForceMail ofc it must be protected...
                                        _protectedItems.Add(itemId);
                                        _protectedItems.Add(name.ToLowerInvariant());

                                        // ...and we need to mail it
                                        _forceMail.Add(itemId);
                                        _forceMail.Add(name.ToLowerInvariant());
                                    }
                                    catch (ProfileException ex)
                                    {
                                        Logging.Write(ex.Message);
                                    }
                                }
                                break;

                            case "grindarea":
                                {
                                    _grindArea = GrindArea.FromXML(element);
                                    if (_targetMinLevel.HasValue)
                                    {
                                        _grindArea.TargetMinLevel = _targetMinLevel.Value;
                                    }

                                    if (_targetMaxLevel.HasValue)
                                    {
                                        _grindArea.TargetMaxLevel = _targetMaxLevel.Value;
                                    }

                                    HashSet<uint> factions = Factions;
                                    if (factions.Count > 0 && _grindArea.Factions.Count <= 0)
                                    {
                                        foreach (uint faction in factions)
                                        {
                                            _grindArea.Factions.Add((int)faction);
                                        }
                                    }
                                    break;
                                }

                            case "subprofile":
                                SubProfiles.Add(new Profile(element, this));
                                break;

                            case "hotspots":
                                {
                                    var manager = new HotspotManager(element);
                                    _hotspotManager = manager;
                                    _grindArea = new GrindArea(manager);
                                    break;
                                }

                            case "vendors":
                                _vendorManager = new VendorManager(element);
                                break;

                            case "factions":
                                {
                                    string factionString = element.Value;
                                    MatchCollection factions = Regex.Matches(factionString, "\\d+");
                                    foreach (Match faction in factions)
                                    {
                                        _factions.Add(Convert.ToUInt32(faction.Value));
                                    }

                                    if (_grindArea != null && _grindArea.Factions.Count <= 0)
                                    {
                                        _grindArea.Factions.AddRange(_factions.Cast<int>());
                                    }

                                    Logging.WriteDiagnostic(
                                        StyxResources.
                                            Warning___Factions__has_been_moved_to_the__GrindArea___Please_reflect_this_change_in_your_profiles_);
                                    break;
                                }

                            case "avoidmobs":
                            case "avoids":
                                AvoidMobs = ParseAvoidMobs(element);
                                break;

                            case "blacklist":
                                Blacklist = ParseBlacklist(element);
                                break;

                            case "blackspots":
                            case "ignorespots":
                                XElement[] blackspots = element.Elements().ToArray();
                                foreach (XElement blackspot in blackspots)
                                {
                                    try
                                    {
                                        switch (blackspot.Name.ToString().ToLowerInvariant())
                                        {
                                            case "spot":
                                            case "blackspot":
                                                ParseBlackspot(blackspot);
                                                break;

                                            case "aerialblackspot":
                                                {
                                                    WoWFactionGroup factionGroup = WoWFactionGroup.Neutral;
                                                    uint mapId = 0;
                                                    var points = new List<Vector2>();

                                                    XAttribute[] attributes = blackspot.Attributes().ToArray();
                                                    foreach (XAttribute attribute in attributes)
                                                    {
                                                        try
                                                        {
                                                            switch (attribute.Name.ToString().ToLowerInvariant())
                                                            {
                                                                case "faction":
                                                                    if (!Enum.TryParse(attribute.Value, out factionGroup))
                                                                    {
                                                                        throw new ProfileAttributeExpectedException(attribute, "Neutral", "Alliance", "Horde");
                                                                    }
                                                                    break;
                                                                case "name":
                                                                    break;
                                                                case "mapid":
                                                                    if (!uint.TryParse(attribute.Value, out mapId))
                                                                    {
                                                                        throw new ProfileAttributeExpectedException<uint>(attribute);
                                                                    }
                                                                    break;
                                                                default:
                                                                    throw new ProfileUnknownAttributeException(attribute, "Faction", "Name");
                                                            }
                                                        }
                                                        catch (ProfileException ex)
                                                        {
                                                            Logging.Write(ex.Message);
                                                        }
                                                    }

                                                    XElement[] pointElements = blackspot.Elements().ToArray();
                                                    foreach (XElement pointElement in pointElements)
                                                    {
                                                        XAttribute[] pointAttributes = pointElement.Attributes().ToArray();
                                                        float pointX = 0f;
                                                        float pointY = 0f;
                                                        foreach (XAttribute attribute in pointAttributes)
                                                        {
                                                            try
                                                            {
                                                                switch (attribute.Name.ToString().ToLowerInvariant())
                                                                {
                                                                    case "x":
                                                                        if (!float.TryParse(attribute.Value, out pointX))
                                                                        {
                                                                            throw new ProfileAttributeExpectedException<float>(attribute);
                                                                        }
                                                                        break;
                                                                    case "y":
                                                                        if (!float.TryParse(attribute.Value, out pointY))
                                                                        {
                                                                            throw new ProfileAttributeExpectedException<float>(attribute);
                                                                        }
                                                                        break;
                                                                    default:
                                                                        throw new ProfileUnknownAttributeException(attribute, "X", "Y");
                                                                }
                                                            }
                                                            catch (ProfileException ex)
                                                            {
                                                                Logging.Write(ex.Message);
                                                            }
                                                        }
                                                        points.Add(new Vector2(pointX, pointY));
                                                    }

                                                    var key = new Tuple<uint, WoWFactionGroup>(mapId, factionGroup);
                                                    if (!_aerialBlackspots.ContainsKey(key))
                                                    {
                                                        _aerialBlackspots.Add(key, new List<Vector2[]>());
                                                    }
                                                    _aerialBlackspots[key].Add(points.ToArray());
                                                    break;
                                                }

                                            default:
                                                throw new ProfileUnknownElementException(blackspot, "Blackspot");
                                        }
                                    }
                                    catch (ProfileException ex)
                                    {
                                        Logging.Write(ex.Message);
                                    }
                                }
                                break;

                            case "questblacklist":
                            case "blacklistedquests":
                                XElement[] blacklistedQuests = element.Elements().ToArray();
                                foreach (XElement blacklistedQuest in blacklistedQuests)
                                {
                                    try
                                    {
                                        string name = null;
                                        uint id = 0;
                                        string tag = blacklistedQuest.Name.ToString().ToLowerInvariant();
                                        if (tag == "quest" || tag == "blacklistedquest")
                                        {
                                            XAttribute[] attributes = blacklistedQuest.Attributes().ToArray();
                                            foreach (XAttribute attribute in attributes)
                                            {
                                                try
                                                {
                                                    switch (attribute.Name.ToString().ToLowerInvariant())
                                                    {
                                                        case "entry":
                                                        case "id":
                                                            if (!uint.TryParse(attribute.Value, out id))
                                                            {
                                                                throw new ProfileAttributeExpectedException<int>(attribute);
                                                            }
                                                            break;
                                                        case "name":
                                                            name = attribute.Value;
                                                            break;
                                                        default:
                                                            throw new ProfileUnknownAttributeException(attribute);
                                                    }
                                                }
                                                catch (ProfileException ex)
                                                {
                                                    Logging.Write(ex.Message);
                                                }
                                            }
                                        }

                                        if (name == null && id == 0)
                                        {
                                            throw new ProfileMissingAttributeException<int>("Id", blacklistedQuest);
                                        }

                                        if (name != null)
                                        {
                                            _blacklistedQuests.Add(name.ToLowerInvariant());
                                        }

                                        if (id != 0)
                                        {
                                            _blacklistedQuests.Add(id);
                                        }
                                    }
                                    catch (ProfileException ex)
                                    {
                                        Logging.Write(ex.Message);
                                    }
                                }
                                break;

                            case "questgiverblacklist":
                            case "blacklistedquestgivers":
                            case "blacklistedquestnpcs":
                                XElement[] blacklistedQuestsgivers = element.Elements().ToArray();
                                foreach (XElement blacklistedQuest in blacklistedQuestsgivers)
                                {
                                    try
                                    {
                                        string name = null;
                                        uint id = 0;
                                        string tag = blacklistedQuest.Name.ToString().ToLowerInvariant();
                                        if (tag == "questgiver" || tag == "blacklistedquestgiver")
                                        {
                                            XAttribute[] attributes = blacklistedQuest.Attributes().ToArray();
                                            foreach (XAttribute attribute in attributes)
                                            {
                                                try
                                                {
                                                    switch (attribute.Name.ToString().ToLowerInvariant())
                                                    {
                                                        case "entry":
                                                        case "id":
                                                            if (!uint.TryParse(attribute.Value, out id))
                                                            {
                                                                throw new ProfileAttributeExpectedException<int>(attribute);
                                                            }
                                                            break;
                                                        case "name":
                                                            name = attribute.Value;
                                                            break;
                                                        default:
                                                            throw new ProfileUnknownAttributeException(attribute);
                                                    }
                                                }
                                                catch (ProfileException ex)
                                                {
                                                    Logging.Write(ex.Message);
                                                }
                                            }
                                        }

                                        if (name == null && id == 0)
                                        {
                                            throw new ProfileMissingAttributeException<int>("Id", blacklistedQuest);
                                        }

                                        if (name != null)
                                        {
                                            _blacklistedQuestGivers.Add(name.ToLowerInvariant());
                                        }

                                        if (id != 0)
                                        {
                                            _blacklistedQuestGivers.Add(id);
                                        }
                                    }
                                    catch (ProfileException ex)
                                    {
                                        Logging.Write(ex.Message);
                                    }
                                }
                                break;

                            case "quest":
                            case "questinfo":
                                Profile root = GetRootProfile();
                                root._quests.Add(QuestInfo.FromXML(element));
                                break;

                            case "questorder":
                                _questOrder = OrderNodeCollection.FromXml(element);
                                break;

                            default:
                                // Allow XML defs
                                if (element.NodeType == XmlNodeType.XmlDeclaration)
                                {
                                    break;
                                }
                                if (element.NodeType == XmlNodeType.Comment)
                                {
                                    break;
                                }

                                if (OnUnknownProfileElement != null)
                                {
                                    var args = new UnknownProfileElementEventArgs(element);
                                    OnUnknownProfileElement(this, args);

                                    if (!args.Handled)
                                    {
                                        throw new ProfileUnknownElementException(element);
                                    }
                                    break;
                                }
                                throw new ProfileUnknownElementException(element);
                        }
                    }
                    catch (ProfileException ex)
                    {
                        Logging.WriteQuiet(Colors.Red, ex.Message);
                    }
                }
            }
        }

        private void ParseBlackspot(XElement element)
        {
            float? x = null;
            float? y = null;
            float? z = null;
            float radius = 40f;
            float height = 10f;
            string name = null;
            int mapId = -1;

            XAttribute[] attributes = element.Attributes().ToArray();
            foreach (XAttribute attribute in attributes)
            {
                try
                {
                    switch (attribute.Name.ToString().ToLowerInvariant())
                    {
                        case "x":
                            float tempX;
                            if (!float.TryParse(attribute.Value, out tempX))
                            {
                                throw new ProfileAttributeExpectedException<float>(attribute);
                            }
                            x = tempX;
                            break;
                        case "y":
                            float tempY;
                            if (!float.TryParse(attribute.Value, out tempY))
                            {
                                throw new ProfileAttributeExpectedException<float>(attribute);
                            }
                            y = tempY;
                            break;
                        case "z":
                            if (!x.HasValue || !y.HasValue)
                            {
                                throw new ProfileException(StyxResources.You_have_placed_the__Z__attribute_before_the__X__and__Y__attribute_);
                            }

                            float tempZ;
                            if (!ProfileHelper.ParseHeightString(x.Value, y.Value, attribute.Value, out tempZ))
                            {
                                throw new ProfileAttributeExpectedException<float>(attribute);
                            }
                            z = tempZ;
                            break;
                        case "rad":
                        case "radius":
                            if (!float.TryParse(attribute.Value, out radius))
                            {
                                throw new ProfileAttributeExpectedException<float>(attribute);
                            }
                            break;
                        case "height":
                            if (!float.TryParse(attribute.Value, out height))
                            {
                                throw new ProfileAttributeExpectedException<float>(attribute);
                            }
                            break;
                        case "name":
                            name = attribute.Value;
                            break;
                        case "mapid":
                            if (!int.TryParse(attribute.Value, out mapId))
                            {
                                throw new ProfileAttributeExpectedException<int>(attribute);
                            }
                            break;
                        default:
                            throw new ProfileUnknownAttributeException(attribute, "X", "Y", "Z");
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.Write(ex.Message);
                }
            }

            if (!x.HasValue || !y.HasValue || !z.HasValue)
            {
                throw new ProfileMissingAttributeException<float>(x.HasValue ? (y.HasValue ? "Z" : "Y") : "X", element);
            }

            _blackspots.Add(new Blackspot(new WoWPoint(x.Value, y.Value, z.Value), radius, height, name, mapId));
        }

        public Profile(string path, Profile parent)
            : this(XElement.Load(path, LoadOptions.SetLineInfo), parent)
        {
            Path = path;
            _fileHash = ComputeFileHash();
        }

        private byte[] ComputeFileHash()
        {
            byte[] hash;
            using (FileStream fileStream = File.Open(Path, FileMode.Open, FileAccess.Read))
            {
                using (SHA256 sha = SHA256.Create())
                {
                    hash = sha.ComputeHash(fileStream);
                }
            }
            return hash;
        }

        public bool HasChanged()
        {
            if (Path == null || !File.Exists(Path))
            {
                return false;
            }
            if (_fileHash == null)
            {
                return false;
            }
            byte[] hash = ComputeFileHash();
            return !hash.SequenceEqual(_fileHash);
        }

        public string Path { get; private set; }

        /// <summary>Gets the XElement that actually represents this Profile.</summary>
        /// <value>The xml element.</value>
        public XElement XmlElement { get; private set; }

        public string Name { get; internal set; }

        public int ContinentId
        {
            get
            {
                if (_mapId.HasValue)
                {
                    return _mapId.Value;
                }
                return Parent != null ? Parent.ContinentId : -1;
            }
        }

        public double? TargetingDistance
        {
            get
            {
                if (_targetingDistance.HasValue)
                {
                    return _targetingDistance.Value;
                }
                return Parent != null ? Parent.TargetingDistance : null;
            }
            set { _targetingDistance = value; }
        }

        public bool? LootMobs
        {
            get
            {
                if (_lootMobs.HasValue)
                {
                    return _lootMobs.Value;
                }
                return Parent != null ? Parent.LootMobs : null;
            }
            set { _lootMobs = value; }
        }

        public bool? UseMount
        {
            get
            {
                if (_useMount.HasValue)
                {
                    return _useMount.Value;
                }
                return Parent != null ? Parent.UseMount : null;
            }
            set { _useMount = value; }
        }

        public double? LootRadius
        {
            get
            {
                if (_lootRadius.HasValue)
                {
                    return _lootRadius.Value;
                }
                return Parent != null ? Parent.LootRadius : null;
            }
            set { _lootRadius = value; }
        }

        public int MinLevel
        {
            get
            {
                if (_minLevel.HasValue)
                {
                    return _minLevel.Value;
                }

                return Parent != null ? Parent.MinLevel : 0;
            }
            internal set { _minLevel = value; }
        }

        public int MaxLevel
        {
            get
            {
                if (_maxLevel.HasValue)
                {
                    return _maxLevel.Value;
                }

                return Parent != null ? Parent.MaxLevel : int.MaxValue;
            }
            internal set { _maxLevel = value; }
        }

        public int TargetMinLevel
        {
            get
            {
                if (_targetMinLevel.HasValue)
                {
                    return _targetMinLevel.Value;
                }

                return Parent != null ? Parent.TargetMinLevel : 0;
            }
        }

        public int TargetMaxLevel
        {
            get
            {
                if (_targetMaxLevel.HasValue)
                {
                    return _targetMaxLevel.Value;
                }

                return Parent != null ? Parent.TargetMaxLevel : int.MaxValue;
            }
        }

        public int MinMailLevel
        {
            get
            {
                if (_minMailLevel.HasValue)
                {
                    return _minMailLevel.Value;
                }

                return Parent != null ? Parent.MinMailLevel : 5;
            }
        }

        public float MinDurability
        {
            get
            {
                if (_minDurability.HasValue)
                {
                    return _minDurability.Value;
                }

                return Parent != null ? Parent.MinDurability : 0.4f;
            }
        }

        public int MinFreeBagSlots
        {
            get
            {
                if (_minFreeBagSlots.HasValue)
                {
                    return _minFreeBagSlots.Value;
                }

                // don't put this above zero please - pk 11/7/10
                return Parent != null ? Parent.MinFreeBagSlots : 0;
            }
        }

        public bool SellGrey
        {
            get
            {
                if (_sellGrey.HasValue)
                {
                    return _sellGrey.Value;
                }

                return Parent == null || Parent.SellGrey;
            }
        }

        public bool SellWhite
        {
            get
            {
                if (_sellWhite.HasValue)
                {
                    return _sellWhite.Value;
                }

                return Parent != null && Parent.SellWhite;
            }
        }

        public bool SellGreen
        {
            get
            {
                if (_sellGreen.HasValue)
                {
                    return _sellGreen.Value;
                }

                return Parent != null && Parent.SellGreen;
            }
        }

        public bool SellBlue
        {
            get
            {
                if (_sellBlue.HasValue)
                {
                    return _sellBlue.Value;
                }

                return Parent != null && Parent.SellBlue;
            }
        }

        public bool SellPurple
        {
            get
            {
                if (_sellPurple.HasValue)
                {
                    return _sellPurple.Value;
                }

                return Parent != null && Parent.SellPurple;
            }
        }

        public bool MailGrey
        {
            get
            {
                if (_mailGrey.HasValue)
                {
                    return _mailGrey.Value;
                }

                return Parent != null && Parent.MailGrey;
            }
        }

        public bool MailWhite
        {
            get
            {
                if (_mailWhite.HasValue)
                {
                    return _mailWhite.Value;
                }

                return Parent == null || Parent.MailWhite;
            }
        }

        public bool MailGreen
        {
            get
            {
                if (_mailGreen.HasValue)
                {
                    return _mailGreen.Value;
                }

                return Parent == null || Parent.MailGreen;
            }
        }

        public bool MailBlue
        {
            get
            {
                if (_mailBlue.HasValue)
                {
                    return _mailBlue.Value;
                }

                return Parent == null || Parent.MailBlue;
            }
        }

        public bool MailPurple
        {
            get
            {
                if (_mailPurple.HasValue)
                {
                    return _mailPurple.Value;
                }

                return Parent == null || Parent.MailPurple;
            }
        }

        public bool TargetElites
        {
            get
            {
                if (_targetElites.HasValue)
                {
                    return _targetElites.Value;
                }

                return Parent != null && Parent.TargetElites;
            }
        }

        public GrindArea GrindArea
        {
            get
            {
                if (_grindArea != null)
                {
                    return _grindArea;
                }

                return Parent != null ? Parent.GrindArea : null;
            }
            internal set { _grindArea = value; }
        }

        public DualHashSet<uint, string> ProtectedItems
        {
            get
            {
                if (_protectedItems.Count > _protectedItemsDefaultCount)
                {
                    return _protectedItems;
                }

                return Parent != null ? Parent.ProtectedItems : _protectedItems;
            }
        }

        public DualHashSet<uint, string> ForceMail
        {
            get
            {
                if (_forceMail.Count > 0)
                {
                    return _forceMail;
                }

                return Parent != null ? Parent.ForceMail : _forceMail;
            }
        }

        public HotspotManager HotspotManager { get { return _hotspotManager ?? (Parent != null ? Parent.HotspotManager : null); } }

        public VendorManager VendorManager
        {
            get
            {
                if (_vendorManager != null)
                {
                    return _vendorManager;
                }
                if (Parent != null)
                {
                    return Parent.VendorManager;
                }
                return _vendorManager = new VendorManager();
            }
        }

        public MailboxManager MailboxManager
        {
            get
            {
                if (_mailboxManager != null)
                {
                    return _mailboxManager;
                }
                if (Parent != null)
                {
                    return Parent.MailboxManager;
                }
                return _mailboxManager = new MailboxManager();
            }
        }

        public HashSet<uint> Factions
        {
            get
            {
                if (_factions.Count > 0)
                {
                    return _factions;
                }

                return Parent != null ? Parent.Factions : _factions;
            }
        }

        [Obsolete("Always empty; see GrindArea instead")]
        public HashSet<uint> MobIDs
        {
            get { return new HashSet<uint>(); }
        }

        public List<WoWItemQuality> MailQualities
        {
            get
            {
                var temp = new List<WoWItemQuality>();
                if (MailGrey)
                {
                    temp.Add(WoWItemQuality.Poor);
                }
                if (MailWhite)
                {
                    temp.Add(WoWItemQuality.Common);
                }
                if (MailGreen)
                {
                    temp.Add(WoWItemQuality.Uncommon);
                }
                if (MailBlue)
                {
                    temp.Add(WoWItemQuality.Rare);
                }
                if (MailPurple)
                {
                    temp.Add(WoWItemQuality.Epic);
                }

                return temp;
            }
        }

        public DualHashSet<uint, string> AvoidMobs
        {
            get
            {
                if (_avoidMobs.Any())
                {
                    return _avoidMobs;
                }

                return Parent != null ? Parent.AvoidMobs : _avoidMobs;
            }
            internal set { _avoidMobs = value ?? new DualHashSet<uint, string>(); }
        }

        public Dictionary<uint, BlacklistFlags> Blacklist
        {
            get
            {
                if (_blacklist.Count == _defaultBlacklist.Count)
                {
                    return _blacklist;
                }

                return Parent != null ? Parent.Blacklist : _blacklist;
            }
            internal set
            {
                _blacklist = value ?? new Dictionary<uint, BlacklistFlags>();
                foreach (KeyValuePair<uint, BlacklistFlags> entry in _defaultBlacklist)
                {
                    if (!_blacklist.ContainsKey(entry.Key))
                    {
                        _blacklist.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        _blacklist[entry.Key] = _blacklist[entry.Key] | entry.Value;
                    }
                }
            }
        }

        public List<Blackspot> Blackspots
        {
            get
            {
                if (_blackspots.Count > 0)
                {
                    return _blackspots;
                }

                return Parent != null ? Parent.Blackspots : _blackspots;
            }
        }

        public Dictionary<Tuple<uint, WoWFactionGroup>, List<Vector2[]>> AerialBlackspots
        {
            get
            {
                if (_aerialBlackspots.Count > 0)
                {
                    return _aerialBlackspots;
                }

                return Parent != null ? Parent.AerialBlackspots : _aerialBlackspots;
            }
        }

        public DualHashSet<string, uint> BlacklistedQuests
        {
            get
            {
                if (_blacklistedQuests.Count > 0)
                {
                    return _blacklistedQuests;
                }

                return Parent != null ? Parent.BlacklistedQuests : _blacklistedQuests;
            }
        }

        public DualHashSet<string, uint> BlacklistedQuestgivers
        {
            get
            {
                if (_blacklistedQuestGivers.Count > 0)
                {
                    return _blacklistedQuestGivers;
                }

                return Parent != null ? Parent.BlacklistedQuestgivers : _blacklistedQuestGivers;
            }
        }

        public List<QuestInfo> Quests
        {
            get
            {
                if (_quests.Count > 0)
                {
                    return _quests;
                }

                return Parent != null ? Parent.Quests : _quests;
            }
        }

        public OrderNodeCollection QuestOrder
        {
            get
            {
                if (_questOrder.Count > 0)
                {
                    return _questOrder;
                }

                return Parent != null ? Parent.QuestOrder : _questOrder;
            }
        }

        public List<Profile> SubProfiles { get; internal set; }
        public Profile Parent { get; private set; }

        #region IEquatable<Profile> Members

        public bool Equals(Profile other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return other._guid.Equals(_guid);
        }

        #endregion

        public static event EventHandler<UnknownProfileElementEventArgs> OnUnknownProfileElement;

        public QuestInfo FindQuest(uint id)
        {
            foreach (QuestInfo q in Quests)
            {
                if (q.ID == id)
                {
                    return q;
                }
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (!(obj is Profile))
            {
                return false;
            }
            return Equals((Profile)obj);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public static bool operator ==(Profile left, Profile right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Profile left, Profile right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns the root profile.
        /// </summary>
        /// <returns>The root profile. If this is the root profile, it returns itself.</returns>
        public Profile GetRootProfile()
        {
            Profile p = this;
            while (p.Parent != null)
            {
                p = p.Parent;
            }

            return p;
        }

        /// <summary>
        /// Gets all children profiles and children of children profiles, returning a list where the most inner profiles are first.
        /// </summary>
        /// <returns></returns>
        public List<Profile> GetScopeSortedProfiles()
        {
            var profiles = new List<ProfileScope>();
            GetScopeSortedProfiles(0, profiles);
            profiles.Sort((ps1, ps2) => ps2.Scope.CompareTo(ps1.Scope));

            List<Profile> tempProfiles = profiles.ConvertAll(ps => ps.Profile);
            return tempProfiles;
        }

        private void GetScopeSortedProfiles(int curScope, ICollection<ProfileScope> profiles)
        {
            profiles.Add(new ProfileScope { Scope = curScope, Profile = this });
            curScope++;
            if (SubProfiles != null)
            {
                for (int i = 0; i < SubProfiles.Count; i++)
                {
                    SubProfiles[i].GetScopeSortedProfiles(curScope, profiles);
                }
            }
        }

        internal static Dictionary<uint, BlacklistFlags> ParseBlacklist(XElement element)
        {
            var dictionary = new Dictionary<uint, BlacklistFlags>();
            foreach (XElement entry in element.Elements())
            {
                try
                {
                    XAttribute[] attributes = entry.Attributes().ToArray();
                    uint id = 0;
                    BlacklistFlags flags = BlacklistFlags.All;
                    foreach (XAttribute attribute in attributes)
                    {
                        try
                        {
                            switch (attribute.Name.ToString().ToLowerInvariant())
                            {
                                case "name":
                                    break;
                                case "id":
                                case "entry":
                                    if (!uint.TryParse(attribute.Value, out id))
                                    {
                                        throw new ProfileAttributeExpectedException<uint>(attribute);
                                    }
                                    break;
                                case "flags":
                                    if (!Enum.TryParse(attribute.Value, true, out flags))
                                    {
                                        throw new ProfileAttributeExpectedException(attribute, Enum.GetNames(typeof(BlacklistFlags)));
                                    }
                                    break;
                                default:
                                    throw new ProfileUnknownAttributeException(attribute);
                            }
                        }
                        catch (ProfileException ex)
                        {
                            Logging.Write(ex.Message);
                        }
                    }

                    if (!dictionary.ContainsKey(id))
                    {
                        dictionary.Add(id, flags);
                    }
                    else
                    {
                        dictionary[id] = dictionary[id] | flags;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.Write(ex.Message);
                }
            }

            return dictionary;
        }

        internal static DualHashSet<uint, string> ParseAvoidMobs(XElement element)
        {
            var dualHashSet = new DualHashSet<uint, string>();
            foreach (XElement avoid in element.Elements())
            {
                try
                {
                    string tag = avoid.Name.ToString().ToLowerInvariant();
                    if (tag != "avoid" && tag != "avoidmob" && tag != "mob")
                    {
                        throw new ProfileUnknownElementException(avoid, "Name", "Entry");
                    }

                    XAttribute[] attributes = avoid.Attributes().ToArray();
                    foreach (XAttribute attribute in attributes)
                    {
                        try
                        {
                            switch (attribute.Name.ToString().ToLowerInvariant())
                            {
                                case "name":
                                    string name = attribute.Value.ToLowerInvariant();
                                    if (!dualHashSet.Contains(name))
                                    {
                                        dualHashSet.Add(name);
                                    }
                                    break;
                                case "id":
                                case "entry":
                                    uint entry;
                                    if (!uint.TryParse(attribute.Value, out entry))
                                    {
                                        throw new ProfileAttributeExpectedException<uint>(attribute);
                                    }

                                    if (!dualHashSet.Contains(entry))
                                    {
                                        dualHashSet.Add(entry);
                                    }
                                    break;
                                default:
                                    throw new ProfileUnknownAttributeException(attribute);
                            }
                        }
                        catch (ProfileException ex)
                        {
                            Logging.Write(ex.Message);
                        }
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.Write(ex.Message);
                }
            }

            return dualHashSet;
        }

        #region Nested type: ProfileScope

        private struct ProfileScope
        {
            public Profile Profile;
            public int Scope;
        }

        #endregion
    }
}
