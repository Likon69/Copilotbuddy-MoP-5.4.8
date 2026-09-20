using System;

namespace Styx.WoWInternals
{
    public class WoWPetSpell
    {
        public WoWSpell Spell { get; private set; }

        public PetSpellType SpellType { get; private set; }

        public PetAction Action { get; private set; }

        public PetStance Stance { get; private set; }

        public bool Cooldown
        {
            get { return Spell != null && Spell.Cooldown; }
        }

        public int ActionBarIndex { get; private set; }

        private WoWPetSpell()
        {
            ActionBarIndex = -1;
            SpellType = PetSpellType.Unknown;
            Action = PetAction.None;
            Stance = PetStance.None;
        }

        internal WoWPetSpell(uint spellMask, int index)
            : this()
        {
            ActionBarIndex = index;
            uint num = spellMask & 16777215U;
            switch ((spellMask >> 24) & 63U)
            {
            case 1U:
            case 8U:
            case 9U:
            case 10U:
            case 11U:
            case 12U:
            case 13U:
            case 14U:
            case 15U:
            case 16U:
            case 17U:
                SpellType = PetSpellType.Spell;
                Spell = WoWSpell.FromId((int)num);
                return;
            default:
                SpellType = PetSpellType.Unknown;
                return;
            case 6U:
                SpellType = PetSpellType.Stance;
                Stance = (PetStance)(spellMask & 16777215U);
                return;
            case 7U:
                SpellType = PetSpellType.Action;
                Action = (PetAction)(spellMask & 16777215U);
                return;
            }
        }

        public override string ToString()
        {
            switch (SpellType)
            {
            case PetSpellType.Unknown:
                return "Unknown";
            case PetSpellType.Spell:
                if (!(Spell != null))
                {
                    return "Unknown";
                }
                return Spell.Name;
            case PetSpellType.Action:
                if (Action == PetAction.MoveTo)
                {
                    return "Move To";
                }
                return Action.ToString();
            case PetSpellType.Stance:
                return Stance.ToString();
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public enum PetSpellType
        {
            Unknown,
            Spell,
            Action,
            Stance
        }

        public enum PetAction
        {
            None = -1,
            Wait,
            Follow,
            Attack,
            Dismiss,
            MoveTo
        }

        public enum PetStance
        {
            None = -1,
            Passive,
            Defensive,
            Aggressive,
            Assist
        }
    }
}
