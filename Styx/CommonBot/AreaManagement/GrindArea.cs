using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.Resources;
using Vector3 = Tripper.Tools.Math.Vector3;

namespace Styx.CommonBot.AreaManagement
{
    public class GrindArea : Area
    {
        public GrindArea()
        {
            StyxWoW.AreaManager.Add(this);

            TargetMinLevel = 0;
            TargetMaxLevel = int.MaxValue;
            Factions = new List<int>();
            MobIDs = new List<int>();
        }

        public GrindArea(HotspotManager manager) : this()
        {
            if (manager == null)
                return;

            Hotspots.AddRange(manager.Hotspots.ConvertAll(obj => obj.ToHotspot()));

            foreach (WoWPoint hotspot in manager.Hotspots)
            {
                CircledHotspots.Enqueue(hotspot);
            }
        }

        #region Overrides of Area

        public override AreaType Type { get { return AreaType.Grind; } }

        #endregion

        public string Name { get; set; }
        public double? LootRadius { get; set; }
        public bool RandomizeHotspots { get; set; }
        public double? MaxDistance { get; set; }
        public bool? LootMobs { get; set; }
        public bool? UseMount { get; set; }
        public int? MaximumHotspotTime { get; set; }
        public List<int> Factions { get; set; }
        public List<int> MobIDs { get; set; }
        public int TargetMinLevel { get; set; }
        public int TargetMaxLevel { get; set; }

        public Dictionary<uint, BlacklistFlags> Blacklist = new Dictionary<uint, BlacklistFlags>();

        private readonly Stopwatch _timer = new Stopwatch();
        private Hotspot _currentHotspot = WoWPoint.Zero;
        public Hotspot LastHotSpot { get; set; }
        public bool HotspotChanged { get { return CurrentHotSpot != LastHotSpot; } }
        private Profile _lastProfile;

        /// <summary>
        /// Returns the current hotspot the bot should move against.
        /// </summary>
        public Hotspot CurrentHotSpot
        {
            get
            {
                try
                {
                    EnsureHotspots();
                }
                catch (UserException ex)
                {
                    Logging.Write(ex.Message);
                }
                catch (Exception ex)
                {
                    Logging.WriteException(ex);
                }

                return _currentHotspot;
            }
        }

        public virtual List<List<Vector3>> AreaDefinitions { get { return null; } }

        private WaitTimer _canNavTimer = WaitTimer.FiveSeconds;

        /// <summary>
        /// Used internally to set the current hotspot properly and/or to deque randomzie or w/e the profile wants.
        /// </summary>
        private void EnsureHotspots()
        {
            if (Hotspots.Count <= 0 && CircledHotspots.Count <= 0)
            {
                Logging.Write(StyxResources.No_suitable_grind_or_quest_hotspots_in__0__, ProfileManager.CurrentProfile.Name);
                throw new UserException(StyxResources.No_grind_or_quest_hotspots_have_been_defined_);
            }

            float distance = StyxWoW.Me.Location.Distance2D(_currentHotspot.Position);
            if (_currentHotspot != WoWPoint.Zero && distance <= Navigator.PathPrecision)
            {
                LastHotSpot = _currentHotspot;
                _timer.Restart();
            }

            if (ProfileManager.CurrentProfile != _lastProfile)
            {
                _lastProfile = ProfileManager.CurrentProfile;
                LastHotSpot = _currentHotspot;

                if (RandomizeHotspots)
                {
                    var hotspot = GetRandomHotspot();
                    _currentHotspot = hotspot ?? CircledHotspots.Dequeue();
                }
                else
                    _currentHotspot = CircledHotspots.Dequeue();

                _timer.Reset();
            }

            if (CircledHotspots.Count > 1 && ShouldDequeue)
            {
                LastHotSpot = _currentHotspot;

                if (RandomizeHotspots)
                {
                    var hotspot = GetRandomHotspot();
                    _currentHotspot = hotspot ?? CircledHotspots.Dequeue();
                }
                else
                    _currentHotspot = CircledHotspots.Dequeue();

                _timer.Reset();
            }
        }

        private bool ShouldDequeue
        {
            get
            {
                int time = MaximumHotspotTime.HasValue ? MaximumHotspotTime.Value : 900000;

                bool dequeue =
                    _timer.Elapsed.TotalMilliseconds > time
                    || (LastHotSpot == _currentHotspot && Targeting.Instance.TargetList.Count == 0)
                    || (LastHotSpot == _currentHotspot && LevelbotSettings.Instance.GroundMountFarmingMode)
                    || (LastHotSpot == _currentHotspot && StyxWoW.Me.Mounted && StyxWoW.Me.Combat);

                return dequeue;
            }
        }

        private static Regex _factionParser = new Regex("\\d+", RegexOptions.Compiled);

        /// <summary>
        /// Returns an <see cref="GrindArea" /> object of the passede in <see cref="XElement" /> object
        /// </summary>
        public static GrindArea FromXML(XElement value)
        {
            var ret = new GrindArea();

            if (value != null)
            {
                var name = value.Element("Name");
                if (name != null)
                    ret.Name = name.Value;

                var lootRadius = value.Element("LootRadius");
                if (lootRadius != null)
                    ret.LootRadius = lootRadius.Value.ToFloat();

                var randomizeHotspots = value.Element("RandomizeHotspots");
                if (randomizeHotspots != null)
                    ret.RandomizeHotspots = randomizeHotspots.Value.ToBoolean();

                var maxDistance = value.Element("MaxDistance");
                if (maxDistance != null)
                    ret.MaxDistance = maxDistance.Value.ToFloat();

                var targetingDistance = value.Element("TargetingDistance");
                if (targetingDistance != null)
                    ret.MaxDistance = targetingDistance.Value.ToFloat();

                var useMount = value.Element("UseMount");
                if (useMount != null)
                    ret.UseMount = useMount.Value.ToBoolean();

                var lootMobs = value.Element("LootMobs");
                if (lootMobs != null)
                    ret.LootMobs = lootMobs.Value.ToBoolean();

                var maximumHotspotTime = value.Element("MaximumHotspotTime");
                if (maximumHotspotTime != null)
                    ret.MaximumHotspotTime = maximumHotspotTime.Value.ToInt32();

                var targetMinLevel = value.Element("TargetMinLevel");
                if (targetMinLevel != null)
                    ret.TargetMinLevel = targetMinLevel.Value.ToInt32();

                var targetMaxLevel = value.Element("TargetMaxLevel");
                if (targetMaxLevel != null)
                    ret.TargetMaxLevel = targetMaxLevel.Value.ToInt32();

                var hotspots = value.Element("Hotspots");
                if (hotspots != null)
                {
                    foreach (XElement hotspot in hotspots.Descendants("Hotspot"))
                    {
                        ret.CircledHotspots.Enqueue(new Hotspot(hotspot));
                        ret.Hotspots.Add(new Hotspot(hotspot));
                    }
                }

                var blacklist = value.Element("Blacklist");
                if (blacklist != null)
                {
                    foreach (XElement entry in blacklist.Elements())
                    {
                        try
                        {
                            uint id = 0U;
                            BlacklistFlags flags = BlacklistFlags.All;
                            foreach (XAttribute attribute in entry.Attributes())
                            {
                                try
                                {
                                    switch (attribute.Name.ToString().ToLowerInvariant())
                                    {
                                        case "name":
                                            break;
                                        case "id":
                                        case "entry":
                                            if (!uint.TryParse(attribute.Value, out id))
                                                throw new ProfileAttributeExpectedException<uint>(attribute);
                                            break;
                                        case "flags":
                                            if (!Enum.TryParse(attribute.Value, true, out flags))
                                                throw new ProfileAttributeExpectedException(attribute, Enum.GetNames(typeof(BlacklistFlags)));
                                            break;
                                        default:
                                            throw new ProfileUnknownAttributeException(attribute);
                                    }
                                }
                                catch (ProfileException ex)
                                {
                                    Logging.Write(ex.Message);
                                }
                            }

                            if (!ret.Blacklist.ContainsKey(id))
                                ret.Blacklist.Add(id, flags);
                            else
                                ret.Blacklist[id] |= flags;
                        }
                        catch (ProfileException ex)
                        {
                            Logging.Write(ex.Message);
                        }
                    }
                }

                XElement factionElem = ProfileHelper.FindElement(value, true, "factions");
                if (factionElem != null)
                {
                    foreach (Match m in _factionParser.Matches(factionElem.Value))
                        ret.Factions.Add(Convert.ToInt32(m.Value));
                }

                XElement mobIDElem = ProfileHelper.FindElement(value, true, "mobIDs");
                if (mobIDElem != null)
                {
                    foreach (Match m in _factionParser.Matches(mobIDElem.Value))
                        ret.MobIDs.Add(Convert.ToInt32(m.Value));
                }
            }

            return ret;
        }

        public override string ToString()
        {
            return string.Format("[GrindArea Name: {0}, LootRadius: {1}, RandomizeHotspots: {2}, MaxDistance: {3}, MaximumHotspotTime: {4}, Hotspots: Count: {5}]", Name, LootRadius, RandomizeHotspots, MaxDistance, MaximumHotspotTime, Hotspots.Count);
        }
    }
}
