using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Infralution.Localization.Wpf
{
    public static class CultureManager
    {
        private static CultureInfo _uiCulture;
        private static CultureSelectWindow _cultureSelectWindow;
        private static NotifyIcon _notifyIcon;
        private static bool _synchronizeThreadCulture = true;
        private static EventHandler _uiCultureChanged;

        public static event EventHandler UICultureChanged
        {
            add
            {
                EventHandler eventHandler = _uiCultureChanged;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Combine(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref _uiCultureChanged, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
            remove
            {
                EventHandler eventHandler = _uiCultureChanged;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Remove(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref _uiCultureChanged, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
        }

        public static CultureInfo UICulture
        {
            get
            {
                if (_uiCulture == null)
                {
                    _uiCulture = Thread.CurrentThread.CurrentUICulture;
                }
                return _uiCulture;
            }
            set
            {
                if (value != UICulture)
                {
                    _uiCulture = value;
                    Thread.CurrentThread.CurrentUICulture = value;
                    if (SynchronizeThreadCulture)
                    {
                        SetThreadCulture(value);
                    }
                    UICultureExtension.UpdateAllTargets();
                    ResxExtension.UpdateAllTargets();
                    if (_uiCultureChanged != null)
                    {
                        _uiCultureChanged(null, EventArgs.Empty);
                    }
                    UpdateCultureMenuItems();
                }
            }
        }

        public static bool SynchronizeThreadCulture
        {
            get { return _synchronizeThreadCulture; }
            set
            {
                _synchronizeThreadCulture = value;
                if (value)
                {
                    SetThreadCulture(UICulture);
                }
            }
        }

        private static void SetThreadCulture(CultureInfo culture)
        {
            if (culture.IsNeutralCulture)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture.Name);
                return;
            }
            Thread.CurrentThread.CurrentCulture = culture;
        }

        internal static void EnsureCultureNotifyIcon()
        {
            if (_notifyIcon == null)
            {
                _notifyIcon = new NotifyIcon();
                _notifyIcon.MouseClick += OnNotifyIconMouseClick;
                _notifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
                _notifyIcon.Text = "Select";
                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Other Cultures");
                toolStripMenuItem.Click += OnOtherCulturesClick;
                contextMenuStrip.Items.Add(toolStripMenuItem);
                AddCultureMenuItem(contextMenuStrip);
                _notifyIcon.ContextMenuStrip = contextMenuStrip;
                _notifyIcon.Visible = true;
            }
        }

        private static void ShowCultureSelectWindow()
        {
            if (_cultureSelectWindow == null)
            {
                _cultureSelectWindow = new CultureSelectWindow();
                _cultureSelectWindow.Title = _notifyIcon.Text;
                _cultureSelectWindow.Closed += OnCultureSelectWindowClosed;
                _cultureSelectWindow.Show();
            }
        }

        private static void AddCultureMenuItem(ContextMenuStrip menu)
        {
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(UICulture.DisplayName);
            toolStripMenuItem.Checked = true;
            toolStripMenuItem.CheckOnClick = true;
            toolStripMenuItem.Tag = UICulture;
            toolStripMenuItem.CheckedChanged += OnCultureMenuItemCheckedChanged;
            menu.Items.Insert(menu.Items.Count - 2, toolStripMenuItem);
        }

        private static void UpdateCultureMenuItems()
        {
            if (_notifyIcon != null)
            {
                ContextMenuStrip contextMenuStrip = _notifyIcon.ContextMenuStrip;
                bool flag = false;
                foreach (object obj in contextMenuStrip.Items)
                {
                    ToolStripItem toolStripItem = (ToolStripItem)obj;
                    ToolStripMenuItem toolStripMenuItem = toolStripItem as ToolStripMenuItem;
                    if (toolStripMenuItem != null)
                    {
                        toolStripMenuItem.Checked = toolStripMenuItem.Tag == UICulture;
                        if (toolStripMenuItem.Checked)
                        {
                            flag = true;
                        }
                    }
                }
                if (!flag)
                {
                    AddCultureMenuItem(contextMenuStrip);
                }
            }
        }

        private static void OnNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _notifyIcon.ContextMenuStrip.Show(Control.MousePosition);
            }
        }

        private static void OnNotifyIconMouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowCultureSelectWindow();
        }

        private static void OnOtherCulturesClick(object sender, EventArgs e)
        {
            ShowCultureSelectWindow();
        }

        private static void OnCultureMenuItemCheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
            if (toolStripMenuItem.Checked)
            {
                UICulture = toolStripMenuItem.Tag as CultureInfo;
            }
        }

        private static void OnCultureSelectWindowClosed(object sender, EventArgs e)
        {
            _cultureSelectWindow = null;
        }
    }
}
