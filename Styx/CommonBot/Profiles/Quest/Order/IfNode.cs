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
    public class IfNode : OrderNode, INodeContainer
    {
        public IfNode(Func<bool> condition, IEnumerable<OrderNode> body)
            : base(OrderNodeType.If)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            Condition = condition;
            Body = body != null ? new OrderNodeCollection(body) : new OrderNodeCollection();
            ElseIfs = new List<ElseIf>();
        }

        public IfNode(Func<bool> condition, IEnumerable<OrderNode> body, IEnumerable<ElseIf> elseIfs)
            : this(condition, body)
        {
            if (elseIfs != null)
                ElseIfs.AddRange(elseIfs);
        }

        public IfNode(Func<bool> condition, IEnumerable<OrderNode> body, IEnumerable<ElseIf> elseIfs, Else @else, string conditionText = null)
            : this(condition, body, elseIfs)
        {
            Else = @else;
            ConditionText = conditionText;
        }

        public string ConditionText { get; private set; }

        public IfNode(Func<bool> condition, IEnumerable<OrderNode> body, Else @else)
            : this(condition, body, null, @else, null)
        {
        }

        public Func<bool> Condition { get; private set; }
        public OrderNodeCollection Body { get; private set; }
        public List<ElseIf> ElseIfs { get; private set; }
        public Else Else { get; private set; }

        public static new IfNode FromXml(XElement element)
        {
            XAttribute conditionAttrib = ProfileHelper.FindAttribute(element, true, "condition");
            if (conditionAttrib == null)
                throw new ProfileMissingAttributeException("condition", element);

            Func<bool> condition = ConditionHelper.ParseConditionString(conditionAttrib);
            if (condition == null)
                throw new ProfileException(element.GetError(string.Format(StyxResources.Could_not_parse__If___Condition_code___0___could_not_be_compiled_into_C__code_, conditionAttrib.Value)));

            var elseIfs = new List<ElseIf>();
            foreach (XElement elseIf in ProfileHelper.FindElements(element, true, "elseif"))
            {
                elseIfs.Add(ElseIf.FromXml(elseIf));
            }

            Else @else = null;
            XElement elseElem = ProfileHelper.FindElement(element, true, "else");
            if (elseElem != null)
            {
                @else = Else.FromXml(elseElem);
            }

            var body = new List<OrderNode>();
            foreach (XElement bodyNode in element.Elements().Where(bodyNode => bodyNode.NodeType != XmlNodeType.Comment))
            {
                switch (bodyNode.Name.ToString().ToLowerInvariant())
                {
                    case "else":
                    case "elseif":
                        continue;
                }

                try
                {
                    body.Add(OrderNode.FromXml(bodyNode));
                }
                catch (ProfileException ex)
                {
                    if (GlobalSettings.Instance.ProfileDebuggingMode)
                        Logging.WriteException(ex);

                    throw new ProfileException(element.GetError(string.Format(StyxResources.Could_not_parse__If__body_node_)), ex);
                }
            }

            return new IfNode(condition, body, elseIfs, @else, conditionAttrib.Value);
        }

        public IEnumerable<OrderNode> GetNodes()
        {
            if (Body != null)
                foreach (OrderNode node in Body)
                    yield return node;

            if (ElseIfs != null)
                foreach (ElseIf elseIf in ElseIfs)
                    if (elseIf.Body != null)
                        foreach (OrderNode node in elseIf.Body)
                            yield return node;

            if (Else != null)
                if (Else.Body != null)
                    foreach (OrderNode node in Else.Body)
                        yield return node;
        }
    }
}
