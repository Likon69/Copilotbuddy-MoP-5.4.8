using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class CodeNode : OrderNode
    {
        public CodeNode(string path, Type behaviorType, Dictionary<string, string> args, XElement element)
            : base(OrderNodeType.Code, element)
        {
            Path = path;
            BehaviorType = behaviorType;
            Arguments = args;
        }

        public string Path { get; private set; }

        public Dictionary<string, string> Arguments { get; private set; }

        public Type BehaviorType { get; private set; }

        public static new CodeNode FromXml(XElement element)
        {
            XAttribute fileAttrib = ProfileHelper.FindAttribute(element, true, "file");
            if (fileAttrib == null)
                throw new ProfileMissingAttributeException<string>("File", element);

            List<XAttribute> attributes = element.Attributes()
                .Where(attrib => attrib != null && attrib.Name.ToString().ToLowerInvariant() != "file")
                .ToList();

            Dictionary<string, string> args = new Dictionary<string, string>();
            foreach (XAttribute attrib in attributes)
            {
                string name = attrib.Name.ToString();
                if (!args.ContainsKey(attrib.Name.ToString()))
                    args.Add(name, attrib.Value);
            }

            if (args.Keys.Contains("Url"))
            {
                Type type = QuestBehaviorHelper.GetBehaviorInstanceType(fileAttrib.Value);
                string value = args.FirstOrDefault(ret => ret.Key == "Url").Value;
                XAttribute urlAttrib = ProfileHelper.FindAttribute(element, true, "Url");
                type = QuestBehaviorHelper.GetBehaviorUrlInstanceType(fileAttrib.Value, urlAttrib.Value);
                return new CodeNode(fileAttrib.Value, type, args, element);
            }

            Type behaviorInstanceType = QuestBehaviorHelper.GetBehaviorInstanceType(fileAttrib.Value);
            if (behaviorInstanceType == null)
                throw new ProfileException();

            return new CodeNode(fileAttrib.Value, behaviorInstanceType, args, element);
        }

        public override string ToString()
        {
            return "CodeNode: " + Path + " - " + Element;
        }
    }
}
