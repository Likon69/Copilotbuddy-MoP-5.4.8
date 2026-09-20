using System;
using System.Collections.Generic;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    public class WoWAuraCollection : List<WoWAura>
    {
        public WoWAuraCollection()
        {
        }

        public WoWAuraCollection(int capacity)
            : base(capacity)
        {
        }

        public WoWAuraCollection(IEnumerable<WoWAura> collection)
            : base(collection)
        {
        }

        public int GetTotalAuraModifier(WoWApplyAuraType auraType)
        {
            int result = 0;
            foreach (WoWAura aura in this)
            {
                for (int i = 0; i < 3; i++)
                {
                    SpellEffect se = aura.Spell.GetSpellEffect(i);
                    if (se.AuraType == auraType)
                    {
                        result += se.BasePoints;
                    }
                }
            }
            return result;
        }
    }
}
