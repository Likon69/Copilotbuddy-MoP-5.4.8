using System.Windows.Media;
using Bots.BGBuddy.Resources;
using Styx.Common;

namespace Bots.BGBuddy.Helpers
{
    public static class Logger
    {
        public static void Write(Color color, string format, params object[] args)
        {
            Logging.Write(color, BGBuddyResources.LogPrefix + string.Format(format, args));
        }

        public static void Write(string format, params object[] args)
        {
            Write(Colors.PowderBlue, string.Format(format,args));
        }

        public static void Write(string message)
        {
            Write(Colors.PowderBlue, message);
        }

        public static void WriteDebug(string message)
        {
            //if (!AuctionizerSettings.Instance.EnableDebugLogging)
            //    return;

            Logging.WriteDiagnostic(Colors.Orange, BGBuddyResources.DebugLogPrefix + message);
        }

        public static void WriteDebug(string format, params object[] args)
        {
            WriteDebug(string.Format(format, args));
        }
    }
}
