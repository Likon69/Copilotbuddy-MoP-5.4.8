using System;
using Styx.WoWInternals;

namespace Styx.CommonBot
{
    public class SpellFindResults
    {
        public SpellFindResults(WoWSpell original, WoWSpell @override = null)
        {
            Original = original;
            Override = @override;
        }

        public WoWSpell Original { get; private set; }

        public WoWSpell Override { get; private set; }
    }
}
