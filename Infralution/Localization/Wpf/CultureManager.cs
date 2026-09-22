using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Infralution.Localization.Wpf
{
    public static class CultureManager
    {
        private static CultureInfo cultureInfo_0;
        private static CultureSelectWindow cultureSelectWindow_0;
        private static NotifyIcon notifyIcon_0;
        private static bool bool_0 = true;
        private static EventHandler eventHandler_0;

        public static event EventHandler UICultureChanged
        {
            add
            {
                EventHandler eventHandler = eventHandler_0;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Combine(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref eventHandler_0, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
            remove
            {
                EventHandler eventHandler = eventHandler_0;
                EventHandler eventHandler2;
                do
                {
                    eventHandler2 = eventHandler;
                    EventHandler eventHandler3 = (EventHandler)Delegate.Remove(eventHandler2, value);
                    eventHandler = Interlocked.CompareExchange<EventHandler>(ref eventHandler_0, eventHandler3, eventHandler2);
                }
                while (eventHandler != eventHandler2);
            }
        }

        public static CultureInfo UICulture
        {
            get
            {
                if (cultureInfo_0 == null)
                {
                    cultureInfo_0 = Thread.CurrentThread.CurrentUICulture;
                }
                return cultureInfo_0;
            }
            set
            {
                if (value != UICulture)
                {
                    cultureInfo_0 = value;
                    Thread.CurrentThread.CurrentUICulture = value;
                    if (SynchronizeThreadCulture)
                    {
                        smethod_0(value);
                    }
                    UICultureExtension.UpdateAllTargets();
                    ResxExtension.UpdateAllTargets();
                    if (eventHandler_0 != null)
                    {
                        eventHandler_0(null, EventArgs.Empty);
                    }
                    smethod_4();
                }
            }
        }

        public static bool SynchronizeThreadCulture
        {
            get { return bool_0; }
            set
            {
                bool_0 = value;
                if (value)
                {
                    smethod_0(UICulture);
                }
            }
        }

        private static void smethod_0(CultureInfo cultureInfo_1)
        {
            if (cultureInfo_1.IsNeutralCulture)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo_1.Name);
                return;
            }
            Thread.CurrentThread.CurrentCulture = cultureInfo_1;
        }

        internal static void smethod_1()
        {
            if (notifyIcon_0 == null)
            {
                notifyIcon_0 = new NotifyIcon();
                notifyIcon_0.MouseClick += smethod_5;
                notifyIcon_0.MouseDoubleClick += smethod_6;
                notifyIcon_0.Text = "Select";
                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Other Cultures");
                toolStripMenuItem.Click += smethod_7;
                contextMenuStrip.Items.Add(toolStripMenuItem);
                smethod_3(contextMenuStrip);
                notifyIcon_0.ContextMenuStrip = contextMenuStrip;
                notifyIcon_0.Visible = true;
            }
        }

        private static void smethod_2()
        {
            if (cultureSelectWindow_0 == null)
            {
                cultureSelectWindow_0 = new CultureSelectWindow();
                cultureSelectWindow_0.Title = notifyIcon_0.Text;
                cultureSelectWindow_0.Closed += smethod_9;
                cultureSelectWindow_0.Show();
            }
        }

        private static void smethod_3(ContextMenuStrip contextMenuStrip_0)
        {
            ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(UICulture.DisplayName);
            toolStripMenuItem.Checked = true;
            toolStripMenuItem.CheckOnClick = true;
            toolStripMenuItem.Tag = UICulture;
            toolStripMenuItem.CheckedChanged += smethod_8;
            contextMenuStrip_0.Items.Insert(contextMenuStrip_0.Items.Count - 2, toolStripMenuItem);
        }

        private static void smethod_4()
        {
            if (notifyIcon_0 != null)
            {
                ContextMenuStrip contextMenuStrip = notifyIcon_0.ContextMenuStrip;
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
                    smethod_3(contextMenuStrip);
                }
            }
        }

        private static void smethod_5(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                notifyIcon_0.ContextMenuStrip.Show(Control.MousePosition);
            }
        }

        private static void smethod_6(object sender, MouseEventArgs e)
        {
            smethod_2();
        }

        private static void smethod_7(object sender, EventArgs e)
        {
            smethod_2();
        }

        private static void smethod_8(object sender, EventArgs e)
        {
            ToolStripMenuItem toolStripMenuItem = sender as ToolStripMenuItem;
            if (toolStripMenuItem.Checked)
            {
                UICulture = toolStripMenuItem.Tag as CultureInfo;
            }
        }

        private static void smethod_9(object sender, EventArgs e)
        {
            cultureSelectWindow_0 = null;
        }
    }
}
