using System;
using System.Xml.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles.Quest.Order;

namespace Styx.CommonBot.Profiles
{
    public class Mailbox : IEquatable<Mailbox>
    {
        public WoWPoint Location { get; private set; }

        public NavType NavType { get; private set; }

        public string Name { get; private set; }

        public Func<bool> UsableWhen { get; private set; }

        public Mailbox(WoWPoint location, string name = null, NavType navType = NavType.Run)
        {
            Location = location;
            Name = name ?? "Mailbox";
            NavType = navType;
        }

        public Mailbox(XElement element)
        {
            Location = ProfileHelper.ExtractLocationFromElement(element, false);
            foreach (XAttribute attribute in element.Attributes())
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
                            throw new ProfileUnknownAttributeException(attribute, "NavType", "X", "Y", "Z");
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }
        }

        #region IEquatable<Mailbox> Members

        public bool Equals(Mailbox other)
        {
            return Location == other.Location;
        }

        #endregion

        public override string ToString()
        {
            return string.Format("[Mailbox Location: {0}]", Location);
        }
    }
}
