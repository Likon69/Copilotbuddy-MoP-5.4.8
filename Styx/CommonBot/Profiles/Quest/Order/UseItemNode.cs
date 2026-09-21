using System;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.Helpers;

using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class UseItemNode : OrderNode
    {
        public UseItemNode(Func<WoWItem> itemRetriever, uint questId, WoWPoint location, NavType navType)
            : this(itemRetriever, null, questId, location, navType)
        {

        }

        public UseItemNode(Func<WoWItem> itemRetriever, Func<WoWObject> targetRetriever, uint questId, WoWPoint location, NavType navType)
            : this(itemRetriever, targetRetriever, UseItemTargetType.None, false, questId, location, navType)
        {

        }

        public UseItemNode(Func<WoWItem> itemRetriever, Func<WoWObject> targetRetriever, UseItemTargetType targetType, bool forceUse, uint questId, WoWPoint location, NavType navType)
            : base(OrderNodeType.UseItem)
        {
            if (itemRetriever == null)
                throw new ArgumentNullException("itemRetriever");

            ItemRetriever = itemRetriever;
            if (targetRetriever == null)
                targetRetriever = () => null;

            TargetRetriever = targetRetriever;
            ForceUse = forceUse;
            QuestId = questId;
            Location = location;
            TargetType = targetType;
            NavType = navType;
        }

        public Func<WoWItem> ItemRetriever { get; private set; }
        public Func<WoWObject> TargetRetriever { get; private set; }
        public bool ForceUse { get; private set; }
        public uint QuestId { get; private set; }
        public WoWPoint Location { get; private set; }
        public UseItemTargetType TargetType { get; private set; }
        public NavType NavType { get; private set; }

        public static new UseItemNode FromXml(XElement element)
        {
            uint targetId = 0;
            XAttribute targetIdAttrib = ProfileHelper.FindAttribute(element, true, "TargetId");
            if (targetIdAttrib != null)
            {
                if (!uint.TryParse(targetIdAttrib.Value, out targetId))
                    throw new ProfileAttributeExpectedException<int>(targetIdAttrib);
            }

            UseItemTargetType targetType = UseItemTargetType.None;
            XAttribute targetTypeAttrib = ProfileHelper.FindAttribute(element, true, "TargetType");
            if (targetTypeAttrib != null)
            {
                try
                {
                    targetType = (UseItemTargetType)Enum.Parse(typeof(UseItemTargetType), targetTypeAttrib.Value, true);
                }
                catch
                {
                    throw new ProfileAttributeExpectedException<UseItemTargetType>(targetTypeAttrib, "Npc", "Gameobject", "None");
                }
            }

            XAttribute questIdAttrib = ProfileHelper.FindAttribute(element, true, "QuestId");
            if (questIdAttrib == null)
                throw new ProfileMissingAttributeException<uint>("QuestId", element);

            uint questId;
            if (!uint.TryParse(questIdAttrib.Value, out questId))
                throw new ProfileAttributeExpectedException<int>(questIdAttrib);

            XAttribute itemIdAttrib = ProfileHelper.FindAttribute(element, true, "ItemId");
            if (itemIdAttrib == null)
                throw new ProfileMissingAttributeException<int>("ItemId", element);

            uint itemId;
            if (!uint.TryParse(itemIdAttrib.Value, out itemId))
                throw new ProfileAttributeExpectedException<int>(itemIdAttrib);

            XAttribute xAttrib = ProfileHelper.FindAttribute(element, true, "X");
            if (xAttrib == null)
                throw new ProfileMissingAttributeException<float>("X", element);

            XAttribute yAttrib = ProfileHelper.FindAttribute(element, true, "Y");
            if (yAttrib == null)
                throw new ProfileMissingAttributeException<float>("Y", element);

            XAttribute zAttrib = ProfileHelper.FindAttribute(element, true, "Z");
            if (zAttrib == null)
                throw new ProfileMissingAttributeException<float>("Z", element);

            float x, y, z;
            if (!float.TryParse(xAttrib.Value, out x))
                throw new ProfileAttributeExpectedException<float>(xAttrib);

            if (!float.TryParse(yAttrib.Value, out y))
                throw new ProfileAttributeExpectedException<float>(yAttrib);

            if (!float.TryParse(zAttrib.Value, out z))
                throw new ProfileAttributeExpectedException<float>(zAttrib);

            XAttribute navTypeAttrib = ProfileHelper.FindAttribute(element, true, "Nav", "NavType");
            NavType navType = NavType.Run;
            if (navTypeAttrib != null && !Enum.TryParse<NavType>(navTypeAttrib.Value, out navType))
                throw new ProfileAttributeExpectedException(navTypeAttrib, Enum.GetNames(typeof(NavType)));

            Func<WoWItem> itemRetriever = () => FindItemById(itemId);
            Func<WoWObject> targetRetriever = () => FindObjectById(targetId, targetType);

            return new UseItemNode(itemRetriever, targetRetriever, targetType, false, questId, new WoWPoint(x, y, z), navType);
        }

        private static WoWObject FindObjectById(uint entry, UseItemTargetType targetType)
        {
            WoWObject obj = null;
            switch (targetType)
            {
                case UseItemTargetType.Npc:
                    obj = ObjectManager.GetObjectsOfType<WoWUnit>(true).OrderBy(ret => ret.Distance).FirstOrDefault(ret => ret.Entry == entry);
                    break;

                case UseItemTargetType.Gameobject:
                    obj = ObjectManager.GetObjectsOfType<WoWGameObject>().OrderBy(ret => ret.Distance).FirstOrDefault(ret => ret.Entry == entry);
                    break;
            }

            if (obj == null && targetType != UseItemTargetType.None)
                Logging.Write(StyxResources.Could_not_find_target_to_use_item_on_with_ID__0___ID_provided__1_, entry);

            return obj;
        }

        private static WoWItem FindItemById(uint entry)
        {
            WoWItem obj = StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == entry);
            if (obj == null)
                Logging.Write(StyxResources.Could_not_find_item_to_use_by_ID__ID_provided___0_, entry);

            return obj;
        }

        public enum UseItemTargetType
        {
            None,
            Npc,
            Gameobject
        }
    }
}
