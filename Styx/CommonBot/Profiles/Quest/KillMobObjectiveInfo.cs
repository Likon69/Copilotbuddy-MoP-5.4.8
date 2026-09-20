using System.Xml.Linq;
using Styx.Common;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest
{
    public class KillMobObjectiveInfo : ObjectiveInfo
    {
        public KillMobObjectiveInfo()
            : base(ObjectiveType.KillMob)
        {
        }

        public HotspotCollection OverridedHotspots { get; private set; }
        public uint MobID { get; private set; }
        public uint Count { get; private set; }
        public int TargetMaxLevel { get; private set; }
        public int TargetMinLevel { get; private set; }

        internal static new KillMobObjectiveInfo FromXML(XElement element)
        {
            var objective = new KillMobObjectiveInfo
            {
                TargetMaxLevel = int.MaxValue,
                TargetMinLevel = int.MinValue
            };

            foreach (XAttribute attribute in element.Attributes())
            {
                try
                {
                    switch (attribute.Name.ToString().ToLowerInvariant())
                    {
                        case "id":
                        case "entry":
                        case "mobid":
                        case "mobentry":
                            uint id;
                            if (!uint.TryParse(attribute.Value, out id))
                                throw new ProfileAttributeExpectedException<uint>(attribute);

                            objective.MobID = id;
                            break;
                        case "slaycount":
                        case "killcount":
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

            if (objective.MobID == 0)
                throw new ProfileException(StyxResources.You_did_not_specify_the_ID_of_the_mob_in_the_KillMob_objective_);

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
