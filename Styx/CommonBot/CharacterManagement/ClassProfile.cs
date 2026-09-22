using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Styx.CommonBot.CharacterManagement
{
    public class ClassProfile
    {
        public ClassProfile(string name, WoWClass @class, int specialization, WeightSet weightSet = null, TalentPlacementSet talents = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name must not be empty", "name");
            }
            if (weightSet == null)
            {
                weightSet = new WeightSet(new Dictionary<WeighableStatType, float>());
            }
            if (talents == null)
            {
                talents = new TalentPlacementSet();
            }
            this.Name = name;
            this.Class = @class;
            this.Specialization = specialization;
            this.WeightSet = weightSet;
            this.Talents = talents;
        }

        public string Name { get; private set; }

        public WoWClass Class { get; private set; }

        public int Specialization { get; private set; }

        public WeightSet WeightSet { get; private set; }

        public TalentPlacementSet Talents { get; private set; }

        public WoWSpec GetSpec()
        {
            return GetSpecFromIndex(this.Class, this.Specialization);
        }

        public override string ToString()
        {
            return string.Format("{0} - for {1}", this.Name, this.GetSpec());
        }

        private static WoWSpec GetSpecFromIndex(WoWClass wowClass, int index)
        {
            WoWSpec[] array;
            if (!DefaultSpecsByClass.TryGetValue(wowClass, out array))
            {
                return WoWSpec.None;
            }
            int num = index - 1;
            if (num >= 0 && num < array.Length)
            {
                return array[num];
            }
            return WoWSpec.None;
        }

        private static void ParseAttribute(string fileName, XElement element, string attributeName, Func<string, bool> parser)
        {
            XAttribute xattribute = element.Attribute(attributeName);
            if (xattribute == null)
            {
                throw new ClassProfileLoadException(fileName, string.Format("Could not find '{0}' attribute", attributeName), null);
            }
            if (!parser(xattribute.Value))
            {
                throw new ClassProfileLoadException(fileName, string.Format("Could not parse '{0}' attribute", attributeName), null);
            }
        }

        private static WeightSet ParseWeightSet(string fileName, XElement element)
        {
            Dictionary<WeighableStatType, float> dictionary = new Dictionary<WeighableStatType, float>();
            foreach (XElement xelement in element.Elements())
            {
                string localName = xelement.Name.LocalName;
                WeighableStatType weighableStatType;
                if (!Enum.TryParse<WeighableStatType>(localName, out weighableStatType))
                {
                    throw new ClassProfileLoadException(fileName, string.Format("'{0}' is not a valid stat", localName), null);
                }
                float num;
                if (!float.TryParse(xelement.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                {
                    throw new ClassProfileLoadException(fileName, string.Format("Weight for '{0}' could not be parsed", weighableStatType), null);
                }
                if (dictionary.ContainsKey(weighableStatType))
                {
                    throw new ClassProfileLoadException(fileName, string.Format("'{0}' defined twice", localName), null);
                }
                dictionary.Add(weighableStatType, num);
            }
            return new WeightSet(dictionary);
        }

        private static TalentPlacementSet ParseTalentTree(string fileName, XElement element)
        {
            List<TalentPlacement> list = new List<TalentPlacement>();
            foreach (XElement xelement in element.Elements("Talent"))
            {
                int tier = 0;
                ParseAttribute(fileName, xelement, "Tier", val => int.TryParse(val, out tier));
                int index = 0;
                ParseAttribute(fileName, xelement, "Index", val => int.TryParse(val, out index));
                string name = "";
                ParseAttribute(fileName, xelement, "Name", val => { name = val; return true; });
                list.Add(new TalentPlacement(tier - 1, index - 1, name));
            }
            TalentPlacementSet talentPlacementSet;
            try
            {
                talentPlacementSet = new TalentPlacementSet(list);
            }
            catch (ArgumentException ex)
            {
                throw new ClassProfileLoadException(fileName, ex.Message, null);
            }
            return talentPlacementSet;
        }

        public static ClassProfile Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Could not find file", filePath);
            }
            string fileName = Path.GetFileName(filePath);
            XDocument xdocument;
            try
            {
                xdocument = XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                throw new ClassProfileLoadException(fileName, ex.Message, ex);
            }
            XElement xelement = xdocument.Element("ClassProfile");
            if (xelement == null)
            {
                throw new ClassProfileLoadException(fileName, "No root 'ClassProfile' element found", null);
            }
            if (xelement.Attribute("Class") == null)
            {
                throw new ClassProfileLoadException(fileName, "No 'Class' attribute found in 'ClassProfile'", null);
            }

            WoWClass wowClass = WoWClass.None;
            ParseAttribute(fileName, xelement, "Class", val => Enum.TryParse<WoWClass>(val, out wowClass));
            int specIndex = 0;
            ParseAttribute(fileName, xelement, "Specialization", val => int.TryParse(val, out specIndex));
            if (GetSpecFromIndex(wowClass, specIndex) == WoWSpec.None)
            {
                throw new ClassProfileLoadException(fileName, "This specialization is unknown, please use a proper specialization index", null);
            }
            string name = "";
            ParseAttribute(fileName, xelement, "Name", val => { name = val; return true; });
            XElement xelement2 = xelement.Element("WeightSet");
            WeightSet weightSet = xelement2 != null ? ParseWeightSet(fileName, xelement2) : new WeightSet(new Dictionary<WeighableStatType, float>());
            XElement xelement3 = xelement.Element("TalentTree");
            TalentPlacementSet talentPlacementSet = xelement3 != null ? ParseTalentTree(fileName, xelement3) : new TalentPlacementSet();
            return new ClassProfile(name, wowClass, specIndex, weightSet, talentPlacementSet);
        }

        private static readonly Dictionary<WoWClass, WoWSpec[]> DefaultSpecsByClass = new Dictionary<WoWClass, WoWSpec[]>
        {
            { WoWClass.Warrior, new[] { WoWSpec.WarriorArms, WoWSpec.WarriorFury, WoWSpec.WarriorProtection } },
            { WoWClass.Paladin, new[] { WoWSpec.PaladinHoly, WoWSpec.PaladinProtection, WoWSpec.PaladinRetribution } },
            { WoWClass.Hunter, new[] { WoWSpec.HunterBeastMastery, WoWSpec.HunterMarksmanship, WoWSpec.HunterSurvival } },
            { WoWClass.Rogue, new[] { WoWSpec.RogueAssassination, WoWSpec.RogueCombat, WoWSpec.RogueSubtlety } },
            { WoWClass.Priest, new[] { WoWSpec.PriestDiscipline, WoWSpec.PriestHoly, WoWSpec.PriestShadow } },
            { WoWClass.DeathKnight, new[] { WoWSpec.DeathKnightBlood, WoWSpec.DeathKnightFrost, WoWSpec.DeathKnightUnholy } },
            { WoWClass.Shaman, new[] { WoWSpec.ShamanElemental, WoWSpec.ShamanEnhancement, WoWSpec.ShamanRestoration } },
            { WoWClass.Mage, new[] { WoWSpec.MageArcane, WoWSpec.MageFire, WoWSpec.MageFrost } },
            { WoWClass.Warlock, new[] { WoWSpec.WarlockAffliction, WoWSpec.WarlockDemonology, WoWSpec.WarlockDestruction } },
            { WoWClass.Druid, new[] { WoWSpec.DruidBalance, WoWSpec.DruidFeral, WoWSpec.DruidGuardian, WoWSpec.DruidRestoration } },
            { WoWClass.Monk, new[] { WoWSpec.MonkBrewmaster, WoWSpec.MonkMistweaver, WoWSpec.MonkWindwalker } }
        };
    }
}
