using System.Globalization;
using System.Xml.Linq;
using Styx.Helpers;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    /// <summary>
    /// A quest checkpoint is a node that does not modify behavior. It only tells HB points of which to start from.
    /// </summary>
    public class CheckpointNode : OrderNode
    {
        public CheckpointNode(float level) : base(OrderNodeType.Checkpoint)
        {
            Level = level;
        }

        public CheckpointNode(float level, XElement xml) : base(OrderNodeType.Checkpoint, xml)
        {
            Level = level;
        }

        public float Level { get; private set; }

        public static new CheckpointNode FromXml(XElement element)
        {
            XAttribute levelAttrib = ProfileHelper.FindAttribute(element, true, "level");
            if (levelAttrib != null)
            {
                float level;
                if (!float.TryParse(levelAttrib.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out level))
                {
                    throw new ProfileAttributeExpectedException<float>(levelAttrib);
                }

                return new CheckpointNode(level, element);
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format("[CheckpointNode Level: {0}]", Level.ToStringInvariant());
        }
    }
}
