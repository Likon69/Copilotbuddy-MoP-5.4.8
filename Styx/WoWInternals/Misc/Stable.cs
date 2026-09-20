using System;
using System.Collections.Generic;
using GreenMagic;

namespace Styx.WoWInternals.Misc
{
    public class Stable
    {
        public StabledPet GetPet(int index)
        {
            if (index < 0 || index >= MaxNumPets)
            {
                return null;
            }

            NativeStabledPet nativeStabledPet = StyxWoW.Memory.Read<NativeStabledPet>(StyxWoW.Offsets.Statics.StabledPets + index * MarshalCache<NativeStabledPet>.Size);
            if (nativeStabledPet.PetNumber == 0U)
            {
                return null;
            }

            return new StabledPet(nativeStabledPet, index);
        }

        public List<StabledPet> GetCarriedPets()
        {
            return GetFirst(0, MaxNumCarriedPets);
        }

        public List<StabledPet> GetNonCarriedPets()
        {
            return GetFirst(MaxNumCarriedPets, MaxNumPets - MaxNumCarriedPets);
        }

        public List<StabledPet> GetPets()
        {
            return GetFirst(0, MaxNumPets);
        }

        private List<StabledPet> GetFirst(int index, int count)
        {
            NativeStabledPet[] array = StyxWoW.Memory.ReadArray<NativeStabledPet>(StyxWoW.Offsets.Statics.StabledPets + index * MarshalCache<NativeStabledPet>.Size, count);
            List<StabledPet> list = new List<StabledPet>();
            for (int i = 0; i < count; i++)
            {
                if (array[i].PetNumber != 0U)
                {
                    list.Add(new StabledPet(array[i], i));
                }
            }

            return list;
        }

        private const int MaxNumPets = 25;

        private const int MaxNumCarriedPets = 5;
    }
}
