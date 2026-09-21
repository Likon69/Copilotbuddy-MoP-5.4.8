using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class Else
    {
        public Else(IEnumerable<OrderNode> body)
        {
            Body = body != null ? new OrderNodeCollection(body) : new OrderNodeCollection();
        }

        public OrderNodeCollection Body { get; private set; }

        public static Else FromXml(XElement element)
        {
            var list = new List<OrderNode>();
            foreach (XElement xelement in element.Elements().Where(e => e.NodeType != XmlNodeType.Comment))
            {
                try
                {
                    list.Add(OrderNode.FromXml(xelement));
                }
                catch (ProfileException ex)
                {
                    if (GlobalSettings.Instance.ProfileDebuggingMode)
                    {
                        Logging.WriteException(ex);
                    }
                    throw new ProfileException(element.GetError(StyxResources.Could_not_parse__Else__body_node_), ex);
                }
            }
            return new Else(list);
        }
    }
}
