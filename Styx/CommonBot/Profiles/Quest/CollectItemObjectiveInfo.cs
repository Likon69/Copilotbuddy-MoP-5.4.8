using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest
{
    public class CollectItemObjectiveInfo : ObjectiveInfo
    {
        public CollectItemObjectiveInfo()
            : base(ObjectiveType.CollectItem)
        {
        }

        public CollectFromCollection OverridedCollectFrom { get; private set; }
        public HotspotCollection OverridedHotspots { get; private set; }
        public uint ItemID { get; private set; }
        public uint Count { get; private set; }
        public int TargetMaxLevel { get; private set; }
        public int TargetMinLevel { get; private set; }

        internal static new CollectItemObjectiveInfo FromXML(XElement element)
        {
            var objective = new CollectItemObjectiveInfo();

            foreach (XAttribute attribute in element.Attributes())
            {
                try
                {
                    switch (attribute.Name.ToString().ToLowerInvariant())
                    {
                        case "id":
                        case "entry":
                        case "itemid":
                        case "itementry":
                            uint id;
                            if (!uint.TryParse(attribute.Value, out id))
                                throw new ProfileAttributeExpectedException<uint>(attribute);

                            objective.ItemID = id;
                            break;
                        case "collectcount":
                        case "count":
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

            if (objective.ItemID == 0)
                throw new ProfileException(StyxResources.You_did_not_specify_the_ID_of_the_item_in_the_CollectItem_objective_);

            foreach (XElement subElement in element.Elements())
            {
                try
                {
                    switch (subElement.Name.ToString().ToLowerInvariant())
                    {
                        case "targetmaxlevel":
                        case "mobmaxlevel":
                            int targetMaxLevel;
                            if (!int.TryParse(subElement.Value, out targetMaxLevel))
                                throw new ProfileException("Couldn't parse integer for TargetMaxLevel in objective element: " + subElement);

                            objective.TargetMaxLevel = targetMaxLevel;
                            break;

                        case "targetminlevel":
                        case "mobminlevel":
                            int targetMinLevel;
                            if (!int.TryParse(subElement.Value, out targetMinLevel))
                                throw new ProfileException("Couldn't parse integer for TargetMinLevel in objective element: " + subElement);

                            objective.TargetMinLevel = targetMinLevel;
                            break;
                        case "collectfrom":
                            var collectFromCollection = CollectFromCollection.FromXML(subElement, "GameObject", "Mob", "Object", "Vendor");
                            if (collectFromCollection.Count > 0)
                                objective.OverridedCollectFrom = collectFromCollection;
                            break;
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
