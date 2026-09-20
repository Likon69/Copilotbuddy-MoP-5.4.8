using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using GreenMagic;
using Styx.Common;
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

        public static Styx.WoWInternals.WoWCache.WoWCache Cache { get; private set; }

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

            ObjectManager.Update();
        }

        internal static GraphicsApi ReadGraphicsApi(ExternalProcessMemory memory, bool rebased)
        {
            string api = memory.ReadString(memory.Read<IntPtr>(!rebased, Offsets.Statics.GxApi, (IntPtr)0x28), Encoding.ASCII);
            return (GraphicsApi)Enum.Parse(typeof(GraphicsApi), api);
        }
    }
}
