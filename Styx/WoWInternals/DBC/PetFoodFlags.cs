using System;

namespace Styx.WoWInternals.DBC
{
    [Flags]
    public enum PetFoodFlags
    {
        None = 0,
        Meat = 1,
        Fish = 2,
        Cheese = 4,
        Bread = 8,
        Fungus = 16,
        Fruit = 32,
        Raw_Meat_Maybe = 64,
        Raw_Fish_Maybe = 128
    }
}
