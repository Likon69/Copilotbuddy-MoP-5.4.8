using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Overlay;
using GreenMagic;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.AreaManagement;
using Styx.Helpers;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.Misc;
using Styx.WoWInternals.WoWObjects;

namespace Styx
{
    public static class StyxWoW
    {
        public static ExternalProcessMemory Memory { get; private set; }

        public static WoWClient WoWClient { get; private set; }

        public static WoWDb Db { get; private set; }
        public static Styx.WoWInternals.World.WorldScene WorldScene
        {
            get { return new Styx.WoWInternals.World.WorldScene(Memory.Read<IntPtr>(Offsets.Statics.WorldScene)); }
        }

        public static Styx.WoWInternals.WoWCache.WoWCache Cache { get; private set; }

        public static AreaManager AreaManager { get; private set; }

        public static Landmarks Landmarks { get; private set; }

        public static WoWCamera Camera { get; private set; }

        public static bool ExecutorLogging { get; private set; }

        private static StyxLog _executorLog;

        private static OverlayManager overlayManager;

        // HB 6.2.3 StyxWoW.cs:299-316. `null` for the dispatcher is deliberate: OverlayManager
        // spins up its own STA thread when no dispatcher is supplied, so it is self-sufficient here.
        public static OverlayManager Overlay
        {
            get
            {
                if (StyxWoW.overlayManager == null)
                {
                    StyxWoW.overlayManager = new OverlayManager(StyxWoW.Memory.Process, null, null);
                    StyxWoW.overlayManager.Activate();
                    if (!StyxWoW.overlayManager.IsDesktopCompositionEnabled)
                    {
                        Task.Delay(9000).ContinueWith(task => StyxWoW.overlayManager.Deactivate());
                        StyxWoW.overlayManager.AddToast("Deactivating the overlay because Desktop Composition is disabled ", 7000);
                        Logging.Write(Colors.Red, "Deactivating the overlay because Desktop Composition is disabled which causes a significant drop in FPS while overlay is active. We recommend enabling Desktop Composition because the overlay provides a safe way for Honorbuddy and 3rd party extensions to display in-game notifications and UI elements");
                    }
                }
                return StyxWoW.overlayManager;
            }
        }

        private static readonly Dictionary<string, StyxLog> Logs = new Dictionary<string, StyxLog>();

        internal static StyxLog GetLog(string name)
        {
            StyxLog log;
            lock (Logs)
            {
                string key = name.ToLowerInvariant();
                if (Logs.TryGetValue(key, out log))
                {
                    return log;
                }
                log = new StyxLog(TimestampType.Default);
                Logs[key] = log;
            }
            string prefix = string.IsNullOrWhiteSpace(name) ? "" : name + " ";
            DateTime startTime = log.StartTime;
            string fileName = string.Format("{0}{1} {2:0000}-{3:00}-{4:00} {5:00}.{6:00}.txt", prefix,
                Process.GetCurrentProcess().Id, startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute);
            string directory = GlobalSettings.Instance.SeperatedLogFolders
                ? Path.Combine(Utilities.AssemblyDirectory, "Logs", string.Format("{0} - {1}", Me.RealmName, Me.Name))
                : Path.Combine(Utilities.AssemblyDirectory, "Logs");
            log.ChangeLogFilePath(Path.Combine(directory, fileName));
            log.FileLogging = true;
            return log;
        }

        internal static void SetSeparatedLogFolders(bool separated)
        {
            string root = Path.Combine(Utilities.AssemblyDirectory, "Logs");
            string character = Path.Combine(root, string.Format("{0} - {1}", Me.RealmName, Me.Name));
            foreach (StyxLog log in StyxLog.GetLogs())
            {
                if (!log.FileLogging || string.IsNullOrWhiteSpace(log.LogFilePath))
                    continue;
                log.ChangeLogFilePath(Path.Combine(separated ? character : root, Path.GetFileName(log.LogFilePath)));
            }
        }

        internal static T CallInjected<T>(IntPtr address, CallingConvention callingConvention, params object[] args) where T : struct
        {
            return Memory.CallInjected<T>(address, callingConvention, args);
        }

        internal static void CallInjected(IntPtr address, CallingConvention callingConvention, params object[] args)
        {
            CallInjected<int>(address, callingConvention, args);
        }

        internal static OffsetManager Offsets { get; private set; }

        public static LocalPlayer Me
        {
            get { return ObjectManager.Me; }
        }

        public static bool WowExited
        {
            get { return Memory == null || Memory.Process == null || Memory.Process.HasExited; }
        }

        public static bool IsInGame
        {
            get { return !WowExited && Memory.Read<byte>(Offsets.Statics.IsInGame) != 0; }
        }

        public static GraphicsApi GameGraphicsApi
        {
            get { return ReadGraphicsApi(Memory, true); }
        }

        internal static bool IsInitialized
        {
            get { return Memory != null; }
        }

        public static Version GameVersion
        {
            get
            {
                if (Memory != null && Memory.Process != null)
                {
                    FileVersionInfo fileVersionInfo = Memory.Process.MainModule.FileVersionInfo;
                    return new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);
                }
                return new Version(0, 0, 0, 0);
            }
        }

        public static GameState GameState => (GameState)Memory.Read<uint>(Offsets.Statics.GameState);

        public static GlueScreen GlueState => (GlueScreen)Memory.Read<uint>(Offsets.Statics.GlueState);

        public static bool IsInWorld
        {
            get
            {
                if (IsInGame)
                {
                    return GameState != GameState.Zoning;
                }
                return false;
            }
        }

        public static void ResetAfk()
        {
            if (!IsInGame || Me == null)
            {
                return;
            }
            uint num = (uint)WoWClient.PerformanceCounter();
            LastHardwareAction = num;
            if (Memory.Read<uint>(Offsets.Statics.LocalPlayerIsAfk) == 0U)
            {
                return;
            }
            CallInjected(Offsets.Functions.CGPlayer_C__ClearAFK, CallingConvention.ThisCall, new object[]
            {
                Me.BaseAddress,
                1
            });
        }

        public static uint LastHardwareAction
        {
            get { return Memory.Read<uint>(Offsets.Statics.LastHardwareAction); }
            private set { Memory.Write<uint>(Offsets.Statics.LastHardwareAction, value); }
        }

        internal static void InitOffsets()
        {
            Offsets = new OffsetManager();
            Offsets.Rebase(IntPtr.Zero);
        }

        internal static void Initialize(Process process, GraphicsApi api)
        {
            if (api != GraphicsApi.D3D9 && api != GraphicsApi.D3D11)
            {
                throw new ArgumentOutOfRangeException("api", "API must be either DX9 or DX11");
            }

            Offsets.Rebase(process.MainModule.BaseAddress);

            int buildNumber = process.MainModule.FileVersionInfo.FilePrivatePart;
            if (buildNumber != 18414)
            {
                Logging.Write(Colors.Red, StyxResources.CurrentBuildOfWowNotSupported, buildNumber);
                throw new Exception(string.Format(StyxResources.CurrentBuildNotSupportedException, buildNumber, Application.ProductVersion));
            }

            Logging.Write("Attaching to {0}", api);

            Memory = new ExternalProcessMemory(process, true, api == GraphicsApi.D3D9, true, IntPtr.Zero, false, 0, true);

            Db = new WoWDb();
            Cache = new Styx.WoWInternals.WoWCache.WoWCache();
            AreaManager = new AreaManager();
            WoWClient = new WoWClient();
            Landmarks = new Landmarks();
            Camera = new WoWCamera();
            ObjectManager.Update();
        }

        public static void SleepForLagDuration()
        {
            Sleep((int)(WoWClient.Latency + 100U));
        }

        public static void Sleep(int duration)
        {
            Sleep(TimeSpan.FromMilliseconds(duration));
        }

        public static void Sleep(TimeSpan timeSpan)
        {
            if (TreeRoot.CurrentThreadIsBotThread)
            {
                using (Memory.ReleaseFrame(true))
                {
                    Thread.Sleep(timeSpan);
                    return;
                }
            }
            Thread.Sleep(timeSpan);
        }

        public static void EnableExecutorLogging()
        {
            if (!ExecutorLogging)
            {
                if (_executorLog == null)
                {
                    _executorLog = GetLog("Executor");
                }
                Memory.Executor.OnBeginExecute += ExecutorOnBeginExecute;
                Memory.Executor.OnEndExecute += ExecutorOnEndExecute;
                Memory.Executor.OnExecute += ExecutorOnExecute;
                ExecutorLogging = true;
            }
        }

        public static void DisableExecutorLogging()
        {
            if (ExecutorLogging)
            {
                Memory.Executor.OnBeginExecute -= ExecutorOnBeginExecute;
                Memory.Executor.OnEndExecute -= ExecutorOnEndExecute;
                Memory.Executor.OnExecute -= ExecutorOnExecute;
                ExecutorLogging = false;
            }
        }

        private static void ExecutorOnExecute(object sender, EventArgs e)
        {
            _executorLog.Write("OnInjection{0}{1}", Environment.NewLine, Utilities.GetStackTrace(3, -1));
        }

        private static void ExecutorOnEndExecute(object sender, EventArgs e)
        {
            _executorLog.Write("OnEndExecute{0}{1}", Environment.NewLine, Utilities.GetStackTrace(3, -1));
        }

        private static void ExecutorOnBeginExecute(object sender, EventArgs e)
        {
            _executorLog.Write("OnBeginExecute{0}{1}", Environment.NewLine, Utilities.GetStackTrace(3, -1));
        }

        public static string LastRedErrorMessage
        {
            get { return Memory.ReadString(Offsets.Statics.LastRedErrorMessage, Encoding.UTF8); }
        }

        internal static GraphicsApi ReadGraphicsApi(ExternalProcessMemory memory, bool rebased)
        {
            string api = memory.ReadString(memory.Read<IntPtr>(!rebased, Offsets.Statics.GxApi, (IntPtr)0x28), Encoding.ASCII);
            return (GraphicsApi)Enum.Parse(typeof(GraphicsApi), api);
        }
    }
}
