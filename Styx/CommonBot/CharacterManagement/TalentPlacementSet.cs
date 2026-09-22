using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Styx.CommonBot.CharacterManagement
{
    public class TalentPlacementSet : IEnumerable<TalentPlacement>, IEnumerable
    {
        public const int NumTalents = 6;
        public const int NumTalentChoicesPerTier = 3;

        private readonly TalentPlacement[] _talents = new TalentPlacement[6];

        public TalentPlacementSet()
        {
        }

        public TalentPlacementSet(IEnumerable<TalentPlacement> talents)
        {
            if (talents == null)
            {
                throw new ArgumentNullException("talents");
            }
            foreach (TalentPlacement talentPlacement in talents)
            {
                if (talentPlacement == null)
                {
                    throw new ArgumentException("A null TalentPlacement instance was found", "talents");
                }
                if (talentPlacement.Tier < 0 || talentPlacement.Tier >= 6)
                {
                    throw new ArgumentException("A talent placement has an invalid tier. Valid values are 0 through " + 5, "talents");
                }
                if (talentPlacement.Index < 0 || talentPlacement.Index >= 3)
                {
                    throw new ArgumentException("A talent placement has an invalid index. Valid values are 0 through " + 2, "talents");
                }
                int tier = talentPlacement.Tier;
                if (this._talents[tier] != null)
                {
                    throw new ArgumentException("Found more than one talent placement with tier " + talentPlacement.Tier, "talents");
                }
                this._talents[tier] = talentPlacement;
            }
        }

        public TalentPlacement this[int tier]
        {
            get
            {
                if (tier < 0 || tier >= 6)
                {
                    throw new ArgumentOutOfRangeException("tier");
                }
                return this._talents[tier];
            }
        }

        public IEnumerator<TalentPlacement> GetEnumerator()
        {
            return this._talents.Where(t => t != null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
