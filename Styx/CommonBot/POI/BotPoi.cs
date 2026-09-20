using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Common;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot.POI
{
    public class BotPoi
    {
        private static BotPoi _currPoi = new BotPoi(PoiType.None, NavType.Run);
        private readonly object _poiObj;

        static BotPoi()
        {
            BotEvents.Player.OnPlayerDied += Player_OnPlayerDied;
        }

        private static void Player_OnPlayerDied()
        {
            Logging.WriteDiagnostic("We died, Clearing current POI");
            Clear("Player Died");
        }

        public BotPoi(PoiType type, NavType navType = NavType.Run)
        {
            Type = type;
            NavType = navType;
        }

        public BotPoi(WoWObject obj, PoiType type, NavType navType = NavType.Run)
            : this(type, navType)
        {
            if (obj == null || obj.BaseAddress == IntPtr.Zero)
            {
                Type = PoiType.None;
                return;
            }

            try
            {
                Name = obj.SafeName;
                Guid = obj.Guid;
                Entry = obj.Entry;
                Location = obj.Location;
                _cachedObject = obj;
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
                Name = null;
                Guid = 0UL;
                Entry = 0U;
                Type = PoiType.None;
                _poiObj = null;
            }
        }

        public BotPoi(WoWPoint location, PoiType type, NavType navType = NavType.Run)
            : this(type, navType)
        {
            Location = location;
        }

        public BotPoi(Quest quest, NavType navType = NavType.Run)
            : this(PoiType.Quest, navType)
        {
            Entry = quest.Id;
            Name = quest.Name;
            _poiObj = quest;
        }

        public BotPoi(Mailbox mailbox, NavType navType = NavType.Run)
            : this(PoiType.Mail, navType)
        {
            Location = mailbox.Location;
            _poiObj = mailbox;
            Name = mailbox.Name;
        }

        public BotPoi(Vendor vendor, PoiType type)
            : this(type, NavType.Run)
        {
            Name = vendor.Name;
            Entry = (uint)vendor.Entry;
            Location = vendor.Location;
            NavType = vendor.NavType;
            _poiObj = vendor;
        }

        public BotPoi(Mailbox mailbox)
            : this(PoiType.Mail, NavType.Run)
        {
            Name = mailbox.Name;
            Entry = 0U;
            Location = mailbox.Location;
            NavType = mailbox.NavType;
            _poiObj = mailbox;
        }

        public BotPoi(PickUpNode pickUp)
            : this(PoiType.QuestPickUp, NavType.Run)
        {
            _poiObj = pickUp;
            Entry = pickUp.GiverId;
            Location = pickUp.GiverLocation;
            NavType = pickUp.NavType;
            Name = pickUp.GiverName;
        }

        public BotPoi(TurnInNode turnIn)
            : this(PoiType.QuestTurnIn, NavType.Run)
        {
            _poiObj = turnIn;
            Entry = turnIn.TurnInId;
            Name = turnIn.TurnInName;
            NavType = turnIn.NavType;
        }

        public static BotPoi Current
        {
            get { return _currPoi; }
            set
            {
                if (_currPoi == value)
                {
                    return;
                }

                BotPoi newPoi = value ?? new BotPoi(PoiType.None, NavType.Run);
                NotifyPoiChanged(_currPoi, newPoi);
                _currPoi = newPoi;
            }
        }

        private static void NotifyPoiChanged(BotPoi previous, BotPoi current)
        {
            if (previous.Type == PoiType.Fly && current.Type != PoiType.Fly)
            {
                FlightPaths.Reset();
            }

            if (current.Type == PoiType.None)
            {
                Logging.WriteDiagnostic("Cleared POI");
                return;
            }

            Logging.WriteDiagnostic("Changed POI to: {0}", current);
        }

        public PoiType Type { get; set; }

        public string Name { get; set; }

        public ulong Guid { get; set; }

        public uint Entry { get; set; }

        public NavType NavType { get; set; }

        private WoWPoint _loc;

        public WoWPoint Location
        {
            get
            {
                switch (Type)
                {
                    case PoiType.Quest:
                    case PoiType.QuestTurnIn:
                    case PoiType.QuestPickUp:
                    case PoiType.Kill:
                    case PoiType.Loot:
                    case PoiType.Skin:
                    case PoiType.Interact:
                    case PoiType.Buy:
                    case PoiType.Sell:
                    case PoiType.Repair:
                    case PoiType.Train:
                    case PoiType.Mail:
                        WoWObject asObject = AsObject;
                        if (asObject != null)
                        {
                            _loc = asObject.Location;
                        }
                        return _loc;
                    default:
                        return _loc;
                }
            }
            set { _loc = value; }
        }

        private WoWObject _cachedObject;

        public WoWObject AsObject
        {
            get
            {
                if (_cachedObject == null || !_cachedObject.IsValid)
                {
                    _cachedObject = ResolveObject();
                }
                return _cachedObject;
            }
        }

        private WoWObject ResolveObject()
        {
            switch (Type)
            {
                case PoiType.QuestTurnIn:
                {
                    IEnumerable<WoWObject> candidates;
                    var turnIn = _poiObj as TurnInNode;
                    if (turnIn != null && turnIn.TurnInType != null)
                    {
                        switch (turnIn.TurnInType.Value)
                        {
                            case QuestObjectType.GameObject:
                                candidates = ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
                                    .Where(g => g.Entry == Entry);
                                break;
                            case QuestObjectType.Npc:
                                candidates = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                                    .Where(u => u.Entry == Entry);
                                break;
                            default:
                                candidates = Enumerable.Empty<WoWObject>();
                                break;
                        }
                    }
                    else
                    {
                        candidates = ObjectManager.GetObjectsOfType<WoWObject>(true, true)
                            .Where(o => (o is WoWUnit || o is WoWGameObject) && o.Entry == Entry);
                    }

                    ulong myGuid = StyxWoW.Me.Guid;
                    return candidates
                        .OrderByDescending(o => o is WoWUnit && ((WoWUnit)o).CreatedByUnitGuid == myGuid)
                        .ThenByDescending(o => o is WoWUnit && ((WoWUnit)o).HasQuestCursor)
                        .ThenBy(o => o.Location.DistanceSqr(_loc))
                        .FirstOrDefault();
                }
                case PoiType.QuestPickUp:
                {
                    IEnumerable<WoWObject> candidates;
                    var pickUp = _poiObj as PickUpNode;
                    if (pickUp != null && pickUp.GiverType != null)
                    {
                        switch (pickUp.GiverType.Value)
                        {
                            case QuestObjectType.GameObject:
                                candidates = ObjectManager.GetObjectsOfType<WoWGameObject>(false, false)
                                    .Where(g => g.Entry == Entry);
                                break;
                            case QuestObjectType.Npc:
                                candidates = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                                    .Where(u => u.Entry == Entry);
                                break;
                            case QuestObjectType.Item:
                                candidates = ObjectManager.GetObjectsOfType<WoWItem>(false, false)
                                    .Where(i => i.Entry == Entry);
                                break;
                            default:
                                candidates = Enumerable.Empty<WoWObject>();
                                break;
                        }
                    }
                    else
                    {
                        candidates = ObjectManager.GetObjectsOfType<WoWObject>(true, true)
                            .Where(o => (o is WoWUnit || o is WoWGameObject) && o.Entry == Entry);
                    }

                    ulong myGuid = StyxWoW.Me.Guid;
                    return candidates
                        .OrderByDescending(o => o is WoWUnit && ((WoWUnit)o).CreatedByUnitGuid == myGuid)
                        .ThenByDescending(o => o is WoWUnit && ((WoWUnit)o).HasQuestCursor)
                        .ThenBy(o => o.Location.DistanceSqr(_loc))
                        .FirstOrDefault();
                }
                case PoiType.Buy:
                case PoiType.Sell:
                case PoiType.Repair:
                case PoiType.Train:
                case PoiType.Fly:
                {
                    var vendor = _poiObj as Vendor;
                    if (vendor == null)
                    {
                        return ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                            .FirstOrDefault(u => u.Entry == Entry);
                    }

                    return ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                        .FirstOrDefault(u => !u.IsDead && (u.Entry == Entry || (vendor.Entry2 != null && (ulong)u.Entry == (ulong)(long)vendor.Entry2.Value)));
                }
                case PoiType.Mail:
                {
                    WoWObject courier = ObjectManager.GetObjectsOfType<WoWUnit>(false, false)
                        .Where(u => u.IsMailCourier && u.CanGossip)
                        .OrderBy(u => u.Location.DistanceSqr(_loc))
                        .FirstOrDefault();
                    if (courier != null)
                    {
                        return courier;
                    }

                    WoWObject mailbox = FindNearestMailbox(10f);
                    if (mailbox == null && _loc.DistanceSqr(StyxWoW.Me.Location) < 1600f)
                    {
                        return FindNearestMailbox(float.MaxValue);
                    }
                    return mailbox;
                }
            }
            return ObjectManager.GetObjectByGuid(Guid);
        }

        private WoWObject FindNearestMailbox(float maxDistance)
        {
            return ObjectManager.GetObjectsOfType<WoWGameObject>(true, false)
                .Where(g => g.SubType == WoWGameObjectType.Mailbox)
                .Select(g => new { o = g, distanceSqrToPoi = g.Location.DistanceSqr(_loc) })
                .Where(pair => pair.distanceSqrToPoi < maxDistance * maxDistance)
                .OrderBy(pair => pair.distanceSqrToPoi)
                .Select(pair => pair.o)
                .FirstOrDefault();
        }

        public Quest AsQuest
        {
            get { return _poiObj as Quest; }
        }

        public Vendor AsVendor
        {
            get { return _poiObj as Vendor; }
        }

        public Mailbox AsMailbox
        {
            get { return _poiObj as Mailbox; }
        }

        public static void Clear()
        {
            Clear("");
        }

        public static void Clear(string reason)
        {
            if (Current.Type != PoiType.None)
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    Logging.WriteDiagnostic("Clearing POI - Reason {0}", reason);
                }
                _currPoi = new BotPoi(PoiType.None, NavType.Run);
            }
            Navigator.Clear();
            FlightPaths.Reset();
        }

        public override string ToString()
        {
            string ret = "Type: " + Type;
            switch (Type)
            {
                case PoiType.None:
                case PoiType.Hotspot:
                case PoiType.Corpse:
                    ret = ret + ", Loc: " + Location;
                    break;
                case PoiType.Quest:
                case PoiType.Kill:
                case PoiType.Loot:
                case PoiType.Skin:
                case PoiType.Buy:
                case PoiType.Sell:
                case PoiType.Repair:
                case PoiType.Train:
                    ret = ret + ", Name: " + Name;
                    break;
                case PoiType.Mail:
                    ret += string.Format(", Loc: {0}, Name: {1}", Location, Name);
                    break;
                case PoiType.Fly:
                    ret = ret + ", R: " + FlightPaths.Reason;
                    if (FlightPaths.TakingPathTo != null)
                    {
                        ret = ret + ", " + FlightPaths.TakingPathTo.Name;
                    }
                    break;
            }
            return ret;
        }
    }
}
