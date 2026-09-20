using System;
using System.Runtime.InteropServices;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct WoWQuestStepsCollection
    {
        public override string ToString()
        {
            return string.Format("StepsCount: {0}, Steps: {1}", StepsCount, string.Join<WoWQuestStep>(" || ", Steps));
        }

        public WoWQuestStep[] Steps
        {
            get { return GetSteps(); }
        }

        private WoWQuestStep[] GetSteps()
        {
            if (intptr_0 != IntPtr.Zero)
            {
                return StyxWoW.Memory.ReadArray<WoWQuestStep>(intptr_0, (int)StepsCount);
            }
            return new WoWQuestStep[StepsCount];
        }

        public uint StepsCount;

        private IntPtr intptr_0;

        private uint uint_0;
    }
}
