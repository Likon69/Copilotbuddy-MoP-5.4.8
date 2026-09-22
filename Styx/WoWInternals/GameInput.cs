using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Styx.WoWInternals
{
    public static class GameInput
    {
        public static bool IsKeyDown(Keys key)
        {
            return StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.EventIsKeyDown, CallingConvention.Cdecl, new object[] { (int)key }) != 0;
        }

        public static bool IsMouseButtonDown(InputMouseButton button)
        {
            return StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.EventIsButtonDown, CallingConvention.Cdecl, new object[] { GameInput.GetMouseButtonMask((int)button) }) != 0;
        }

        private static uint GetMouseButtonMask(int button)
        {
            uint mask;
            switch (button)
            {
            case 1:
                mask = 1U;
                break;
            case 2:
                mask = 4U;
                break;
            case 3:
                mask = 2U;
                break;
            case 4:
                mask = 8U;
                break;
            case 5:
                mask = 16U;
                break;
            case 6:
                mask = 32U;
                break;
            case 7:
                mask = 64U;
                break;
            case 8:
                mask = 128U;
                break;
            case 9:
                mask = 256U;
                break;
            case 10:
                mask = 512U;
                break;
            case 11:
                mask = 1024U;
                break;
            case 12:
                mask = 2048U;
                break;
            case 13:
                mask = 4096U;
                break;
            case 14:
                mask = 8192U;
                break;
            case 15:
                mask = 16384U;
                break;
            case 16:
                mask = 32768U;
                break;
            case 17:
                mask = 65536U;
                break;
            case 18:
                mask = 131072U;
                break;
            case 19:
                mask = 262144U;
                break;
            case 20:
                mask = 524288U;
                break;
            case 21:
                mask = 1048576U;
                break;
            case 22:
                mask = 2097152U;
                break;
            case 23:
                mask = 4194304U;
                break;
            case 24:
                mask = 8388608U;
                break;
            case 25:
                mask = 16777216U;
                break;
            case 26:
                mask = 33554432U;
                break;
            case 27:
                mask = 67108864U;
                break;
            case 28:
                mask = 134217728U;
                break;
            case 29:
                mask = 268435456U;
                break;
            case 30:
                mask = 536870912U;
                break;
            case 31:
                mask = 1073741824U;
                break;
            default:
                mask = 0U;
                break;
            }
            return mask;
        }
    }
}
