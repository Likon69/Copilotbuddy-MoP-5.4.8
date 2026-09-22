using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Styx.Common;

namespace Styx.CommonBot
{
    public class XmlFlightNode
    {
        public XmlFlightNode(string name, uint continent, WoWPoint location)
        {
            Name = name;
            Continent = continent;
            Location = location;
            Connections = new List<string>();
        }

        public XmlFlightNode(uint masterEntry, string name, uint continent, WoWPoint location)
            : this(name, continent, location)
        {
            MasterEntry = masterEntry;
        }

        public XmlFlightNode(XElement xml)
        {
            using (new ForcedCulture())
            {
                Name = xml.Attribute("name").Value;
                Continent = Convert.ToUInt32(xml.Attribute("continent").Value, CultureInfo.InvariantCulture);
                if (xml.Attribute("master") != null)
                    MasterEntry = Convert.ToUInt32(xml.Attribute("master").Value, CultureInfo.InvariantCulture);
                if (xml.Attribute("x") != null)
                    X = Convert.ToSingle(xml.Attribute("x").Value, CultureInfo.InvariantCulture);
                if (xml.Attribute("y") != null)
                    Y = Convert.ToSingle(xml.Attribute("y").Value, CultureInfo.InvariantCulture);
                if (xml.Attribute("z") != null)
                    Z = Convert.ToSingle(xml.Attribute("z").Value, CultureInfo.InvariantCulture);

                Connections = new List<string>();
                foreach (XElement connection in xml.Descendants("Connection"))
                    Connections.Add(connection.Attribute("name").Value);
            }
        }

        public List<string> Connections { get; set; }
        public uint Continent { get; set; }
        public uint MasterEntry { get; set; }
        public string Name { get; set; }

        public WoWPoint Location
        {
            get { return new WoWPoint(X, Y, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public void Connect(string to)
        {
            if (!Connections.Contains(to))
                Connections.Add(to);
        }

        public XElement ToXml()
        {
            var node = new XElement("Node",
                new XAttribute("name", Name),
                new XAttribute("continent", Continent));
            if (MasterEntry != 0U)
                node.Add(new XAttribute("master", MasterEntry));
            if (X != 0f)
                node.Add(new XAttribute("x", X));
            if (Y != 0f)
                node.Add(new XAttribute("y", Y));
            if (Z != 0f)
                node.Add(new XAttribute("z", Z));

            var connections = new XElement("Connections");
            foreach (string connection in Connections)
                connections.Add(new XElement("Connection", new XAttribute("name", connection)));
            node.Add(connections);
            return node;
        }
    }
}
