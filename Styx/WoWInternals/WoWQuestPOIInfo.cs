using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Size = 180)]
    public struct WoWQuestPOIInfo
    {
        public override string ToString()
        {
            return string.Format("QuestId: {0}, Steps: {1}, CurrentStep: {2}, CurrentState: {3}, Dword2C: {4:X}, Dword30: {5:X}, Dword34: {6:X}, Dword38: {7:X}, Dword3C: {8:X}, Dword40: {9:X}, Dword44: {10:X}, Dword48: {11:X}, Dword4C: {12:X}, Dword50: {13:X}, Dword54: {14:X}, Dword58: {15:X}, Dword5C: {16:X}, Dword60: {17:X}, Dword64: {18:X}, Dword68: {19:X}, Dword6C: {20:X}, Dword70: {21:X}, Dword74: {22:X}, Dword78: {23:X}, Dword7C: {24:X}, Dword80: {25:X}, Dword84: {26:X}, Dword88: {27:X}, Dword8C: {28:X}, Dword90: {29:X}, Dword94: {30:X}, Dword98: {31:X}, Dword9C: {32:X}, DwordA0: {33:X}, DwordA4: {34:X}, DwordA8: {35:X}, DwordAc: {36:X}, DwordB0: {37:X}".Replace(",", Environment.NewLine), new object[]
            {
                QuestID, Steps, CurrentStep, CurrentState, dword2C, dword30, dword34, dword38, dword3C, dword40,
                dword44, dword48, dword4C, dword50, dword54, dword58, dword5C, dword60, dword64, dword68,
                dword6C, dword70, dword74, dword78, dword7C, dword80, dword84, dword88, dword8C, dword90,
                dword94, dword98, dword9C, dwordA0, dwordA4, dwordA8, dwordAC, dwordB0
            });
        }

        [MarshalAs(UnmanagedType.I4)]
        public int QuestID;

        public WoWQuestStepsCollection Steps;

        public WoWQuestCurrentStep CurrentStep;

        public WoWQuestState CurrentState;

        public uint dword2C;

        public uint dword30;

        public uint dword34;

        public uint dword38;

        public uint dword3C;

        public uint dword40;

        public uint dword44;

        public uint dword48;

        public uint dword4C;

        public uint dword50;

        public uint dword54;

        public uint dword58;

        public uint dword5C;

        public uint dword60;

        public uint dword64;

        public uint dword68;

        public uint dword6C;

        public uint dword70;

        public uint dword74;

        public uint dword78;

        public uint dword7C;

        public uint dword80;

        public uint dword84;

        public uint dword88;

        public uint dword8C;

        public uint dword90;

        public uint dword94;

        public uint dword98;

        public uint dword9C;

        public uint dwordA0;

        public uint dwordA4;

        public uint dwordA8;

        public uint dwordAC;

        public uint dwordB0;
    }
}
