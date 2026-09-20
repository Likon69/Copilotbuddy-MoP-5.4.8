using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.CommonBot;
using Styx.Patchables;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    public class WoWSpell : IEquatable<WoWSpell>
    {
        private WoWSpell(int id, WoWDb.Row row)
        {
            _id = id;
            _entry = row.GetStruct<SpellEntry>();
            LocalizedName = row.GetStringField<SpellEntry>("Name");
            NameSubText = row.GetStringField<SpellEntry>("NameSubText");
            Description = row.GetStringField<SpellEntry>("Description");
            AuraDescription = row.GetStringField<SpellEntry>("AuraDescription");
        }

        public static WoWSpell FromId(int id)
        {
            return DbcCache<WoWSpell>.GetOrAdd(ClientDb.Spell, (uint)id, row => new WoWSpell(id, row));
        }

        #region General / IDs

        public bool IsValid
        {
            get { return _id != 0; }
        }

        public uint BaseLevel
        {
            get { return _entry.SpellLevels.BaseLevel; }
        }

        public uint Level
        {
            get { return _entry.SpellLevels.SpellLevel; }
        }

        public int Id
        {
            get { return _id; }
        }

        public uint Category
        {
            get { return _entry.SpellCategories.Category; }
        }

        public SpellEntry InternalInfo
        {
            get { return _entry; }
        }

        public string Name
        {
            get { return GetName(Id); }
        }

        public string LocalizedName { get; private set; }

        public string NameSubText { get; private set; }

        public string Description { get; private set; }

        public string AuraDescription { get; private set; }

        public uint SpellMissileId
        {
            get { return _entry.SpellMissileId; }
        }

        public uint SpellDescriptionVariableId
        {
            get { return _entry.SpellDescriptionVariableId; }
        }

        public uint ResearchProjectId
        {
            get { return _entry.ResearchProjectId; }
        }

        public WoWSpellSchool School
        {
            get { return (WoWSpellSchool)InternalInfo.Misc.SpellSchoolMask; }
        }

        #endregion

        public bool IsHealingSpell
        {
            get
            {
                return SpellEffects.Any(se => se.EffectType == WoWSpellEffectType.Heal || se.EffectType == WoWSpellEffectType.HealPct || se.EffectType == WoWSpellEffectType.HealMechanical || se.EffectType == WoWSpellEffectType.HealMaxHealth);
            }
        }

        public SpellAttributes Attributes
        {
            get { return (SpellAttributes)GetAttributes(0); }
        }

        public SpellAttributesEx AttributesEx
        {
            get { return (SpellAttributesEx)GetAttributes(1); }
        }

        public SpellAttributesEx2 AttributesEx2
        {
            get { return (SpellAttributesEx2)GetAttributes(2); }
        }

        public SpellAttributesEx3 AttributesEx3
        {
            get { return (SpellAttributesEx3)GetAttributes(3); }
        }

        public SpellAttributesEx4 AttributesEx4
        {
            get { return (SpellAttributesEx4)GetAttributes(4); }
        }

        public SpellAttributesEx5 AttributesEx5
        {
            get { return (SpellAttributesEx5)GetAttributes(5); }
        }

        public SpellAttributesEx6 AttributesEx6
        {
            get { return (SpellAttributesEx6)GetAttributes(6); }
        }

        public SpellAttributesEx7 AttributesEx7
        {
            get { return (SpellAttributesEx7)GetAttributes(7); }
        }

        public SpellAttributesEx8 AttributesEx8
        {
            get { return (SpellAttributesEx8)GetAttributes(8); }
        }

        public unsafe uint GetAttributes(int index)
        {
            if (index < 0 || index >= SpellMiscRec.NumAttributeFields)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            SpellMiscRec misc = InternalInfo.Misc;
            return misc.Attributes[index];
        }

        public bool HasAttribute(SpellAttributes attribute)
        {
            return (Attributes & attribute) != ~(SpellAttributes.Unk0 | SpellAttributes.Ranged | SpellAttributes.OnNextSwing1 | SpellAttributes.Unk3 | SpellAttributes.Unk4 | SpellAttributes.TradeSkillSpell | SpellAttributes.Passive | SpellAttributes.Unk7 | SpellAttributes.Unk8 | SpellAttributes.Unk9 | SpellAttributes.OnNextSwing2 | SpellAttributes.Unk11 | SpellAttributes.DaytimeOnly | SpellAttributes.NightOnly | SpellAttributes.IndoorsOnly | SpellAttributes.OutdoorsOnly | SpellAttributes.NotShapeshift | SpellAttributes.OnlyStealthed | SpellAttributes.Unk18 | SpellAttributes.LevelDamageCalculation | SpellAttributes.StopAttackTarget | SpellAttributes.ImpossibleDodgeParryBlock | SpellAttributes.SetTrackingTarget | SpellAttributes.Unk23 | SpellAttributes.CastableWhileMounted | SpellAttributes.DisabledWhileActive | SpellAttributes.Unk26 | SpellAttributes.CastableWhileSitting | SpellAttributes.CantUsedInCombat | SpellAttributes.UnaffectedByInvulnerability | SpellAttributes.Unk30 | SpellAttributes.CantCancel);
        }

        public bool HasAttribute(SpellAttributesEx attribute)
        {
            return (AttributesEx & attribute) != ~(SpellAttributesEx.Unk0 | SpellAttributesEx.DrainAllPower | SpellAttributesEx.Channeled1 | SpellAttributesEx.CantReflected | SpellAttributesEx.Unk4 | SpellAttributesEx.NotBreakStealth | SpellAttributesEx.Channeled2 | SpellAttributesEx.Negative | SpellAttributesEx.NotInCombatTarget | SpellAttributesEx.Unk9 | SpellAttributesEx.NoThreat | SpellAttributesEx.Unk11 | SpellAttributesEx.Unk12 | SpellAttributesEx.Farsight | SpellAttributesEx.Unk14 | SpellAttributesEx.DispelAurasOnImmunity | SpellAttributesEx.UnaffectedBySchoolImmune | SpellAttributesEx.Unk17 | SpellAttributesEx.Unk18 | SpellAttributesEx.Unk19 | SpellAttributesEx.ReqTargetComboPoints | SpellAttributesEx.Unk21 | SpellAttributesEx.ReqComboPoints | SpellAttributesEx.Unk23 | SpellAttributesEx.Unk24 | SpellAttributesEx.Unk25 | SpellAttributesEx.Unk26 | SpellAttributesEx.Unk27 | SpellAttributesEx.Unk28 | SpellAttributesEx.Unk29 | SpellAttributesEx.Unk30 | SpellAttributesEx.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx2 attribute)
        {
            return (AttributesEx2 & attribute) != ~(SpellAttributesEx2.Unk0 | SpellAttributesEx2.Unk1 | SpellAttributesEx2.IgnoreLos | SpellAttributesEx2.Unk3 | SpellAttributesEx2.Unk4 | SpellAttributesEx2.AutoRepeated | SpellAttributesEx2.Unk6 | SpellAttributesEx2.Unk7 | SpellAttributesEx2.Unk8 | SpellAttributesEx2.Unk9 | SpellAttributesEx2.Unk10 | SpellAttributesEx2.HealthFunnel | SpellAttributesEx2.Unk12 | SpellAttributesEx2.Unk13 | SpellAttributesEx2.Unk14 | SpellAttributesEx2.Unk15 | SpellAttributesEx2.Unk16 | SpellAttributesEx2.Unk17 | SpellAttributesEx2.Unk18 | SpellAttributesEx2.NotNeedShapeshift | SpellAttributesEx2.Unk20 | SpellAttributesEx2.DamageReducedShield | SpellAttributesEx2.Unk22 | SpellAttributesEx2.Unk23 | SpellAttributesEx2.Unk24 | SpellAttributesEx2.Unk25 | SpellAttributesEx2.Unk26 | SpellAttributesEx2.Unk27 | SpellAttributesEx2.Unk28 | SpellAttributesEx2.CantCrit | SpellAttributesEx2.Unk30 | SpellAttributesEx2.FoodBuff);
        }

        public bool HasAttribute(SpellAttributesEx3 attribute)
        {
            return (AttributesEx3 & attribute) != ~(SpellAttributesEx3.Unk0 | SpellAttributesEx3.Unk1 | SpellAttributesEx3.Unk2 | SpellAttributesEx3.Unk3 | SpellAttributesEx3.Unk4 | SpellAttributesEx3.Unk5 | SpellAttributesEx3.Unk6 | SpellAttributesEx3.Unk7 | SpellAttributesEx3.TargetOnlyPlayer | SpellAttributesEx3.Unk9 | SpellAttributesEx3.MainHand | SpellAttributesEx3.Battleground | SpellAttributesEx3.CastOnDead | SpellAttributesEx3.Unk13 | SpellAttributesEx3.Unk14 | SpellAttributesEx3.Unk15 | SpellAttributesEx3.Unk16 | SpellAttributesEx3.NoInitialAggro | SpellAttributesEx3.CantMiss | SpellAttributesEx3.Unk19 | SpellAttributesEx3.DeathPersistent | SpellAttributesEx3.Unk21 | SpellAttributesEx3.ReqWand | SpellAttributesEx3.Unk23 | SpellAttributesEx3.ReqOffhand | SpellAttributesEx3.Unk25 | SpellAttributesEx3.Unk26 | SpellAttributesEx3.Unk27 | SpellAttributesEx3.Unk28 | SpellAttributesEx3.Unk29 | SpellAttributesEx3.Unk30 | SpellAttributesEx3.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx4 attribute)
        {
            return (AttributesEx4 & attribute) != ~(SpellAttributesEx4.Unk0 | SpellAttributesEx4.Unk1 | SpellAttributesEx4.Unk2 | SpellAttributesEx4.Unk3 | SpellAttributesEx4.Unk4 | SpellAttributesEx4.Unk5 | SpellAttributesEx4.NotStealable | SpellAttributesEx4.Unk7 | SpellAttributesEx4.StackDotModifier | SpellAttributesEx4.Unk9 | SpellAttributesEx4.SpellVsExtendCost | SpellAttributesEx4.Unk11 | SpellAttributesEx4.Unk12 | SpellAttributesEx4.Unk13 | SpellAttributesEx4.Unk14 | SpellAttributesEx4.Unk15 | SpellAttributesEx4.NotUsableInArena | SpellAttributesEx4.UsableInArena | SpellAttributesEx4.Unk18 | SpellAttributesEx4.Unk19 | SpellAttributesEx4.Unk20 | SpellAttributesEx4.Unk21 | SpellAttributesEx4.Unk22 | SpellAttributesEx4.Unk23 | SpellAttributesEx4.Unk24 | SpellAttributesEx4.Unk25 | SpellAttributesEx4.CastOnlyInOutland | SpellAttributesEx4.Unk27 | SpellAttributesEx4.Unk28 | SpellAttributesEx4.Unk29 | SpellAttributesEx4.Unk30 | SpellAttributesEx4.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx5 attribute)
        {
            return (AttributesEx5 & attribute) != ~(SpellAttributesEx5.Unk0 | SpellAttributesEx5.NoReagentWhilePrep | SpellAttributesEx5.Unk2 | SpellAttributesEx5.UsableWhileStunned | SpellAttributesEx5.Unk4 | SpellAttributesEx5.SingleTargetSpell | SpellAttributesEx5.Unk6 | SpellAttributesEx5.Unk7 | SpellAttributesEx5.Unk8 | SpellAttributesEx5.StartPeriodicAtApply | SpellAttributesEx5.HideDuration | SpellAttributesEx5.Unk11 | SpellAttributesEx5.Unk12 | SpellAttributesEx5.Unk13 | SpellAttributesEx5.Unk14 | SpellAttributesEx5.Unk15 | SpellAttributesEx5.Unk16 | SpellAttributesEx5.UsableWhileFeared | SpellAttributesEx5.UsableWhileConfused | SpellAttributesEx5.Unk19 | SpellAttributesEx5.Unk20 | SpellAttributesEx5.Unk21 | SpellAttributesEx5.Unk22 | SpellAttributesEx5.Unk23 | SpellAttributesEx5.Unk24 | SpellAttributesEx5.Unk25 | SpellAttributesEx5.Unk26 | SpellAttributesEx5.Unk27 | SpellAttributesEx5.Unk28 | SpellAttributesEx5.Unk29 | SpellAttributesEx5.Unk30 | SpellAttributesEx5.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx6 attribute)
        {
            return (AttributesEx6 & attribute) != ~(SpellAttributesEx6.Unk0 | SpellAttributesEx6.OnlyInArena | SpellAttributesEx6.Unk2 | SpellAttributesEx6.Unk3 | SpellAttributesEx6.Unk4 | SpellAttributesEx6.Unk5 | SpellAttributesEx6.Unk6 | SpellAttributesEx6.Unk7 | SpellAttributesEx6.Unk8 | SpellAttributesEx6.Unk9 | SpellAttributesEx6.Unk10 | SpellAttributesEx6.NotInRaidInstance | SpellAttributesEx6.Unk12 | SpellAttributesEx6.Unk13 | SpellAttributesEx6.Unk14 | SpellAttributesEx6.Unk15 | SpellAttributesEx6.Unk16 | SpellAttributesEx6.Unk17 | SpellAttributesEx6.Unk18 | SpellAttributesEx6.Unk19 | SpellAttributesEx6.Unk20 | SpellAttributesEx6.Unk21 | SpellAttributesEx6.Unk22 | SpellAttributesEx6.Unk23 | SpellAttributesEx6.Unk24 | SpellAttributesEx6.Unk25 | SpellAttributesEx6.Unk26 | SpellAttributesEx6.Unk27 | SpellAttributesEx6.Unk28 | SpellAttributesEx6.NoDmgMods | SpellAttributesEx6.Unk30 | SpellAttributesEx6.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx7 attribute)
        {
            return (AttributesEx7 & attribute) != ~(SpellAttributesEx7.Unk0 | SpellAttributesEx7.Unk1 | SpellAttributesEx7.PaladinAura | SpellAttributesEx7.Unk3 | SpellAttributesEx7.Unk4 | SpellAttributesEx7.TotemSpell | SpellAttributesEx7.Unk6 | SpellAttributesEx7.Unk7 | SpellAttributesEx7.Unk8 | SpellAttributesEx7.Unk9 | SpellAttributesEx7.Unk10 | SpellAttributesEx7.Unk11 | SpellAttributesEx7.Unk12 | SpellAttributesEx7.Unk13 | SpellAttributesEx7.Unk14 | SpellAttributesEx7.Unk15 | SpellAttributesEx7.Unk16 | SpellAttributesEx7.Unk17 | SpellAttributesEx7.Unk18 | SpellAttributesEx7.Unk19 | SpellAttributesEx7.Unk20 | SpellAttributesEx7.Unk21 | SpellAttributesEx7.Unk22 | SpellAttributesEx7.Unk23 | SpellAttributesEx7.Unk24 | SpellAttributesEx7.Unk25 | SpellAttributesEx7.Unk26 | SpellAttributesEx7.Unk27 | SpellAttributesEx7.Unk28 | SpellAttributesEx7.Unk29 | SpellAttributesEx7.Unk30 | SpellAttributesEx7.Unk31);
        }

        public bool HasAttribute(SpellAttributesEx8 attribute)
        {
            return (AttributesEx8 & attribute) != ~(SpellAttributesEx8.Unk0 | SpellAttributesEx8.Unk1 | SpellAttributesEx8.Unk2 | SpellAttributesEx8.Unk3 | SpellAttributesEx8.Unk4 | SpellAttributesEx8.Unk5 | SpellAttributesEx8.Unk6 | SpellAttributesEx8.Unk7 | SpellAttributesEx8.Unk8 | SpellAttributesEx8.Unk9 | SpellAttributesEx8.Unk10 | SpellAttributesEx8.Unk11 | SpellAttributesEx8.AuraSendsAmount | SpellAttributesEx8.Unk13 | SpellAttributesEx8.Unk14 | SpellAttributesEx8.Unk15 | SpellAttributesEx8.Unk16 | SpellAttributesEx8.Unk17 | SpellAttributesEx8.Unk18 | SpellAttributesEx8.IgnoreTargetForComboPoints | SpellAttributesEx8.ArmorSpecialization | SpellAttributesEx8.Unk21 | SpellAttributesEx8.Unk22 | SpellAttributesEx8.Unk23 | SpellAttributesEx8.Unk24 | SpellAttributesEx8.Unk25 | SpellAttributesEx8.RaidMarker | SpellAttributesEx8.Unk27 | SpellAttributesEx8.GuildPerks | SpellAttributesEx8.Mastery | SpellAttributesEx8.Unk30 | SpellAttributesEx8.Unk31);
        }

        #region Mechanics

        public WoWDispelType DispelType
        {
            get { return (WoWDispelType)_entry.SpellCategories.Dispel; }
        }

        public WoWSpellMechanic Mechanic
        {
            get { return (WoWSpellMechanic)_entry.SpellCategories.Mechanic; }
        }

        public int PowerCost
        {
            get { return GetSpellInfo().PowerCost; }
        }

        public string Icon
        {
            get { return GetSpellInfo().Icon; }
        }

        public bool IsFunnel
        {
            get { return GetSpellInfo().IsFunnel; }
        }

        public uint MaxStackCount
        {
            get { return _entry.SpellAuraOptions.StackAmount; }
        }

        public uint CreatesItemId
        {
            get
            {
                foreach (SpellEffect se in SpellEffects)
                {
                    switch (se.EffectType)
                    {
                        case WoWSpellEffectType.CreateItem:
                        case WoWSpellEffectType.CreateItem2:
                            return se.ItemType;
                    }
                }

                return 0;
            }
        }

        #endregion

        #region Cast Info

        public void Cast()
        {
            SpellManager.CastSpellById(Id);
        }

        public uint CastTime
        {
            get { return GetSpellInfo().CastTime; }
        }

        public bool CanCast
        {
            get
            {
                string text = string.Format("IsUsableSpell({0}) or IsUsableSpell((GetSpellInfo({0})))", Id);
                SpellFindResults spellFindResults;
                if (SpellManager.FindSpell(Id, out spellFindResults))
                {
                    return Lua.GetReturnVal<bool>("return " + text, 0U);
                }

                return Lua.GetReturnVal<bool>(string.Format("return (IsSpellKnown({0}) or IsSpellKnown({0}, 1) or IsPlayerSpell({0})) and ({1})", Id, text), 0U);
            }
        }

        #endregion

        #region Distance Calculations

        public bool IsMeleeSpell
        {
            get { return _entry.Misc.SpellRangeId == 2U; }
        }

        public bool IsSelfOnlySpell
        {
            get { return _entry.Misc.SpellRangeId == 1U; }
        }

        public float MinRange
        {
            get { return GetSpellInfo().MinRange; }
        }

        public float MaxRange
        {
            get { return GetSpellInfo().MaxRange; }
        }

        public bool HasRange
        {
            get { return !IsMeleeSpell && !IsSelfOnlySpell; }
        }

        #endregion

        #region Targets

        public uint MaxTargets
        {
            get { return _entry.SpellTargetRestrictions.Targets; }
        }

        public WoWCreatureType TargetType
        {
            get { return (WoWCreatureType)_entry.SpellTargetRestrictions.TargetCreatureType; }
        }

        #endregion

        private SpellEffectCacheEntry? GetSpellEffectCacheEntry()
        {
            int maxId = StyxWoW.Memory.Read<int>(StyxWoW.Offsets.Statics.SpellCacheMaxId);
            if (Id >= 0 && Id < maxId)
            {
                SpellEffectCacheEntry entry = StyxWoW.Memory.Read<SpellEffectCacheEntry>(false, StyxWoW.Offsets.Statics.SpellCacheBase, (IntPtr)(MarshalCache<SpellEffectCacheEntry>.Size * Id));
                return entry;
            }

            return null;
        }

        public int NumSpellEffects
        {
            get
            {
                SpellEffectCacheEntry? entry = GetSpellEffectCacheEntry();
                if (entry == null)
                {
                    return 0;
                }

                return entry.Value.Count;
            }
        }

        #region Spell Effects

        public SpellEffect SpellEffect1
        {
            get { return GetSpellEffect(0); }
        }

        public SpellEffect SpellEffect2
        {
            get { return GetSpellEffect(1); }
        }

        public SpellEffect SpellEffect3
        {
            get { return GetSpellEffect(2); }
        }

        public SpellEffect[] SpellEffects
        {
            get
            {
                SpellEffectCacheEntry? entry = GetSpellEffectCacheEntry();
                if (entry == null)
                {
                    return new SpellEffect[0];
                }

                uint[] array = StyxWoW.Memory.ReadArray<uint>(entry.Value.EffectIds, entry.Value.Count);
                return array.Select(SpellEffect.FromId).Where(se => se != null).ToArray();
            }
        }

        public SpellEffect GetSpellEffect(int index)
        {
            SpellEffectCacheEntry? entry = GetSpellEffectCacheEntry();
            if (entry != null && index >= 0 && index < entry.Value.Count)
            {
                IntPtr effectIds = entry.Value.EffectIds;
                uint id = StyxWoW.Memory.Read<uint>(effectIds + 4 * index);
                return SpellEffect.FromId(id);
            }

            return null;
        }

        #endregion

        #region Cooldown/Duration

        public bool Cooldown
        {
            get { return CooldownTimeLeft.TotalMilliseconds > 0.0; }
        }

        public TimeSpan CooldownTimeLeft
        {
            get
            {
                double returnVal = Lua.GetReturnVal<double>(string.Format("local x,y=GetSpellCooldown({0}); return x+y-GetTime()", Id), 0U);
                if (returnVal <= 0.0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromSeconds(returnVal);
            }
        }

        public uint BaseCooldown
        {
            get { return _entry.SpellCooldowns.RecoveryTime; }
        }

        public int BaseDuration
        {
            get { return _entry.SpellDuration.BaseDuration; }
        }

        public int DurationPerLevel
        {
            get { return _entry.SpellDuration.PerLevel; }
        }

        public int MaxDuration
        {
            get { return _entry.SpellDuration.MaxDuration; }
        }

        #endregion

        #region Internal Memory Funcions

        private SpellInfo GetSpellInfo()
        {
            SpellInfo info;
            if (!CachedInfo.TryGetValue(Id, out info))
            {
                List<string> returnValues = Lua.GetReturnValues("return GetSpellInfo(" + Id + ")", "WoW.lua");
                int powerCost;
                int.TryParse(returnValues[3], out powerCost);
                bool isFunnel;
                bool.TryParse(returnValues[4], out isFunnel);
                uint castTime;
                uint.TryParse(returnValues[6], out castTime);
                float minRange;
                float.TryParse(returnValues[7], out minRange);
                float maxRange;
                float.TryParse(returnValues[8], out maxRange);
                info = new SpellInfo
                {
                    Icon = returnValues[2],
                    PowerCost = powerCost,
                    IsFunnel = isFunnel,
                    CastTime = castTime,
                    MinRange = minRange,
                    MaxRange = maxRange
                };
                CachedInfo[Id] = info;
            }

            return info;
        }

        #endregion

        internal static string GetName(int id)
        {
            return DbHelper.Instance.GetName(id);
        }

        #region Overides of: Object

        public override string ToString()
        {
            return Name;
        }

        public bool Equals(WoWSpell other)
        {
            return !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || _id == other._id);
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((WoWSpell)obj)));
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public static bool operator ==(WoWSpell left, WoWSpell right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WoWSpell left, WoWSpell right)
        {
            return !Equals(left, right);
        }

        #endregion

        private readonly int _id;

        internal readonly SpellEntry _entry;

        private static readonly Dictionary<int, SpellInfo> CachedInfo = new Dictionary<int, SpellInfo>();

        public struct SpellFlyoutEntry
        {
            public override string ToString()
            {
                return string.Format("[Id: {0}, Dword4: {1}, Dword8: {2}, DwordC: {3}, SpellIconId: {4}, Name: {5}, DescriptionPtr: {6}]", Id, dword4, dword8, dwordC, SpellIconId, NamePtr, DescriptionPtr);
            }

            public uint Id;

            public uint dword4;

            public uint dword8;

            public uint dwordC;

            public uint SpellIconId;

            public uint NamePtr;

            public uint DescriptionPtr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Flag96
        {
            public override string ToString()
            {
                return string.Format("<DwA: {0} - DwB: {1} - DwC: {2}>", DwA, DwB, DwC);
            }

            public uint DwA;

            public uint DwB;

            public uint DwC;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellTargetRestrictionsEntry
        {
            public override string ToString()
            {
                return string.Format("[Id: {0}, SpellId: {1}, DifficultyId: {2}, ConeAngle: {3}, Width: {4}, MaxAffectedTargets: {5}, MaxTargetLevel: {6}, TargetCreatureType: {7}, Targets: 0x{8:X}]", Id, SpellId, DifficultyId, ConeAngle, Width, MaxAffectedTargets, MaxTargetLevel, TargetCreatureType, Targets);
            }

            public readonly uint Id;

            public readonly uint SpellId;

            public readonly uint DifficultyId;

            public readonly float ConeAngle;

            public readonly float Width;

            public readonly uint MaxAffectedTargets;

            public readonly uint MaxTargetLevel;

            public readonly uint TargetCreatureType;

            public readonly uint Targets;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellAuraOptionsEntry
        {
            public override string ToString()
            {
                return string.Format("[Id: {0}, StackAmount: {1}, ProcChance: {2}, ProcCharges: {3}, ProcFlags: {4}]", Id, StackAmount, ProcChance, ProcCharges, ProcFlags);
            }

            public uint Id;

            public uint StackAmount;

            public uint ProcChance;

            public uint ProcCharges;

            public uint ProcFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellAuraRestrictionsEntry
        {
            public override string ToString()
            {
                return string.Format("[Id: {0}, CasterAuraState: {1}, TargetAuraState: {2}, CasterAuraStateNot: {3}, TargetAuraStateNot: {4}, CasterAuraSpell: {5}, TargetAuraSpell: {6}, ExcludeCasterAuraSpell: {7}, ExcludeTargetAuraSpell: {8}]", Id, CasterAuraState, TargetAuraState, CasterAuraStateNot, TargetAuraStateNot, CasterAuraSpell, TargetAuraSpell, ExcludeCasterAuraSpell, ExcludeTargetAuraSpell);
            }

            public uint Id;

            public uint CasterAuraState;

            public uint TargetAuraState;

            public uint CasterAuraStateNot;

            public uint TargetAuraStateNot;

            public uint CasterAuraSpell;

            public uint TargetAuraSpell;

            public uint ExcludeCasterAuraSpell;

            public uint ExcludeTargetAuraSpell;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellCastingRequirementsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, FacingCasterFlags: {1}, MinFactionId: {2}, MinReputation: {3}, AreaGroupId: {4}, RequiredAuraVision: {5}, RequiresSpellFocus: {6}", Id, FacingCasterFlags, MinFactionId, MinReputation, AreaGroupId, RequiredAuraVision, RequiresSpellFocus);
            }

            public uint Id;

            public uint FacingCasterFlags;

            public uint MinFactionId;

            public uint MinReputation;

            public int AreaGroupId;

            public uint RequiredAuraVision;

            public uint RequiresSpellFocus;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellCategoriesEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, Category: {1}, DamageClass: {2}, Dispel: {3}, Mechanic: {4}, PreventionType: {5}, StartRecoveryCategory: {6}", Id, Category, DamageClass, Dispel, Mechanic, PreventionType, StartRecoveryCategory);
            }

            public uint Id;

            public uint dword4;

            public uint dword8;

            public uint dwordC;

            public uint DamageClass;

            public uint Dispel;

            public uint Mechanic;

            public uint PreventionType;

            public uint StartRecoveryCategory;

            public uint Category;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellClassOptionsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, ModalNextSpell: {1}, SpellFamilyFlags: {2}, SpellFamilyFlags2: {3}, SpellFamilyName: {4}, Description: {5}", Id, ModalNextSpell, SpellFamilyFlags, SpellFamilyFlags2, SpellFamilyName, Description);
            }

            public uint Id;

            public uint ModalNextSpell;

            public ulong SpellFamilyFlags;

            public uint SpellFamilyFlags2;

            public uint SpellFamilyName;

            public uint Description;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellCooldownsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, CategoryRecoveryTime: {1}, RecoveryTime: {2}, StartRecoveryTime: {3}", Id, CategoryRecoveryTime, RecoveryTime, StartRecoveryTime);
            }

            public uint Id;

            public uint CategoryRecoveryTime;

            public uint RecoveryTime;

            public uint StartRecoveryTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellEffectEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, EffectType: {1}, EffectMultipleValue: {2}, EffectApplyAuraName: {3}, EffectAmplitude: {4}, EffectBasePoints: {5}, Unk3204: {6}, DmgMultiplier: {7}, EffectChainTarget: {8}, EffectDieSides: {9}, EffectItemType: {10}, EffectMechanic: {11}, EffectMiscValue: {12}, EffectMiscValueB: {13}, EffectPointsPerComboPoint: {14}, EffectRadiusIndex: {15}, EffectRadiusMaxIndex: {16}, EffectRealPointsPerLevel: {17}, EffectSpellClassMask: {18}, EffectTriggerSpell: {19}, EffectImplicitTargetA: {20}, EffectImplicitTargetB: {21}, EffectSpellId: {22}, EffectIndex: {23}", new object[]
                {
                    Id, EffectType, EffectMultipleValue, EffectApplyAuraName, EffectAmplitude, EffectBasePoints, unk_320_4, DmgMultiplier, EffectChainTarget, EffectDieSides,
                    EffectItemType, EffectMechanic, EffectMiscValue, EffectMiscValueB, EffectPointsPerComboPoint, EffectRadiusIndex, EffectRadiusMaxIndex, EffectRealPointsPerLevel, EffectSpellClassMask, EffectTriggerSpell,
                    EffectImplicitTargetA, EffectImplicitTargetB, EffectSpellId, EffectIndex
                });
            }

            public uint Id;

            public uint Dword4;

            public uint EffectType;

            public float EffectMultipleValue;

            public uint EffectApplyAuraName;

            public uint EffectAmplitude;

            public int EffectBasePoints;

            public float unk_320_4;

            public float DmgMultiplier;

            public uint EffectChainTarget;

            public int EffectDieSides;

            public uint EffectItemType;

            public uint EffectMechanic;

            public int EffectMiscValue;

            public int EffectMiscValueB;

            public float EffectPointsPerComboPoint;

            public uint EffectRadiusIndex;

            public uint EffectRadiusMaxIndex;

            public float EffectRealPointsPerLevel;

            public Flag96 EffectSpellClassMask;

            public uint EffectTriggerSpell;

            public float Unk;

            public uint EffectImplicitTargetA;

            public uint EffectImplicitTargetB;

            public uint EffectIndex;

            public uint EffectSpellId;

            public uint Dword70;

            public uint Dword74;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellEquippedItemsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, EquippedItemClass: {1}, EquippedItemInventoryTypeMask: {2}, EquippedItemSubClassMask: {3}", Id, EquippedItemClass, EquippedItemInventoryTypeMask, EquippedItemSubClassMask);
            }

            public uint Id;

            public int EquippedItemClass;

            public int EquippedItemInventoryTypeMask;

            public int EquippedItemSubClassMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellInterruptsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, AuraInterruptFlags: {1}, Unk: {2}, ChannelInterruptFlags: {3}, Unk2: {4}, InterruptFlags: {5}", Id, AuraInterruptFlags, Unk, ChannelInterruptFlags, Unk2, InterruptFlags);
            }

            public uint Id;

            public uint AuraInterruptFlags;

            private uint Unk;

            public uint ChannelInterruptFlags;

            private uint Unk2;

            public uint InterruptFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellLevelsEntry
        {
            public uint Id;

            public uint SpellId;

            public uint Flag;

            public uint BaseLevel;

            public uint MaxLevel;

            public uint SpellLevel;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellPowerEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, ManaCost: {1}, ManaCostPerLevel: {2}, ManaCostPercentage: {3}, ManaPerSecond: {4}, PowerDisplayId: {5}, Unk: {6}", Id, ManaCost, ManaCostPerLevel, ManaCostPercentage, ManaPerSecond, PowerDisplayId, Unk);
            }

            public uint Id;

            public uint ManaCost;

            public uint ManaCostPerLevel;

            public uint ManaCostPercentage;

            public uint ManaPerSecond;

            public uint PowerDisplayId;

            private int Unk;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellReagentsEntry
        {
            public uint Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I4)]
            public int[] Reagent;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] ReagentCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellShapeshiftEntry
        {
            public override string ToString()
            {
                return string.Format("Entry: {0}, Stances: {1}, Unk: {2}, StancesNot: {3}, Unk1: {4}, StanceBarOrder: {5}", Entry, Stances, Unk, StancesNot, Unk1, StanceBarOrder);
            }

            public uint Entry;

            public uint Stances;

            private uint Unk;

            public uint StancesNot;

            private uint Unk1;

            public uint StanceBarOrder;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellTotemsEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, TotemCategory: {1}, Totem: {2}", Id, TotemCategory, Totem);
            }

            public uint Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] TotemCategory;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
            public uint[] Totem;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellCastTimesEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, CastTime: {1}, CastTimePerLevel: {2}, MinCastTime: {3}", Id, CastTime, CastTimePerLevel, MinCastTime);
            }

            public uint Id;

            public int CastTime;

            public float CastTimePerLevel;

            public int MinCastTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellRadiusEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, Radius: {1}, Radius2: {2}, Unk: {3}", Id, Radius, Radius2, _unk);
            }

            public uint Id;

            public float Radius;

            public float Radius2;

            private uint _unk;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellRangeEntry
        {
            public string Range1Str
            {
                get { return StyxWoW.Memory.ReadString(Range1, Encoding.UTF8); }
            }

            public string Range2Str
            {
                get { return StyxWoW.Memory.ReadString(Range2, Encoding.UTF8); }
            }

            public override string ToString()
            {
                return string.Format("Id: {0}, MinRange: {1}, MinRangeFriendly: {2}, MaxRange: {3}, MaxRangeFriendly: {4}, Flags: {5}, Range1: {6}, Range2: {7}", Id, MinRange, MinRangeFriendly, MaxRange, MaxRangeFriendly, Flags, Range1Str, Range2Str);
            }

            public uint Id;

            public float MinRange;

            public float MinRangeFriendly;

            public float MaxRange;

            public float MaxRangeFriendly;

            public uint Flags;

            public IntPtr Range1;

            public IntPtr Range2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SpellMiscRec
        {
            public const int NumAttributeFields = 14;

            public override string ToString()
            {
                return string.Format("Id: {0}, SpellId: {1}, Dword8: {2}, SpellCastTimesId: {3}, SpellDurationId: {4}, SpellRangeId: {5}, Speed: {6}, SpellVisualId: {7}, SpellVisualId2: {8}, SpellIconId: {9}, Dword58: {10}, SpellSchoolMask: {11}", new object[]
                {
                    Id, SpellId, dword8, SpellCastTimesId, SpellDurationId, SpellRangeId, Speed, SpellVisualId, SpellVisualId2, SpellIconId,
                    dword58, SpellSchoolMask
                });
            }

            public uint Id;

            public uint SpellId;

            public uint dword8;

            public fixed uint Attributes[14];

            public uint SpellCastTimesId;

            public uint SpellDurationId;

            public uint SpellRangeId;

            private float Speed;

            public uint SpellVisualId;

            public uint SpellVisualId2;

            public uint SpellIconId;

            public uint dword58;

            public uint SpellSchoolMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellRuneCostEntry
        {
            public bool NoRuneCost
            {
                get { return RuneCost[0] == 0U && RuneCost[1] == 0U && RuneCost[2] == 0U; }
            }

            public bool NoRunicPowerGain
            {
                get { return RunePowerGain == 0U; }
            }

            public override string ToString()
            {
                return string.Format("Entry: {0}, RuneCost: {1}, RunePowerGain: {2}", Entry, RuneCost, RunePowerGain);
            }

            public uint Entry;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
            public uint[] RuneCost;

            public uint RunePowerGain;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellShapeshiftFormEntry
        {
            public bool IsHumanoid
            {
                get { return CreatureType <= 0; }
            }

            public override string ToString()
            {
                return string.Format("Id: {0}, ButtonPosition: {1}, Name: {2}, Flags1: {3}, CreatureType: {4}, Unk1: {5}, AttackSpeed: {6}, AllianceModelId: {7}, HordeModelId: {8}, Unk2: {9}, Unk3: {10}, Unk4: {11}, SpellBarIds: {12}", new object[]
                {
                    Id, ButtonPosition, Name, Flags1, CreatureType, unk1, AttackSpeed, AllianceModelId, HordeModelId, unk2,
                    unk3, _unk4, SpellBarIds
                });
            }

            public uint Id;

            public uint ButtonPosition;

            public uint Name;

            public uint Flags1;

            public int CreatureType;

            private uint unk1;

            public uint AttackSpeed;

            public uint AllianceModelId;

            public uint HordeModelId;

            private uint unk2;

            private uint unk3;

            private uint _unk4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U4)]
            public uint[] SpellBarIds;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellDifficultyEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, SpellId: {1}", Id, SpellId);
            }

            public uint Id;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
            public uint[] SpellId;
        }

        public struct SpellDurationEntry
        {
            public override string ToString()
            {
                return string.Format("Id: {0}, BaseDuration: {1}, PerLevel: {2}, MaxDuration: {3}", Id, BaseDuration, PerLevel, MaxDuration);
            }

            public uint Id;

            public int BaseDuration;

            public int PerLevel;

            public int MaxDuration;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SpellEntry
        {
            public SpellMiscRec Misc
            {
                get
                {
                    IntPtr ptr = StyxWoW.CallInjected<IntPtr>(StyxWoW.Offsets.Functions.GetSpellMiscRecord, CallingConvention.Cdecl, SpellMiscId);
                    if (ptr != IntPtr.Zero)
                    {
                        return StyxWoW.Memory.Read<SpellMiscRec>(ptr);
                    }

                    return default(SpellMiscRec);
                }
            }

            public SpellRuneCostEntry RuneCost
            {
                get
                {
                    if (RuneCostId == 0U)
                    {
                        return default(SpellRuneCostEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellRuneCost].GetRow(RuneCostId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellRuneCostEntry>();
                    }

                    return default(SpellRuneCostEntry);
                }
            }

            public SpellRangeEntry SpellRange
            {
                get
                {
                    if (Misc.SpellRangeId == 0U)
                    {
                        return default(SpellRangeEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellRange].GetRow(Misc.SpellRangeId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellRangeEntry>();
                    }

                    return default(SpellRangeEntry);
                }
            }

            public SpellDurationEntry SpellDuration
            {
                get
                {
                    SpellMiscRec misc = Misc;
                    if (misc.SpellDurationId == 0U)
                    {
                        return default(SpellDurationEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellDuration].GetRow(misc.SpellDurationId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellDurationEntry>();
                    }

                    return default(SpellDurationEntry);
                }
            }

            public SpellCastTimesEntry CastingTime
            {
                get
                {
                    SpellMiscRec misc = Misc;
                    if (misc.SpellCastTimesId == 0U)
                    {
                        return default(SpellCastTimesEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellCastTimes].GetRow(misc.SpellCastTimesId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellCastTimesEntry>();
                    }

                    return default(SpellCastTimesEntry);
                }
            }

            public SpellTotemsEntry SpellTotems
            {
                get
                {
                    if (SpellTotemsId == 0U)
                    {
                        return default(SpellTotemsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellTotems].GetRow(SpellTotemsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellTotemsEntry>();
                    }

                    return default(SpellTotemsEntry);
                }
            }

            public SpellShapeshiftEntry SpellShapeshift
            {
                get
                {
                    if (SpellShapeshiftId == 0U)
                    {
                        return default(SpellShapeshiftEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellShapeshift].GetRow(SpellShapeshiftId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellShapeshiftEntry>();
                    }

                    return default(SpellShapeshiftEntry);
                }
            }

            public SpellReagentsEntry SpellReagents
            {
                get
                {
                    if (SpellReagentsId == 0U)
                    {
                        return default(SpellReagentsEntry);
                    }

                    IntPtr cachePtr = StyxWoW.Offsets.Statics.SpellReagentsCacheObj;
                    IntPtr funcPtr = StyxWoW.Offsets.Functions.GetSpellReagentsRecord;
                    IntPtr retPtr = StyxWoW.CallInjected<IntPtr>(funcPtr, CallingConvention.ThisCall, (uint)(int)cachePtr, SpellReagentsId, 0, 0, 0, 0);
                    if (retPtr == IntPtr.Zero)
                    {
                        return default(SpellReagentsEntry);
                    }

                    return StyxWoW.Memory.Read<SpellReagentsEntry>(retPtr);
                }
            }

            public SpellLevelsEntry SpellLevels
            {
                get
                {
                    if (SpellLevelId == 0U)
                    {
                        return default(SpellLevelsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellLevels].GetRow(SpellLevelId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellLevelsEntry>();
                    }

                    return default(SpellLevelsEntry);
                }
            }

            public SpellInterruptsEntry SpellInterrupts
            {
                get
                {
                    if (SpellInterruptsId == 0U)
                    {
                        return default(SpellInterruptsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellInterrupts].GetRow(SpellInterruptsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellInterruptsEntry>();
                    }

                    return default(SpellInterruptsEntry);
                }
            }

            public SpellEquippedItemsEntry SpellEquippedItems
            {
                get
                {
                    if (SpellEquippedItemsId == 0U)
                    {
                        return default(SpellEquippedItemsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellEquippedItems].GetRow(SpellEquippedItemsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellEquippedItemsEntry>();
                    }

                    return default(SpellEquippedItemsEntry);
                }
            }

            public SpellCooldownsEntry SpellCooldowns
            {
                get
                {
                    if (SpellCooldownsId == 0U)
                    {
                        return default(SpellCooldownsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellCooldowns].GetRow(SpellCooldownsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellCooldownsEntry>();
                    }

                    return default(SpellCooldownsEntry);
                }
            }

            public SpellClassOptionsEntry SpellClassOptions
            {
                get
                {
                    if (SpellClassOptionsId == 0U)
                    {
                        return default(SpellClassOptionsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellClassOptions].GetRow(SpellClassOptionsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellClassOptionsEntry>();
                    }

                    return default(SpellClassOptionsEntry);
                }
            }

            public SpellAuraOptionsEntry SpellAuraOptions
            {
                get
                {
                    if (SpellAuraOptionsId == 0U)
                    {
                        return default(SpellAuraOptionsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellAuraOptions].GetRow(SpellAuraOptionsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellAuraOptionsEntry>();
                    }

                    return default(SpellAuraOptionsEntry);
                }
            }

            public SpellAuraRestrictionsEntry SpellAuraRestrictions
            {
                get
                {
                    if (SpellAuraRestrictionsId == 0U)
                    {
                        return default(SpellAuraRestrictionsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellAuraRestrictions].GetRow(SpellAuraRestrictionsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellAuraRestrictionsEntry>();
                    }

                    return default(SpellAuraRestrictionsEntry);
                }
            }

            public SpellCastingRequirementsEntry SpellCastingRequirements
            {
                get
                {
                    if (SpellCastingRequirementsId == 0U)
                    {
                        return default(SpellCastingRequirementsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellCastingRequirements].GetRow(SpellCastingRequirementsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellCastingRequirementsEntry>();
                    }

                    return default(SpellCastingRequirementsEntry);
                }
            }

            public SpellCategoriesEntry SpellCategories
            {
                get
                {
                    if (SpellCategoriesId == 0U)
                    {
                        return default(SpellCategoriesEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellCategories].GetRow(SpellCategoriesId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellCategoriesEntry>();
                    }

                    return default(SpellCategoriesEntry);
                }
            }

            public SpellTargetRestrictionsEntry SpellTargetRestrictions
            {
                get
                {
                    if (SpellTargetRestrictionsId == 0U)
                    {
                        return default(SpellTargetRestrictionsEntry);
                    }

                    WoWDb.Row row = StyxWoW.Db[ClientDb.SpellTargetRestrictions].GetRow(SpellTargetRestrictionsId);
                    if (row != null && row.IsValid)
                    {
                        return row.GetStruct<SpellTargetRestrictionsEntry>();
                    }

                    return default(SpellTargetRestrictionsEntry);
                }
            }

            public int Id;

            public IntPtr Name;

            public IntPtr NameSubText;

            public IntPtr Description;

            public IntPtr AuraDescription;

            public uint RuneCostId;

            public uint SpellMissileId;

            public uint SpellDescriptionVariableId;

            public float float20;

            public uint SpellScalingId;

            public uint SpellAuraOptionsId;

            public uint SpellAuraRestrictionsId;

            public uint SpellCastingRequirementsId;

            public uint SpellCategoriesId;

            public uint SpellClassOptionsId;

            public uint SpellCooldownsId;

            public uint SpellEquippedItemsId;

            public uint SpellInterruptsId;

            public uint SpellLevelId;

            public uint SpellReagentsId;

            public uint SpellShapeshiftId;

            public uint SpellTargetRestrictionsId;

            public uint SpellTotemsId;

            public uint ResearchProjectId;

            public uint SpellMiscId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SpellEffectCacheEntry
        {
            public int Count;

            public IntPtr EffectIds;

            public int Unknown1;

            public int Unknown2;
        }

        #region Nested Type: SpellInfo

        private class SpellInfo
        {
            public string Icon;

            public int PowerCost;

            public bool IsFunnel;

            public uint CastTime;

            public float MinRange;

            public float MaxRange;
        }

        #endregion
    }
}
