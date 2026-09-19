using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using GreenMagic;
using MahApps.Metro.Controls;
using Styx;
using Styx.Common;
using Styx.Localization;
using Styx.WoWInternals;

namespace CopilotBuddy
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            StyxWoW.InitOffsets();

            Logging.OnLogMessage += OnLogMessage;
            StyxLog.StartLogThread();

            Logging.Write("CopilotBuddy v" + Assembly.GetEntryAssembly().GetName().Version + " started.");

            bool found64Bits;
            if (FindAndAttachToWoW(out found64Bits) && StyxWoW.Me != null && StyxWoW.Memory != null)
            {
                Title = string.Format(Globalization.MainWindowTitleFormat, Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location), StyxWoW.Memory.Process.Id);

                // TEMPORARY MILESTONE 1 TEST - remove before the next step
                Logging.Write("Local player: {0}", StyxWoW.Me.Name);
                Logging.Write("Position: {0}", StyxWoW.Me.Location);

                string locale = Lua.GetReturnVal<string>("return GetLocale()", 0U);
                Logging.WriteDiagnostic("GetLocale() = {0}", locale);
                // END TEMPORARY MILESTONE 1 TEST
            }
            else if (found64Bits)
            {
                Logging.Write(Colors.OrangeRed, Globalization.StartUpError64Bit);
            }
            else
            {
                Logging.Write(Colors.OrangeRed, Globalization.StartUpErrorNotInGame);
            }
        }

        private void SelectWoWProcess()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.BeginInvoke(new ThreadStart(SelectWoWProcess));
                return;
            }

            ProcessSelectorWindow processSelectorWindow = new ProcessSelectorWindow();
            if (processSelectorWindow.ShowDialog() == true)
            {
                _selectedWoWProc = processSelectorWindow.Entry.ProcessIdentifier;
                return;
            }

            _selectedWoWProc = 0;
        }

        private bool FindAndAttachToWoW(out bool found64Bits)
        {
            found64Bits = false;
            _selectedWoWProc = null;

            List<Process> processes = Process.GetProcessesByName("Wow").ToList();

            _freeProcesses.Clear();
            foreach (Process process in processes)
            {
                using (ExternalProcessMemory externalProcessMemory = new ExternalProcessMemory(process, false, false, false))
                {
                    bool wasCreated;
                    using (MutexHelper.CreateMutexForProcess(process.Id, out wasCreated))
                    {
                        if (!wasCreated)
                        {
                            continue;
                        }
                    }

                    if (externalProcessMemory.Read<byte>(true, StyxWoW.Offsets.Statics.IsInGame) == 0)
                    {
                        continue;
                    }
                }

                _freeProcesses.Add(process.Id, process.MainModule.FileVersionInfo.FilePrivatePart);
            }

            List<KeyValuePair<int, int>> loggedInProcesses = _freeProcesses.Where(d => d.Value == 18414).ToList();
            if (loggedInProcesses.Count == 1)
            {
                _selectedWoWProc = loggedInProcesses.First().Key;
            }

            if (loggedInProcesses.Count == 0)
            {
                if (Process.GetProcessesByName("Wow-64").Any())
                {
                    found64Bits = true;
                }

                return false;
            }

            if (_selectedWoWProc == null)
            {
                SelectWoWProcess();
                while (_selectedWoWProc == null)
                {
                    Thread.Sleep(100);
                }
            }

            if (_selectedWoWProc == 0)
            {
                return false;
            }

            try
            {
                using (ExternalProcessMemory externalProcessMemory = new ExternalProcessMemory(Process.GetProcessById(_selectedWoWProc.Value), false, false, false))
                {
                    GraphicsApi graphicsApi = StyxWoW.ReadGraphicsApi(externalProcessMemory, false);

                    if (externalProcessMemory.Read<byte>(true, StyxWoW.Offsets.Statics.IsInGame) == 0)
                    {
                        Logging.Write("Error attaching CopilotBuddy to WoW! - Process ID {0} is not logged in game!", _selectedWoWProc.Value);
                        return false;
                    }

                    if (graphicsApi != GraphicsApi.D3D9 && graphicsApi != GraphicsApi.D3D11)
                    {
                        Logging.Write("Error attaching CopilotBuddy to WoW! - Process ID {0} is not using DX9 or DX11!", _selectedWoWProc.Value);
                        return false;
                    }

                    StyxWoW.Initialize(externalProcessMemory.Process, graphicsApi);
                    Logging.Write("Attached to WoW with ID {0}", _selectedWoWProc.Value);

                    bool wasCreated;
                    _mut = MutexHelper.CreateMutexForProcess(_selectedWoWProc.Value, out wasCreated);
                }
            }
            catch (Exception ex)
            {
                Logging.Write("Error attaching CopilotBuddy to WoW! - {0}", ex);
                return false;
            }

            return true;
        }

        private LogLevel LogLevel
        {
            get { return Logging.LoggingLevel; }
        }

        private void OnLogMessage(ReadOnlyCollection<Logging.LogMessage> messages)
        {
            if (Thread.CurrentThread != Dispatcher.Thread)
            {
                Dispatcher.BeginInvoke(new Logging.LogMessageDelegate(OnLogMessage), messages);
                return;
            }

            if (_para == null)
            {
                rtbLog.Document.Blocks.Clear();
                _para = new Paragraph();
                rtbLog.Document.Blocks.Add(_para);
            }

            if (_font == null)
            {
                _font = new FontFamily("Consolas");
            }

            if (_logCount >= 1000)
            {
                _para.Inlines.Clear();
                _logCount = 0;
            }

            Color color = messages[0].Color;
            StringBuilder stringBuilder = new StringBuilder();
            Action<StringBuilder, Color> appendRun = delegate(StringBuilder sb, Color col)
            {
                SolidColorBrush solidColorBrush = new SolidColorBrush(col);
                solidColorBrush.Freeze();
                _para.Inlines.Add(new Run(sb.ToString())
                {
                    Foreground = solidColorBrush,
                    FontFamily = _font
                });
            };

            foreach (Logging.LogMessage logMessage in messages)
            {
                if (logMessage.Level <= LogLevel)
                {
                    if (logMessage.Color != color)
                    {
                        appendRun(stringBuilder, color);
                        stringBuilder.Clear();
                        color = logMessage.Color;
                    }

                    stringBuilder.AppendLine(logMessage.Message);
                }
            }

            appendRun(stringBuilder, color);
            _logCount += messages.Count;

            if (scrollLog.ScrollableHeight == scrollLog.ContentVerticalOffset)
            {
                scrollLog.ScrollToEnd();
            }
        }

        private int? _selectedWoWProc;

        private readonly Dictionary<int, int> _freeProcesses = new Dictionary<int, int>();

        private Mutex _mut;

        private int _logCount;

        private Paragraph _para;

        private FontFamily _font;
    }
}
