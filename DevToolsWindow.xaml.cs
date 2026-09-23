using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using MahApps.Metro.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.Helpers;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace CopilotBuddy
{
    public partial class DevToolsWindow : MetroWindow
    {
        private class WAura
        {
            public int SpellId { get; set; }

            public string Name { get; set; }

            public string Creator { get; set; }

            public ulong CreatorGuid { get; set; }

            public TimeSpan TimeLeft { get; set; }

            public uint StackCount { get; set; }

            public AuraFlags Flags { get; set; }
        }

        private class WMerchantItem
        {
            public uint ItemId { get; set; }

            public string Name { get; set; }

            public int NumAvailable { get; set; }

            public ulong BuyPrice { get; set; }

            public int Quantity { get; set; }
        }

        internal delegate void AddNewSnippletCallback(string text);

        private class DisableSelectionChangeHandler : IDisposable
        {
            private readonly System.Windows.Controls.ComboBox _combo;

            private readonly SelectionChangedEventHandler _handler;

            public DisableSelectionChangeHandler(System.Windows.Controls.ComboBox combo, SelectionChangedEventHandler handler)
            {
                _combo = combo;
                _handler = handler;
                _combo.SelectionChanged -= _handler;
            }

            public void Dispose()
            {
                _combo.SelectionChanged += _handler;
            }
        }

        public class ConsoleSettings : Settings
        {
            internal static readonly ConsoleSettings Instance;

            [Setting]
            [Styx.Helpers.DefaultValue(new string[] { "" })]
            public string[] CSharpSnippets { get; set; }

            [Styx.Helpers.DefaultValue(new string[] { "Untitled1" })]
            [Setting]
            public string[] CSharpSnippetNames { get; set; }

            [Setting]
            [Styx.Helpers.DefaultValue(0)]
            public int CSharpSelectedIndex { get; set; }

            [Setting]
            [Styx.Helpers.DefaultValue(Keys.None)]
            public Keys Keybind { get; set; }

            private ConsoleSettings()
                : base(Path.Combine(Settings.SettingsDirectory, "Console.xml"))
            {
            }

            static ConsoleSettings()
            {
                Instance = new ConsoleSettings();
            }
        }

        private class WObject
        {
            public uint Entry { get; set; }

            public string Name { get; set; }

            public ulong Guid { get; set; }

            public WoWPoint Location { get; set; }

            public double Distance { get; set; }

            public WoWObjectType Type { get; set; }
        }

        private class WGameObject : WObject
        {
            public WoWLockType LockType { get; set; }

            public WoWGameObjectType SubType { get; set; }

            public WoWSpellFocus SpellFocus { get; set; }
        }

        private class WItem : WObject
        {
            public uint StackCount { get; set; }
        }

        private class WUnit : WObject
        {
            public uint FactionId { get; set; }
        }

        private sealed class CurrentTargetInfo
        {
            public string Entry { get; private set; }

            public string FactionId { get; private set; }

            public string FactionName { get; private set; }

            public string GroupId0 { get; private set; }

            public string GroupId1 { get; private set; }

            public string Location { get; private set; }

            public string Name { get; private set; }

            public string VendorType { get; private set; }

            public CurrentTargetInfo()
            {
                Refresh();
            }

            public void Refresh()
            {
                WoWUnit currentTarget;
                if (StyxWoW.IsInGame && !(StyxWoW.Me == null) && !((currentTarget = StyxWoW.Me.CurrentTarget) == null))
                {
                    Entry = currentTarget.Entry.ToString();
                    FactionId = currentTarget.FactionId.ToString();
                    FactionName = currentTarget.FactionTemplate.Faction.Name;
                    Location = string.Format("{0}, {1}, {2} (Dist : {3})", currentTarget.Location.X.ToString("F2", CultureInfo.InvariantCulture), currentTarget.Location.Y.ToString("F2", CultureInfo.InvariantCulture), currentTarget.Location.Z.ToString("F2", CultureInfo.InvariantCulture), currentTarget.Distance.ToString("F2", CultureInfo.InvariantCulture));
                    Name = currentTarget.Name;
                    if (currentTarget.GetCachedInfo(out var info))
                    {
                        GroupId0 = info.GroupID[0].ToString();
                        GroupId1 = info.GroupID[1].ToString();
                    }
                    VendorType = ((UnitNPCFlags)(currentTarget.NpcFlags & 0xFFFFFFFEu)).ToString();
                }
                else
                {
                    string text = (VendorType = "N/A");
                    string text3 = (Name = text);
                    string text5 = (Location = text3);
                    string text7 = (GroupId1 = text5);
                    string text9 = (GroupId0 = text7);
                    string text11 = (FactionName = text9);
                    string entry = (FactionId = text11);
                    Entry = entry;
                }
            }
        }

        public class PlayerQuestInfo
        {
            public uint Id { get; private set; }

            public string Name { get; private set; }

            public ReadOnlyCollection<Quest.QuestObjective> Objectives { get; private set; }

            public PlayerQuestInfo(PlayerQuest pq)
            {
                Id = pq.Id;
                Name = pq.Name;
                Objectives = pq.GetObjectives().AsReadOnly();
            }
        }

        private const string Prefix = "\r\n\t\t\t using System;   \r\n\t\t\t using System.Reflection;   \r\n\t\t\t using System.Data;   \r\n\t\t\t using System.Data.SQLite;   \r\n\t\t\t using System.Threading;   \r\n\t\t\t using System.Diagnostics;   \r\n\t\t\t using System.Drawing;   \r\n\t\t\t using System.Collections.Generic;   \r\n\t\t\t using System.Collections;    \r\n\t\t\t using System.Linq;    \r\n\t\t\t using System.Text;    \r\n\t\t\t using System.IO;    \r\n\t\t\t using System.Windows.Forms;   \r\n\t\t\t using System.Text.RegularExpressions;   \r\n\t\t\t using System.Globalization;   \r\n\t\t\t using System.Xml.Linq;   \r\n\r\n\t\t\t using Styx;   \r\n\t\t\t using Styx.Helpers;   \r\n\t\t\t using Styx.Common;\r\n\t\t\t using Styx.WoWInternals;   \r\n\t\t\t using Styx.WoWInternals.WoWObjects;   \r\n\t\t\t using Styx.WoWInternals.WoWCache;   \r\n\t\t\t using Styx.CommonBot.AreaManagement;     \r\n\t\t\t using Styx.CommonBot.Frames; \r\n\t\t\t using Styx.Pathing;   \r\n\t\t\t using Styx.CommonBot.Profiles;   \r\n\t\t\t using Styx.Plugins;    \r\n\t\t\t using Styx.WoWInternals.World;   \r\n\t\t\t using Styx.CommonBot.POI;   \r\n\t\t\t using Styx.CommonBot.Database;   \r\n\t\t\t using Styx.WoWInternals.DBC;   \r\n\t\t\t using Styx.Patchables;   \r\n\t\t\t using Tripper.Tools.Math;   \r\n\t\t\t using Styx.CommonBot; \r\n\r\n\t\t\t public static class Driver   \r\n\t\t\t {   \r\n\t\t\t static LocalPlayer Me = StyxWoW.Me;   \r\npublic static void Run()\r\n{\r\n";

        private const string Postfix = " \r\n} \r\npublic static void Log (object obj)\r\n{\r\n\tCopilotBuddy.DevToolsWindow.Instance.Log(obj.ToString());\r\n}\r\npublic static void Log ( string format, params object[] arg)\r\n{\r\n\tCopilotBuddy.DevToolsWindow.Instance.Log(format,arg);\r\n}\r\npublic static void ClearLog ()\r\n{\r\n\tCopilotBuddy.DevToolsWindow.Instance.ClearLog();\r\n}\r\nstatic List<WoWGameObject> GameObjects {get{return ObjectManager.GetObjectsOfType<WoWGameObject>();}}\r\nstatic List<WoWObject> Objects {get{return ObjectManager.ObjectList;}}\r\nstatic List<WoWUnit> Units {get{return ObjectManager.GetObjectsOfType<WoWUnit>();}}\r\nstatic List<WoWPlayer> Players {get{return ObjectManager.GetObjectsOfType<WoWPlayer>();}}\r\nstatic List<WoWItem> Items {get{return ObjectManager.GetObjectsOfType<WoWItem>();}}\r\nstatic List<WoWDynamicObject> DynamicObjects {get{return ObjectManager.GetObjectsOfType<WoWDynamicObject>();}}\r\nstatic List<WoWAreaTrigger> AreaTriggers {get{return ObjectManager.GetObjectsOfType<WoWAreaTrigger>();}}\r\n}";

        private const string _giverItemIdParserFormat = "_\\[(?<giverItemId>\\d+?)\\]={{name_enus:'{0}',quality:";

        private ulong _auraDisplayUnitGuid;

        private readonly ObservableCollection<WGameObject> _gameobjects = new ObservableCollection<WGameObject>();

        private readonly ObservableCollection<WItem> _items = new ObservableCollection<WItem>();

        private readonly ObservableCollection<WObject> _loots = new ObservableCollection<WObject>();

        private readonly ObservableCollection<WUnit> _targets = new ObservableCollection<WUnit>();

        private readonly ObservableCollection<WUnit> _units = new ObservableCollection<WUnit>();

        private readonly ObservableCollection<WAura> _aurasOnMe = new ObservableCollection<WAura>();

        private readonly ObservableCollection<WAura> _aurasOnTarget = new ObservableCollection<WAura>();

        private readonly ObservableCollection<WAura> _aurasOnUnit = new ObservableCollection<WAura>();

        private readonly ObservableCollection<WMerchantItem> _merchantItems = new ObservableCollection<WMerchantItem>();

        private WoWPoint? _lastBlackspotPos;

        private bool copyAllQuestOverrides;

        private bool copyCTXmlInfo;

        private bool generateBlackspot;

        private bool generateHotspot;

        private bool refreshCTInfo;

        private bool refreshObjectList;

        private bool refreshAuras;

        private bool refreshMerchantItems;

        private bool refreshPosition;

        private bool refreshQuests;

        private bool writeLocalPlayerLocation;

        public static readonly string TempFolder;

        private bool _unsavedConsoleChanges;

        private bool _keyBindMode;

        private Keys _cachedKey;

        private Brush _originalKeybindKeyForegroundBrush;

        private GridViewColumnHeader _lastHeaderClicked;

        private ListSortDirection _lastDirection;

        private static readonly Regex _questDataParser;

        private static readonly string _startItemNameParserFormat;

        private string _woWLocale;

        public static DevToolsWindow Instance { get; private set; }

        private string WoWLocale => _woWLocale ?? (_woWLocale = Lua.GetReturnVal<string>("return GetLocale()", 0u));

        static DevToolsWindow()
        {
            TempFolder = Path.Combine(Utilities.AssemblyDirectory, "CompiledAssemblies");
            _questDataParser = new Regex("\\{ type: '\\d+', point: '(?<point>start|end|sourcestart)', name: '(?<name>.+?)', [\\s|\\S]*?\\]\\], id: (?<id>\\d+)[\\s|\\S]*?(?:\\}|(?:item: '(?<item>.+?)')\\})", RegexOptions.Multiline);
            _startItemNameParserFormat = "(?<=_\\[{0}\\]={{name_enus:').+?(?=',)";
        }

        public DevToolsWindow()
        {
            Instance = this;
            InitializeComponent();
            if (!TreeRoot.IsRunningUnpaused)
            {
                using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
                {
                    ObjectManager.Update();
                    RefreshPositionInfo(GetPositionInfo());
                    RefreshCurrentTargetInfo(new CurrentTargetInfo());
                    RefreshQuestInfo(GetQuestInfos());
                    RefreshTargets(GetTargets());
                    RefreshLoots(GetLoots());
                }
            }
            else
            {
                refreshQuests = true;
                refreshPosition = true;
                refreshCTInfo = true;
            }
            lvGameObjectsDump.ItemsSource = _gameobjects;
            lvUnitsDump.ItemsSource = _units;
            lvItemsDump.ItemsSource = _items;
            lvAurasOnMe.ItemsSource = _aurasOnMe;
            lvAurasOnTarget.ItemsSource = _aurasOnTarget;
            lvAurasOnUnit.ItemsSource = _aurasOnUnit;
            lvMerchantItems.ItemsSource = _merchantItems;
            lvTargetsList.ItemsSource = _targets;
            lvLootList.ItemsSource = _loots;
            BotEvents.OnPulse += BotEvents_OnPulse;
            if (UISettings.Instance.DevToolsWindowWidth > 0)
            {
                base.Width = UISettings.Instance.DevToolsWindowWidth;
            }
            if (UISettings.Instance.DevToolsWindowHeight > 0)
            {
                base.Height = UISettings.Instance.DevToolsWindowHeight;
            }
            base.Left = UISettings.Instance.DevToolsWindowLocationX;
            base.Top = UISettings.Instance.DevToolsWindowLocationY;
            base.WindowState = UISettings.Instance.DevToolsWindowState;
        }

        internal static void Display(Window owner = null)
        {
            if (owner == null)
            {
                owner = System.Windows.Application.Current.MainWindow;
            }
            if (Instance != null && Instance.IsLoaded)
            {
                if (Instance.WindowState == WindowState.Minimized)
                {
                    Instance.WindowState = WindowState.Normal;
                }
                else
                {
                    Instance.Activate();
                }
            }
            else
            {
                DevToolsWindow devToolsWindow = new DevToolsWindow();
                devToolsWindow.Owner = owner;
                devToolsWindow.Show();
            }
        }

        private List<WAura> GetAurasOnMe()
        {
            return GetAurasOnUnit(StyxWoW.Me);
        }

        private List<WAura> GetAurasOnTarget()
        {
            if (StyxWoW.Me == null)
            {
                return new List<WAura>();
            }
            return GetAurasOnUnit(StyxWoW.Me.CurrentTarget);
        }

        private List<WAura> GetAurasOnUnit()
        {
            WoWUnit objectByGuid = ObjectManager.GetObjectByGuid<WoWUnit>(_auraDisplayUnitGuid);
            if (objectByGuid != null)
            {
                tabAurasOnUnit.Header = "Auras on " + objectByGuid.SafeName;
            }
            return GetAurasOnUnit(objectByGuid);
        }

        private List<WAura> GetAurasOnUnit(WoWUnit unit)
        {
            if (unit == null)
            {
                return new List<WAura>();
            }
            return unit.Auras.Values.OrderBy((WoWAura a) => a.Name).Select(delegate(WoWAura a)
            {
                WoWUnit objectByGuid = ObjectManager.GetObjectByGuid<WoWUnit>(a.CreatorGuid);
                return new WAura
                {
                    SpellId = a.SpellId,
                    Name = a.Name,
                    TimeLeft = a.TimeLeft,
                    Creator = ((objectByGuid != null) ? objectByGuid.SafeName : a.CreatorGuid.ToString()),
                    CreatorGuid = a.CreatorGuid,
                    StackCount = a.StackCount,
                    Flags = a.Flags
                };
            }).ToList();
        }

        private void RefreshAurasOnMe(List<WAura> auras)
        {
            _aurasOnMe.Clear();
            foreach (WAura aura in auras)
            {
                _aurasOnMe.Add(aura);
            }
        }

        private void RefreshAurasOnTarget(List<WAura> auras)
        {
            _aurasOnTarget.Clear();
            foreach (WAura aura in auras)
            {
                _aurasOnTarget.Add(aura);
            }
        }

        private void RefreshAurasOnUnit(List<WAura> auras)
        {
            _aurasOnUnit.Clear();
            foreach (WAura aura in auras)
            {
                _aurasOnUnit.Add(aura);
            }
        }

        private void BtnRefreshAuraList_OnClick(object sender, RoutedEventArgs e)
        {
            if (StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    refreshAuras = true;
                    return;
                }
                ObjectManager.Update();
                RefreshAurasOnMe(GetAurasOnMe());
                RefreshAurasOnTarget(GetAurasOnTarget());
                RefreshAurasOnUnit(GetAurasOnUnit());
            }
        }

        private void CopyAuraInfo(Selector listView)
        {
            if (listView.SelectedItem is WAura wAura)
            {
                string xml = $"<Aura Name=\"{wAura.Name}\" SpellId=\"{wAura.SpellId}\" StackCount=\"{wAura.StackCount}\" Creator=\"{wAura.Creator}\" CreatorGuid=\"{wAura.CreatorGuid}\" TimeLeft=\"{wAura.TimeLeft}\" Flags=\"{wAura.Flags}\" />";
                Logging.Write(xml);
                base.Dispatcher.Invoke(delegate
                {
                    SetClipboardText(xml);
                });
            }
        }

        private void CopyAuraInfoMe_OnClick(object sender, RoutedEventArgs e)
        {
            CopyAuraInfo(lvAurasOnMe);
        }

        private void CopyAuraInfoTarget_OnClick(object sender, RoutedEventArgs e)
        {
            CopyAuraInfo(lvAurasOnTarget);
        }

        private void CopyAuraInfoUnit_OnClick(object sender, RoutedEventArgs e)
        {
            CopyAuraInfo(lvAurasOnUnit);
        }

        private void ShowAurasOnUnit(ulong unitGuid)
        {
            _auraDisplayUnitGuid = unitGuid;
            TabItem tabItem = tabAuras;
            tabAurasOnUnit.IsSelected = true;
            tabItem.IsSelected = true;
            tabAurasOnUnit.Visibility = Visibility.Visible;
        }

        private List<WObject> GetLoots()
        {
            if (StyxWoW.Me == null)
            {
                return new List<WObject>();
            }
            WoWPoint myLoc = StyxWoW.Me.Location;
            return LootTargeting.Instance.LootingList.Select((WoWObject o) => new WObject
            {
                Distance = o.Location.Distance(myLoc),
                Name = o.Name
            }).ToList();
        }

        private void RefreshLoots(List<WObject> loots)
        {
            _loots.Clear();
            foreach (WObject loot in loots)
            {
                _loots.Add(loot);
            }
        }

        private List<WUnit> GetTargets()
        {
            if (StyxWoW.Me == null)
            {
                return new List<WUnit>();
            }
            WoWPoint myLoc = StyxWoW.Me.Location;
            return Targeting.Instance.TargetList.Select((WoWUnit o) => new WUnit
            {
                Distance = o.Location.Distance(myLoc),
                Name = o.Name
            }).ToList();
        }

        private void RefreshTargets(List<WUnit> targets)
        {
            _targets.Clear();
            foreach (WUnit target in targets)
            {
                _targets.Add(target);
            }
        }

        private List<WMerchantItem> GetMerchantItems()
        {
            if (!MerchantFrame.Instance.IsVisible)
            {
                return new List<WMerchantItem>();
            }
            return (from m in MerchantFrame.Instance.GetAllMerchantItems()
                select new WMerchantItem
                {
                    ItemId = m.ItemId,
                    Name = m.Name,
                    BuyPrice = m.BuyPrice,
                    NumAvailable = m.NumAvailable,
                    Quantity = m.Quantity
                }).ToList();
        }

        private void RefreshMerchantItems(List<WMerchantItem> items)
        {
            _merchantItems.Clear();
            foreach (WMerchantItem item in items)
            {
                _merchantItems.Add(item);
            }
        }

        private void BtnRefreshMerchantItemsList_OnClick(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            if (TreeRoot.IsRunningUnpaused)
            {
                refreshMerchantItems = true;
                return;
            }
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                ObjectManager.Update();
                RefreshMerchantItems(GetMerchantItems());
            }
        }

        private void CopyMerchantItemInfo_OnClick(object sender, RoutedEventArgs e)
        {
            if (lvMerchantItems.SelectedItem is WMerchantItem wMerchantItem)
            {
                string xml = $"<Item Name=\"{wMerchantItem.Name}\" ItemId=\"{wMerchantItem.ItemId}\" NumAvailable=\"{wMerchantItem.NumAvailable}\" BuyPrice=\"{wMerchantItem.BuyPrice}\" Quantity=\"{wMerchantItem.Quantity}\" />";
                Logging.Write(xml);
                base.Dispatcher.Invoke(delegate
                {
                    SetClipboardText(xml);
                });
            }
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.Source is System.Windows.Controls.TabControl) || !(tabControl.SelectedItem is TabItem tabItem))
            {
                return;
            }
            if (tabItem.Name == "tabAuras")
            {
                if (!TreeRoot.IsRunningUnpaused)
                {
                    using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
                    {
                        ObjectManager.Update();
                        RefreshAurasOnMe(GetAurasOnMe());
                        RefreshAurasOnTarget(GetAurasOnTarget());
                        RefreshAurasOnUnit(GetAurasOnUnit());
                        return;
                    }
                }
                refreshAuras = true;
            }
            else if (tabItem.Name == "tabObjects")
            {
                if (!TreeRoot.IsRunningUnpaused)
                {
                    using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
                    {
                        ObjectManager.Update();
                        RefreshGameObjects(GetGameObjects());
                        RefreshUnits(GetUnits());
                        RefreshItems(GetItems());
                        return;
                    }
                }
                refreshObjectList = true;
            }
            else
            {
                if (!(tabItem.Name == "tabMerchantItems"))
                {
                    return;
                }
                if (!TreeRoot.IsRunningUnpaused)
                {
                    using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
                    {
                        ObjectManager.Update();
                        RefreshMerchantItems(GetMerchantItems());
                        return;
                    }
                }
                refreshMerchantItems = true;
            }
        }

        private void BotEvents_OnPulse(BotEvents.PulseEventArgs args)
        {
            List<WUnit> targets = GetTargets();
            List<WObject> loots = GetLoots();
            List<Action> list = new List<Action>();
            list.Add(delegate
            {
                RefreshTargets(targets);
            });
            list.Add(delegate
            {
                RefreshLoots(loots);
            });
            List<Action> list2 = list;
            if (refreshPosition)
            {
                string posInfo = GetPositionInfo();
                list2.Add(delegate
                {
                    RefreshPositionInfo(posInfo);
                });
                refreshPosition = false;
            }
            if (refreshCTInfo)
            {
                CurrentTargetInfo ctInfo = new CurrentTargetInfo();
                list2.Add(delegate
                {
                    RefreshCurrentTargetInfo(ctInfo);
                });
                refreshCTInfo = false;
            }
            if (refreshObjectList)
            {
                List<WGameObject> gameObjects = GetGameObjects();
                List<WUnit> units = GetUnits();
                List<WItem> items = GetItems();
                list2.Add(delegate
                {
                    RefreshGameObjects(gameObjects);
                    RefreshUnits(units);
                    RefreshItems(items);
                });
                refreshObjectList = false;
            }
            if (refreshAuras)
            {
                List<WAura> aurasOnMe = GetAurasOnMe();
                List<WAura> aurasOnTarget = GetAurasOnTarget();
                list2.Add(delegate
                {
                    RefreshAurasOnMe(aurasOnMe);
                    RefreshAurasOnTarget(aurasOnTarget);
                    RefreshAurasOnUnit(GetAurasOnUnit());
                });
                refreshAuras = false;
            }
            if (refreshMerchantItems)
            {
                List<WMerchantItem> merchantItems = GetMerchantItems();
                list2.Add(delegate
                {
                    RefreshMerchantItems(merchantItems);
                });
                refreshMerchantItems = false;
            }
            if (refreshQuests)
            {
                List<PlayerQuestInfo> questInfo = GetQuestInfos();
                list2.Add(delegate
                {
                    RefreshQuestInfo(questInfo);
                });
                refreshQuests = false;
            }
            if (copyCTXmlInfo)
            {
                CopyCTXmlInfo();
                copyCTXmlInfo = false;
            }
            if (generateBlackspot)
            {
                GenerateBlackspot();
                generateBlackspot = false;
            }
            if (generateHotspot)
            {
                GenerateHotspot();
                generateHotspot = false;
            }
            if (copyAllQuestOverrides)
            {
                CopyAllQuestOverrides();
                copyAllQuestOverrides = false;
            }
            if (writeLocalPlayerLocation)
            {
                WriteLocalPlayerLocation();
                writeLocalPlayerLocation = false;
            }
            base.Dispatcher.BeginInvoke((Action<List<Action>>)delegate(List<Action> methods)
            {
                methods.ForEach(delegate(Action m)
                {
                    m();
                });
            }, list2);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _loots.Clear();
            _targets.Clear();
            _gameobjects.Clear();
            _units.Clear();
            _items.Clear();
            tvQuests.DataContext = null;
            SaveLocation();
            ConsoleSettings.Instance.Save();
            UISettings.Instance.Save();
            BotEvents.OnPulse -= BotEvents_OnPulse;
            base.OnClosing(e);
        }

        private void SaveLocation()
        {
            UISettings.Instance.DevToolsWindowHeight = (int)base.Height;
            UISettings.Instance.DevToolsWindowWidth = (int)base.Width;
            UISettings.Instance.DevToolsWindowState = base.WindowState;
            if (base.Left == base.RestoreBounds.Left && base.Top == base.RestoreBounds.Top)
            {
                UISettings.Instance.DevToolsWindowLocationX = (int)base.Left;
                UISettings.Instance.DevToolsWindowLocationY = (int)base.Top;
            }
        }

        private void SetClipboardText(string text)
        {
            System.Windows.Clipboard.SetDataObject(text);
        }

        private void TabConsole_OnLoaded(object sender, RoutedEventArgs e)
        {
            using (new DisableSelectionChangeHandler(consoleScriptSelector, ConsoleScriptSelector_OnSelectionChanged))
            {
                Styx.Common.Extensions.ForEach(ConsoleSettings.Instance.CSharpSnippetNames, delegate(string s)
                {
                    consoleScriptSelector.Items.Add(s);
                });
            }
            int num = ((ConsoleSettings.Instance.CSharpSelectedIndex < ConsoleSettings.Instance.CSharpSnippets.Length) ? ConsoleSettings.Instance.CSharpSelectedIndex : 0);
            consoleScriptSnippet.Text = ConsoleSettings.Instance.CSharpSnippets[num];
            consoleScriptSelector.Text = ConsoleSettings.Instance.CSharpSnippetNames[num];
            consoleScriptSelector.SelectedIndex = ConsoleSettings.Instance.CSharpSelectedIndex;
            consoleKeybind.Content = GetKeyString(ConsoleSettings.Instance.Keybind);
        }

        private void ConsoleScriptSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = consoleScriptSelector.SelectedIndex;
            _unsavedConsoleChanges |= RenameScript(ConsoleSettings.Instance.CSharpSelectedIndex);
            if (selectedIndex < ConsoleSettings.Instance.CSharpSnippets.Length && selectedIndex != -1)
            {
                consoleScriptSnippet.Text = ConsoleSettings.Instance.CSharpSnippets[selectedIndex];
                ConsoleSettings.Instance.CSharpSelectedIndex = selectedIndex;
                _unsavedConsoleChanges = true;
            }
        }

        private void ConsoleScriptSelector_OnLostFocus(object sender, RoutedEventArgs e)
        {
            _unsavedConsoleChanges |= RenameScript(ConsoleSettings.Instance.CSharpSelectedIndex);
        }

        private bool RenameScript(int indexToChange)
        {
            string[] cSharpSnippetNames = ConsoleSettings.Instance.CSharpSnippetNames;
            if (indexToChange < cSharpSnippetNames.Length && indexToChange >= 0 && !(cSharpSnippetNames[indexToChange] == consoleScriptSelector.Text) && !string.IsNullOrEmpty(consoleScriptSelector.Text) && indexToChange < consoleScriptSelector.Items.Count && consoleScriptSelector.Items.Count != 0)
            {
                if (cSharpSnippetNames.Where((string s, int i) => i != indexToChange && s == consoleScriptSelector.Text).Any())
                {
                    System.Windows.MessageBox.Show($"There is already a script named {consoleScriptSelector.Text}");
                    consoleScriptSelector.Text = cSharpSnippetNames[indexToChange];
                    return false;
                }
                cSharpSnippetNames[indexToChange] = consoleScriptSelector.Text;
                using (new DisableSelectionChangeHandler(consoleScriptSelector, ConsoleScriptSelector_OnSelectionChanged))
                {
                    int selectedIndex = consoleScriptSelector.SelectedIndex;
                    consoleScriptSelector.Items[indexToChange] = consoleScriptSelector.Text;
                    consoleScriptSelector.SelectedIndex = selectedIndex;
                }
                return true;
            }
            return false;
        }

        private void ConsoleNewSnipletButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewSnipplet(GetNewScriptName());
        }

        private void ConsoleDeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (consoleScriptSelector.SelectedIndex == -1 || System.Windows.MessageBox.Show("Are you sure?", "Delete this?", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            List<string> list = new List<string>(ConsoleSettings.Instance.CSharpSnippets);
            List<string> list2 = new List<string>(ConsoleSettings.Instance.CSharpSnippetNames);
            list.RemoveAt(consoleScriptSelector.SelectedIndex);
            list2.RemoveAt(consoleScriptSelector.SelectedIndex);
            ConsoleSettings.Instance.CSharpSnippets = list.ToArray();
            ConsoleSettings.Instance.CSharpSnippetNames = list2.ToArray();
            int num = consoleScriptSelector.SelectedIndex;
            using (new DisableSelectionChangeHandler(consoleScriptSelector, ConsoleScriptSelector_OnSelectionChanged))
            {
                consoleScriptSelector.Items.RemoveAt(consoleScriptSelector.SelectedIndex);
                if (num == consoleScriptSelector.Items.Count)
                {
                    num--;
                }
                if (num < 0)
                {
                    AddNewSnipplet(GetNewScriptName());
                    num++;
                }
                consoleScriptSelector.Items.Clear();
                string[] cSharpSnippetNames = ConsoleSettings.Instance.CSharpSnippetNames;
                foreach (string newItem in cSharpSnippetNames)
                {
                    consoleScriptSelector.Items.Add(newItem);
                }
            }
            consoleScriptSnippet.Text = ConsoleSettings.Instance.CSharpSnippets[num];
            consoleScriptSelector.SelectedIndex = num;
        }

        private void Console_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ConsoleCompileAndRun();
                e.Handled = true;
            }
        }

        private void ConsoleScriptSnippet_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (consoleScriptSelector.SelectedIndex >= 0)
            {
                ConsoleSettings.Instance.CSharpSnippets[consoleScriptSelector.SelectedIndex] = consoleScriptSnippet.Text;
                _unsavedConsoleChanges = true;
            }
        }

        private void ConsoleScriptSnippet_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SaveConsoleSettings();
        }

        private void ConsoleKeybind_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_keyBindMode)
            {
                SetKeybindButtonToEditMode((System.Windows.Controls.Button)sender);
                _keyBindMode = true;
            }
        }

        private void BtnConsoleRun_Click(object sender, RoutedEventArgs e)
        {
            ConsoleCompileAndRun();
        }

        private void ConsoleKeybind_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_keyBindMode)
            {
                return;
            }
            _cachedKey = (Keys)(KeyInterop.VirtualKeyFromKey(e.Key) | KeyInterop.VirtualKeyFromKey(e.SystemKey) | (int)GetModifierKeys());
            e.Handled = true;
            if (_cachedKey != Keys.None)
            {
                if (_cachedKey == Keys.Escape)
                {
                    _cachedKey = Keys.None;
                    return;
                }
                System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
                button.Content = GetKeyString(_cachedKey);
            }
        }

        private void ConsoleKeybind_OnPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_keyBindMode || IsModifierKey(_cachedKey))
            {
                return;
            }
            _keyBindMode = false;
            if (_cachedKey != ConsoleSettings.Instance.Keybind)
            {
                _unsavedConsoleChanges = true;
                ConsoleSettings.Instance.Keybind = _cachedKey;
                SaveConsoleSettings();
                HotkeysManager.Unregister("Console");
                if (_cachedKey != Keys.None)
                {
                    HotkeysManager.Register("Console", ConsoleSettings.Instance.Keybind & Keys.KeyCode, GetModifierKeys(ConsoleSettings.Instance.Keybind), ToggleConsole);
                }
            }
            SetKeybindButtonToNormalMode((System.Windows.Controls.Button)sender);
        }

        private static void ToggleConsole(Hotkey kHotkey)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Display();
                Instance.Activate();
                Instance.tabConsole.IsSelected = true;
                if (Instance.consoleScriptSnippet.IsVisible)
                {
                    Instance.consoleScriptSnippet.Focus();
                }
                else
                {
                    Instance.consoleDeleteScript.Loaded += ConsoleDeleteScript_Loaded;
                }
            });
        }

        private static void ConsoleDeleteScript_Loaded(object sender, RoutedEventArgs e)
        {
            if (Instance.consoleScriptSnippet.IsVisible)
            {
                Instance.consoleScriptSnippet.Focus();
            }
            Instance.consoleDeleteScript.Loaded -= ConsoleDeleteScript_Loaded;
        }

        internal static void InstallConsoleKeybind()
        {
            if (ConsoleSettings.Instance.Keybind != Keys.None)
            {
                HotkeysManager.Register("Console", ConsoleSettings.Instance.Keybind & Keys.KeyCode, GetModifierKeys(ConsoleSettings.Instance.Keybind), ToggleConsole);
            }
        }

        private Keys GetModifierKeys()
        {
            Keys keys = Keys.None;
            System.Windows.Input.ModifierKeys modifiers = Keyboard.Modifiers;
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            {
                keys |= Keys.Alt;
            }
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            {
                keys |= Keys.Control;
            }
            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            {
                keys |= Keys.Shift;
            }
            return keys;
        }

        public static Styx.Common.ModifierKeys GetModifierKeys(Keys keys)
        {
            Styx.Common.ModifierKeys modifierKeys = (Styx.Common.ModifierKeys)0;
            Keys keys2 = ConsoleSettings.Instance.Keybind & Keys.Modifiers;
            if (keys2.HasFlag(Keys.Shift))
            {
                modifierKeys |= Styx.Common.ModifierKeys.Shift;
            }
            if (keys2.HasFlag(Keys.Control))
            {
                modifierKeys |= Styx.Common.ModifierKeys.Control;
            }
            if (keys2.HasFlag(Keys.LWin) || keys2.HasFlag(Keys.RWin))
            {
                modifierKeys |= Styx.Common.ModifierKeys.Win;
            }
            if (keys2.HasFlag(Keys.Alt))
            {
                modifierKeys |= Styx.Common.ModifierKeys.Alt;
            }
            return modifierKeys;
        }

        private void SetKeybindButtonToEditMode(System.Windows.Controls.Button btn)
        {
            btn.Content = "Press key combination";
            _originalKeybindKeyForegroundBrush = btn.Foreground;
            btn.Foreground = Brushes.Gold;
        }

        private void SetKeybindButtonToNormalMode(System.Windows.Controls.Button btn)
        {
            btn.Foreground = _originalKeybindKeyForegroundBrush;
            _originalKeybindKeyForegroundBrush = null;
            btn.Content = GetKeyString(ConsoleSettings.Instance.Keybind);
        }

        private static string GetKeyString(Keys keys)
        {
            Keys keys2 = keys & Keys.KeyCode;
            Keys mod = keys & Keys.Modifiers;
            string text = "";
            if (mod != Keys.None)
            {
                text = (from Keys k in Enum.GetValues(typeof(Keys))
                    where mod.HasFlag(k)
                    select k).Aggregate(text, (string current, Keys k) => (k == Keys.None) ? string.Empty : (current + " " + k));
            }
            if (keys2 != Keys.None && !IsModifierKey(keys2))
            {
                string arg = ((keys2 < Keys.D0 || keys2 > Keys.D9) ? keys2.ToString() : ((int)(keys2 - 48)).ToString());
                text += $" {arg}";
            }
            text = text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
            return "Click to set Keybind";
        }

        private static bool IsModifierKey(Keys key)
        {
            if (key != Keys.LMenu && key != Keys.RMenu && key != Keys.LShiftKey && key != Keys.RShiftKey && key != Keys.LControlKey && key != Keys.RControlKey && key != Keys.LWin)
            {
                return key == Keys.RWin;
            }
            return true;
        }

        private void SaveConsoleSettings()
        {
            if (_unsavedConsoleChanges)
            {
                ConsoleSettings.Instance.Save();
                _unsavedConsoleChanges = false;
            }
        }

        private void AddNewSnipplet(string name)
        {
            ConsoleSettings.Instance.CSharpSnippets = new List<string>(ConsoleSettings.Instance.CSharpSnippets).Concat(new string[1] { "" }).ToArray();
            ConsoleSettings.Instance.CSharpSnippetNames = new List<string>(ConsoleSettings.Instance.CSharpSnippetNames).Concat(new string[1] { name }).ToArray();
            consoleScriptSelector.Items.Add(name);
            consoleScriptSelector.SelectedIndex = ConsoleSettings.Instance.CSharpSnippets.Length - 1;
        }

        private static string GetNewScriptName()
        {
            int num = 1;
            string text;
            while (true)
            {
                if (num < int.MaxValue)
                {
                    text = "Untitled" + num;
                    if (!ConsoleSettings.Instance.CSharpSnippetNames.Contains(text))
                    {
                        break;
                    }
                    num++;
                    continue;
                }
                return Path.GetRandomFileName();
            }
            return text;
        }

        private void ConsoleCompileAndRun()
        {
            string code = consoleScriptSnippet.Text;
            SaveConsoleSettings();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (!TreeRoot.IsRunningUnpaused)
                    {
                        ObjectManager.Update();
                    }
                    CompileAndRun(code);
                }
                catch (Exception ex)
                {
                    Instance.Log(ex.ToString());
                }
            });
        }

        public void CompileAndRun(string input)
        {
            CompilerParameters compilerParameters = new CompilerParameters();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.IsDynamic)
                {
                    compilerParameters.ReferencedAssemblies.Add(assembly.Location);
                }
            }
            compilerParameters.GenerateExecutable = false;
            compilerParameters.TempFiles = new TempFileCollection(TempFolder, keepFiles: false);
            compilerParameters.IncludeDebugInformation = false;
            compilerParameters.OutputAssembly = $"{TempFolder}\\CodeAssembly{Guid.NewGuid():N}.dll";
            compilerParameters.CompilerOptions = "/optimize";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Prefix);
            stringBuilder.Append(input.Replace("\r", ""));
            stringBuilder.Append(Postfix);
            CompilerResults compilerResults = CompileAssemblyFromSource(compilerParameters, stringBuilder.ToString());
            if (compilerResults.Errors.HasErrors)
            {
                StringBuilder stringBuilder2 = new StringBuilder();
                int num = Prefix.Count((char c) => c == '\n');
                foreach (CompilerError error in compilerResults.Errors)
                {
                    stringBuilder2.AppendFormat("{0}) {1}\n", error.Line - num, error.ErrorText);
                }
                Instance.Log(stringBuilder2.ToString());
                return;
            }
            Type type = compilerResults.CompiledAssembly.GetType("Driver");
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                type.InvokeMember("Run", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, null);
            }
        }

        private static CompilerResults CompileAssemblyFromSource(CompilerParameters compilerParameters, string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8));

            List<MetadataReference> references = compilerParameters.ReferencedAssemblies
                .Cast<string>()
                .Where(reference => !string.IsNullOrEmpty(reference) && File.Exists(reference))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(reference => (MetadataReference)MetadataReference.CreateFromFile(reference))
                .ToList();

            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: compilerParameters.CompilerOptions != null && compilerParameters.CompilerOptions.Contains("/unsafe"),
                optimizationLevel: compilerParameters.IncludeDebugInformation ? OptimizationLevel.Debug : OptimizationLevel.Release);

            CSharpCompilation compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(compilerParameters.OutputAssembly),
                new[] { syntaxTree },
                references,
                compilationOptions);

            CompilerResults compilerResults = new CompilerResults(compilerParameters.TempFiles) { PathToAssembly = compilerParameters.OutputAssembly };

            EmitResult emitResult;
            using (FileStream assemblyStream = File.Create(compilerParameters.OutputAssembly))
            {
                emitResult = compilation.Emit(assemblyStream);
            }

            foreach (Diagnostic diagnostic in emitResult.Diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error && diagnostic.Severity != DiagnosticSeverity.Warning)
                {
                    continue;
                }
                FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
                compilerResults.Errors.Add(new CompilerError(span.Path ?? string.Empty, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, diagnostic.Id, diagnostic.GetMessage(CultureInfo.InvariantCulture))
                {
                    IsWarning = diagnostic.Severity == DiagnosticSeverity.Warning
                });
            }

            if (emitResult.Success)
            {
                compilerResults.CompiledAssembly = Assembly.LoadFrom(compilerParameters.OutputAssembly);
            }

            return compilerResults;
        }

        public void Log(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (!base.Dispatcher.CheckAccess())
            {
                base.Dispatcher.BeginInvoke((Action)delegate
                {
                    Log(text);
                });
                return;
            }
            try
            {
                consoleScriptResult.AppendText(text + Environment.NewLine);
                consoleScriptResult.ScrollToEnd();
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ex.ToString());
            }
        }

        public void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public void ClearLog()
        {
            if (!base.Dispatcher.CheckAccess())
            {
                base.Dispatcher.BeginInvoke(new Action(ClearLog));
            }
            else
            {
                consoleScriptResult.Clear();
            }
        }

        private List<WItem> GetItems()
        {
            return (from o in ObjectManager.GetObjectsOfType<WoWItem>()
                orderby o.Name
                select new WItem
                {
                    Entry = o.Entry,
                    Name = o.Name,
                    Type = o.Type,
                    StackCount = o.StackCount
                }).ToList();
        }

        private void RefreshItems(List<WItem> items)
        {
            _items.Clear();
            foreach (WItem item in items)
            {
                _items.Add(item);
            }
        }

        private List<WUnit> GetUnits()
        {
            return (from o in ObjectManager.GetObjectsOfType<WoWUnit>()
                orderby o.DistanceSqr
                select new WUnit
                {
                    Distance = o.Distance,
                    Entry = o.Entry,
                    Guid = o.Guid,
                    Location = o.Location,
                    Name = o.Name,
                    FactionId = o.FactionId,
                    Type = o.Type
                }).ToList();
        }

        private void RefreshUnits(IEnumerable<WUnit> units)
        {
            _units.Clear();
            foreach (WUnit unit in units)
            {
                _units.Add(unit);
            }
        }

        private List<WGameObject> GetGameObjects()
        {
            return (from o in ObjectManager.GetObjectsOfType<WoWGameObject>()
                orderby o.DistanceSqr
                select new WGameObject
                {
                    Distance = o.Distance,
                    Entry = o.Entry,
                    Guid = o.Guid,
                    Location = o.Location,
                    LockType = o.LockType,
                    Name = o.Name,
                    SpellFocus = o.SpellFocus,
                    SubType = o.SubType,
                    Type = o.Type
                }).ToList();
        }

        private void RefreshGameObjects(IEnumerable<WGameObject> gameObjects)
        {
            _gameobjects.Clear();
            foreach (WGameObject gameObject in gameObjects)
            {
                _gameobjects.Add(gameObject);
            }
        }

        private void btnRefreshObjectList_Click(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            if (TreeRoot.IsRunningUnpaused)
            {
                refreshObjectList = true;
                return;
            }
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                ObjectManager.Update();
                RefreshGameObjects(GetGameObjects());
                RefreshUnits(GetUnits());
                RefreshItems(GetItems());
            }
        }

        private void WoWObjectListViewColumn_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader { Role: not GridViewColumnHeaderRole.Padding } gridViewColumnHeader)
            {
                ListSortDirection listSortDirection = ((gridViewColumnHeader == _lastHeaderClicked && _lastDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending);
                string sortBy = gridViewColumnHeader.Column.Header as string;
                Sort(sortBy, listSortDirection, sender as System.Windows.Controls.ListView);
                if (listSortDirection == ListSortDirection.Ascending)
                {
                    gridViewColumnHeader.Column.HeaderTemplate = base.Resources["HeaderTemplateArrowUp"] as DataTemplate;
                }
                else
                {
                    gridViewColumnHeader.Column.HeaderTemplate = base.Resources["HeaderTemplateArrowDown"] as DataTemplate;
                }
                if (_lastHeaderClicked != null && _lastHeaderClicked != gridViewColumnHeader)
                {
                    _lastHeaderClicked.Column.HeaderTemplate = null;
                }
                _lastHeaderClicked = gridViewColumnHeader;
                _lastDirection = listSortDirection;
            }
        }

        private void Sort(string sortBy, ListSortDirection direction, System.Windows.Controls.ListView lv)
        {
            ICollectionView defaultView = CollectionViewSource.GetDefaultView(lv.ItemsSource);
            defaultView.SortDescriptions.Clear();
            SortDescription item = new SortDescription(sortBy, direction);
            defaultView.SortDescriptions.Add(item);
            defaultView.Refresh();
        }

        private void CopyGameObjectInfo_OnClick(object sender, RoutedEventArgs e)
        {
            if (lvGameObjectsDump.SelectedItem is WGameObject wGameObject)
            {
                string xml = string.Format(CultureInfo.InvariantCulture, "<GameObject Name=\"{0}\" Entry=\"{1}\" X=\"{2}\" Y=\"{3}\" Z=\"{4}\" />", wGameObject.Name, wGameObject.Entry, wGameObject.Location.X, wGameObject.Location.Y, wGameObject.Location.Z);
                Logging.Write(xml);
                base.Dispatcher.Invoke(delegate
                {
                    SetClipboardText(xml);
                });
            }
        }

        private void CopyUnitInfo_OnClick(object sender, RoutedEventArgs e)
        {
            if (lvUnitsDump.SelectedItem is WUnit wUnit)
            {
                string xml = string.Format(CultureInfo.InvariantCulture, "<WoWUnit Name=\"{0}\" Entry=\"{1}\" X=\"{2}\" Y=\"{3}\" Z=\"{4}\" />", wUnit.Name, wUnit.Entry, wUnit.Location.X, wUnit.Location.Y, wUnit.Location.Z);
                Logging.Write(xml);
                base.Dispatcher.Invoke(delegate
                {
                    SetClipboardText(xml);
                });
            }
        }

        private void ViewAurasOnUnit_OnClick(object sender, RoutedEventArgs e)
        {
            if (lvUnitsDump.SelectedItem is WUnit wUnit)
            {
                ShowAurasOnUnit(wUnit.Guid);
            }
        }

        private void CopyItemInfo_OnClick(object sender, RoutedEventArgs e)
        {
            if (lvItemsDump.SelectedItem is WItem wItem)
            {
                string xml = $"<WoWItem Name=\"{wItem.Name}\" Entry=\"{wItem.Entry}\" />";
                Logging.Write(xml);
                base.Dispatcher.Invoke(delegate
                {
                    SetClipboardText(xml);
                });
            }
        }

        private string GetPositionInfo()
        {
            WoWPoint woWPoint = ((StyxWoW.Me != null) ? StyxWoW.Me.Location : WoWPoint.Zero);
            return string.Format("<{0}, {1}, {2}>", woWPoint.X.ToString("F2", CultureInfo.InvariantCulture), woWPoint.Y.ToString("F2", CultureInfo.InvariantCulture), woWPoint.Z.ToString("F2", CultureInfo.InvariantCulture));
        }

        private void RefreshPositionInfo(string positionText)
        {
            tbLocalPlayerLocation.Text = positionText;
        }

        private void RefreshCurrentTargetInfo(CurrentTargetInfo ctInfo)
        {
            tbCTEntry.Text = ctInfo.Entry;
            tbCTFactionId.Text = ctInfo.FactionId;
            tbCTFactionName.Text = ctInfo.FactionName;
            tbCTGroup1.Text = ctInfo.GroupId0;
            tbCTGroup2.Text = ctInfo.GroupId1;
            tbCTLocation.Text = ctInfo.Location;
            tbCTName.Text = ctInfo.Name;
            tbCTVendorType.Text = ctInfo.VendorType;
        }

        private void btnRefreshQuests_Click(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            if (TreeRoot.IsRunningUnpaused)
            {
                refreshQuests = true;
                return;
            }
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                ObjectManager.Update();
                RefreshQuestInfo(GetQuestInfos());
            }
        }

        private List<PlayerQuestInfo> GetQuestInfos()
        {
            if (StyxWoW.IsInGame && !(StyxWoW.Me == null))
            {
                return (from q in StyxWoW.Me.QuestLog.GetAllQuests()
                    select new PlayerQuestInfo(q)).ToList();
            }
            return new List<PlayerQuestInfo>();
        }

        private void RefreshQuestInfo(List<PlayerQuestInfo> questInfos)
        {
            tvQuests.Items.Clear();
            groupBox2.Header = "Quests (" + questInfos.Count + " total)";
            foreach (PlayerQuestInfo questInfo in questInfos)
            {
                TreeViewItem treeViewItem = new TreeViewItem();
                treeViewItem.Header = $"[{questInfo.Id}] {questInfo.Name}";
                TreeViewItem treeViewItem2 = treeViewItem;
                foreach (Quest.QuestObjective objective in questInfo.Objectives)
                {
                    if (!objective.IsEmpty)
                    {
                        TreeViewItem treeViewItem3 = new TreeViewItem();
                        string text = string.Concat("[", objective.Type, "] ", objective.ID);
                        if (objective.Count != 0)
                        {
                            text = text + " x" + objective.Count;
                        }
                        treeViewItem3.Header = text;
                        treeViewItem2.Items.Add(treeViewItem3);
                    }
                }
                treeViewItem2.Tag = questInfo;
                tvQuests.Items.Add(treeViewItem2);
            }
        }

        private void btnCopyCTXmlInfo_Click(object sender, RoutedEventArgs e)
        {
            if (StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    copyCTXmlInfo = true;
                    return;
                }
                ObjectManager.Update();
                CopyCTXmlInfo();
            }
        }

        private void CopyCTXmlInfo()
        {
            WoWUnit currentTarget;
            if (StyxWoW.Me == null || (currentTarget = StyxWoW.Me.CurrentTarget) == null)
            {
                return;
            }
            Logging.Write("Information about {0}", currentTarget.Name);
            WoWPoint location = currentTarget.Location;
            string text = Utilities.FormatString("<{0}, {1}, {2}>", Styx.Helpers.Extensions.ToStringInvariant(location.X), Styx.Helpers.Extensions.ToStringInvariant(location.Y), Styx.Helpers.Extensions.ToStringInvariant(location.Z));
            Logging.Write("Name = {0}\r\nWowhead Id = {1}\r\nFaction = {2} [{3}]\r\nLocation = {4}", currentTarget.Name, currentTarget.Entry, currentTarget.FactionId, currentTarget.FactionTemplate.Faction.Name, text);
            if (currentTarget.GetCachedInfo(out var info))
            {
                string text2 = "";
                if (info.GroupID[0] != 0)
                {
                    text2 = text2 + "Group Id = " + info.GroupID[0];
                }
                if (info.GroupID[1] != 0)
                {
                    text2 = text2 + ", Group Id 2 = " + info.GroupID[1];
                }
                Logging.Write(text2);
            }
            string text3 = (currentTarget.IsFoodVendor ? "Food" : ((!currentTarget.IsAmmoVendor) ? "Repair" : "Ammo"));
            string xml = $"<Vendor Name=\"{new XText(currentTarget.Name)}\" Entry=\"{currentTarget.Entry}\" Type=\"{text3}\" X=\"{Styx.Helpers.Extensions.ToStringInvariant(location.X)}\" Y=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Y)}\" Z=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Z)}\" />";
            Logging.Write(xml);
            base.Dispatcher.Invoke(delegate
            {
                SetClipboardText(xml);
            });
        }

        private void btnGenerateBlackspot_Click(object sender, RoutedEventArgs e)
        {
            if (StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    generateBlackspot = true;
                    return;
                }
                ObjectManager.Update();
                GenerateBlackspot();
            }
        }

        private void GenerateBlackspot()
        {
            if (!_lastBlackspotPos.HasValue)
            {
                _lastBlackspotPos = StyxWoW.Me.Location;
                Logging.Write("First edge saved. Now run to the opposite end.");
                return;
            }
            WoWPoint location = StyxWoW.Me.Location;
            WoWPoint woWPoint = (_lastBlackspotPos.Value + location) / 2f;
            float value = location.Distance2D(_lastBlackspotPos.Value) / 2f;
            float num = Math.Abs(_lastBlackspotPos.Value.Z - location.Z) / 2f;
            Logging.Write("<Blackspot X=\"{0}\" Y=\"{1}\" Z=\"{2}\" Radius=\"{3}\" {4}/>", Styx.Helpers.Extensions.ToStringInvariant(woWPoint.X), Styx.Helpers.Extensions.ToStringInvariant(woWPoint.Y), Styx.Helpers.Extensions.ToStringInvariant(woWPoint.Z), Styx.Helpers.Extensions.ToStringInvariant(value), (num > 10f) ? $"Height=\"{Styx.Helpers.Extensions.ToStringInvariant(num + 3f)}\" " : "");
            _lastBlackspotPos = null;
        }

        private void btnGenerateHotspot_Click(object sender, RoutedEventArgs e)
        {
            if (StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    generateHotspot = true;
                    return;
                }
                ObjectManager.Update();
                GenerateHotspot();
            }
        }

        private static void GenerateHotspot()
        {
            WoWPoint location = StyxWoW.Me.Location;
            string message = $"<Hotspot X=\"{Styx.Helpers.Extensions.ToStringInvariant(location.X)}\" Y=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Y)}\" Z=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Z)}\" />";
            Logging.Write(message);
        }

        private void btnCopyAllQuests_Click(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.Control)sender).IsEnabled = false;
            PlayerQuestInfo[] state = (from i in tvQuests.Items.OfType<TreeViewItem>()
                select i.Tag).OfType<PlayerQuestInfo>().ToArray();
            Task.Factory.StartNew(RetrieveWoWHeadData, state).ContinueWith((Task t) => System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                ((System.Windows.Controls.Control)sender).IsEnabled = true;
            }));
        }

        private void btnCopyAllQuestOverrides_Click(object sender, RoutedEventArgs e)
        {
            if (StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    copyAllQuestOverrides = true;
                    return;
                }
                ObjectManager.Update();
                CopyAllQuestOverrides();
            }
        }

        private void CopyAllQuestOverrides()
        {
            StringBuilder str = new StringBuilder();
            foreach (PlayerQuest allQuest in StyxWoW.Me.QuestLog.GetAllQuests())
            {
                List<Quest.QuestObjective> objectives = allQuest.GetObjectives();
                str.AppendFormat("<Quest Id=\"{0}\" Name=\"{1}\">\n", allQuest.Id, new XText(allQuest.Name));
                for (int i = 0; i < objectives.Count; i++)
                {
                    if (!objectives[i].IsEmpty)
                    {
                        string text;
                        string text2;
                        string text3;
                        switch (objectives[i].Type)
                        {
                        case Quest.QuestObjectiveType.KillMob:
                            text = "KillMob";
                            text2 = "MobId";
                            text3 = "KillCount";
                            break;
                        case Quest.QuestObjectiveType.UseGameObject:
                            text = "UseObject";
                            text2 = "ObjectId";
                            text3 = "UseCount";
                            break;
                        case Quest.QuestObjectiveType.CollectItem:
                        case Quest.QuestObjectiveType.CollectIntermediateItem:
                            text = "CollectItem";
                            text2 = "ItemId";
                            text3 = "CollectCount";
                            break;
                        default:
                            continue;
                        }
                        str.AppendFormat("\t<Objective Type=\"{0}\" {1}=\"{2}\" {3}=\"{4}\">\n", text, text2, objectives[i].ID, text3, objectives[i].Count);
                        if (objectives[i].Type == Quest.QuestObjectiveType.CollectItem)
                        {
                            str.AppendFormat("\t\t<CollectFrom>\n\t\t</CollectFrom>\n");
                        }
                        str.AppendFormat("\t\t<Hotspots>\n\t\t</Hotspots>\n\t</Objective>\n");
                    }
                }
                str.AppendFormat("</Quest>\n");
                Logging.Write(str.ToString());
            }
            base.Dispatcher.Invoke(delegate
            {
                SetClipboardText(str.ToString());
            });
            Logging.Write("Data copied to clipboard!");
        }

        private void tbLocalPlayerLocation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && StyxWoW.IsInGame)
            {
                if (TreeRoot.IsRunningUnpaused)
                {
                    writeLocalPlayerLocation = true;
                    return;
                }
                ObjectManager.Update();
                WriteLocalPlayerLocation();
            }
        }

        private static void WriteLocalPlayerLocation()
        {
            WoWPoint location = StyxWoW.Me.Location;
            string message = $"<Hotspot X=\"{Styx.Helpers.Extensions.ToStringInvariant(location.X)}\" Y=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Y)}\" Z=\"{Styx.Helpers.Extensions.ToStringInvariant(location.Z)}\" />";
            Logging.Write(message);
        }

        private void CopyQuestOrderXML(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            TreeViewItem selectedQuestItem = GetSelectedQuestItem((TreeViewItem)tvQuests.SelectedItem);
            if (selectedQuestItem == null)
            {
                Logging.Write("You must select a quest first!");
                return;
            }
            ((System.Windows.Controls.Control)sender).IsEnabled = false;
            Task.Factory.StartNew(RetrieveWoWHeadData, selectedQuestItem.Tag).ContinueWith((Task t) => System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                ((System.Windows.Controls.Control)sender).IsEnabled = true;
            }));
        }

        private void RetrieveWoWHeadData(object args)
        {
            StringBuilder sb = new StringBuilder();
            PlayerQuestInfo playerQuestInfo = args as PlayerQuestInfo;
            Logging.Write("Please wait while we retrieve the data from WoWHead...");
            if (playerQuestInfo != null)
            {
                BuildQuestInfo(sb, playerQuestInfo);
            }
            else
            {
                if (!(args is PlayerQuestInfo[] quests))
                {
                    throw new ArgumentException("A PlayerQuestInfo or PlayerQuestInfo array type is required", "args");
                }
                BuildQuestInfo(sb, quests);
            }
            Logging.Write(sb.ToString());
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                SetClipboardText(sb.ToString());
            });
            Logging.Write("Data copied to clipboard!");
        }

        private void HandleTextBlockDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                SetClipboardText(((TextBlock)sender).Text);
            }
        }

        private void btnRefreshCTInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            if (TreeRoot.IsRunningUnpaused)
            {
                refreshCTInfo = true;
                return;
            }
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                ObjectManager.Update();
                RefreshCurrentTargetInfo(new CurrentTargetInfo());
            }
        }

        private void btnRefreshPosition_Click(object sender, RoutedEventArgs e)
        {
            if (!StyxWoW.IsInGame)
            {
                return;
            }
            if (TreeRoot.IsRunningUnpaused)
            {
                refreshPosition = true;
                return;
            }
            using (StyxWoW.Memory.AcquireFrame(isHardLock: true))
            {
                ObjectManager.Update();
                RefreshPositionInfo(GetPositionInfo());
            }
        }

        private TreeViewItem GetSelectedQuestItem(TreeViewItem item)
        {
            if (item != null && item.Tag != null)
            {
                if (item.Parent == tvQuests)
                {
                    return item;
                }
                return GetSelectedQuestItem((TreeViewItem)item.Parent);
            }
            return null;
        }

        private string GetWoWheadUrlForWowLocale()
        {
            switch (WoWLocale)
            {
            case "esES":
            case "esMX":
                return "http://es.wowhead.com/";
            case "ruRU":
                return "http://ru.wowhead.com/";
            case "frFR":
                return "http://fr.wowhead.com/";
            case "deDE":
                return "http://de.wowhead.com/";
            default:
                return "http://www.wowhead.com/";
            }
        }

        private void BuildQuestInfo(StringBuilder builder, params PlayerQuestInfo[] quests)
        {
            var parallelQuery = quests.AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(delegate(PlayerQuestInfo q)
            {
                string giverName = "";
                string turnInName = "";
                int? giverId = 0;
                int? turnInId = 0;
                bool giverIsItem = false;
                bool parsed;
                try
                {
                    string requestUriString = $"{GetWoWheadUrlForWowLocale()}quest={q.Id}";
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
                    httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.107 Safari/537.36";
                    HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    string source;
                    using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        source = streamReader.ReadToEnd();
                    }
                    parsed = ParseWowheadSource(source, out giverName, out giverId, out turnInName, out turnInId, out giverIsItem);
                }
                catch (Exception)
                {
                    parsed = false;
                }
                return new { Parsed = parsed, Quest = q, GiverName = giverName, GiverId = giverId, TurnInName = turnInName, TurnInId = turnInId, GiverIsItem = giverIsItem };
            });
            foreach (var item in parallelQuery)
            {
                builder.AppendFormat("<PickUp QuestName=\"{0}\" QuestId=\"{1}\" GiverName=\"{2}\" GiverId=\"{3}\" {4}/>\n", item.Quest.Name, item.Quest.Id, item.GiverName, item.GiverId, item.GiverIsItem ? "GiverType=\"Item\" " : string.Empty);
                ReadOnlyCollection<Quest.QuestObjective> objectives = item.Quest.Objectives;
                for (int num = 0; num < objectives.Count; num++)
                {
                    if (!objectives[num].IsEmpty)
                    {
                        string text;
                        string text2;
                        string text3;
                        switch (objectives[num].Type)
                        {
                        case Quest.QuestObjectiveType.KillMob:
                            text = "KillMob";
                            text2 = "MobId";
                            text3 = "KillCount";
                            break;
                        case Quest.QuestObjectiveType.UseGameObject:
                            text = "UseObject";
                            text2 = "ObjectId";
                            text3 = "UseCount";
                            break;
                        case Quest.QuestObjectiveType.CollectItem:
                        case Quest.QuestObjectiveType.CollectIntermediateItem:
                            text = "CollectItem";
                            text2 = "ItemId";
                            text3 = "CollectCount";
                            break;
                        default:
                            continue;
                        }
                        builder.AppendFormat("<Objective QuestName=\"{0}\" QuestId=\"{1}\" Type=\"{2}\" {3}=\"{4}\" {5}=\"{6}\" />\n", item.Quest.Name, item.Quest.Id, text, text2, objectives[num].ID, text3, objectives[num].Count);
                    }
                }
                builder.AppendFormat("<TurnIn QuestName=\"{0}\" QuestId=\"{1}\" TurnInName=\"{2}\" TurnInId=\"{3}\" />\n\n", item.Quest.Name, item.Quest.Id, item.TurnInName, item.TurnInId);
            }
        }

        private static bool ParseWowheadSource(string source, out string giverName, out int? giverId, out string turnInName, out int? turnInId, out bool giverIsItem)
        {
            giverName = null;
            turnInName = null;
            giverId = null;
            turnInId = null;
            giverIsItem = false;
            var source2 = (from Match m in _questDataParser.Matches(source)
                select new
                {
                    Point = m.Groups["point"].Value,
                    Name = m.Groups["name"].Value.Replace('"'.ToString(CultureInfo.CurrentCulture), "").Replace("\\'", ""),
                    Id = int.Parse(m.Groups["id"].Value),
                    ItemName = m.Groups["item"].Value
                }).ToList();
            if (!source2.Any())
            {
                return false;
            }
            var anon = source2.FirstOrDefault(m => m.Point == "end");
            if (anon == null)
            {
                return false;
            }
            turnInId = anon.Id;
            turnInName = anon.Name;
            anon = source2.FirstOrDefault(m => m.Point == "start");
            if (anon != null)
            {
                giverId = anon.Id;
                giverName = anon.Name;
            }
            else
            {
                anon = source2.FirstOrDefault(m => m.Point == "sourcestart");
                if (anon != null && !string.IsNullOrEmpty(anon.ItemName))
                {
                    giverIsItem = true;
                    string pattern = string.Format("_\\[(?<giverItemId>\\d+?)\\]={{name_enus:'{0}',quality:", anon.ItemName.Replace("\\", "\\\\"));
                    Match match = Regex.Match(source, pattern);
                    if (!match.Success)
                    {
                        return false;
                    }
                    giverName = anon.ItemName.Replace("\\'", "");
                    giverId = int.Parse(match.Groups["giverItemId"].Value);
                }
            }
            return true;
        }
    }
}
