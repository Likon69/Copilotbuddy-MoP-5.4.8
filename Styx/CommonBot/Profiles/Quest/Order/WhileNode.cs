using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class WhileNode : OrderNode, INodeContainer
    {
        public WhileNode(Func<bool> condition, IEnumerable<OrderNode> body) : base(OrderNodeType.While)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            Condition = condition;
            Body = body != null ? new OrderNodeCollection(body) : new OrderNodeCollection();
        }

        public Func<bool> Condition { get; private set; }
        public OrderNodeCollection Body { get; private set; }

        public override string ToString()
        {
            return "[WhileNode]";
        }

        public IEnumerable<OrderNode> GetNodes()
        {
            if (Body != null)
                return Body;

            return Enumerable.Empty<OrderNode>();
        }

        public static new WhileNode FromXml(XElement element)
        {
            XAttribute conditionAttrib = ProfileHelper.FindAttribute(element, true, "condition");
            if (conditionAttrib == null)
                throw new ProfileMissingAttributeException("condition", element);

            Func<bool> condition = ConditionHelper.ParseConditionString(conditionAttrib);
            if (condition == null)
                throw new ProfileException(string.Format("Could not parse 'While'. Condition code \"{0}\" could not be compiled into C# code.", conditionAttrib.Value));

            var body = new List<OrderNode>();
            foreach (XElement bodyNode in element.Elements().Where(bodyNode => bodyNode.NodeType != XmlNodeType.Comment))
            {
                try
                {
                    body.Add(OrderNode.FromXml(bodyNode));
                }
                catch (ProfileException ex)
                {
                    if (GlobalSettings.Instance.ProfileDebuggingMode)
                        Logging.WriteException(ex);

                    throw new ProfileException("Could not parse While body node", ex);
                }
            }

            return new WhileNode(condition, body);
        }
    }
}
