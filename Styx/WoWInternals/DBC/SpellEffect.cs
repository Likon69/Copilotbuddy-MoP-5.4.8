using System;
using Styx.Patchables;

namespace Styx.WoWInternals.DBC
{
    public class SpellEffect
    {
        private readonly WoWSpell.SpellEffectEntry _entry;

        internal SpellEffect(uint id, WoWDb.Row row)
        {
            Id = id;
            _entry = row.GetStruct<WoWSpell.SpellEffectEntry>();
        }

        public uint Id { get; private set; }

        public WoWApplyAuraType AuraType
        {
            get { return (WoWApplyAuraType)_entry.EffectApplyAuraName; }
        }

        public WoWSpellEffectType EffectType
        {
            get { return (WoWSpellEffectType)_entry.EffectType; }
        }

        public float RealPointsPerLevel
        {
            get { return _entry.EffectRealPointsPerLevel; }
        }

        public int BasePoints
        {
            get { return _entry.EffectBasePoints; }
        }

        public uint Mechanic
        {
            get { return _entry.EffectMechanic; }
        }

        public uint ImplicitTargetA
        {
            get { return _entry.EffectImplicitTargetA; }
        }

        public uint ImplicitTargetB
        {
            get { return _entry.EffectImplicitTargetB; }
        }

        public uint RadiusIndex
        {
            get { return _entry.EffectRadiusIndex; }
        }

        public uint Amplitude
        {
            get { return _entry.EffectAmplitude; }
        }

        public float MultipleValue
        {
            get { return _entry.EffectMultipleValue; }
        }

        public uint ChainTarget
        {
            get { return _entry.EffectChainTarget; }
        }

        public uint ItemType
        {
            get { return _entry.EffectItemType; }
        }

        public int MiscValueA
        {
            get { return _entry.EffectMiscValue; }
        }

        public int MiscValueB
        {
            get { return _entry.EffectMiscValueB; }
        }

        public uint TriggerSpell
        {
            get { return _entry.EffectTriggerSpell; }
        }

        public float PointsPerComboPoint
        {
            get { return _entry.EffectPointsPerComboPoint; }
        }

        public WoWSpell.Flag96 SpellClassMask
        {
            get { return _entry.EffectSpellClassMask; }
        }

        public override string ToString()
        {
            return string.Format("AuraType: {0}, RealPointsPerLevel: {1}, BasePoints: {2}, Mechanic: {3}, ImplicitTargetA: {4}, ImplicitTargetB: {5}, RadiusIndex: {6}, Amplitude: {7}, MultipleValue: {8}, ChainTarget: {9}, ItemType: {10}, MiscValueA: {11}, MiscValueB: {12}, TriggerSpell: {13}, PointsPerComboPoint: {14}, SpellClassMask: {15}",
                AuraType, RealPointsPerLevel, BasePoints, Mechanic, ImplicitTargetA, ImplicitTargetB, RadiusIndex, Amplitude, MultipleValue, ChainTarget,
                ItemType, MiscValueA, MiscValueB, TriggerSpell, PointsPerComboPoint, SpellClassMask);
        }

        public static SpellEffect FromId(uint id)
        {
            return DbcCache<SpellEffect>.GetOrAdd(ClientDb.SpellEffect, id, row => new SpellEffect(id, row));
        }
    }
}
