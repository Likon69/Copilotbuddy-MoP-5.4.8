using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest
{
    public class UseObjectObjectiveInfo : ObjectiveInfo
    {
        public UseObjectObjectiveInfo()
            : base(ObjectiveType.UseObject)
        {
        }

        public uint ObjectID { get; private set; }
        public uint Count { get; private set; }
        public HotspotCollection OverridedHotspots { get; private set; }

        public new static UseObjectObjectiveInfo FromXML(XElement element)
        {
            var objective = new UseObjectObjectiveInfo();

            foreach (XAttribute attribute in element.Attributes())
            {
                try
                {
                    switch (attribute.Name.ToString().ToLowerInvariant())
                    {
                        case "id":
                        case "entry":
                        case "gameobjectid":
                        case "objectid":
                        case "gameobjectentry":
                        case "objectentry":
                        case "usegameobject":
                        case "useobject":
                            uint id;
                            if (!uint.TryParse(attribute.Value, out id))
                                throw new ProfileAttributeExpectedException<uint>(attribute);

                            objective.ObjectID = id;
                            break;
                        case "count":
                        case "gameobjectcount":
                        case "objectcount":
                        case "usecount":
                            uint count;
                            if (!uint.TryParse(attribute.Value, out count))
                                throw new ProfileAttributeExpectedException<uint>(attribute);

                            objective.Count = count;
                            break;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            if (objective.ObjectID == 0)
                throw new ProfileException(StyxResources.You_did_not_specify_the_ID_of_the_game_object_in_the_UseObject_element_);

            foreach (XElement subElement in element.Elements())
            {
                try
                {
                    switch (subElement.Name.ToString().ToLowerInvariant())
                    {
                        case "hotspots":
                        case "spots":
                        case "locations":
                            if (objective.OverridedHotspots == null)
                                objective.OverridedHotspots = HotspotCollection.FromXML(subElement, "hotspot", "spot", "location");
                            else
                                objective.OverridedHotspots.AddRange(HotspotCollection.FromXML(subElement, "hotspot", "spot", "location"));
                            break;
                    }
                }
                catch (ProfileException ex)
                {
                    Logging.WriteDiagnostic(ex.Message);
                }
            }

            return objective;
        }
    }
}
