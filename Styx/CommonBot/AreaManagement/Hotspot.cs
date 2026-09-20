using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.Resources;

namespace Styx.CommonBot.AreaManagement
{
    public class Hotspot
    {
        public WoWPoint Position { get; set; }
        public string Name { get; set; }

        public Hotspot(XElement element)
        {
            try
            {
                try
                {
                    float? x = null;
                    float? y = null;
                    float? z = null;

                    XAttribute[] attributes = element.Attributes().ToArray();

                    foreach (XAttribute attribute in attributes)
                    {
                        try
                        {
                            switch (attribute.Name.ToString().ToLowerInvariant())
                            {
                                case "x":
                                    float tempX;
                                    if (!float.TryParse(attribute.Value, out tempX))
                                    {
                                        throw new ProfileAttributeExpectedException<float>(attribute);
                                    }

                                    x = tempX;
                                    break;
                                case "y":
                                    float tempY;
                                    if (!float.TryParse(attribute.Value, out tempY))
                                    {
                                        throw new ProfileAttributeExpectedException<float>(attribute);
                                    }

                                    y = tempY;
                                    break;
                                case "z":
                                    if (!x.HasValue || !y.HasValue)
                                        throw new ProfileException(StyxResources.You_have_placed_the__Z__attribute_before_the__X__and__Y__attribute_);

                                    float tempZ;
                                    if (!ProfileHelper.ParseHeightString(x.Value, y.Value, attribute.Value, out tempZ))
                                    {
                                        throw new ProfileAttributeExpectedException<float>(attribute);
                                    }

                                    z = tempZ;
                                    break;
                                case "name":
                                    Name = attribute.Value;
                                    break;
                                default:
                                    throw new ProfileUnknownAttributeException(attribute, "X", "Y", "Z", "Name");
                            }
                        }
                        catch (ProfileException ex)
                        {
                            Logging.WriteException(ex);
                        }
                    }

                    if (x.HasValue && y.HasValue && z.HasValue)
                    {
                        Position = new WoWPoint(x.Value, y.Value, z.Value);
                    }
                    else
                        throw new ProfileMissingAttributeException<float>(x.HasValue ? (y.HasValue ? "Z" : "Y") : "X",
                                                                          element);
                }
                catch (ProfileException ex)
                {
                    Logging.WriteException(ex);
                }
            }
            catch (ProfileException ex)
            {
                Logging.WriteException(ex);
            }
        }

        public Hotspot()
        {
            Position = WoWPoint.Empty;
        }

        public Hotspot(float x, float y, float z)
        {
            Position = new WoWPoint(x, y, z);
        }

        public static implicit operator WoWPoint(Hotspot hotspot)
        {
            return hotspot.Position;
        }

        public static implicit operator Hotspot(WoWPoint hotspot)
        {
            return new Hotspot(hotspot.X, hotspot.Y, hotspot.Z);
        }

        public WoWPoint ToWoWPoint()
        {
            return this;
        }

        public XElement ToXML()
        {
            var element = new XElement("Hotspot",
                new XAttribute("X", Position.X),
                new XAttribute("Y", Position.Y),
                new XAttribute("Z", Position.Z));

            if (Name != null)
                element.Add(new XAttribute("Name", Name));

            return element;
        }

        public override string ToString()
        {
            return Position.ToString();
        }
    }
}
