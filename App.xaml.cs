using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Styx.Common;

namespace CopilotBuddy
{
    public partial class App : Application
    {
        public App()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            if (e.Exception is ThreadAbortException)
            {
                return;
            }

            if (e.Exception != null)
            {
                Logging.WriteException(e.Exception);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
