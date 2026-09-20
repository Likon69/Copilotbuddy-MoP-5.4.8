using System;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWDynamicObject : WoWObject
    {
        public WoWDynamicObject(IntPtr baseAdress)
            : base(baseAdress)
        {
        }

        public int SpellId
        {
            get { return GetStorageField<int>((uint)WoWDynamicObjectFields.SpellID); }
        }

        public WoWSpell Spell
        {
            get { return WoWSpell.FromId(SpellId); }
        }

        public WoWUnit Caster
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CasterGuid); }
        }

        public ulong CasterGuid
        {
            get { return GetStorageField<ulong>((uint)WoWDynamicObjectFields.Caster); }
        }

        public float Radius
        {
            get { return GetStorageField<float>((uint)WoWDynamicObjectFields.Radius); }
        }

        public uint CastTime
        {
            get { return GetStorageField<uint>((uint)WoWDynamicObjectFields.CastTime); }
        }
    }
}
