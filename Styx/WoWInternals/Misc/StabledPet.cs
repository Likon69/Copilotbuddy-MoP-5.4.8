using System;
using Styx.CommonBot;
using Styx.Helpers;

namespace Styx.WoWInternals.Misc
{
    public class StabledPet
    {
        internal StabledPet(NativeStabledPet nativeStabledPet, int index)
        {
            NativeStabledPet = nativeStabledPet;
            Index = index;
        }

        internal NativeStabledPet NativeStabledPet { get; private set; }

        public int Index { get; private set; }

        public bool CanSummon
        {
            get { return Index >= 0 && Index < 5; }
        }

        public uint PetNumber
        {
            get { return NativeStabledPet.PetNumber; }
        }

        public int PetLevel
        {
            get { return NativeStabledPet.PetLevel; }
        }

        public uint CreatureId
        {
            get { return NativeStabledPet.CreatureId; }
        }

        public string Name
        {
            get
            {
                string text;
                if ((text = _name) == null)
                {
                    text = (_name = NativeStabledPet.PetName.ToUTF8String());
                }
                return text;
            }
        }

        public bool Summon()
        {
            return CanSummon && SpellManager.Cast("Call Pet " + (Index + 1));
        }

        public int GetSummonSpellId()
        {
            if (!CanSummon)
            {
                return 0;
            }
            WoWSpell woWSpell;
            if (!SpellManager.Spells.TryGetValue("Call Pet " + (Index + 1), out woWSpell))
            {
                return 0;
            }
            return woWSpell.Id;
        }

        private string _name;
    }
}
