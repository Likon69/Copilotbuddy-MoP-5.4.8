using System;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWAnimatedSubObject : WoWSubObject
    {
        internal WoWAnimatedSubObject(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public byte AnimationState
        {
            get { return GetField<byte>(AnimatedSubObjectOffsets.AnimationState); }
        }

        private T GetField<T>(AnimatedSubObjectOffsets offset) where T : struct
        {
            return StyxWoW.Memory.Read<T>(BaseAddress + (int)offset);
        }

        private enum AnimatedSubObjectOffsets
        {
            AnimationState = 16
        }
    }
}
