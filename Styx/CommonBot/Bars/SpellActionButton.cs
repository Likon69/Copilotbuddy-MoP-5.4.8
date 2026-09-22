using System;
using Styx.WoWInternals;

namespace Styx.CommonBot.Bars
{
    public sealed class SpellActionButton : ActionButton
    {
        internal SpellActionButton(int slot, int offset, string idString, ActionButtonType type, ActionButtonSubType subType)
            : base(slot, offset, idString, type, subType)
        {
        }

        public override string Name
        {
            get
            {
                return this.Spell.Name;
            }
        }

        public int SpellId
        {
            get
            {
                return base.Id;
            }
        }

        public Styx.WoWInternals.WoWSpell Spell
        {
            get
            {
                return Styx.WoWInternals.WoWSpell.FromId(this.SpellId);
            }
        }
    }
}
