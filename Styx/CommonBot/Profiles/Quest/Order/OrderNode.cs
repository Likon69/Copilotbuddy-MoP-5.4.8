using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public abstract class OrderNode
    {
        protected OrderNode(OrderNodeType type)
        {
            Type = type;
        }

        protected OrderNode(OrderNodeType type, XElement fromElement) : this(type)
        {
            Element = fromElement;
        }

        public OrderNodeType Type { get; private set; }

        public XElement Element { get; protected set; }

        public static OrderNode FromXml(XElement element)
        {
            OrderNodeType? type = ParseType(element.Name.ToString());
            if (!type.HasValue)
                throw new ProfileUnknownElementException(element);

            return _parsers[type.Value](element);
        }

        private static OrderNodeType? ParseType(string typeName)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "checkpoint":
                    return OrderNodeType.Checkpoint;
                case "pickup":
                case "pickupquest":
                    return OrderNodeType.PickUp;
                case "turnin":
                case "turninquest":
                case "handin":
                case "handinquest":
                    return OrderNodeType.TurnIn;
                case "objective":
                    return OrderNodeType.Objective;
                case "clearavoidmobs":
                    return OrderNodeType.ClearAvoidMobs;
                case "clearblacklist":
                    return OrderNodeType.ClearBlacklist;
                case "cleargrindarea":
                    return OrderNodeType.ClearGrindArea;
                case "clearmailbox":
                    return OrderNodeType.ClearMailbox;
                case "clearvendor":
                    return OrderNodeType.ClearVendor;
                case "setavoidmobs":
                    return OrderNodeType.SetAvoidMobs;
                case "setblacklist":
                    return OrderNodeType.SetBlacklist;
                case "setgrindarea":
                    return OrderNodeType.SetGrindArea;
                case "setmailbox":
                    return OrderNodeType.SetMailbox;
                case "setvendor":
                    return OrderNodeType.SetVendor;
                case "grindto":
                case "grinduntil":
                    return OrderNodeType.GrindTo;
                case "disablerepair":
                    return OrderNodeType.DisableRepair;
                case "enablerepair":
                    return OrderNodeType.EnableRepair;
                case "if":
                    return OrderNodeType.If;
                case "while":
                    return OrderNodeType.While;
                case "abandon":
                case "abandonquest":
                    return OrderNodeType.AbandonQuest;
                case "walkto":
                case "strollto":
                case "rideto":
                case "moveto":
                case "navigateto":
                case "move":
                case "navigate":
                case "runto":
                case "run":
                    return OrderNodeType.MoveTo;
                case "useitem":
                    return OrderNodeType.UseItem;
                case "code":
                case "customobjective":
                case "custombehavior":
                case "questbehavior":
                case "script":
                    return OrderNodeType.Code;
                case "enablebehavior":
                case "disablebehavior":
                    return OrderNodeType.ToggleBehavior;
                case "targetingdistance":
                    return OrderNodeType.SetTargetingDistance;
                case "usemount":
                    return OrderNodeType.SetUseMount;
                case "lootmobs":
                    return OrderNodeType.SetLootMobs;
                case "lootradius":
                    return OrderNodeType.SetLootRadius;
                case "navtype":
                case "setnavtype":
                    return OrderNodeType.SetNavType;
            }

            return null;
        }

        private static readonly Dictionary<OrderNodeType, Func<XElement, OrderNode>> _parsers = new Dictionary<OrderNodeType, Func<XElement, OrderNode>>
                                                                                    {
                                                                                        {OrderNodeType.Checkpoint, CheckpointNode.FromXml},
                                                                                        {OrderNodeType.PickUp, PickUpNode.FromXml},
                                                                                        {OrderNodeType.TurnIn, TurnInNode.FromXml},
                                                                                        {OrderNodeType.Objective, ObjectiveNode.FromXml},
                                                                                        {OrderNodeType.ClearAvoidMobs, ClearAvoidMobsNode.FromXml},
                                                                                        {OrderNodeType.ClearBlacklist, ClearBlacklistNode.FromXml},
                                                                                        {OrderNodeType.ClearGrindArea, ClearGrindAreaNode.FromXml},
                                                                                        {OrderNodeType.ClearMailbox, ClearMailboxNode.FromXml},
                                                                                        {OrderNodeType.ClearVendor, ClearVendorNode.FromXml},
                                                                                        {OrderNodeType.SetGrindArea, SetGrindAreaNode.FromXml},
                                                                                        {OrderNodeType.SetMailbox, SetMailboxNode.FromXml},
                                                                                        {OrderNodeType.SetVendor, SetVendorNode.FromXml},
                                                                                        {OrderNodeType.SetAvoidMobs, SetAvoidMobsNode.FromXml},
                                                                                        {OrderNodeType.SetBlacklist, SetBlacklistNode.FromXml},
                                                                                        {OrderNodeType.GrindTo, GrindToNode.FromXml},
                                                                                        {OrderNodeType.If, IfNode.FromXml},
                                                                                        {OrderNodeType.While, WhileNode.FromXml},
                                                                                        {OrderNodeType.AbandonQuest, AbandonQuestNode.FromXml},
                                                                                        {OrderNodeType.MoveTo, MoveToNode.FromXml},
                                                                                        {OrderNodeType.UseItem, UseItemNode.FromXml},
                                                                                        {OrderNodeType.Code, CodeNode.FromXml},
                                                                                        {OrderNodeType.EnableRepair, EnableRepairNode.FromXml},
                                                                                        {OrderNodeType.DisableRepair, DisableRepairNode.FromXml},
                                                                                        {OrderNodeType.ToggleBehavior, ToggleBehaviorNode.FromXml},
                                                                                        {OrderNodeType.SetTargetingDistance, SetTargetingDistanceNode.FromXml},
                                                                                        {OrderNodeType.SetUseMount, SetUseMountNode.FromXml},
                                                                                        {OrderNodeType.SetLootMobs, SetLootMobsNode.FromXml},
                                                                                        {OrderNodeType.SetLootRadius, SetLootRadiusNode.FromXml},
                                                                                        {OrderNodeType.SetNavType, SetNavTypeNode.FromXml}
                                                                                    };
    }
}
