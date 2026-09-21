using System;
using System.Globalization;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles
{
    public struct Blackspot : IEquatable<Blackspot>
    {
        public float Height { get; private set; }
        public WoWPoint Location { get; private set; }
        public float Radius { get; private set; }
        public string Name { get; private set; }
        public int MapID { get; private set; }

        public Blackspot(WoWPoint location, float radius, float height, string name = "", int mapID = -1)
            : this()
        {
            Location = location;
            Radius = radius;
            Height = height;
            Name = string.IsNullOrWhiteSpace(name) ? "" : name;
            MapID = mapID;
        }

        public Blackspot(XElement element)
            : this(
                new WoWPoint(
                    float.Parse(element.Attribute("X").Value, CultureInfo.InvariantCulture),
                    float.Parse(element.Attribute("Y").Value, CultureInfo.InvariantCulture),
                    float.Parse(element.Attribute("Z").Value, CultureInfo.InvariantCulture)),
                float.Parse(element.Attribute("Radius").Value, CultureInfo.InvariantCulture),
                element.Attribute("Height") != null ? float.Parse(element.Attribute("Height").Value, CultureInfo.InvariantCulture) : 5f,
                element.Attribute("Name") != null ? element.Attribute("Name").Value : "",
                element.Attribute("MapID") != null ? int.Parse(element.Attribute("MapID").Value) : -1)
        {
        }

        public bool Equals(Blackspot other)
        {
            return Height.Equals(other.Height) && Location.Equals(other.Location) && Radius.Equals(other.Radius) && string.Equals(Name, other.Name) && MapID == other.MapID;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is Blackspot && Equals((Blackspot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Height.GetHashCode();
                result = (result * 397) ^ Location.GetHashCode();
                result = (result * 397) ^ Radius.GetHashCode();
                result = (result * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                return (result * 397) ^ MapID;
            }
        }
    }
}
