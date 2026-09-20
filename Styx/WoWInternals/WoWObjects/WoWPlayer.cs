using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.Offsets;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWPlayer : WoWUnit
    {
        private T GetStorageField<T>(WoWPlayerFields field) where T : struct
        {
            return GetStorageField<T>((uint)field);
        }

        public WoWPlayer(IntPtr baseAdress)
            : base(baseAdress)
        {
        }

        public WoWFactionGroup FactionGroup
        {
            get
            {
                if (_factionGroupCache != null && _factionGroupCache.Value != WoWFactionGroup.Neutral)
                {
                    return _factionGroupCache.Value;
                }

                if (_factionGroupCache == null || _factionCacheTimer.IsFinished)
                {
                    _factionGroupCache = GetFactionGroup();
                    _factionCacheTimer.Reset();
                }

                return _factionGroupCache.Value;
            }
        }

        public bool IsNeutralPandaren
        {
            get { return FactionGroup == WoWFactionGroup.Neutral; }
        }

        public bool IsHorde
        {
            get { return FactionGroup == WoWFactionGroup.Horde; }
        }

        public bool IsAlliance
        {
            get { return FactionGroup == WoWFactionGroup.Alliance; }
        }

        private BitVector32 PlayerFlags
        {
            get { return GetStorageField<BitVector32>(648U); } // WoWPlayerFields.PlayerFlags
        }

        public bool IsGhost
        {
            get { return PlayerFlags[16]; } // WoWPlayerFlags.Ghost
        }

        public bool IsGroupLeader
        {
            get { return StyxWoW.Me.GroupInfo.GroupLeaderGuid == Guid; }
        }

        public bool IsAFKFlagged
        {
            get { return PlayerFlags[2]; } // WoWPlayerFlags.AFK
        }

        public bool IsDNDFlagged
        {
            get { return PlayerFlags[4]; } // WoWPlayerFlags.DND
        }

        public bool IsGM
        {
            get { return PlayerFlags[8]; } // WoWPlayerFlags.GM
        }

        public bool IsResting
        {
            get { return PlayerFlags[32]; } // WoWPlayerFlags.Resting
        }

        public bool IsFFAPvPFlagged
        {
            get { return PlayerFlags[128]; } // WoWPlayerFlags.FreeForAllPVP
        }

        public bool ContestedPvPFlagged
        {
            get { return PlayerFlags[256]; } // WoWPlayerFlags.ContestedPVP
        }

        public bool IsPvPFlagged
        {
            get { return PlayerFlags[512]; } // WoWPlayerFlags.InPVP
        }

        public bool IsHidingHelm
        {
            get { return PlayerFlags[1024]; } // WoWPlayerFlags.HideHelm
        }

        public bool IsHidingCloak
        {
            get { return PlayerFlags[2048]; } // WoWPlayerFlags.HideCloak
        }

        public bool IsOutOfBounds
        {
            get { return PlayerFlags[16384]; } // WoWPlayerFlags.OutOfBounds
        }

        public bool IsPvPTimerActive
        {
            get { return PlayerFlags[262144]; } // WoWPlayerFlags.PVPTimer
        }

        public bool IsInsideSanctuary
        {
            get { return PlayerFlags[65536]; } // WoWPlayerFlags.Sanctuary
        }

        public uint GuildRank
        {
            get { return GetStorageField<uint>(WoWPlayerFields.GuildRankID); }
        }

        public uint GuildDeleteDate
        {
            get { return GetStorageField<uint>(WoWPlayerFields.GuildDeleteDate); }
        }

        public uint GuildLevel
        {
            get { return GetStorageField<uint>(WoWPlayerFields.GuildLevel); }
        }

        public uint DuelTeamId
        {
            get { return GetStorageField<uint>(WoWPlayerFields.DuelTeam); }
        }

        public uint GuildTimestamp
        {
            get { return GetStorageField<uint>(WoWPlayerFields.GuildTimeStamp); }
        }

        public uint ChosenTitle
        {
            get { return GetStorageField<uint>(WoWPlayerFields.PlayerTitle); }
        }

        public WoWInebriationLevel Inebriation
        {
            get { return (WoWInebriationLevel)GetStorageField<uint>(WoWPlayerFields.FakeInebriation); }
        }

        public uint Experience
        {
            get { return GetStorageField<uint>(WoWPlayerFields.XP); }
        }

        public uint NextLevelExperience
        {
            get { return GetStorageField<uint>(WoWPlayerFields.NextLevelXP); }
        }

        public uint CharacterPoints
        {
            get { return GetStorageField<uint>(WoWPlayerFields.CharacterPoints); }
        }

        public float BlockPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.BlockPercentage); }
        }

        public float DodgePercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.DodgePercentage); }
        }

        public float ParryPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.ParryPercentage); }
        }

        public uint Expertise
        {
            get { return GetStorageField<uint>(WoWPlayerFields.MainhandExpertise); }
        }

        public uint ExpertiseOffHand
        {
            get { return GetStorageField<uint>(WoWPlayerFields.OffhandExpertise); }
        }

        public uint RangedExpertise
        {
            get { return GetStorageField<uint>(WoWPlayerFields.RangedExpertise); }
        }

        public float CritPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.CritPercentage); }
        }

        public float RangedCritPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.RangedCritPercentage); }
        }

        public float OffHandCritPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.OffhandCritPercentage); }
        }

        public uint ShieldBlock
        {
            get { return GetStorageField<uint>(WoWPlayerFields.ShieldBlock); }
        }

        public float ShieldBlockCritPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.ShieldBlockCritPercentage); }
        }

        public float Mastery
        {
            get { return GetStorageField<float>(WoWPlayerFields.Mastery); }
        }

        public uint RestedExperience
        {
            get { return GetStorageField<uint>(WoWPlayerFields.RestStateBonusPool); }
        }

        public uint SelfResurrectSpellId
        {
            get { return GetStorageField<uint>(WoWPlayerFields.SelfResSpell); }
        }

        public uint PvpMedalCount
        {
            get { return GetStorageField<uint>(WoWPlayerFields.PvpMedals); }
        }

        public uint HonorableKills
        {
            get { return GetStorageField<uint>(WoWPlayerFields.YesterdayHonorableKills); }
        }

        public uint LifetimeHonorableKills
        {
            get { return GetStorageField<uint>(WoWPlayerFields.LifetimeHonorableKills); }
        }

        public uint WatchedFactionIndex
        {
            get { return GetStorageField<uint>(WoWPlayerFields.WatchedFactionIndex); }
        }

        public uint MaxLevel
        {
            get { return GetStorageField<uint>(WoWPlayerFields.MaxLevel); }
        }

        public ulong DuelArbiterGuid
        {
            get { return GetStorageField<ulong>(WoWPlayerFields.DuelArbiter); }
        }

        public float PhysicalCritPercent
        {
            get { return GetStorageField<float>(6460U); } // WoWPlayerFields.SpellCritPercentage
        }

        public float HolyCritPercent
        {
            get { return GetStorageField<float>(6461U); } // WoWPlayerFields.SpellCritPercentage + 1
        }

        public float FireCritPercent
        {
            get { return GetStorageField<float>(6462U); } // WoWPlayerFields.SpellCritPercentage + 2
        }

        public float NatureCritPercent
        {
            get { return GetStorageField<float>(6463U); } // WoWPlayerFields.SpellCritPercentage + 3
        }

        public float FrostCritPercent
        {
            get { return GetStorageField<float>(6464U); } // WoWPlayerFields.SpellCritPercentage + 4
        }

        public float ShadowCritPercent
        {
            get { return GetStorageField<float>(6465U); } // WoWPlayerFields.SpellCritPercentage + 5
        }

        public float ArcaneCritPercent
        {
            get { return GetStorageField<float>(6466U); } // WoWPlayerFields.SpellCritPercentage + 6
        }

        public uint PhysicalBonusPositive
        {
            get { return GetStorageField<uint>(7312U); } // WoWPlayerFields.ModDamageDonePos
        }

        public uint HolyBonusPositive
        {
            get { return GetStorageField<uint>(7313U); } // WoWPlayerFields.ModDamageDonePos + 1
        }

        public uint FireBonusPositive
        {
            get { return GetStorageField<uint>(7314U); } // WoWPlayerFields.ModDamageDonePos + 2
        }

        public uint NatureBonusPositive
        {
            get { return GetStorageField<uint>(7315U); } // WoWPlayerFields.ModDamageDonePos + 3
        }

        public uint FrostBonusPositive
        {
            get { return GetStorageField<uint>(7316U); } // WoWPlayerFields.ModDamageDonePos + 4
        }

        public uint ShadowBonusPositive
        {
            get { return GetStorageField<uint>(7317U); } // WoWPlayerFields.ModDamageDonePos + 5
        }

        public uint ArcaneBonusPositive
        {
            get { return GetStorageField<uint>(7318U); } // WoWPlayerFields.ModDamageDonePos + 6
        }

        public uint PhysicalBonusNegative
        {
            get { return GetStorageField<uint>(7340U); } // WoWPlayerFields.ModDamageDoneNeg
        }

        public uint HolyBonusNegative
        {
            get { return GetStorageField<uint>(7341U); } // WoWPlayerFields.ModDamageDoneNeg + 1
        }

        public uint FireBonusNegative
        {
            get { return GetStorageField<uint>(7342U); } // WoWPlayerFields.ModDamageDoneNeg + 2
        }

        public uint NatureBonusNegative
        {
            get { return GetStorageField<uint>(7343U); } // WoWPlayerFields.ModDamageDoneNeg + 3
        }

        public uint FrostBonusNegative
        {
            get { return GetStorageField<uint>(7344U); } // WoWPlayerFields.ModDamageDoneNeg + 4
        }

        public uint ShadowBonusNegative
        {
            get { return GetStorageField<uint>(7345U); } // WoWPlayerFields.ModDamageDoneNeg + 5
        }

        public uint ArcaneBonusNegative
        {
            get { return GetStorageField<uint>(7346U); } // WoWPlayerFields.ModDamageDoneNeg + 6
        }

        public uint PhysicalBonusPercent
        {
            get { return GetStorageField<uint>(7368U); } // WoWPlayerFields.ModDamageDonePercent
        }

        public uint HolyBonusPercent
        {
            get { return GetStorageField<uint>(7369U); } // WoWPlayerFields.ModDamageDonePercent + 1
        }

        public uint FireBonusPercent
        {
            get { return GetStorageField<uint>(7370U); } // WoWPlayerFields.ModDamageDonePercent + 2
        }

        public uint NatureBonusPercent
        {
            get { return GetStorageField<uint>(7371U); } // WoWPlayerFields.ModDamageDonePercent + 3
        }

        public uint FrostBonusPercent
        {
            get { return GetStorageField<uint>(7372U); } // WoWPlayerFields.ModDamageDonePercent + 4
        }

        public uint ShadowBonusPercent
        {
            get { return GetStorageField<uint>(7373U); } // WoWPlayerFields.ModDamageDonePercent + 5
        }

        public uint ArcaneBonusPercent
        {
            get { return GetStorageField<uint>(7374U); } // WoWPlayerFields.ModDamageDonePercent + 6
        }

        public uint HealingBonusPositive
        {
            get { return GetStorageField<uint>(WoWPlayerFields.ModHealingDonePos); }
        }

        public float HealingModifierPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.ModHealingDonePercent); }
        }

        public float HealingBonusPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.ModHealingPercent); }
        }

        public float SpellPowerModifierPercent
        {
            get { return GetStorageField<float>(WoWPlayerFields.ModSpellPowerPercent); }
        }

        public uint TargetResistanceModifier
        {
            get { return GetStorageField<uint>(WoWPlayerFields.ModTargetResistance); }
        }

        public uint TargetArmorModifier
        {
            get { return GetStorageField<uint>(WoWPlayerFields.ModTargetPhysicalResistance); }
        }

        public WoWSpec Specialization
        {
            get { return (WoWSpec)GetStorageField<uint>(3848U); } // WoWPlayerFields.CurrentSpecID, no 5.3 member at this call site
        }

        public List<WoWArenaTeamInfo> ArenaTeams
        {
            get
            {
                List<WoWArenaTeamInfo> list = new List<WoWArenaTeamInfo>();
                for (uint i = 0U; i < 3U; i += 1U)
                {
                }

                return list;
            }
        }

        public float[] RuneRegen
        {
            get
            {
                float[] array = new float[4];
                for (uint i = 0U; i < 4U; i += 1U)
                {
                    array[i] = GetStorageField<float>(7776U + i); // WoWPlayerFields.RuneRegen
                }

                return array;
            }
        }

        public uint GlyphsEnabled
        {
            get { return GetStorageField<uint>(WoWPlayerFields.GlyphSlotsEnabled); }
        }

        public List<WoWGlyphInfo> Glyphs
        {
            get
            {
                List<WoWGlyphInfo> list = new List<WoWGlyphInfo>();
                for (uint i = 0U; i < 9U; i += 1U)
                {
                    uint slot = GetStorageField<uint>(7808U + i); // WoWPlayerFields.GlyphSlots
                    uint id = GetStorageField<uint>(7832U + i); // WoWPlayerFields.Glyphs
                    list.Add(new WoWGlyphInfo((int)i, slot != 0U, id));
                }

                return list;
            }
        }

        public uint PetSpellPower
        {
            get { return GetStorageField<uint>(WoWPlayerFields.PetSpellPower); }
        }

        public ulong Copper
        {
            get { return GetStorageField<ulong>(WoWPlayerFields.Coinage); }
        }

        public ulong Silver
        {
            get { return Copper / 100UL; }
        }

        public ulong Gold
        {
            get { return Copper / 10000UL; }
        }

        public float LevelFraction
        {
            get { return (float)Level + Experience / NextLevelExperience; }
        }

        public bool IsInMyParty
        {
            get { return StyxWoW.Me.GroupInfo.IsInCurrentParty(Guid); }
        }

        public bool IsInMyRaid
        {
            get { return StyxWoW.Me.GroupInfo.IsInCurrentRaid(Guid); }
        }

        public bool IsInMyPartyOrRaid
        {
            get { return IsInMyParty || IsInMyRaid; }
        }

        public override bool IsAlive
        {
            get { return !IsDead && !IsGhost; }
        }

        public override bool Mounted
        {
            get
            {
                if (Class == WoWClass.Druid)
                {
                    ShapeshiftForm shapeshift = Shapeshift;
                    if (shapeshift == ShapeshiftForm.EpicFlightForm || shapeshift == ShapeshiftForm.FlightForm)
                    {
                        return true;
                    }
                }

                return base.Mounted;
            }
        }

        public override string SafeName
        {
            get { return string.Format("Lvl {0} {1} {2}{3}", Level, FactionGroup, Class, PvpFlagged ? " (flagged)" : ""); }
        }

        public List<WoWUnit> Minions
        {
            get
            {
                if (_minionsCache == null)
                {
                    _minionsCache = new PerFrameCachedValue<List<WoWUnit>>(() => ObjectManager.GetObjectsOfType<WoWUnit>().Where(unit => !unit.IsNonCombatPet && unit.ControllingPlayer == this).ToList());
                }

                return _minionsCache;
            }
        }

        public int GetCombatRating(WoWPlayerCombatRating rating)
        {
            return GetStorageField<int>(WoWPlayerFields.CombatRatings + ((int)rating * 4));
        }

        private WoWFactionGroup? _factionGroupCache;

        private readonly WaitTimer _factionCacheTimer = new WaitTimer(TimeSpan.FromSeconds(15.0));

        private PerFrameCachedValue<List<WoWUnit>> _minionsCache;
    }
}
