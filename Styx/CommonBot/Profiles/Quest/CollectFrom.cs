using System;
using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest
{
    public class CollectFrom
    {
        public uint ID;
        public string Name;
        public CollectFromType Type;

        public CollectFrom(uint id, string name, CollectFromType type)
        {
            ID = id;
            Name = name;
            Type = type;
        }

        public static CollectFrom FromXML(XElement element)
        {
            uint id = 0;
            string name = null;
            CollectFromType? type = null;
            foreach (XAttribute attrib in element.Attributes())
            {
                try
                {
                    switch (attrib.Name.ToString().ToLowerInvariant())
                    {
                        case "id":
                        case "entry":
                            if (!uint.TryParse(attrib.Value, out id))
                                throw new ProfileAttributeExpectedException<uint>(attrib);
                            break;
                        case "name":
                            name = attrib.Value;
                            break;
                        case "type":
                            type = ParseType(attrib.Value);
                            break;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            if (id == 0 && name == null)
            {
                throw new ProfileMissingAttributeException<uint>("Id", element);
            }
            if (!type.HasValue && !(type = ParseType(element.Name.ToString())).HasValue)
            {
                throw new ProfileException(StyxResources.Expected_object_type__Mob_or_GameObject_);
            }

            return new CollectFrom(id, name, type.Value);
        }

        private static CollectFromType? ParseType(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "mob":
                case "unit":
                case "npc":
                    return CollectFromType.Mob;
                case "gameobject":
                case "object":
                    return CollectFromType.GameObject;
                case "vendor":
                case "merchant":
                case "buy":
                    return CollectFromType.Vendor;
                default:
                    return null;
            }
        }
    }
}
