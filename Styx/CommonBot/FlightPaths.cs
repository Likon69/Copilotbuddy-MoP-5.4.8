using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.Pathing;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    /// <summary>
    /// Flight paths.
    /// </summary>
    public static class FlightPaths
    {
        // 5.4.8's IsIgnored/IgnoreFp + DualHashSet mechanism replaces 5.3's Blacklist-based ignore list.
        // Field name kept from 5.3 (including its "Ingore" typo) since 5.3 is the naming authority.
        private static HashSet<uint> _hordeIngoreFPs = new HashSet<uint> { 12617U };

        private static HashSet<uint> _allianceIgnoreFPs = new HashSet<uint> { 12636U, 18788U };

        // New in 5.4.8 (was dualHashSet_0). No 5.3 counterpart exists; named to match the style of its
        // neighbours (_hordeIngoreFPs / _fpWaitTimer).
        private static DualHashSet<uint, WoWPoint> _ignoredFlightPaths = new DualHashSet<uint, WoWPoint>();

        private static WaitTimer _fpWaitTimer = new WaitTimer(TimeSpan.FromSeconds(15.0));

        public static bool IsIgnored(uint entry)
        {
            if (_ignoredFlightPaths.Contains(entry))
            {
                return true;
            }
            if (StyxWoW.Me.IsHorde)
            {
                return _hordeIngoreFPs.Contains(entry);
            }
            return _allianceIgnoreFPs.Contains(entry);
        }

        public static bool IsIgnored(WoWPoint location)
        {
            return _ignoredFlightPaths.Contains(location);
        }

        public static bool IgnoreFp(uint entry)
        {
            return _ignoredFlightPaths.Add(entry);
        }

        public static bool IgnoreFp(WoWPoint location)
        {
            return _ignoredFlightPaths.Add(location);
        }

        /// <summary>Gets the nearest flight merchant.</summary>
        /// <value>The nearest flight merchant.</value>
        public static WoWUnit NearestFlightMerchant
        {
            get
            {
                WoWUnit nearestFlightPathNpc = (from u in ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                                                 where u.IsFlightMaster && !u.IsHostile && !u.IsDead && !IsIgnored(u.Entry)
                                                 orderby u.Distance
                                                 select u).FirstOrDefault();
                if (nearestFlightPathNpc != null)
                {
                    if (nearestFlightPathNpc.FactionId == 934U)
                    {
                        if (StyxWoW.Me.GetReputationLevelWith(932U) >= WoWUnitReaction.Friendly)
                        {
                            IgnoreFp(nearestFlightPathNpc.Entry);
                            return null;
                        }
                    }
                    else if (nearestFlightPathNpc.FactionId == 932U && StyxWoW.Me.GetReputationLevelWith(934U) >= WoWUnitReaction.Friendly)
                    {
                        IgnoreFp(nearestFlightPathNpc.Entry);
                        return null;
                    }
                    if (!StyxWoW.Me.IsFalling && !Navigator.CanNavigateFully(StyxWoW.Me.Location, nearestFlightPathNpc.Location))
                    {
                        Logging.Write("Ignoring flight master {0} since we can't fully navigate to it.", new object[] { nearestFlightPathNpc.Name });
                        IgnoreFp(nearestFlightPathNpc.Entry);
                        return null;
                    }
                }
                return nearestFlightPathNpc;
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether the need flight path.
        /// </summary>
        public static bool NeedFlightPath { get; set; }

        /// <summary>
        ///   Gets or sets the reason.
        /// </summary>
        public static FlightPathReason Reason { get; set; }

        /// <summary>
        ///   Gets or sets the xml nodes.
        /// </summary>
        public static List<XmlFlightNode> XmlNodes { get; set; }

        public static XmlFlightNode TakingPathTo { get; set; }

        public static XmlFlightNode TakingPathFrom { get; set; }

        public static bool CanTakeFlightPaths
        {
            get
            {
                return StyxWoW.Me.IsAlive && !StyxWoW.Me.IsOnTransport;
            }
        }

        public static bool IsAtStart
        {
            get
            {
                return TakingPathFrom != null && StyxWoW.Me.Location.DistanceSqr(TakingPathFrom.Location) < 25f;
            }
        }

        public static bool IsAtEnd
        {
            get
            {
                return TakingPathTo != null && StyxWoW.Me.Location.DistanceSqr(TakingPathTo.Location) < 25f;
            }
        }

        /// <summary>
        ///   Gets the full pathname of the legacy (5.3-style) xml file.
        /// </summary>
        private static string OldXmlPath
        {
            get
            {
                return Path.Combine(Utilities.AssemblyDirectory, string.Format("Settings\\FlightPaths_{0}.xml", StyxWoW.Me.Name));
            }
        }

        /// <summary>
        ///   Gets the full pathname of the xml file.
        /// </summary>
        private static string XmlPath
        {
            get
            {
                return Path.Combine(Utilities.AssemblyDirectory, string.Format("Settings\\{0}\\{1}\\FlightPaths.xml", StyxWoW.Me.RealmName, StyxWoW.Me.Name));
            }
        }

        public static void Reset()
        {
            TakingPathFrom = null;
            TakingPathTo = null;
            Reason = FlightPathReason.None;
            // Note: unlike 5.3 (which always cleared the Navigator), 5.4.8 only clears it when the
            // current POI is still the flight POI. Ported as-is.
            if (BotPoi.Current.Type == PoiType.Fly)
            {
                Navigator.Clear();
                BotPoi.Clear("FlightPaths.Reset()");
            }
        }

        public static bool SetFlightPathUsage(WoWPoint from, WoWPoint to, out WoWPoint startFp, out WoWPoint endFp)
        {
            if (!CanTakeFlightPaths)
            {
                startFp = endFp = WoWPoint.Empty;
                return false;
            }

            XmlFlightNode startNode;
            XmlFlightNode endNode;
            GetClosestFlightPathNodes(from, to, out startNode, out endNode);

            // Note: 5.4.8 dropped 5.3's null-safety here (no check that startNode/endNode are non-null
            // before dereferencing .Location below, and no fallback to FlightPathReason.Update). If
            // GetClosestFlightPathNodes() returns nulls, this throws a NullReferenceException, exactly
            // as the decompiled 5.4.8 IL does. Ported as-is (not a bug we're allowed to fix).
            TakingPathTo = endNode;
            TakingPathFrom = startNode;
            Reason = FlightPathReason.Use;
            SetPoi(startNode);

            startFp = startNode.Location;
            endFp = endNode.Location;
            return true;
        }

        public static void TakeFlightPath()
        {
            if (!CanTakeFlightPaths)
            {
                return;
            }

            HandleTaxiMapOpened(null, null);
        }

        /// <summary>
        /// Returns the time required to take a flight path
        /// </summary>
        public static TimeSpan GetFlightPathTime(WoWPoint from, WoWPoint to)
        {
            float flightDistance = from.Distance2D(to);
            flightDistance *= 1.2f;
            float flightSpeed = 19.6f;
            float flightTime = flightDistance / flightSpeed;
            return new TimeSpan(0, 0, (int)Math.Ceiling((double)flightTime));
        }

        /// <summary> Gets a <see cref="TimeSpan"/> showing the amount of time required to travel from start, to end </summary>
        public static TimeSpan GetFullTravelTime(WoWPoint start, WoWPoint end, WoWPoint flightPathStart, WoWPoint flightPathEnd, float travelSpeed)
        {
            WoWPoint[] toFlightPath = Navigator.GeneratePath(start, flightPathStart);
            WoWPoint[] toEnd = Navigator.GeneratePath(flightPathEnd, end);

            TimeSpan travelTime = GetFlightPathTime(flightPathStart, flightPathEnd);

            if (toFlightPath.Length != 0 && toFlightPath[toFlightPath.Length - 1].DistanceSqr(flightPathStart) <= 25f
                && toEnd.Length != 0 && toEnd[toEnd.Length - 1].DistanceSqr(end) <= 25f)
            {
                travelTime += GetRunPathTime(toFlightPath, travelSpeed);
                return travelTime + GetRunPathTime(toEnd, travelSpeed);
            }

            return TimeSpan.MaxValue;
        }

        /// <summary> Returns the amount of time required to travel along a path, by foot. </summary>
        public static TimeSpan GetRunPathTime(WoWPoint[] path, float travelSpeed)
        {
            if (path.Length == 0)
            {
                return TimeSpan.MaxValue;
            }

            float pathDistance = GetPathDistance(path);
            float pathTraversalTime = pathDistance / travelSpeed;
            return new TimeSpan(0, 0, (int)Math.Ceiling((double)pathTraversalTime));
        }

        /// <summary>
        /// Initializes the flightpaths system.
        /// </summary>
        public static void Initialize()
        {
            Lua.Events.AttachEvent("TAXIMAP_OPENED", new LuaEventHandlerDelegate(HandleTaxiMapOpened));
            BotEvents.OnBotStopped += args => Reset();

            if (File.Exists(OldXmlPath) && !File.Exists(XmlPath))
            {
                // Bug preserved from 5.4.8: this creates a directory AT XmlPath itself (which includes
                // the "FlightPaths.xml" filename), not at Path.GetDirectoryName(XmlPath) as the
                // preceding existence check just tested. Ported as-is.
                if (!Directory.Exists(Path.GetDirectoryName(XmlPath)))
                {
                    Directory.CreateDirectory(XmlPath);
                }
                File.Move(OldXmlPath, XmlPath);
            }

            XmlNodes = new List<XmlFlightNode>();
            if (!File.Exists(XmlPath))
            {
                return;
            }

            XElement xml = XElement.Load(XmlPath);
            foreach (XElement node in xml.Elements("Node"))
            {
                XmlNodes.Add(new XmlFlightNode(node));
            }
        }

        public static void SetPoi(XmlFlightNode node = null)
        {
            switch (Reason)
            {
                case FlightPathReason.Learn:
                case FlightPathReason.Update:
                    BotPoi.Current = new BotPoi(NearestFlightMerchant, PoiType.Fly, NavType.Run);
                    return;
                case FlightPathReason.Use:
                    if (node == null)
                    {
                        return;
                    }
                    BotPoi botPoi = new BotPoi(node.Location, PoiType.Fly, NavType.Run)
                    {
                        Entry = node.MasterEntry
                    };
                    BotPoi.Current = botPoi;
                    return;
                default:
                    return;
            }
        }

        /// <summary>
        /// New in 5.4.8 (no 5.3 counterpart; 5.3's closest equivalent was NeedNearbyUpdate(), which
        /// 5.4.8 no longer has). Name taken verbatim from the 5.4.8 decompile - it is a public member
        /// and was not obfuscated.
        /// </summary>
        public static bool NeedToGrabNode
        {
            get
            {
                if (StyxWoW.Me.IsOnTransport)
                {
                    return false;
                }
                WoWUnit activeMover = WoWMovement.ActiveMover;
                if (activeMover == null || activeMover.IsFlying)
                {
                    return false;
                }
                WoWUnit nearestFlightMerchant = NearestFlightMerchant;
                if (nearestFlightMerchant != null && GetNodeFromMaster(nearestFlightMerchant.Entry) == null)
                {
                    Reason = FlightPathReason.Learn;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns whether we should take a flight path, given the start, and end of a path
        /// </summary>
        public static bool ShouldTakeFlightpath(WoWPoint start, WoWPoint end, float travelSpeed)
        {
            if (!CanTakeFlightPaths)
            {
                return false;
            }
            if (Reason == FlightPathReason.Use)
            {
                return false;
            }
            if (!_fpWaitTimer.IsFinished)
            {
                return false;
            }
            _fpWaitTimer.Reset();

            TimeSpan runPathTime = GetRunPathTime(Navigator.GeneratePath(start, end), travelSpeed);

            XmlFlightNode startNpc;
            XmlFlightNode endNpc;
            GetClosestFlightPathNodes(start, end, out startNpc, out endNpc);

            if (startNpc == null || endNpc == null || startNpc.Location == WoWPoint.Empty || endNpc.Location == WoWPoint.Empty || startNpc.Name == endNpc.Name)
            {
                return false;
            }

            TimeSpan fullTravelTime = GetFullTravelTime(start, end, startNpc.Location, endNpc.Location, travelSpeed);

            int difference = (int)Math.Abs((runPathTime - fullTravelTime).TotalSeconds);
            if (difference <= 30)
            {
                return false;
            }

            Logging.WriteDiagnostic("Flight time: " + fullTravelTime);
            Logging.WriteDiagnostic("Run Time: " + runPathTime);
            Logging.WriteDiagnostic("Difference: " + TimeSpan.FromSeconds((double)difference));

            return fullTravelTime < runPathTime;
        }

        // Was smethod_0. Note: this is 5.4.8's (more elaborate) algorithm, not 5.3's - it additionally
        // filters ignored nodes, picks the end node as the closest-to-`end` node that is actually
        // connected to the chosen start node (via First(predicate), which throws
        // InvalidOperationException if none of the continent's nodes are connected to the start node -
        // ported as-is, this is 5.4.8's real behaviour), and finally nulls out both nodes if the start
        // node turns out to be closer to `end` than the chosen end node is.
        private static void GetClosestFlightPathNodes(WoWPoint start, WoWPoint end, out XmlFlightNode startNode, out XmlFlightNode endNode)
        {
            if (XmlNodes.Count == 0)
            {
                startNode = endNode = null;
                return;
            }

            uint myContinent = StyxWoW.Me.MapId;
            List<XmlFlightNode> nodesOnThisContinent = XmlNodes.Where(n => n.Continent == myContinent && !IsIgnored(n.MasterEntry) && !IsIgnored(n.Location)).ToList();
            if (nodesOnThisContinent.Count == 0)
            {
                startNode = endNode = null;
                return;
            }

            startNode = nodesOnThisContinent.OrderBy(n => n.Location.DistanceSqr(start)).First();
            List<string> startConnections = startNode.Connections;
            endNode = nodesOnThisContinent.OrderBy(n => n.Location.DistanceSqr(end)).First(n => startConnections.Contains(n.Name));

            float startDistanceToEnd = startNode.Location.DistanceSqr(end);
            float endDistanceToEnd = endNode.Location.DistanceSqr(end);
            if (startDistanceToEnd < endDistanceToEnd)
            {
                endNode = null;
                startNode = null;
            }
        }

        // Was smethod_1.
        private static XmlFlightNode GetNode(string name)
        {
            return XmlNodes.FirstOrDefault(x => x.Name == name);
        }

        // Was smethod_2. 5.3 had a 2-arg overload (masterEntry, myLevel) filtering on UpdateLevel too,
        // but 5.4.8's XmlFlightNode has no UpdateLevel member (confirmed against both the 5.4.8
        // decompile and this project's XmlFlightNode.cs), so 5.4.8 only takes masterEntry.
        private static XmlFlightNode GetNodeFromMaster(uint masterEntry)
        {
            return XmlNodes.FirstOrDefault(x => x.MasterEntry == masterEntry);
        }

        // Was smethod_3.
        private static float GetPathDistance(WoWPoint[] path)
        {
            float length = 0f;
            for (int i = 0; i < path.Length - 1; i++)
            {
                length += path[i].Distance(path[i + 1]);
            }
            return length;
        }

        // Was smethod_4.
        private static void HandleTaxiMapOpened(object sender, LuaEventArgs e)
        {
            try
            {
                if (BotPoi.Current.Type == PoiType.Fly)
                {
                    Logging.Write(StyxResources.TaxiMap_is_opened__Updating_known_nodes_list_);
                    WoWTaxi woWTaxi = new WoWTaxi();
                    TaxiNodes currentNode = woWTaxi.CurrentNode;
                    XmlFlightNode xmlFlightNode = GetNode(currentNode.Name);
                    if (xmlFlightNode == null)
                    {
                        xmlFlightNode = new XmlFlightNode(BotPoi.Current.Entry, currentNode.Name, (uint)woWTaxi.CurrentNode.MapId, woWTaxi.CurrentNode.Location);
                        XmlNodes.Add(xmlFlightNode);
                    }
                    else
                    {
                        xmlFlightNode.MasterEntry = BotPoi.Current.Entry;
                    }
                    uint nodeCount = woWTaxi.NodeCount;
                    for (uint num = 0U; num < nodeCount; num += 1U)
                    {
                        try
                        {
                            TaxiNodes taxiNodes = woWTaxi.GetNode(num);
                            TaxiFrame.TaxiFrameNode taxiFrameNode = TaxiFrame.Instance.Nodes[(int)num];
                            if (taxiNodes != currentNode)
                            {
                                XmlFlightNode xmlFlightNode2 = GetNode(taxiNodes.Name);
                                if (xmlFlightNode2 == null)
                                {
                                    xmlFlightNode2 = new XmlFlightNode(taxiNodes.Name, StyxWoW.Me.MapId, taxiNodes.Location);
                                    XmlNodes.Add(xmlFlightNode2);
                                }
                                if (taxiFrameNode.Reachable)
                                {
                                    xmlFlightNode.Connect(xmlFlightNode2.Name);
                                    xmlFlightNode2.Connect(xmlFlightNode.Name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Write(ex.ToString());
                        }
                    }
                    XElement xElement = new XElement("FlightNodes");
                    foreach (XmlFlightNode xmlFlightNode3 in XmlNodes)
                    {
                        xElement.Add(xmlFlightNode3.ToXml());
                    }
                    xElement.Save(XmlPath);
                    if (Reason != FlightPathReason.Use)
                    {
                        BotPoi.Clear(StyxResources.Learned_Updated_Flight_Path_Information);
                    }
                }
            }
            catch (Exception ex2)
            {
                Logging.WriteException(ex2);
            }
            finally
            {
                if (BotPoi.Current.Type == PoiType.Fly && Reason != FlightPathReason.Use)
                {
                    TaxiFrame.Instance.Hide();
                    BotPoi.Clear("HandleTaxiMapOpened");
                }
            }
        }
    }
}
