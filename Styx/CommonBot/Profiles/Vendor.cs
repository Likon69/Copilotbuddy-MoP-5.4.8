using System;
using System.Xml.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Profiles
{
    public class Vendor : IEquatable<Vendor>
    {
        #region Nested type: VendorType

        public enum VendorType
        {
            Unknown,
            Repair,
            Food,
            Sell,
            Restock,
            Train,
            FlightMaster,
            InnKeeper,
        }

        #endregion

        public Vendor(int entry, string name, VendorType type, WoWPoint location, NavType navType = NavType.Run)
        {
            Name = name;
            Entry = entry;
            Type = type;
            Location = location;
            NavType = navType;
        }

        public Vendor(XElement xml)
        {
            Location = ProfileHelper.ExtractLocationFromElement(xml, false);
            Type = VendorType.Unknown;
            Name = string.Empty;
            TrainClass = WoWClass.None;
            foreach (XAttribute attribute in xml.Attributes())
            {
                try
                {
                    switch (attribute.Name.ToString().ToLowerInvariant())
                    {
                        case "usablewhen":
                            UsableWhen = ConditionHelper.ParseConditionString(attribute);
                            if (UsableWhen == null)
                            {
                                throw new ProfileException("Could not parse UsableWhen condition. " + attribute.Value);
                            }
                            break;
                        case "name":
                            Name = attribute.Value;
                            break;
                        case "id":
                        case "entry":
                            int entry;
                            if (!int.TryParse(attribute.Value, out entry))
                            {
                                throw new ProfileAttributeExpectedException<int>(attribute);
                            }

                            Entry = entry;

                            break;
                        case "id2":
                        case "entry2":
                            int entry2;
                            if (!int.TryParse(attribute.Value, out entry2))
                            {
                                throw new ProfileAttributeExpectedException<int>(attribute);
                            }

                            Entry2 = entry2;
                            break;
                        case "type":
                            try
                            {
                                Type = (VendorType)Enum.Parse(typeof(VendorType), attribute.Value);
                            }
                            catch (ArgumentException)
                            {
                                throw new ProfileAttributeExpectedException<VendorType>(attribute, "Repair", "Food", "Sell", "Restock", "Train");
                            }
                            break;
                        case "trainclass":
                            try
                            {
                                TrainClass = (WoWClass)Enum.Parse(typeof(WoWClass), attribute.Value);
                            }
                            catch (ArgumentException)
                            {
                                throw new ProfileAttributeExpectedException<WoWClass>(attribute, "Deathknight", "Druid", "Hunter", "Mage", "Monk", "Paladin", "Priest", "Rogue", "Shaman", "Warlock", "Warrior");
                            }
                            break;
                        case "nav":
                        case "navtype":
                            try
                            {
                                NavType = (NavType)Enum.Parse(typeof(NavType), attribute.Value);
                            }
                            catch (ArgumentException)
                            {
                                throw new ProfileAttributeExpectedException(attribute, Enum.GetNames(typeof(NavType)));
                            }
                            break;
                        case "x":
                        case "y":
                        case "z":
                            break;
                        default:
                            throw new ProfileUnknownAttributeException(attribute, "Name", "Entry", "Type", "X", "Y", "Z", "TrainClass", "NavType");
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }
        }

        public Vendor(WoWObject unit, VendorType type)
        {
            Name = unit.Name;
            Entry = (int)unit.Entry;
            Type = type;
            Location = unit.Location;
        }

        public string Name { get; private set; }
        public int Entry { get; private set; }

        public int? Entry2 { get; private set; }
        public VendorType Type { get; private set; }
        public WoWPoint Location { get; set; }
        public WoWClass TrainClass { get; set; }
        public NavType NavType { get; set; }
        public Func<bool> UsableWhen { get; set; }

        #region IEquatable<Vendor> Members

        public bool Equals(Vendor other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Entry == other.Entry && Location == other.Location && Name == other.Name && Type == other.Type;
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[Vendor Name: {0}, Entry: {1}, Type: {2}, Location: {3}, TrainClass: {4}]",
                                 Name,
                                 Entry,
                                 Type,
                                 Location,
                                 TrainClass
                );
        }
    }
}
