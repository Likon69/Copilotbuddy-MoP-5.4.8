using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Bots.BGBuddy.Helpers;
using Bots.BGBuddy.Resources;
using Styx.CommonBot.Profiles;
using Tripper.Tools.Math;

namespace Bots.BGBuddy
{
    public enum BattlegroundSide
    {
        Horde,
        Alliance,

        Attack,
        Defend
    }

    public class BgBotProfile
    {
        [Conditional("DEBUG")]
        public static void Test()
        {
            BgBotProfile.Load("G:\\Code\\WoW\\Honorbuddy 2\\trunk\\Honorbuddy\\bin\\Debug\\Default Profiles\\BGBot2\\Eye of the Storm.xml");
        }
        public string Name { get; set; }
        public List<Blackspot> Blackspots { get; set; }

        public Dictionary<BattlegroundSide, List<MapBox>> Boxes { get; set; }
        public Dictionary<BattlegroundSide, Vector3> StartLocations { get; set; }
        public Dictionary<BattlegroundSide, Vector3> StartFaceLocations { get; set; }

        public BgBotProfile()
        {
            Blackspots = new List<Blackspot>();
            Boxes = new Dictionary<BattlegroundSide, List<MapBox>>();
        }

        public static BgBotProfile Load(string filePath)
        {
            var profile = new BgBotProfile();

            if (!File.Exists(filePath))
                return profile;

            var settings = new XmlReaderSettings
                               {
                                   IgnoreComments = true,
                                   IgnoreProcessingInstructions = true,
                                   IgnoreWhitespace = true
                               };
            XmlReader reader = XmlReader.Create(File.OpenRead(filePath), settings);
            var xmlRoot = XElement.Load(reader);


            profile.Name = xmlRoot.Element("Name").Value;

            profile.StartLocations = (from e in xmlRoot.Elements("StartLocation")
                                      let startLoc =
                                          new Vector3(
                                          float.Parse(e.Attribute("X").Value, CultureInfo.InvariantCulture),
                                          float.Parse(e.Attribute("Y").Value, CultureInfo.InvariantCulture),
                                          float.Parse(e.Attribute("Z").Value, CultureInfo.InvariantCulture))
                                      let side =
                                          (BattlegroundSide)
                                          Enum.Parse(typeof(BattlegroundSide), e.Attribute("Side").Value, true)
                                      select new KeyValuePair<BattlegroundSide, Vector3>(side, startLoc)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            profile.StartFaceLocations = (from e in xmlRoot.Elements("StartingFaceLocation")
                                      let startLoc =
                                          new Vector3(
                                          float.Parse(e.Attribute("X").Value, CultureInfo.InvariantCulture),
                                          float.Parse(e.Attribute("Y").Value, CultureInfo.InvariantCulture),
                                          float.Parse(e.Attribute("Z").Value, CultureInfo.InvariantCulture))
                                      let side =
                                          (BattlegroundSide)
                                          Enum.Parse(typeof(BattlegroundSide), e.Attribute("Side").Value, true)
                                      select new KeyValuePair<BattlegroundSide, Vector3>(side, startLoc)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if(xmlRoot.Element("Blackspots") != null)
                profile.Blackspots = (from e in xmlRoot.Element("Blackspots").Elements()
                                      select new Blackspot(e)).ToList();

            profile.Boxes = PartitionBoxes(xmlRoot);

            return profile;
        }

        static void settings_ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            Logger.Write(BGBuddyResources.BgBotProfile_settings_ValidationEventHandler_Error_loading_BGBuddy_profile____0___ + e.Message, e.Severity);
        }

        private static Dictionary<BattlegroundSide, List<MapBox>> PartitionBoxes(XElement root)
        {
            Dictionary<BattlegroundSide, List<MapBox>> ret = new Dictionary<BattlegroundSide, List<MapBox>>();

            foreach(var element in root.Elements("Boxes"))
            {
                BattlegroundSide side = (BattlegroundSide)Enum.Parse(typeof(BattlegroundSide), element.Attribute("Side").Value, true);
                if(!ret.ContainsKey(side))
                    ret.Add(side, new List<MapBox>());
                foreach(var ele in element.Elements("Box"))
                {
                    ret[side].Add(MapBox.FromXml(ele));
                }
            }


            return ret;
        }
    }
}
