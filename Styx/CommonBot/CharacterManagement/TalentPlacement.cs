using System;

namespace Styx.CommonBot.CharacterManagement
{
    public class TalentPlacement
    {
        public TalentPlacement(int tier, int index, string name)
        {
            this.Tier = tier;
            this.Index = index;
            this.Name = name;
        }

        public int Tier { get; private set; }

        public int Index { get; private set; }

        public string Name { get; private set; }
    }
}
