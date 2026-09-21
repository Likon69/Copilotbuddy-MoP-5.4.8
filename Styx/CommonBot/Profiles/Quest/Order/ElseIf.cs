using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ElseIf
    {
        public ElseIf(Func<bool> condition, IEnumerable<OrderNode> body)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            Condition = condition;
            Body = body != null ? new OrderNodeCollection(body) : new OrderNodeCollection();
        }

        public Func<bool> Condition { get; private set; }
        public OrderNodeCollection Body { get; private set; }

        public static ElseIf FromXml(XElement element)
        {
            XAttribute conditionAttrib = ProfileHelper.FindAttribute(element, true, "condition");
            if (conditionAttrib == null)
                throw new ProfileMissingAttributeException("Condition", element);

            Func<bool> condition = ConditionHelper.ParseConditionString(conditionAttrib);
            if (condition == null)
                throw new ProfileException(element.GetError(string.Format(StyxResources.Could_not_parse__ElseIf___Condition_code___0___could_not_be_compiled_into_C__code_, conditionAttrib.Value)));

            var body = new List<OrderNode>();
            foreach (XElement node in element.Elements().Where(node => node.NodeType != XmlNodeType.Comment))
            {
                try
                {
                    body.Add(OrderNode.FromXml(node));
                }
                catch (ProfileException ex)
                {
                    if (GlobalSettings.Instance.ProfileDebuggingMode)
                        Logging.WriteException(ex);

                    throw new ProfileException(element.GetError(string.Format(StyxResources.ElseIf_FromXml_Could_not_parse__ElseIf__body_node___0_, ex.Message)), ex);
                }
            }

            return new ElseIf(condition, body);
        }
    }
}
