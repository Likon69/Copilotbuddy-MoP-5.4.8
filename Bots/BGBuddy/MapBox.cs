using System;
using System.Globalization;
using System.Xml.Linq;

using Tripper.Tools.Math;

namespace Bots.BGBuddy
{

    /// <summary>
    ///   Map box.
    /// </summary>
    /// <remarks>
    ///   Created 12/19/2010.
    /// </remarks>
    public struct MapBox
    {
        /// <summary>
        ///   The bottom right
        /// </summary>
        public Vector3 BottomRight;

        /// <summary>
        ///   The top left
        /// </summary>
        public Vector3 TopLeft;
        public string Name { get; set; }
        public Vector3 Center { get; set; }

        public bool Contains(Vector2 point)
        {
            if (point.Equals(new Vector2(Center.X, Center.Y)))
                return true;

            var bb = BoundingBox2.CreateFromPoints(new[] { new Vector2(TopLeft.X, TopLeft.Y), new Vector2(BottomRight.X, BottomRight.Y) });
            return bb.Contains(point);
        }

        internal static MapBox FromXml(XElement xml)
        {
            MapBox ret = new MapBox();
            var tl = xml.Element("TopLeft");
            var br = xml.Element("BottomRight");
            var cent = xml.Element("Center");

            ret.TopLeft = new Vector3(
                Convert.ToSingle(tl.Attribute("X").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(tl.Attribute("Y").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(tl.Attribute("Z").Value, CultureInfo.InvariantCulture));
            ret.BottomRight = new Vector3(
                Convert.ToSingle(br.Attribute("X").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(br.Attribute("Y").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(br.Attribute("Z").Value, CultureInfo.InvariantCulture));
            ret.Center = new Vector3(
                Convert.ToSingle(cent.Attribute("X").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(cent.Attribute("Y").Value, CultureInfo.InvariantCulture),
                Convert.ToSingle(cent.Attribute("Z").Value, CultureInfo.InvariantCulture));

            ret.Name = xml.Attribute("Name").Value;

            return ret;
        }

        internal static MapBox fromBox(Vector3 topLeft, Vector3 bottomRight, Vector3 center, string name)
        {
            MapBox ret = new MapBox();
            ret.TopLeft = topLeft;
            ret.BottomRight = bottomRight;
            ret.Center = center;
            ret.Name = name;

            return ret;
        }
    }
}
