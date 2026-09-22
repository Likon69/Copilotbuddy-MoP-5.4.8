using System;
using System.Collections.Generic;
using Styx.WoWInternals;

namespace Styx.CommonBot.Frames
{
    public class TaxiFrame : Frame
    {
        public static readonly TaxiFrame Instance = new TaxiFrame();

        public TaxiFrame()
            : base("TaxiFrame")
        {
        }

        public override void Hide()
        {
            Lua.DoString("CloseTaxiMap()", "WoW.lua");
        }

        public uint NumNodes
        {
            get { return Lua.GetReturnVal<uint>("return NumTaxiNodes()", 0U); }
        }

        public List<TaxiFrameNode> Nodes
        {
            get
            {
                var nodes = new List<TaxiFrameNode>();
                using (StyxWoW.Memory.AcquireFrame())
                {
                    uint numNodes = NumNodes;
                    for (uint i = 1U; i <= numNodes; i += 1U)
                        nodes.Add(new TaxiFrameNode(i));
                }
                return nodes;
            }
        }

        public void Close()
        {
            Lua.DoString("CloseTaxiMap()", "WoW.lua");
        }

        public enum NodeType
        {
            Current,
            Reachable,
            None
        }

        public class TaxiFrameNode
        {
            private string _cachedName;
            private NodeType? _cachedType;

            internal TaxiFrameNode(uint slot)
            {
                Slot = slot;
            }

            public string Name
            {
                get
                {
                    if (string.IsNullOrEmpty(_cachedName))
                        _cachedName = Lua.GetReturnVal<string>("return TaxiNodeName(" + Slot + ")", 0U);
                    return _cachedName;
                }
            }

            public uint Slot { get; set; }

            public void TakeNode()
            {
                Lua.DoString("TakeTaxiNode(" + Slot + ")", "WoW.lua");
            }

            public NodeType Type
            {
                get
                {
                    if (!_cachedType.HasValue)
                    {
                        _cachedType = (NodeType)Enum.Parse(typeof(NodeType),
                            Lua.GetReturnVal<string>("return TaxiNodeGetType(" + Slot + ")", 0U), true);
                    }
                    return _cachedType.Value;
                }
            }

            public bool IsCurrent
            {
                get { return Type == NodeType.Current; }
            }

            public bool Reachable
            {
                get { return Type == NodeType.Reachable || IsCurrent; }
            }

            public override string ToString()
            {
                return string.Format("Slot: {0}, Name: {1}, Type: {2}", Slot, Name, Type);
            }
        }
    }
}
