using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Linq;
using CommonBehaviors.Actions;
using Bots.Professionbuddy.ComponentBase;
using Bots.Professionbuddy.Components;
using Bots.Professionbuddy.Dynamic;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Bots.Professionbuddy
{
	public class ProfessionbuddyBot : BotBase
	{
		#region Static Members.

		public static readonly string ProfilePath = Path.Combine(Utilities.AssemblyDirectory, "Default Profiles", "Professionbuddy");

		public static ProfessionbuddyBot Instance { get; private set; }

		internal static bool AllowProfileEditing { get; private set; }

		#endregion

		#region Fields

		private readonly Dictionary<uint, int> _materialList = new Dictionary<uint, int>();
		private readonly PbProfileSettings _profileSettings = new PbProfileSettings();
		private List<TradeSkill> _tradeSkillList;

		#endregion

		#region Constructor

		public ProfessionbuddyBot()
		{
			Instance = this;
			BotEvents.Profile.OnNewOuterProfileLoaded += Profile_OnNewOuterProfileLoaded;
			Profile.OnUnknownProfileElement += Profile_OnUnknownProfileElement;
			CurrentProfile = PbProfile.EmptyProfile;
		}

		#endregion

		#region Events

		public event EventHandler OnTradeSkillsLoaded;

		#endregion

		#region Properties

		private LocalPlayer Me
		{
			get { return StyxWoW.Me; }
		}

		public ProfessionBuddySettings MySettings
		{
			get { return ProfessionBuddySettings.Instance; }
		}

		internal List<uint> TradeskillTools { get; private set; }

		public bool IsRunning { get; internal set; }

		/// <summary>
		/// Gets a value indicating whether [skip branch reset on startup]. This is used when Honorbudy is stopped
		/// to execute a action that can only be safely executed while not running
		/// </summary>
		public static bool IsExecutingActionWhileHonorbuddyIsStopped { get; internal set; }

		public List<TradeSkill> TradeSkillList
		{
			get
			{
				lock (tradeSkillLocker)
				{
					return _tradeSkillList;
				}
			}
			private set { _tradeSkillList = value; }
		}

		// dictionary that keeps track of material list using item ID for key and number required as value
		public Dictionary<uint, int> MaterialList
		{
			get
			{
				lock (materialLocker)
				{
					return _materialList;
				}
			}
		}

		// <itemId,count>
		public DataStore DataStore { get; private set; }
		public bool IsTradeSkillsLoaded { get; private set; }

		// ReSharper disable InconsistentNaming
		// DataStore is an addon for WOW thats stores bag/ah/mail item info and more.
		public bool HasDataStoreAddon
		{
			get { return DataStore != null && DataStore.HasDataStoreAddon; }
		}

		public PbProfileSettings ProfileSettings
		{
			get { return _profileSettings; }
		}

		internal IPBComponent[] PBComponents { get; private set; }

		#endregion

		#region Overrides

		private MainForm _gui;

		public override string Name
		{
			get { return "ProfessionBuddy"; }
		}

		public override PulseFlags PulseFlags
		{
			get { return PulseFlags.All; }
		}

		public override Form ConfigurationForm
		{
			get
			{
				if (!_init)
					Initialize();
				if (!MainForm.IsValid)
				{
					_gui = new MainForm();
				}
				else
				{
					_gui.Activate();
				}
				return _gui;
			}
		}

		public override void Start()
		{
			PBLog.Debug("Start Called");
			IsRunning = true;
			AttachEvents();
			// make sure bank frame is closed on start to ensure Util.IsGBankFrameOpen is synced
			Util.CloseBankFrames();

			if (IsExecutingActionWhileHonorbuddyIsStopped)
			{
				IsExecutingActionWhileHonorbuddyIsStopped = false;
				ResetSecondaryBot();
			}
			else
			{
				// reset all actions
				Reset();
				if (DynamicCodeCompiler.CodeIsModified)
				{
					DynamicCodeCompiler.GenorateDynamicCode();
				}
			}

			if (MainForm.IsValid)
				MainForm.Instance.UpdateControls();

			if (!SecondaryBot.Initialized)
				SecondaryBot.DoInitialize();
		}

		public override void Stop()
		{
			IsRunning = false;
			DetachEvents();

			PBLog.Debug("Stop Called");
			if (MainForm.IsValid)
				MainForm.Instance.UpdateControls();
			if (SecondaryBot != null)
				SecondaryBot.Stop();
		}

		public override void Pulse()
		{
			if (SecondaryBot != null)
				SecondaryBot.Pulse();
		}

		public override void Initialize()
		{
			try
			{
				if (_init)
					return;

				PBLog.Debug("Initializing ...");

				IsTradeSkillsLoaded = false;
				TradeSkillList = new List<TradeSkill>();
				LoadTradeSkills();
				DataStore = new DataStore();
				DataStore.ImportDataStore();
				TradeskillTools = LoadTradeskillTools();
				PBComponents = LoadPBComponents();

				// load the previous
				BotBase bot =
					BotManager.Instance.Bots.Values.FirstOrDefault(
						b => b.Name.IndexOf(MySettings.LastBotBase, StringComparison.InvariantCultureIgnoreCase) >= 0);

				if (bot == null)
				{
					// look for combat bot, otherwise select first bot if combat bot is not found
					bot = BotManager.Instance.Bots.Values.FirstOrDefault(b => b.GetType().ToString() == "CombatBot")
						?? BotManager.Instance.Bots.Values.FirstOrDefault();
					MySettings.LastBotBase = bot.Name;
					MySettings.Save();
				}
				SecondaryBot = bot;

				bot.DoInitialize();

				try
				{
					if (!string.IsNullOrEmpty(MySettings.LastProfile))
					{
						if (IsStoreProfile(MySettings.LastProfile))
						{
							ProfileManager.LoadNew(MySettings.LastProfile);
						}
						else
						{
							LoadPBProfile(MySettings.LastProfile);
						}
					}
				}
				catch (Exception ex)
				{
					PBLog.Warn(ex.ToString());
				}

				_init = true;
			}
			catch (Exception ex)
			{
				Logging.Write(Colors.Red, ex.ToString());
			}
		}

		public override void OnSelected()
		{
			if (SecondaryBot != null)
				SecondaryBot.OnSelected();
		}

		public override void OnDeselected()
		{
			if (SecondaryBot != null)
				SecondaryBot.OnDeselected();
		}

		#endregion

		#region Callbacks

		#region OnBagUpdate

		private readonly WaitTimer _onBagUpdateTimer = new WaitTimer(TimeSpan.FromSeconds(1));

		private void OnBagUpdate(object obj, LuaEventArgs args)
		{
			if (!_onBagUpdateTimer.IsFinished)
				return;
			try
			{
				lock (tradeSkillLocker)
				{
					foreach (TradeSkill ts in TradeSkillList)
					{
						ts.PulseBags();
					}
					UpdateMaterials();
					if (MainForm.IsValid)
					{
						MainForm.Instance.RefreshTradeSkillTabs();
						MainForm.Instance.RefreshActionTree(typeof(CastSpellAction));
					}
				}
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
			}
			_onBagUpdateTimer.Reset();
		}

		#endregion

		#region OnSkillUpdate

		private readonly WaitTimer _onSkillUpdateTimer = new WaitTimer(TimeSpan.FromSeconds(1));

		private void OnSkillUpdate(object obj, LuaEventArgs args)
		{
			if (!_onSkillUpdateTimer.IsFinished)
				return;
			try
			{
				lock (tradeSkillLocker)
				{
					UpdateMaterials();
					// check if there was any tradeskills added or removed.
					WoWSkill[] skills = SupportedTradeSkills;
					bool changed;
					lock (tradeSkillLocker)
					{
						changed = !skills.All(s => TradeSkillList.Any(t => t.SkillLine == (SkillLine)s.Id))
							|| !TradeSkillList.All(t => skills.Any(s => t.SkillLine == (SkillLine)s.Id));
					}
					if (changed)
					{
						PBLog.Debug("A profession was added or removed. Reloading Tradeskills (OnSkillUpdateTimerCB)");
						OnTradeSkillsLoaded += Professionbuddy_OnTradeSkillsLoaded;
						LoadTradeSkills();
					}
					else
					{
						PBLog.Debug("Updated tradeskills from OnSkillUpdateTimerCB");
						lock (tradeSkillLocker)
						{
							foreach (TradeSkill ts in TradeSkillList)
							{
								ts.PulseSkill();
							}
						}
						if (MainForm.IsValid)
						{
							MainForm.Instance.RefreshTradeSkillTabs();
							MainForm.Instance.RefreshActionTree(typeof(CastSpellAction));
						}
					}
				}
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
			}
			_onSkillUpdateTimer.Reset();
		}

		#endregion

		public void Professionbuddy_OnTradeSkillsLoaded(object sender, EventArgs e)
		{
			if (MainForm.IsValid)
				MainForm.Instance.InitTradeSkillTab();
			OnTradeSkillsLoaded -= Professionbuddy_OnTradeSkillsLoaded;
		}

		private static bool IsStoreProfile(string path)
		{
			return !string.IsNullOrEmpty(path) && path.StartsWith("store://");
		}

		private void Profile_OnUnknownProfileElement(object sender, UnknownProfileElementEventArgs e)
		{
			if (e.Element.Ancestors().Any(Util.IsProfessionbuddyProfile))
			{
				e.Handled = true;
			}
		}

		private static void Profile_OnNewOuterProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
		{
			var xmlElement = args.NewProfile.XmlElement;

			if (!Util.IsProfessionbuddyProfile(xmlElement))
				return;

			// prevents HB from reloading current profile when bot is started.
			if (!Instance.IsRunning && ProfileManager.XmlLocation == Instance.CurrentProfile.XmlPath)
				return;

			System.Action loadProfile = () =>
			{
				LoadPBProfile(ProfileManager.XmlLocation, args.NewProfile.XmlElement);
				if (MainForm.IsValid)
				{
					MainForm.Instance.ActionTree.SuspendLayout();
					if (Instance.ProfileSettings.SettingsDictionary.Count > 0)
						MainForm.Instance.AddProfileSettingsTab();
					else
						MainForm.Instance.RemoveProfileSettingsTab();
					MainForm.Instance.ActionTree.ResumeLayout();
				}
			};

			if (Instance.IsRunning && TreeRoot.IsRunning)
			{
				Util.ExecuteActionWhileBotIsStopped(loadProfile,
					"Changing profiles (Profile_OnNewOuterProfileLoaded)");
			}
			else
			{
				loadProfile();
			}
		}

		#region OnSpellsChanged

		private readonly WaitTimer _onSpellsChangedTimer = new WaitTimer(TimeSpan.FromSeconds(1));

		private void OnSpellsChanged(object obj, LuaEventArgs args)
		{
			if (!_onSpellsChangedTimer.IsFinished)
				return;
			try
			{
				foreach (TradeSkill ts in TradeSkillList)
				{
					ts.PulseSkill();
				}
				if (MainForm.IsValid)
				{
					MainForm.Instance.InitTradeSkillTab();
					MainForm.Instance.RefreshActionTree(typeof(CastSpellAction));
				}
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
			}
			_onSpellsChangedTimer.Reset();
		}

		#endregion

		#endregion

		#region Behavior Tree

		private Composite _root;
		private BotBase _secondaryBot;
		private bool _calledStart;

		public PbProfile CurrentProfile { get; private set; }

		public override Composite Root
		{
			get
			{
				return _root ??
					(_root = new ActionRunCoroutine(
						async ctx => await Branch || await ExecuteSecondaryBot()));
			}
		}

		public PBBranch Branch
		{
			get { return CurrentProfile.Branch; }
		}

		public BotBase SecondaryBot
		{
			get { return _secondaryBot; }
			set
			{
				if (_secondaryBot == value)
					return;

				try
				{
					if (_secondaryBot != null)
						_secondaryBot.OnDeselected();
					if (value != null)
						value.OnSelected();
				}
				catch (Exception ex)
				{
					Logging.WriteException(ex);
				}
				finally
				{
					_secondaryBot = value;
					_calledStart = false;
				}
			}
		}

		public async Task<bool> ExecuteSecondaryBot()
		{
			if (_secondaryBot == null || _secondaryBot.Root == null)
				return false;

			if (!_calledStart)
			{
				try
				{
					_secondaryBot.Start();
				}
				finally
				{
					_calledStart = true;
				}
			}
			return await _secondaryBot.Root.ExecuteCoroutine();
		}

		public void Reset()
		{
			ResetBranch();
			ResetSecondaryBot();
		}

		public void ResetBranch()
		{
			Branch.Reset();
		}

		public void ResetSecondaryBot()
		{
			_calledStart = false;
		}

		#endregion

		#region Misc

		private static bool _init;
		private static readonly object tradeSkillLocker = new object();
		private static readonly object materialLocker = new object();


		private WoWSkill[] SupportedTradeSkills
		{
			get
			{
				IEnumerable<WoWSkill> skillList = from skill in TradeSkill.SupportedSkills
												  select StyxWoW.Me.GetSkill(skill) into skill
												  where skill.MaxValue > 0
												  select skill;

				return skillList.ToArray();
			}
		}

		void AttachEvents()
		{
			Lua.Events.AttachEvent("BAG_UPDATE", OnBagUpdate);
			Lua.Events.AttachEvent("SKILL_LINES_CHANGED", OnSkillUpdate);
			Lua.Events.AttachEvent("SPELLS_CHANGED", OnSpellsChanged);

			Lua.Events.AttachEvent("GUILDBANKFRAME_OPENED", Util.OnGBankFrameOpened);
			Lua.Events.AttachEvent("GUILDBANKFRAME_CLOSED", Util.OnGBankFrameClosed);

			Lua.Events.AttachEvent("BANKFRAME_OPENED", Util.OnBankFrameOpened);
			Lua.Events.AttachEvent("BANKFRAME_CLOSED", Util.OnBankFrameClosed);
		}

		void DetachEvents()
		{
			Lua.Events.DetachEvent("BAG_UPDATE", OnBagUpdate);
			Lua.Events.DetachEvent("SKILL_LINES_CHANGED", OnSkillUpdate);
			Lua.Events.DetachEvent("SPELLS_CHANGED", OnSpellsChanged);

			Lua.Events.DetachEvent("GUILDBANKFRAME_OPENED", Util.OnGBankFrameOpened);
			Lua.Events.DetachEvent("GUILDBANKFRAME_CLOSED", Util.OnGBankFrameClosed);

			Lua.Events.DetachEvent("BANKFRAME_OPENED", Util.OnBankFrameOpened);
			Lua.Events.DetachEvent("BANKFRAME_CLOSED", Util.OnBankFrameClosed);
		}

		public void LoadTradeSkills()
		{
			try
			{
				using (StyxWoW.Memory.AcquireFrame())
				{
					WoWSkill[] skills = SupportedTradeSkills;
					lock (tradeSkillLocker)
					{
						IEnumerable<TradeSkill> existing = TradeSkillList.Where(t => skills.Any(s => t.SkillLine == (SkillLine)s.Id));
						SkillLine[] added = (from s in skills
							where TradeSkillList.All(t => t.SkillLine != (SkillLine)s.Id)
							select (SkillLine)s.Id).ToArray();
						IEnumerable<WoWSkill> removed = from t in TradeSkillList
							where skills.All(s => t.SkillLine != (SkillLine)s.Id)
							select t.WoWSkill;

						foreach (TradeSkill ts in existing)
						{
							PBLog.Log("Updating TradeSkill {0}", ts.Name);
							ts.Update();
						}
						if (added.Any())
						{
							foreach (TradeSkill ts in TradeSkill.GetTradeSkills(added))
							{
								PBLog.Log("Adding TradeSkill {0}", ts.Name);
								TradeSkillList.Add(ts);
							}
						}
						foreach (WoWSkill skill in removed.ToList())
						{
							PBLog.Log("Removing TradeSkill {0}", skill.Name);
							TradeSkillList.RemoveAll(t => t.SkillLine == (SkillLine)skill.Id);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logging.Write(Colors.Red, ex.ToString());
			}
			finally
			{
				PBLog.Log("Done Loading Tradeskills.");
				IsTradeSkillsLoaded = true;
				if (OnTradeSkillsLoaded != null)
				{
					OnTradeSkillsLoaded(this, null);
				}
			}
		}

		public void UpdateMaterials()
		{
			if (!_init)
				return;
			try
			{
				lock (materialLocker)
				{
					_materialList.Clear();
					List<CastSpellAction> castSpellList = CastSpellAction.GetCastSpellActionList(Branch);
					if (castSpellList == null)
						return;
					foreach (CastSpellAction ca in castSpellList)
					{
						if (!ca.IsRecipe || ca.RepeatType == CastSpellAction.RepeatCalculationType.Craftable)
							continue;
						foreach (Ingredient ingred in ca.Recipe.Ingredients)
						{
							_materialList[ingred.ID] = _materialList.ContainsKey(ingred.ID)
								? _materialList[ingred.ID] +
								(ca.CalculatedRepeat > 0 ? (int)ingred.Required * (ca.CalculatedRepeat - ca.Casted) : 0)
								: (ca.CalculatedRepeat > 0 ? (int)ingred.Required * (ca.CalculatedRepeat - ca.Casted) : 0);
						}
					}
				}
			}
			catch (Exception ex)
			{
				PBLog.Warn(ex.ToString());
			}
		}

		internal static void LoadPBProfile(string path, XElement element = null)
		{
			PbProfile profile = null;
			bool isStore = IsStoreProfile(path);
			if (!string.IsNullOrEmpty(path))
			{
				if (isStore)
				{
					PBLog.Log("Loading profile {0} from store", Path.GetFileName(path));
					profile = PbProfile.LoadFromXml(element, path);
				}
				else
				{
					if (!File.Exists(path))
					{
						PBLog.Warn("Profile: {0} does not exist", path);
						return;
					}
					PBLog.Log("Loading profile {0} from file", Path.GetFileName(path));
					profile = PbProfile.LoadFromFile(path);
				}
				Instance.MySettings.LastProfile = path;
			}
			else if (element != null)
			{
				profile = PbProfile.LoadFromXml(element, path);
				PBLog.Log("Loading profile from Xml element");
			}

			AllowProfileEditing = !isStore;
			if (profile == null)
				return;

			Instance.CurrentProfile = profile;
			Instance.MySettings.LastProfile = path;
			Instance.ProfileSettings.Load();
			DynamicCodeCompiler.GenorateDynamicCode();
			Instance.UpdateMaterials();
			if (MainForm.IsValid)
			{
				if (isStore)
					MainForm.Instance.HideProfileEditorTab();
				else
					MainForm.Instance.ShowProfileEditorTab();
				MainForm.Instance.InitActionTree();
				MainForm.Instance.RefreshTradeSkillTabs();
				MainForm.Instance.UpdateControls();
			}

			Instance.MySettings.Save();
		}

		internal static void NewProfile()
		{
			Instance.CurrentProfile = new PbProfile(new PBBranch());
			if (MainForm.IsValid)
			{
				MainForm.Instance.ShowProfileEditorTab();
				MainForm.Instance.InitActionTree();
				MainForm.Instance.RefreshTradeSkillTabs();
				MainForm.Instance.RemoveProfileSettingsTab();
				MainForm.Instance.UpdateControls();
			}
		}

		private List<uint> LoadTradeskillTools()
		{
			return new List<uint>
			{
				5956u, 4470u, 86559u, 6218u, 6339u, 11145u, 11130u, 16207u, 22461u, 22462u,
				22463u, 44452u, 92370u, 82981u, 84406u, 16006u, 30542u, 18984u, 94748u, 95703u,
				10647u, 18232u, 34113u, 40772u, 84429u, 7148u, 18587u, 84430u, 49040u, 40768u,
				40769u, 18986u, 30544u, 48933u, 23821u, 87215u, 87214u, 10498u, 6219u, 19970u,
				6367u, 12225u, 45991u, 50287u, 34832u, 6366u, 33223u, 6256u, 19971u, 45992u,
				19972u, 44050u, 45858u, 19969u, 25978u, 34836u, 6365u, 44012u, 33820u, 88535u,
				84661u, 84660u, 19727u, 85663u, 39505u, 20815u, 20824u, 15846u, 20723u, 1959u,
				9465u, 1819u, 778u, 2901u, 756u, 87213u, 40893u, 40892u, 12709u, 7005u, 19901u
			};
		}

		private static IPBComponent[] LoadPBComponents()
		{
			bool codeIsModified = DynamicCodeCompiler.CodeIsModified;
			try
			{
				return (from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
					where typeof(IPBComponent).IsAssignableFrom(type) && !type.IsAbstract
					select (IPBComponent)Activator.CreateInstance(type)).ToArray();
			}
			finally
			{
				DynamicCodeCompiler.CodeIsModified = codeIsModified;
			}
		}

		public static void ChangeSecondaryBot(string botName)
		{
			BotBase bot = Util.GetBotByName(botName);
			if (bot != null)
				ChangeSecondaryBot(bot);
			else
				PBLog.Warn("Bot with name: {0} was not found", botName);
		}

		public static void ChangeSecondaryBot(BotBase bot)
		{
			if (bot == null)
				return;

			if (Instance.SecondaryBot != null && Instance.SecondaryBot.Name == bot.Name)
				return;

			Util.ExecuteActionWhileBotIsStopped(
				() =>
				{
					Instance.SecondaryBot = bot;
					if (!bot.Initialized)
						bot.DoInitialize();
					if (ProfessionBuddySettings.Instance.LastBotBase != bot.Name)
					{
						ProfessionBuddySettings.Instance.LastBotBase = bot.Name;
						ProfessionBuddySettings.Instance.Save();
					}
					if (MainForm.IsValid)
						MainForm.Instance.UpdateBotCombo();
				}, "Changing SecondaryBot");
		}

		public static void DisableProfileEditor()
		{
			AllowProfileEditing = false;
			if (MainForm.IsValid)
				MainForm.Instance.HideProfileEditorTab();
		}

		#endregion
	}
}
