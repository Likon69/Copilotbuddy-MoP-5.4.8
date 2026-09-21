using System.Collections.Generic;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    // TODO: Convert to use Styx.Helpers.IndexedList and check usages
    public class OrderNodeCollection : List<OrderNode>, INodeContainer
    {
        public OrderNodeCollection()
        {
        }

        public OrderNodeCollection(int capacity) : base(capacity)
        {
        }

        public OrderNodeCollection(IEnumerable<OrderNode> collection) : base(collection)
        {
        }

        public bool IgnoreCheckpoints { get; set; }

        public static OrderNodeCollection FromXml(XElement element)
        {
            var ret = new OrderNodeCollection();

            XAttribute ignoreCheckpointsAttrib = ProfileHelper.FindAttribute(element, true, "IgnoreCheckpoints");
            if (ignoreCheckpointsAttrib != null)
            {
                bool val;
                if (!bool.TryParse(ignoreCheckpointsAttrib.Value, out val))
                    val = false;

                ret.IgnoreCheckpoints = val;
            }

            foreach (XElement orderNode in element.Elements())
            {
                try
                {
                    ret.Add(OrderNode.FromXml(orderNode));
                }
                catch (ProfileException ex)
                {
                    if (GlobalSettings.Instance.ProfileDebuggingMode)
                        Logging.WriteException(ex);

                    if (!string.IsNullOrEmpty(ex.Message))
                        Logging.Write(StyxResources.Warning___0_, ex.Message);
                }
            }

            return ret;
        }

        public IEnumerable<OrderNode> GetNodes()
        {
            return this;
        }
    }
}
