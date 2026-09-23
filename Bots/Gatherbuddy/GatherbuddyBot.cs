using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Bots.GatherBuddy;
using Bots.GatherBuddy.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;


using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;
using System.Diagnostics;

namespace Bots.Gatherbuddy
{
	public partial class GatherbuddyBot : BotBase
    {
		public static HashSet<WoWPoint> BlacklistNodes = new HashSet<WoWPoint>();
		private static readonly HashSet<WoWPoint> SafeNodes = new HashSet<WoWPoint>();
		public static readonly Dictionary<string, int> NodeCollectionCount = new Dictionary<string, int>();
		private static DateTime _startTime;
		private static int curGuildBankTab = 1;
		private static int numGuildBankTabs;
		private static int harvestAttempts;
		private static WoWGameObject guildVault;
		private static WoWObject lastNode;
		private static List<WoWItem> itemsToDeposit = new List<WoWItem>();
		private static WoWUnit sellVendor;
		private static Vendor repairVendor;
		private static WoWPoint takeOffPoint = WoWPoint.Empty;
		internal static Stopwatch nodeTimer = new Stopwatch();

		public static TimeSpan runningTime { get { return DateTime.Now.Subtract(_startTime); } }

		public override string Name { get { return GatherBuddyResources.BotName; } }

		private Composite _root;
		public override Composite Root { get { return _root ?? (_root = CreateGatherbuddyLogic()); } }

		public override PulseFlags PulseFlags { get { return PulseFlags.All; } }

        public override Form ConfigurationForm { get { return new GUI.GbConfig(); } }

		public override bool IsPrimaryType { get { return true; } }

		public override bool RequiresProfile { get { return true; } }

		public override void Pulse() { }

		public static Version Version = new Version("2.0.0.675");

        private static readonly HashSet<uint> MiningPickItemIds = new HashSet<uint>
	                                                        {
	                                                            1959,
                                                                9465,
                                                                1893,
                                                                20723,
                                                                30855,
                                                                756,
                                                                40893,
                                                                40772,
                                                                40892,
                                                                778,
                                                                2901,
                                                                1819
	                                                        };

        private static readonly HashSet<uint> IgnoredEliteMobs = new HashSet<uint>
                                                                     {
                                                                         65792, // Crab Pile
                                                                         63420, // SLG Generic MoP
                                                                     };


	    //public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;

		public GatherbuddyBot()
		{
			LoadAvoidList();
		}

		public override void Start()
		{
			CreateHardCodedBlacklist();
			TreeRoot.TicksPerSecond = 15;
			try
			{
				Profile.CurrentProfile = new Profile();
			}
			catch (ProfileException exception)
			{
				throw new HonorbuddyUnableToStartException(exception.Message);
			}
			NodeCollectionCount.Clear();
			_startTime = DateTime.Now;
			Vendors.NavType = NavType.Fly;
			Targeting.Instance.IncludeTargetsFilter += Instance_CombatIncludeTargetsFilter;
			LootTargeting.Instance.IncludeTargetsFilter += Instance_IncludeTargetsFilter;
			LootTargeting.Instance.RemoveTargetsFilter += Instance_RemoveTargetsFilter;
		    LootTargeting.Instance.WeighTargetsFilter += Instance_WeighTargetsFilter;
			BotEvents.Profile.OnNewProfileLoaded += Profile_OnNewProfileLoaded;
		}

		private void Profile_OnNewProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
		{
			Log(GatherBuddyResources.NewProfileHasBeenLoadedUpdatingWaypoints);
			try
			{
				Profile.CurrentProfile = new Profile();
			}
			catch (ProfileException exception)
			{
				Logging.WriteQuiet("New profile is invalid! {0}", exception.Message);
			}
		}

		public override void Stop()
		{
			TreeRoot.ResetTicksPerSecond();
		    movePoint = WoWPoint.Zero;
		    lastNode = null;
		    harvestAttempts = 0;
			Log(GatherBuddyResources.StoppedGatherbuddyAfterGathering__0__nodes_in__1_h__2_m__3_s_, NodeCollectionCount.Values.Sum(), runningTime.Hours,
					runningTime.Minutes, runningTime.Seconds);
			
			foreach (var i in NodeCollectionCount)
			{
				Log(i.Key + ": " + i.Value);
			}
			curGuildBankTab = 1;
			Vendors.ForceSell = false;
			Vendors.NavType = null;
			Targeting.Instance.IncludeTargetsFilter -= Instance_CombatIncludeTargetsFilter;
			LootTargeting.Instance.IncludeTargetsFilter -= Instance_IncludeTargetsFilter;
            LootTargeting.Instance.RemoveTargetsFilter -= Instance_RemoveTargetsFilter;
            LootTargeting.Instance.WeighTargetsFilter -= Instance_WeighTargetsFilter;
			BotEvents.Profile.OnNewProfileLoaded -= Profile_OnNewProfileLoaded;
		}

		private void Instance_CombatIncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
		{
			if (StyxWoW.Me.IsFlying)
			{
				return;
			}
			foreach (WoWUnit unit in incomingUnits.OfType<WoWUnit>())
			{
				if (unit.ThreatInfo.ThreatStatus > ThreatStatus.UnitNotInThreatTable)
				{
					outgoingUnits.Add(unit);
				}
			}
		}

        private void Instance_WeighTargetsFilter(List<Targeting.TargetPriority> units)
        {
            foreach (var o in units)
            {
                if (o.Object is WoWUnit)
                    o.Score -= 1000;         
            }
        }

		private void Instance_IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
		{
			foreach (WoWObject incomingUnit in incomingUnits)
			{
				outgoingUnits.Add(incomingUnit);
			}
		}

		private void Instance_RemoveTargetsFilter(List<WoWObject> units)
		{
		    var minFreeBagSlots = ProfileManager.CurrentProfile.MinFreeBagSlots;

		    var ignoreHerbs = !GatherbuddySettings.Instance.GatherHerbs ||
                              BagHelper.EmptyHerbSlots <= minFreeBagSlots;

		    var ignoreMines = !GatherbuddySettings.Instance.GatherMinerals ||
                              BagHelper.EmptyMineSlots <= minFreeBagSlots;

			for (int i = units.Count - 1; i >= 0; i--)
			{
				if (!units[i].IsValid)
				{
					units.RemoveAt(i);
					continue;
				}

				if (units[i].BaseAddress == (IntPtr) 0)
				{
					units.RemoveAt(i);
					continue;
				}

				if (Blacklist.Contains(units[i].Guid, BlacklistFlags.Combat | BlacklistFlags.Node | BlacklistFlags.Pull))
                {
                    units.RemoveAt(i);
                    continue;
                }

				if (GatherbuddySettings.Instance.LootMobs &&
					units[i] is WoWUnit && units[i].ToUnit().CanLoot)
				{
					continue;
				}

				if (!(units[i] is WoWGameObject))
				{
					units.RemoveAt(i);
					continue;
				}

				var unit = units[i].ToGameObject();

				if (nodeAvoidList.Contains(unit.Entry))
				{
					units.RemoveAt(i);
					continue;
				}

				if (!unit.IsHerb && !unit.IsMineral)
				{
					units.RemoveAt(i);
					continue;
				}

				if (unit.IsHerb && ignoreHerbs)
				{
					units.RemoveAt(i);
					continue;
				}

                if (unit.IsMineral && ignoreMines)
				{
					units.RemoveAt(i);
					continue;
				}

				if (!unit.CanLoot)
				{
					units.RemoveAt(i);
					continue;
				}

                var location = unit.Location;

                if (IsBlacklisted(location))
                {
                    units.RemoveAt(i);
                    continue;
                }

                if (IsInBlackspot(location))
                {
                    units.RemoveAt(i);
                    continue;
                }
				
				if (GatherbuddySettings.Instance.NoNinja && Flightor.MountHelper.Mounted)
				{
					var player = ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).
                                    FirstOrDefault(p => p.Location.DistanceSqr(location) < 10 * 10);

					if (player != null)
					{
						dLog("Removing \"{0}\" from loot list. Ninja attempt avoided", unit.Name);
                        Blacklist.Add(unit, TimeSpan.FromSeconds(5));
						units.RemoveAt(i);
						continue;
					}
				}

				var eliteMob = ObjectManager.GetObjectsOfType<WoWUnit>(false, false).
                                    FirstOrDefault(u => !IgnoredEliteMobs.Contains(u.Entry) && u.IsElite() && StyxWoW.Me.Level - u.Level <= 6 &&
                                                        u.Location.Distance(location) < u.MyAggroRange + 5 &&
														u.IsHostile);

				if (eliteMob != null && GatherbuddySettings.Instance.IgnoreElites)
				{
                    dLog("Removing \"{0}\" from loot list. There is an elite mob around. Name: {1}", unit.Name, eliteMob.Name);
                    Blacklist.Add(unit, TimeSpan.FromSeconds(5));
					units.RemoveAt(i);
					continue;
				}

				if (SafeNodes.Contains(location))
					continue;

                if (!StyxWoW.Me.HasAura("Sea Legs") && GameWorld.TraceLine(location.Add(0, 0, 1000), location, GameWorld.CGWorldFrameHitFlags.HitTestLiquid))
				{
                    dLog("Adding underwater node \"{0}\" to blacklist. Location: {1}", unit.Name, location);
                    BlacklistNodes.Add(location);
					units.RemoveAt(i);
					continue;
				}

				if (unit.IsOutdoors)
				{
                    SafeNodes.Add(location);
					continue;
				}

                dLog("Adding underground node \"{0}\" to blacklist. Location: {1}", unit.Name, location);
                BlacklistNodes.Add(location);
				units.RemoveAt(i);
			}
		}
    }
}
