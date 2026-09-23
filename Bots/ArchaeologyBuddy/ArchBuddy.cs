using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ArchaeologyBuddy.Properties;
using ArchaeologyBuddy.Resources;
using Bots.ArchaeologyBuddy.SQLiteHelper;
using Bots.Grind;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;


using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;
using System.Data;

namespace Bots.ArchaeologyBuddy
{
	public partial class ArchBuddy
	{
		#region Variables

        private static int artifactsToCollect = 6;
		private static ulong lastArtifactGuid = 0;
	    private static int collectedArtifact;
		private static bool IsInDigSite, digSiteCheck, shouldFly, wasStuck;
		public static Digsite CurrentDigsite;
		private static string ClosestDigSiteName = "";
		private static int ClosestDigSiteID;
		private static WoWPoint ClosestDigSiteLocation = WoWPoint.Empty;
		private static WoWPoint nextSurveyPoint = WoWPoint.Empty;
		private static WoWPoint myLastLocation = WoWPoint.Empty;
		private static WoWPoint safeMyLastLocation = WoWPoint.Empty;
		private static WoWPoint safestMyLastLocation = WoWPoint.Empty;
		private static WoWPoint ValidDigSitePoint = WoWPoint.Empty;
		private static readonly Stopwatch stuckTimer = new Stopwatch();
		private static readonly Stopwatch safeStuckTimer = new Stopwatch();
        private static IEnumerable<FileInfo> u;
		private static readonly Stopwatch safestStuckTimer = new Stopwatch();
        private static WaitTimer w = new WaitTimer(TimeSpan.FromHours(1));
		internal static List<Digsite> PermanentBlacklist = new List<Digsite>();
	    internal static List<Digsite> DigSites = new List<Digsite>();
		internal static List<Fragment> Fragments = new List<Fragment>();

	    public static IEnumerable<string> BlacklistedDigsites
	    {
	        get
	        {
                // Include temporary blacklists
	            return
	                PermanentBlacklist.Select(d => d.Name)
	                         .Union(TemporaryBlacklist.Where(kv => kv.Value > DateTime.Now).Select(d => d.Key.Name));
	        }
	    }

	    private const string v1 = "cod";
	    private const string v2 = "empl";
	    private const string v3 = "osi";
	    public static Dictionary<string, int> SolveReport = new Dictionary<string, int>();
		private static DateTime startTime = DateTime.Now;
		public static int totalCollectedArtifacts;

		public static TimeSpan runningTime { get { return DateTime.Now.Subtract(startTime); } }
	
		public ArchSettings Settings { get; set; }
		public static ArchBuddy Instance { get; set; }

        //internal static string settingsPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Settings\\AB Settings\\";

		#endregion
		
		#region BotBase Overrides

		public override string Name
		{
			get
			{
			    return ArchaeologyResources.BotName;

			}
		}

		private static Form _configurationForm;
		public override Form ConfigurationForm
		{
			get
			{
				_configurationForm = new GUI.ArchBuddySettings();
				return _configurationForm;
			}
		}

		public override PulseFlags PulseFlags
		{
			get 
			{
				return PulseFlags.All;
			}
		}

		public override bool IsPrimaryType
		{
			get
			{
				return true;
			}
		}

		public string Author { get { return "raphus"; } }

		public static Version Version = new Version("1.1.0.404");

		#endregion

		#region Root

		private Composite _root;
		public override Composite Root
		{
			get
			{
 				return _root ?? (
					_root =
					new PrioritySelector(
						TestBehavior(),
						CreateDeathBehavior(),
						new Decorator(
							ret => StyxWoW.Me.Combat || StyxWoW.Me.HasAura("Food") || StyxWoW.Me.HasAura("Drink"),
							new Action(delegate
								{
									safeStuckTimer.Reset();
									stuckTimer.Reset();
									return RunStatus.Failure;
								})),

						new Decorator(ret => StyxWoW.Me.CurrentTarget == null &&
											 StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.CurrentTarget != null,
							new Action(ret => StyxWoW.Me.Pet.CurrentTarget.Target())),

						new Decorator(ret => !Flightor.MountHelper.Mounted || !StyxWoW.Me.MovementInfo.IsFlying,
							new PrioritySelector(
								LevelBot.CreateCombatBehavior(),
								LevelBot.CreateLootBehavior()
                                )),
						LevelBot.CreateVendorBehavior(),
                        new HookExecutor("ArchBuddy_LootArtifacts", "ArchaeologyBuddy's behavior for looting artifacts.",LootArtifactsBehavior()),
						SolveBehavior(),
						TrainBehavior(),
						FindDigsiteBehavior(),
						MoveToDigsiteBehavior(),
						InsideDigsiteBehavior()
						));
			}
		}

		#endregion

		#region Initialize

        private static string SettingsDirectory
        {
            get { return Path.Combine(Utilities.AssemblyDirectory, "Settings"); }
        }

        private static string GatherbuddyAvoidListPath
        {
            get
            {
                return Path.Combine(Path.Combine(Path.Combine(SettingsDirectory, StyxWoW.Me.RealmName), StyxWoW.Me.Name),
                                    "ABBlacklist.xml");
            }
        }

	    private static bool _initDone;
        public override void Initialize()
        {
            if (_initDone)
                return;

            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllBytes(tempFile, Resources.data);

                var sqdb = new SQLiteManager(tempFile);

                var dt = sqdb.GetDataTable(@"SELECT * FROM fragments");

                Fragments.AddRange(from DataRow row in dt.Rows
                                   let id = (int) row.Field<long>("ID")
                                   let digsiteId = (int) row.Field<long>("digsiteID")
                                   let digsiteName = row.Field<string>("digsiteName")
                                   let location = WoWPointFromString(row.Field<string>("Location"))
                                   let type = row.Field<string>("Type")
                                   let outdoors = Convert.ToBoolean(row.Field<long>("OutDoors"))
                                   let higherzexists = Convert.ToBoolean(row.Field<long>("HigherZExists"))
                                   select
                                       new Fragment(id, digsiteId, digsiteName, location, type, outdoors, higherzexists));

                dt = sqdb.GetDataTable(@"SELECT * FROM digsites");
                
                DigSites.AddRange(from DataRow row in dt.Rows
                                  let id = (int) row.Field<long>("ID")
                                  let name = row.Field<string>("Name")
                                  let location = WoWPointFromString(row.Field<string>("Location"))
                                  let zoneId = uint.Parse(row.Field<string>("ZoneID"))
                                  let continentId = uint.Parse(row.Field<string>("ContinentID"))
                                  let type = row.Field<string>("Type")
                                  let fragments = Fragments.Where(f => f.DigsiteID == id).ToList()
                                  select new Digsite(id, name, location, zoneId, continentId, type, fragments));
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);

				if (!File.Exists(GatherbuddyAvoidListPath))
				{
					int[] digsiteIds = new int[4] { 9415529, 15757949, 1125202, 24005971 };
					foreach (int id in digsiteIds)
					{
						PermanentBlacklist.Add(Digsite.FromId(id));
					}
					SaveBlacklist();
				}
                else
                    LoadBlacklist();

                _initDone = true;
            }
        }

        #endregion

		#region Ctor

		public ArchBuddy()
		{
			Instance = this;
			Settings = new ArchSettings();
		}

		#endregion

		#region Pulse

		public override void Pulse()
		{

		}

		#endregion

		#region Start

		public override void Start()
		{
		    WoWSkill skill = StyxWoW.Me.GetSkill(SkillLine.Archaeology);

			if (skill == null || skill.MaxValue == 0)
			{
				if ((!StyxWoW.Me.IsHorde || StyxWoW.Me.MapId != 1) && (!StyxWoW.Me.IsAlliance || StyxWoW.Me.MapId != 0))
				{
					throw new HonorbuddyUnableToStartException("ArchaeologyBuddy can only train Archaeology in Kalimdor for Horde and Eastern Kingdoms for Alliance. Please start AB in the approaite continent or train Archaeology first.");
				}
				GUI.ArchBuddySettings.ForceTrain = true;
			}

			// Load an empty profile
			ProfileManager.LoadEmpty();
			Vendors.NavType = NavType.Fly;
			Targeting.Instance.IncludeTargetsFilter += LevelBot.LevelBotIncludeTargetsFilter;
			LootTargeting.Instance.IncludeTargetsFilter += LevelBot.LevelbotIncludeLootsFilter;
			Targeting.Instance.IncludeTargetsFilter += IncludeThreatenedTargets;
			Lua.Events.AttachEvent("ARTIFACT_DIG_SITE_UPDATED", ArtifactDigSiteUpdated);
			ResetVariables();
			startTime = DateTime.Now;
			SolveReport.Clear();
			totalCollectedArtifacts = 0;
		}

		#endregion

		#region Stop

		public override void Stop()
		{
            if (CharacterSettings.Instance.LastUsedPath != null && File.Exists(CharacterSettings.Instance.LastUsedPath))
                ProfileManager.LoadNew(CharacterSettings.Instance.LastUsedPath, false);

			Targeting.Instance.IncludeTargetsFilter -= LevelBot.LevelBotIncludeTargetsFilter;
			LootTargeting.Instance.IncludeTargetsFilter -= LevelBot.LevelbotIncludeLootsFilter;
			Targeting.Instance.IncludeTargetsFilter -= IncludeThreatenedTargets;
			Vendors.NavType = null;
			Lua.Events.DetachEvent("ARTIFACT_DIG_SITE_UPDATED", ArtifactDigSiteUpdated);

            Log(ArchaeologyResources.ArchBuddyStopMessage, totalCollectedArtifacts / artifactsToCollect, runningTime.Hours,
					runningTime.Minutes, runningTime.Seconds);

			Log(ArchaeologyResources.SolveReport);
			Log("---------------------------");
			if (SolveReport.Count > 0)
			{
				foreach (var i in SolveReport)
				{
					Log(i.Key + ": " + i.Value);
				}
			}
			else
				Log(ArchaeologyResources.NoSolves);
		}

		private void ArtifactDigSiteUpdated(object sender, LuaEventArgs e)
		{
			ResetVariables();
		}

		private void IncludeThreatenedTargets(List<WoWObject> incomingObjects, HashSet<WoWObject> outgoingObjects)
		{
			if (StyxWoW.Me.IsFlying)
			{
				return;
			}
			foreach (WoWUnit unit in incomingObjects.OfType<WoWUnit>())
			{
				if (unit.ThreatInfo.ThreatStatus > ThreatStatus.UnitNotInThreatTable)
				{
					outgoingObjects.Add(unit);
				}
			}
		}

		#endregion
	}
}
