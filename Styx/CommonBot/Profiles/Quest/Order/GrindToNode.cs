using System;
using System.Globalization;
using System.Xml.Linq;
using Styx.Resources;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class GrindToNode : OrderNode
    {
        public GrindToNode(float level, Func<bool> condition, NavType navType)
            : base(OrderNodeType.GrindTo)
        {
            Level = level;
            Condition = condition;
            NavType = navType;
        }

        public GrindToNode(float level, NavType navType) : this(level, null, navType)
        {
        }

        public float Level { get; private set; }
        public Func<bool> Condition { get; private set; }
        public string GoalText { get; private set; }
        public NavType NavType { get; private set; }

        public override string ToString()
        {
            return string.Format("[GrindToNode Level: {0} GoalText: {1}]", Level, GoalText);
        }

        public static new GrindToNode FromXml(XElement element)
        {
            XAttribute goalTextAttrib = ProfileHelper.FindAttribute(element, true, "goaltext");
            string goalText = goalTextAttrib != null ? goalTextAttrib.Value : "";

            XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
            NavType navType = NavType.Run;
            if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

            XAttribute conditionAttrib = ProfileHelper.FindAttribute(element, true, "condition");
            if (conditionAttrib != null)
            {
                Func<bool> condition = ConditionHelper.ParseConditionString(conditionAttrib);
                if (condition == null)
                    throw new ProfileException(string.Format(StyxResources.Could_not_parse__If___Condition_code___0___could_not_be_compiled_into_C__code_, conditionAttrib.Value));

                return new GrindToNode(-1, condition, navType) { GoalText = goalText };
            }

            XAttribute levelAttrib = ProfileHelper.FindAttribute(element, true, "level");
            if (levelAttrib != null)
            {
                float level;
                if (!float.TryParse(levelAttrib.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out level))
                    throw new ProfileAttributeExpectedException<float>(levelAttrib);

                return new GrindToNode(level, navType) { GoalText = goalText };
            }

            if (levelAttrib == null && conditionAttrib == null)
                throw new ProfileException("You need atleast one level or condition attribute in GrindToNode!");

            return null;
        }
    }
}
