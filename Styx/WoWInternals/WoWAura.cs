using System;
using Styx.Patchables;

namespace Styx.WoWInternals
{
    public class WoWAura
    {
        internal WoWAura(NativeAura backingAura)
        {
            _aura = backingAura;
            _variableEffects = StyxWoW.Memory.ReadArray<float>(backingAura.VariableEffects, backingAura.NumVariableEffects);
        }

        public WoWApplyAuraType ApplyAuraType
        {
            get
            {
                if (!(Spell == null) && Spell.SpellEffect1 != null)
                {
                    return Spell.SpellEffect1.AuraType;
                }
                return WoWApplyAuraType.None;
            }
        }

        public ulong CreatorGuid
        {
            get { return _aura.CreatorGuid; }
        }

        public int SpellId
        {
            get { return (int)_aura.AuraId; }
        }

        public AuraFlags Flags
        {
            get { return (AuraFlags)_aura.Flags; }
        }

        public uint Duration
        {
            get { return _aura.Duration; }
        }

        public uint EndTime
        {
            get { return _aura.EndTime; }
        }

        public TimeSpan TimeLeft
        {
            get
            {
                ulong num = StyxWoW.WoWClient.PerformanceCounter();
                if (num > (ulong)EndTime)
                {
                    return TimeSpan.Zero;
                }
                return new TimeSpan(0, 0, 0, 0, (int)((ulong)EndTime - num));
            }
        }

        public uint StackCount
        {
            get { return (uint)_aura.StackCount; }
        }

        public uint Level
        {
            get { return (uint)_aura.Level; }
        }

        public bool IsHarmful
        {
            get { return (byte)(Flags & AuraFlags.Harmful) != 0; }
        }

        public bool IsActive
        {
            get { return (byte)(Flags & AuraFlags.Active) != 0; }
        }

        public bool IsPassive
        {
            get { return (byte)(Flags & AuraFlags.Passive) != 0; }
        }

        public bool Cancellable
        {
            get { return (byte)(Flags & AuraFlags.Cancelable) != 0; }
        }

        public string Name
        {
            get { return Spell.Name; }
        }

        public float[] VariableEffects
        {
            get { return _variableEffects; }
        }

        public WoWSpell Spell
        {
            get
            {
                WoWSpell spell = _cachedSpell;
                if (spell == null)
                {
                    spell = _cachedSpell = WoWSpell.FromId(SpellId);
                }
                return spell;
            }
        }

        public void TryCancelAura()
        {
            if (Cancellable)
            {
                string text = string.Format("for i = 1,40 do local spellId = select(11, UnitAura('player', i)) if spellId == {0} then CancelUnitBuff('player', i) end end", SpellId);
                Lua.DoString(text, "WoW.lua");
            }
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, SpellId: {1}, CreatorGuid: {2:X8}, TimeLeft: {3}, StackCount: {4}, Flags: {5}", Name, SpellId, CreatorGuid, TimeLeft, StackCount, Flags);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && !(obj.GetType() != typeof(WoWAura)) && (ReferenceEquals(this, obj) || Equals((WoWAura)obj));
        }

        public static bool operator ==(WoWAura left, WoWAura right)
        {
            return ReferenceEquals(left, right) || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.SpellId == right.SpellId);
        }

        public static bool operator !=(WoWAura left, WoWAura right)
        {
            return !(left == right);
        }

        public bool Equals(WoWAura other)
        {
            return !(other == null) && other.SpellId == SpellId;
        }

        public override int GetHashCode()
        {
            int hashCode = SpellId.GetHashCode();
            return (hashCode * 397) ^ Name.GetHashCode();
        }

        private readonly NativeAura _aura;

        private WoWSpell _cachedSpell;

        private readonly float[] _variableEffects;
    }
}
