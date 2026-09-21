using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Styx.WoWInternals
{
    internal class WoWPetBattleOwner
    {
        public List<WoWPetBattlePet> Pets { get; set; }
        public uint Flags { get { return 0; } }
    }

    internal class WoWPetBattlePet
    {
        internal WoWPetBattlePet(IntPtr baseAddress, WoWPetBattleOwner owner)
        {
            BaseAddress = baseAddress;
            Owner = owner;
        }

        public IntPtr BaseAddress { get; private set; }
        public WoWPetBattleOwner Owner { get; private set; }

        private CPetBattlePet Native { get { return StyxWoW.Memory.Read<CPetBattlePet>(BaseAddress); } }

        public string Name { get { return StyxWoW.Memory.ReadString(Native.NamePtr, Encoding.UTF8); } }
        public string SpeciesName { get { return StyxWoW.Memory.ReadString(Native.SpeciesNamePtr, Encoding.UTF8); } }

        public uint DisplayId { get { return Native.DisplayID; } }
        public uint PetId { get { return Native.PetId; } }
        public uint MaxHealth { get { return Native.MaxHealth; } }
        public uint Health { get { return Native.Health; } }
        public uint Level { get { return Native.Level; } }
        public uint XP { get { return Native.XP; } }
        public uint Speed { get { return Native.Speed; } }
        public uint Power { get { return Native.Power; } }
        public uint BreedQuality { get { return Native.BreedQuality; } }
        public uint AbilityFlags { get { return Native.AbilityFlags; } }

        private uint NumAuras { get { return Native.NumAuras; } }
        private int NumAbilities { get { return Native.NumAbilities; } }

        private PetAbility[] Abilities { get { return StyxWoW.Memory.ReadArray<PetAbility>(Native.Abilities, NumAbilities); } }

        public void GetAbilityState(int abilityIndex, out int spellId, out bool isUsable, out int cooldownTime,
            out int cooldownStartOrEnd)
        {
            PetAbility[] abls = Abilities;
            if (abls.Length <= abilityIndex)
                throw new Exception("Invalid ability index!");

            PetAbility abl = abls[abilityIndex];
            spellId = abl.SpellId;
            cooldownTime = abl.CurrentCooldown;
            cooldownStartOrEnd = abl.CooldownTimeStartOrEnd;
            isUsable = (Owner.Flags & 3) == 0 &&
                       (AbilityFlags & 2) == 0 &&
                       abl.CurrentCooldown == 0 &&
                       abl.CooldownTimeStartOrEnd == 0 &&
                       WoWPetBattle.BattleState == WoWPetBattleState.RoundInProgress;
        }

        #region Nested type: CPetBattlePet

        [StructLayout(LayoutKind.Sequential)]
        internal struct CPetBattlePet
        {
            public uint dword0;
            public uint dword4;
            public uint dword8;
            public uint dwordC;
            public uint PetId;
            public uint dword14;
            public uint DisplayID;
            public uint dword1C;
            public IntPtr NamePtr;
            public IntPtr SpeciesNamePtr;
            public uint dword28;
            public uint dword2C;
            public uint dword30;
            public uint dword34;
            public uint dword38;
            public uint NumPets;
            public uint dword40;
            public uint dword44;
            public uint dword48;
            public uint dword4C;
            public uint dword50;
            public uint dword54;
            public uint dword58;
            public uint dword5C;
            public uint dword60;
            public uint MaxHealth;
            public uint Health;
            public uint Level;
            public uint XP;
            public uint Speed;
            public uint Power;
            public uint BreedQuality;
            public uint AbilityFlags;
            public uint dword84;
            public uint NumAuras;
            public uint dword8C;
            public uint dword90;
            public uint dword94;
            public uint dword98;
            public uint dword9C;
            public uint dwordA0;
            public uint dwordA4;
            public int NumAbilities;
            public IntPtr Abilities;
            public uint dwordB0, dwordB4;
        }

        #endregion

        #region Nested type: PetAbility

        [StructLayout(LayoutKind.Sequential)]
        internal struct PetAbility
        {
            public int SpellId;
            public int CurrentCooldown;
            public int CooldownTimeStartOrEnd;
        }

        #endregion
    }

    internal class WoWPetBattle
    {
        private static CPetBattle CurrentBattle { get { return StyxWoW.Memory.Read<CPetBattle>(StyxWoW.Offsets.Statics.C_PetBattles__IsInBattle); } }

        private static CPetBattleOwner[] Owners
        {
            get
            {
                return StyxWoW.Memory.ReadArray<CPetBattleOwner>(CurrentBattle.BattleInfo.Owners,
                    CurrentBattle.BattleInfo.NumOwners);
            }
        }

        public static bool IsInBattle { get { return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.C_PetBattles__IsInBattle) != 0; } }

        public static WoWPetBattleState BattleState { get { return (WoWPetBattleState)CurrentBattle.BattleInfo.BattleState; } }

        #region Structs

        #region Nested type: CPetBattle

        [StructLayout(LayoutKind.Sequential)]
        internal struct CPetBattle
        {
            /// CPetBattleBase
            public CPetBattleBase Base;

            public uint dword138;
            public uint dword13C;

            /// CPetBattleInfo
            public CPetBattleInfo BattleInfo;

            public uint dword1B8;
            public uint dword1BC;
            public uint dword1C0;
            public uint dword1C4;
            public uint dword1C8;
            public uint dword1CC;
            public uint dword1D0;
            public uint dword1D4;
            public uint dword1D8;
            public uint dword1DC;
        }

        #endregion

        #region Nested type: CPetBattleBase

        [StructLayout(LayoutKind.Sequential)]
        internal struct CPetBattleBase
        {
            public uint VTable;
            public uint dword4;
            public uint dword8;
            public uint dwordC;
            public uint dword10;

            /// CPetBattleBase*
            public IntPtr Next;

            public uint NextIsValid;
            public uint dword1C;
            public uint Previous;
            public uint PreviousIsValid;
            public uint dword28;
            public uint dword2C;
            public uint dword30;
            public uint dword34;
            public uint dword38;
            public uint dword3C;
            public uint dword40;
            public uint dword44;
            public uint dword48;
            public uint dword4C;
            public uint dword50;
            public uint dword54;
            public uint dword58;
            public uint dword5C;
            public uint dword60;
            public uint dword64;

            /// Vector3
            public WoWPoint Position;

            public uint dword74;
            public uint dword78;
            public uint dword7C;
            public uint SecureRandomSeed;
            public uint SecureRandomUnk;
            public uint dword88;
            public uint dword8C;
            public uint dword90;
            public uint dword94;
            public uint dword98;
            public uint dword9C;
            public uint dwordA0;
            public uint dwordA4;
            public uint dwordA8;
            public uint dwordAC;
            public uint dwordB0;
            public uint dwordB4;
            public uint dwordB8;
            public uint dwordBC;
            public uint dwordC0;
            public uint dwordC4;
            public byte byteC8;

            /// _BYTE[7]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7, ArraySubType = UnmanagedType.I1)]
            public byte[] fC9;

            public uint dwordD0;
            public uint dwordD4;
            public uint dwordD8;
            public uint dwordDC;
            public uint dwordE0;
            public uint dwordE4;
            public uint dwordE8;
            public uint dwordEC;
            public uint dwordF0;
            public uint dwordF4;
            public uint dwordF8;
            public uint dwordFC;
            public uint dword100;
            public uint dword104;
            public uint dword108;
            public uint dword10C;
            public uint dword110;

            /// struct_v4_1*
            public IntPtr dword114;

            public uint dword118;
            public uint dword11C;
            public uint dword120;
            public uint dword124;
            public uint dword128;
            public uint dword12C;
            public uint dword130;
            public uint dword134;
        }

        #endregion

        #region Nested type: CPetBattleInfo

        [StructLayout(LayoutKind.Sequential)]
        internal struct CPetBattleInfo
        {
            public uint dword0;
            public uint BattleState;
            public uint dword8;
            public uint dwordC;
            public uint TurnTimeStarted;
            public uint TurnTime;
            public uint dword18;
            public int NumOwners;

            /// CPetBattleOwner*
            public IntPtr Owners;

            public uint dword24;
            public uint dword28;
            public uint dword2C;
            public uint dword30;
            public uint dword34;
            public uint dword38;
            public uint dword3C;
            public uint dword40;
            public uint dword44;
            public uint dword48;
            public uint dword4C;
            public uint dword50;
            public uint dword54;
            public uint dword58;
            public uint dword5C;
            public uint dword60;
            public uint dword64;
            public uint dword68;
            public uint dword6C;
            public uint dword70;
            public uint dword74;
        }

        #endregion

        #region Nested type: CPetBattleOwner

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct CPetBattleOwner
        {
            public uint dword0;
            public uint dword4;
            public byte IsPlayerNPC;

            /// char[3]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
            public string f9;

            public uint dwordC;
            public uint ActivePetId;
            public uint Flags;
            public uint dword18;
            public uint dword1C;
            public uint dword20;
            public uint dword24;
            public uint dword28;
            public uint dword2C;
            public uint dword30;
            public uint dword34;
            public uint dword38;
            public uint NumPets;

            /// CPetBattlePet*
            public IntPtr Pets;

            public uint dword44;
        }

        #endregion

        #endregion
    }
}
