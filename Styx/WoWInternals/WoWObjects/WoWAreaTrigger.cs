using System;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWAreaTrigger : WoWObject
    {
        internal WoWAreaTrigger(IntPtr baseAdress)
            : base(baseAdress)
        {
        }

        public ulong CasterGuid
        {
            get { return GetStorageField<ulong>((uint)WoWAreaTriggerFields.Caster); }
        }

        public WoWObject Caster
        {
            get { return ObjectManager.GetObjectByGuid(CasterGuid); }
        }

        public int SpellId
        {
            get { return GetStorageField<int>((uint)WoWAreaTriggerFields.SpellID); }
        }

        public WoWSpell Spell
        {
            get { return WoWSpell.FromId(SpellId); }
        }

        public int SpellVisualId
        {
            get { return GetStorageField<int>((uint)WoWAreaTriggerFields.SpellVisualID); }
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds(GetStorageField<uint>((uint)WoWAreaTriggerFields.Duration)); }
        }

        public TimeSpan TimeLeft
        {
            get
            {
                uint duration = GetStorageField<uint>((uint)WoWAreaTriggerFields.Duration);

                if (duration > 0)
                {
                    ulong current = StyxWoW.WoWClient.PerformanceCounter();

                    uint timestamp = StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.AreaTrigger_Timestamp);

                    uint endTime = timestamp + duration;

                    if (endTime > current)
                        return TimeSpan.FromMilliseconds(endTime - current);
                }

                return TimeSpan.Zero;
            }
        }

        public WoWSimpleMovementInfo MovementInfo
        {
            get { return new WoWSimpleMovementInfo(BaseAddress + (int)StyxWoW.Offsets.Misc.AreaTriggerMovementData); }
        }

        public override float Rotation
        {
            get { return MovementInfo.GetRotation(); }
        }
    }
}
