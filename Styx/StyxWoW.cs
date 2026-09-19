using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using GreenMagic;
using Styx.Common;
using Styx.Resources;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Styx
{
    public static class StyxWoW
    {
        public static ExternalProcessMemory Memory { get; private set; }

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

            ObjectManager.Update();
        }

        internal static GraphicsApi ReadGraphicsApi(ExternalProcessMemory memory, bool rebased)
        {
            string api = memory.ReadString(memory.Read<IntPtr>(!rebased, Offsets.Statics.GxApi, (IntPtr)0x28), Encoding.ASCII);
            return (GraphicsApi)Enum.Parse(typeof(GraphicsApi), api);
        }
    }
}
