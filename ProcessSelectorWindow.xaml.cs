using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using GreenMagic;
using MahApps.Metro.Controls;
using Styx;
using Styx.Localization;

namespace CopilotBuddy
{
    public partial class ProcessSelectorWindow : MetroWindow
    {
        public ProcessSelectorWindow()
        {
            InitializeComponent();
            RefreshProcesses();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            Entry = cbProcesses.SelectedItem as ProcessEntry;
            DialogResult = true;
        }

        public ProcessEntry Entry { get; set; }

        private void RefreshProcesses()
        {
            foreach (ProcessEntry processEntry in _processes)
            {
                processEntry.Mutex.Close();
            }

            _processes.Clear();
            cbProcesses.Items.Clear();

            Process[] processesByName = Process.GetProcessesByName("Wow");
            for (int i = 0; i < processesByName.Length; i++)
            {
                try
                {
                    ExternalProcessMemory externalProcessMemory = new ExternalProcessMemory(processesByName[i], false, false, false);
                    if (externalProcessMemory.Read<bool>(true, StyxWoW.Offsets.Statics.IsInGame))
                    {
                        string text = externalProcessMemory.ReadString(StyxWoW.Offsets.Statics.LocalPlayerName, Encoding.UTF8, 512, true);

                        bool wasCreated;
                        Mutex mutex = MutexHelper.CreateMutexForProcess(processesByName[i].Id, out wasCreated);
                        mutex.Close();
                        if (wasCreated)
                        {
                            ProcessEntry processEntry = new ProcessEntry(text, processesByName[i].Id, mutex);
                            _processes.Add(processEntry);
                            cbProcesses.Items.Add(processEntry);
                        }
                    }
                }
                catch
                {
                }
            }

            if (cbProcesses.Items.Count > 0)
            {
                cbProcesses.SelectedIndex = 0;
            }
        }

        private readonly List<ProcessEntry> _processes = new List<ProcessEntry>();

        public class ProcessEntry
        {
            public ProcessEntry(string localPlayer, int processIdentifier, Mutex mutex)
            {
                _localPlayer = localPlayer;
                ProcessIdentifier = processIdentifier;
                Mutex = mutex;
            }

            public override string ToString()
            {
                return string.Format(Globalization.ProcessTitle, ProcessIdentifier, _localPlayer);
            }

            public readonly Mutex Mutex;

            public readonly int ProcessIdentifier;

            private readonly string _localPlayer;
        }
    }
}
