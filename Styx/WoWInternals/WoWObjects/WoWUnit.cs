using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.Offsets;
using Styx.Patchables;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWCache;
using Tripper.Tools.Math;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWUnit : WoWObject, ILootableObject
    {
        public WoWUnit(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        private T GetStorageField<T>(WoWUnitFields field) where T : struct
        {
            return GetStorageField<T>((uint)field);
        }

        #region Cast Start/End/Id - Can Interrupt

        public DateTime CurrentCastStartTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime(StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitUnitCastStart)); }
        }

        public DateTime CurrentCastEndTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime(StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitUnitCastEnd)); }
        }

        public TimeSpan CurrentCastTimeLeft
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime currentCastEndTime = CurrentCastEndTime;
                if (!(currentCastEndTime > now))
                {
                    return TimeSpan.Zero;
                }

                return currentCastEndTime - now;
            }
        }

        public DateTime CurrentChannelStartTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime(StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitChannelStart)); }
        }

        public DateTime CurrentChannelEndTime
        {
            get { return StyxWoW.WoWClient.PerformanceCounterToDateTime(StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitChannelEnd)); }
        }

        public TimeSpan CurrentChannelTimeLeft
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime currentChannelEndTime = CurrentChannelEndTime;
                if (!(currentChannelEndTime > now))
                {
                    return TimeSpan.Zero;
                }

                return currentChannelEndTime - now;
            }
        }

        public uint CurrentCastId
        {
            get { return StyxWoW.Memory.Read<byte>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitUnitCastId); }
        }

        private PerFrameCachedValue<bool> _canInterruptCurrentSpellCastCache;

        public bool CanInterruptCurrentSpellCast
        {
            get
            {
                if (_canInterruptCurrentSpellCastCache == null)
                {
                    _canInterruptCurrentSpellCastCache = new PerFrameCachedValue<bool>(() =>
                    {
                        EnsureValid("CanInterruptCurrentSpellCast");
                        return StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.CGUnit_C__IsCastNotInterruptible, CallingConvention.ThisCall, (uint)(int)BaseAddress) == 0;
                    });
                }

                return _canInterruptCurrentSpellCastCache;
            }
        }

        public int ChanneledCastingSpellId
        {
            get { return StyxWoW.Memory.Read<int>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitUnitChannelId); }
        }

        public int NonChanneledCastingSpellId
        {
            get { return StyxWoW.Memory.Read<int>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitNonChanneledCastingSpellId); }
        }

        public int CastingSpellId
        {
            get
            {
                int nonChanneledCastingSpellId = NonChanneledCastingSpellId;
                if (nonChanneledCastingSpellId != 0)
                {
                    return nonChanneledCastingSpellId;
                }

                return ChanneledCastingSpellId;
            }
        }

        public WoWSpell CastingSpell
        {
            get
            {
                if (CastingSpellId == 0)
                {
                    return null;
                }

                return WoWSpell.FromId(CastingSpellId);
            }
        }

        public WoWSpell ChanneledSpell
        {
            get
            {
                if (ChanneledCastingSpellId == 0)
                {
                    return null;
                }

                return WoWSpell.FromId(ChanneledCastingSpellId);
            }
        }

        public bool IsCasting
        {
            get { return ChannelObject != null || CastingSpell != null; }
        }

        public bool IsChanneling
        {
            get { return ChanneledSpell != null; }
        }

        public bool IsCastingHealingSpell
        {
            get
            {
                int castingSpellId = CastingSpellId;
                if (castingSpellId == 0)
                {
                    return false;
                }

                WoWSpell spell = WoWSpell.FromId(castingSpellId);
                return spell.IsValid && spell.IsHealingSpell;
            }
        }

        public int IncomingHealsCount()
        {
            return StyxWoW.Memory.Read<int>(BaseAddress + (int)StyxWoW.Offsets.Misc.PredictedHealsCount);
        }

        public IncomingHeal[] IncomingHealsArray()
        {
            IntPtr incomingHealsPtr = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.PredictedHealsArray);
            return StyxWoW.Memory.ReadArray<IncomingHeal>(incomingHealsPtr, IncomingHealsCount());
        }

        public uint GetPredictedHealth(bool includeMyHeals = false)
        {
            IncomingHeal[] heals = IncomingHealsArray();
            return heals.Where(heal => includeMyHeals || heal.OwnerGuid != StyxWoW.Me.Guid)
                .Aggregate(CurrentHealth, (current, heal) => current + heal.HealAmount);
        }

        public float GetPredictedHealthPercent(bool includeMyHeals = false)
        {
            return GetPredictedHealth(includeMyHeals) * 100f / MaxHealth;
        }

        #endregion

        #region Auras

        private PerFrameCachedValue<Dictionary<string, WoWAura>> _aurasCache;

        public Dictionary<string, WoWAura> Auras
        {
            get
            {
                if (_aurasCache == null)
                {
                    _aurasCache = new PerFrameCachedValue<Dictionary<string, WoWAura>>(() => FilterAuras(aura => true));
                }

                return _aurasCache;
            }
        }

        public Dictionary<string, WoWAura> PassiveAuras
        {
            get { return FilterAuras(aura => aura.TimeLeft.TotalMilliseconds <= 0.0); }
        }

        public Dictionary<string, WoWAura> ActiveAuras
        {
            get { return FilterAuras(aura => aura.TimeLeft.TotalMilliseconds > 0.0); }
        }

        public Dictionary<string, WoWAura> Buffs
        {
            get { return FilterAuras(aura => !aura.IsHarmful && aura.TimeLeft.TotalMilliseconds > 0.0); }
        }

        public Dictionary<string, WoWAura> Debuffs
        {
            get { return FilterAuras(aura => aura.IsHarmful && aura.TimeLeft.TotalMilliseconds > 0.0); }
        }

        private Dictionary<string, WoWAura> FilterAuras(Func<WoWAura, bool> predicate)
        {
            WoWAuraCollection allAuras = GetAllAuras();
            Dictionary<string, WoWAura> result = new Dictionary<string, WoWAura>(allAuras.Count);
            foreach (WoWAura aura in allAuras)
            {
                if (predicate(aura) && !result.ContainsKey(aura.Name))
                {
                    result.Add(aura.Name, aura);
                }
            }

            return result;
        }

        public WoWAura GetAuraByName(string name)
        {
            foreach (NativeAura nativeAura in GetNativeAuras())
            {
                if (nativeAura.AuraId != 0U && WoWSpell.GetName((int)nativeAura.AuraId) == name && WoWSpell.FromId((int)nativeAura.AuraId) != null)
                {
                    return new WoWAura(nativeAura);
                }
            }

            return null;
        }

        public WoWAura GetAuraById(int id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (NativeAura nativeAura in GetNativeAuras())
            {
                if (nativeAura.AuraId == (uint)id && WoWSpell.FromId((int)nativeAura.AuraId) != null)
                {
                    return new WoWAura(nativeAura);
                }
            }

            return null;
        }

        public bool HasAura(string name)
        {
            return GetAuraByName(name) != null;
        }

        public bool HasAura(int id)
        {
            return GetAuraById(id) != null;
        }

        private PerFrameCachedValue<WoWAuraCollection> _allAurasCache;

        public WoWAuraCollection GetAllAuras()
        {
            if (_allAurasCache == null)
            {
                _allAurasCache = new PerFrameCachedValue<WoWAuraCollection>(() =>
                {
                    NativeAura[] nativeAuras = GetNativeAuras();
                    WoWAuraCollection collection = new WoWAuraCollection(nativeAuras.Length);
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        collection.AddRange(nativeAuras
                            .Where(nativeAura => nativeAura.AuraId != 0 && WoWSpell.FromId((int)nativeAura.AuraId) != null)
                            .Select(nativeAura => new WoWAura(nativeAura)));
                    }

                    return collection;
                });
            }

            return _allAurasCache;
        }

        private PerFrameCachedValue<NativeAura[]> _nativeAurasCache;

        private NativeAura[] GetNativeAuras()
        {
            if (_nativeAurasCache == null)
            {
                _nativeAurasCache = new PerFrameCachedValue<NativeAura[]>(() =>
                {
                    IntPtr table = BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitAuraTable;
                    int count = StyxWoW.Memory.Read<int>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitAuraCount);
                    if (count == -1)
                    {
                        count = StyxWoW.Memory.Read<int>(table);
                        table = StyxWoW.Memory.Read<IntPtr>(table + 4);
                    }

                    return StyxWoW.Memory.ReadArray<NativeAura>(table, count);
                });
            }

            return _nativeAurasCache;
        }

        #endregion

        #region Movement Data

        public float BoundingHeight
        {
            get { return StyxWoW.Memory.Read<float>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitBoundingHeight); }
        }

        public override WoWPoint Location
        {
            get
            {
                EnsureValid("Location");
                if (IsOnTransport)
                {
                    return base.Location;
                }

                return RelativeLocation;
            }
        }

        public override WoWPoint RelativeLocation
        {
            get
            {
                EnsureValid("RelativeLocation");
                return MovementInfo.Position;
            }
        }

        public float RenderFacing
        {
            get
            {
                EnsureValid("RenderFacing");
                return StyxWoW.Memory.Read<float>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitRenderFacing);
            }
        }

        public override float Rotation
        {
            get { return RenderFacing; }
        }

        public override float InteractRange
        {
            get { return CombatReach + 4f; }
        }

        public override bool WithinInteractRange
        {
            get
            {
                if (Entry != 13158U && Entry != 49044U)
                {
                    return base.WithinInteractRange;
                }

                return DistanceSqr < (double)InteractRangeSqr;
            }
        }

        public bool IsWithinMeleeRange
        {
            get
            {
                EnsureValid("IsWithinMeleeRange");
                return StyxWoW.Me.IsWithinMeleeRangeOf(this);
            }
        }

        public ulong TransportGuid
        {
            get
            {
                EnsureValid("TransportGuid");
                return MovementInfo.TransportGuid;
            }
        }

        public WoWObject Transport
        {
            get { return ObjectManager.GetObjectByGuid<WoWObject>(TransportGuid); }
        }

        public bool IsOnTransport
        {
            get { return TransportGuid != 0UL; }
        }

        public bool IsPlayerBehind
        {
            get { return WoWMathHelper.IsBehind(StyxWoW.Me.Location, Location, Rotation, 3.14159274f); }
        }

        public bool BehindTarget
        {
            get { return Behind(CurrentTarget); }
        }

        public override bool MeIsSafelyBehind
        {
            get { return WoWMathHelper.IsSafelyBehind(StyxWoW.Me.Location, Location, Rotation); }
        }

        public override bool MeIsBehind
        {
            get { return WoWMathHelper.IsBehind(StyxWoW.Me.Location, Location, Rotation, 3.14159274f); }
        }

        public bool IsMoving
        {
            get { return MovementInfo.IsMoving; }
        }

        public bool IsFalling
        {
            get { return MovementInfo.FallingFar; }
        }

        public bool IsSwimming
        {
            get { return MovementInfo.IsSwimming; }
        }

        public bool IsFlying
        {
            get { return MovementInfo.IsFlying; }
        }

        private PerFrameCachedValue<WoWPoint> _traceLinePosCache;

        public WoWPoint GetTraceLinePos()
        {
            if (_traceLinePosCache == null)
            {
                _traceLinePosCache = new PerFrameCachedValue<WoWPoint>(() =>
                {
                    Guard.AgainstInvalidExecutor();
                    Executor executor = StyxWoW.Memory.Executor;
                    lock (executor.AssemblyLock)
                    {
                        using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(12))
                        {
                            executor.Clear();
                            executor.AddLine("push {0}", allocatedMemory.Address);
                            executor.AddLine("push {0}", (uint)(int)BaseAddress);
                            executor.AddLine("call GetTracelinePosition");
                            executor.AddLine("retn");
                            EmitGetTracelinePositionRoutine(executor);
                            executor.Execute();

                            using (StyxWoW.Memory.TemporaryCacheState(false))
                            {
                                // 5.4.8 reads the result through StyxWoW.Memory.RemoteAsm.Memory (see rule 18).
                                return StyxWoW.Memory.Read<WoWPoint>(allocatedMemory.Address);
                            }
                        }
                    }
                });
            }

            return _traceLinePosCache;
        }

        private WoWMovementInfo _movementInfo;

        public WoWMovementInfo MovementInfo
        {
            get
            {
                EnsureValid("MovementInfo");
                IntPtr baseAddress = BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitMovementDataDirect;
                if (_movementInfo == null || _movementInfo.BaseAddress != baseAddress)
                {
                    _movementInfo = new WoWMovementInfo(baseAddress);
                }

                return _movementInfo;
            }
        }

        public override string SafeName
        {
            get
            {
                WoWUnit ownedByRoot = OwnedByRoot;
                if (ownedByRoot != null && ownedByRoot.IsPlayer && (!ownedByRoot.IsMe || CharmedByUnitGuid != 0UL || SummonedByUnitGuid != 0UL || IsTotem))
                {
                    return string.Format("{0}'s pet", ownedByRoot.SafeName);
                }

                return base.SafeName;
            }
        }

        #endregion

        #region Unit Wrappers

        public WoWObject ChannelObject
        {
            get { return ObjectManager.GetObjectByGuid(ChannelObjectGuid); }
        }

        public ulong ChannelObjectGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.ChannelObject); }
        }

        public WoWUnit CharmedUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CharmedUnitGuid); }
        }

        public ulong CharmedUnitGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.Charm); }
        }

        public WoWUnit SummonedUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(SummonedUnitGuid); }
        }

        public ulong SummonedUnitGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.Summon); }
        }

        public WoWUnit VanityPet
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(VanityPetGuid); }
        }

        public ulong VanityPetGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.Critter); }
        }

        public WoWUnit CharmedByUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CharmedByUnitGuid); }
        }

        public ulong CharmedByUnitGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.CharmedBy); }
        }

        public WoWUnit SummonedByUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(SummonedByUnitGuid); }
        }

        public ulong SummonedByUnitGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.SummonedBy); }
        }

        public WoWUnit CreatedByUnit
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CreatedByUnitGuid); }
        }

        public ulong CreatedByUnitGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.CreatedBy); }
        }

        public WoWUnit CurrentTarget
        {
            get { return ObjectManager.GetObjectByGuid<WoWUnit>(CurrentTargetGuid); }
        }

        public virtual ulong CurrentTargetGuid
        {
            get { return GetStorageField<ulong>(WoWUnitFields.Target); }
        }

        public virtual WoWUnit OwnedByUnit
        {
            get
            {
                WoWUnit owner = CharmedByUnit;
                if (owner == null)
                {
                    owner = SummonedByUnit ?? CreatedByUnit;
                }

                return owner;
            }
        }

        public WoWUnit OwnedByRoot
        {
            get
            {
                WoWUnit root = null;
                WoWUnit owner = OwnedByUnit;
                while (owner != null)
                {
                    root = owner;
                    owner = owner.OwnedByUnit;
                }

                return root;
            }
        }

        public WoWPlayer ControllingPlayer
        {
            get
            {
                WoWUnit unit = this;
                ulong guid = CharmedByUnitGuid;
                if (guid == 0UL)
                {
                    guid = CreatedByUnitGuid;
                }

                if (guid != 0UL)
                {
                    unit = ObjectManager.GetObjectByGuid<WoWUnit>(guid);
                }

                if (unit == null)
                {
                    return null;
                }

                if (!(unit is WoWPlayer))
                {
                    guid = unit.CharmedByUnitGuid;
                    if (guid == 0UL)
                    {
                        guid = unit.CreatedByUnitGuid;
                    }

                    unit = ObjectManager.GetObjectByGuid<WoWUnit>(guid);
                }

                return unit as WoWPlayer;
            }
        }

        public bool GotTarget
        {
            get { return CurrentTarget != null; }
        }

        private PerFrameCachedValue<WoWUnitClassificationType> _classificationCache;

        public WoWUnitClassificationType Classification
        {
            get
            {
                if (_classificationCache == null)
                {
                    _classificationCache = new PerFrameCachedValue<WoWUnitClassificationType>(() =>
                    {
                        EnsureValid("Classification");
                        return (WoWUnitClassificationType)StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.CGUnit_C__GetCreatureClassification, CallingConvention.ThisCall, BaseAddress);
                    });
                }

                return _classificationCache;
            }
        }

        private PerFrameCachedValue<bool> _isBossCache;

        public bool IsBoss
        {
            get
            {
                if (_isBossCache == null)
                {
                    _isBossCache = new PerFrameCachedValue<bool>(() =>
                    {
                        EnsureValid("IsBoss");
                        return Classification == WoWUnitClassificationType.WorldBoss || StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.CGUnit_C__IsBoss, CallingConvention.ThisCall, BaseAddress) != 0;
                    });
                }

                return _isBossCache;
            }
        }

        #endregion

        #region Power

        public uint CurrentPower
        {
            get { return GetCurrentPower(PowerType); }
        }

        public uint MaxPower
        {
            get { return GetMaxPower(PowerType); }
        }

        public float PowerPercent
        {
            get { return GetPowerPercent(PowerType); }
        }

        public PowerInfo CurrentPowerInfo
        {
            get { return GetPowerInfo(PowerType); }
        }

        public bool IsDead
        {
            get
            {
                if (CurrentHealth == 4294967295U)
                {
                    return true;
                }

                if (this is WoWPlayer)
                {
                    return CurrentHealth == 0U;
                }

                return CurrentHealth == 0U || ((DynamicFlags >> 6) & 1) != 0;
            }
        }

        public uint CurrentHealth
        {
            get { return GetCurrentPower(WoWPowerType.Health); }
        }

        public uint CurrentMana
        {
            get { return GetCurrentPower(WoWPowerType.Mana); }
        }

        public uint CurrentRage
        {
            get { return GetCurrentPower(WoWPowerType.Rage); }
        }

        public uint CurrentEnergy
        {
            get { return GetCurrentPower(WoWPowerType.Energy); }
        }

        public uint CurrentFocus
        {
            get { return GetCurrentPower(WoWPowerType.Focus); }
        }

        public uint CurrentHappiness
        {
            get { return Convert.ToUInt32(GetCurrentPower(WoWPowerType.Happiness) / 10000.0, CultureInfo.InvariantCulture); }
        }

        public uint CurrentRunicPower
        {
            get { return GetCurrentPower(WoWPowerType.RunicPower); }
        }

        public uint CurrentSoulShards
        {
            get { return GetCurrentPower(WoWPowerType.SoulShards); }
        }

        public int CurrentEclipse
        {
            get { return BitConverter.ToInt32(BitConverter.GetBytes(GetCurrentPower(WoWPowerType.Eclipse)), 0); }
        }

        public uint CurrentHolyPower
        {
            get { return GetCurrentPower(WoWPowerType.HolyPower); }
        }

        public uint MaxHealth
        {
            get { return GetMaxPower(WoWPowerType.Health); }
        }

        public uint MaxMana
        {
            get { return GetMaxPower(WoWPowerType.Mana); }
        }

        public uint MaxRage
        {
            get { return GetMaxPower(WoWPowerType.Rage); }
        }

        public uint MaxEnergy
        {
            get { return GetMaxPower(WoWPowerType.Energy); }
        }

        public uint MaxFocus
        {
            get { return GetMaxPower(WoWPowerType.Focus); }
        }

        public uint MaxHappiness
        {
            get { return GetMaxPower(WoWPowerType.Happiness); }
        }

        public uint MaxRunicPower
        {
            get { return GetMaxPower(WoWPowerType.RunicPower); }
        }

        public uint MaxSoulShards
        {
            get { return GetMaxPower(WoWPowerType.SoulShards); }
        }

        public uint MaxEclipse
        {
            get { return GetMaxPower(WoWPowerType.Eclipse); }
        }

        public uint MaxHolyPower
        {
            get { return GetMaxPower(WoWPowerType.HolyPower); }
        }

        public double EclipsePercent
        {
            get { return GetPowerInfo(WoWPowerType.Eclipse).Percent; }
        }

        public double EnergyPercent
        {
            get { return GetPowerInfo(WoWPowerType.Energy).Percent; }
        }

        public double FocusPercent
        {
            get { return GetPowerInfo(WoWPowerType.Focus).Percent; }
        }

        public double HappinessPercent
        {
            get { return GetPowerInfo(WoWPowerType.Happiness).Percent; }
        }

        public double HealthPercent
        {
            get { return GetPowerInfo(WoWPowerType.Health).Percent; }
        }

        public double HolyPowerPercent
        {
            get { return GetPowerInfo(WoWPowerType.HolyPower).Percent; }
        }

        public double ManaPercent
        {
            get { return GetPowerInfo(WoWPowerType.Mana).Percent; }
        }

        public double RagePercent
        {
            get { return GetPowerInfo(WoWPowerType.Rage).Percent; }
        }

        public double RunesPercent
        {
            get { return GetPowerInfo(WoWPowerType.Runes).Percent; }
        }

        public double RunicPowerPercent
        {
            get { return GetPowerInfo(WoWPowerType.RunicPower).Percent; }
        }

        public double SoulShardsPercent
        {
            get { return GetPowerInfo(WoWPowerType.SoulShards).Percent; }
        }

        public PowerInfo ManaInfo
        {
            get { return GetPowerInfo(WoWPowerType.Mana); }
        }

        public PowerInfo RageInfo
        {
            get { return GetPowerInfo(WoWPowerType.Rage); }
        }

        public PowerInfo EnergyInfo
        {
            get { return GetPowerInfo(WoWPowerType.Energy); }
        }

        public PowerInfo FocusInfo
        {
            get { return GetPowerInfo(WoWPowerType.Focus); }
        }

        public PowerInfo HappinessInfo
        {
            get { return GetPowerInfo(WoWPowerType.Happiness); }
        }

        public PowerInfo RunesPowerInfo
        {
            get { return GetPowerInfo(WoWPowerType.Runes); }
        }

        public PowerInfo RunicPowerInfo
        {
            get { return GetPowerInfo(WoWPowerType.RunicPower); }
        }

        public PowerInfo SoulShardsInfo
        {
            get { return GetPowerInfo(WoWPowerType.SoulShards); }
        }

        public PowerInfo EclipseInfo
        {
            get { return GetPowerInfo(WoWPowerType.Eclipse); }
        }

        public PowerInfo HolyPowerInfo
        {
            get { return GetPowerInfo(WoWPowerType.HolyPower); }
        }

        public PowerInfo ChiInfo
        {
            get { return GetPowerInfo(WoWPowerType.LightForce); }
        }

        public uint CurrentChi
        {
            get { return GetCurrentPower(WoWPowerType.LightForce); }
        }

        public uint MaxChi
        {
            get { return GetMaxPower(WoWPowerType.LightForce); }
        }

        public uint GetCurrentPower(WoWPowerType type)
        {
            if (type == WoWPowerType.Health)
            {
                return GetStorageField<uint>(WoWUnitFields.Health);
            }

            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0U;
            }

            uint value = GetStorageField<uint>((uint)WoWUnitFields.Power + (uint)(index * 4));
            if (type == WoWPowerType.Rage || type == WoWPowerType.RunicPower)
            {
                value /= 10U;
            }
            else if (type == WoWPowerType.SoulShards)
            {
                value /= 100U;
            }

            return value;
        }

        public uint GetMaxPower(WoWPowerType type)
        {
            if (type == WoWPowerType.Health)
            {
                return GetStorageField<uint>(WoWUnitFields.MaxHealth);
            }

            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0U;
            }

            uint value = GetStorageField<uint>((uint)WoWUnitFields.MaxPower + (uint)(index * 4));
            if (type == WoWPowerType.Rage || type == WoWPowerType.RunicPower)
            {
                value /= 10U;
            }
            else if (type == WoWPowerType.SoulShards)
            {
                value /= 100U;
            }

            return value;
        }

        private bool GetPowerIndex(WoWPowerType type, out int index)
        {
            if (IsPlayer)
            {
                IntPtr addr = StyxWoW.Offsets.Statics.UnitPowerIndices;
                uint arrayIndex = (uint)Class + (uint)type + 16 * (uint)Class;
                index = StyxWoW.Memory.Read<int>(addr + (int)(arrayIndex * 4U));
                if (index == -1)
                {
                    return false;
                }
            }
            else
            {
                if (type != PowerType)
                {
                    index = 2 * ((type == WoWPowerType.Alternate) ? 1 : 0) - 1;
                    return true;
                }

                index = 0;
            }

            return true;
        }

        public float GetPowerPercent(WoWPowerType type)
        {
            uint maxPower = GetMaxPower(type);
            if (maxPower == 0U)
            {
                return 0f;
            }

            return GetCurrentPower(type) * 100f / maxPower;
        }

        public float GetPowerRegenFlat(WoWPowerType type)
        {
            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0f;
            }

            return GetStorageField<uint>((uint)WoWUnitFields.PowerRegenFlatModifier + (uint)index);
        }

        public float GetPowerRegenInterrupted(WoWPowerType type)
        {
            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0f;
            }

            return GetStorageField<uint>((uint)WoWUnitFields.PowerRegenInterruptedFlatModifier + (uint)index);
        }

        public PowerInfo GetPowerInfo(WoWPowerType type)
        {
            return new PowerInfo(type, GetCurrentPower(type), GetMaxPower(type), GetPowerRegenFlat(type), GetPowerRegenInterrupted(type), GetPowerCostModifier(type), GetPowerCostMultiplier(type));
        }

        public uint GetPowerCostModifier(WoWPowerType type)
        {
            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0U;
            }

            return GetStorageField<uint>((uint)WoWUnitFields.PowerCostModifier + (uint)index);
        }

        public float GetPowerCostMultiplier(WoWPowerType type)
        {
            int index;
            if (!GetPowerIndex(type, out index))
            {
                return 0f;
            }

            return GetStorageField<float>((uint)WoWUnitFields.PowerCostMultiplier + (uint)index);
        }

        #endregion

        #region Item

        public int MaxItemLevel
        {
            get { return GetStorageField<int>(WoWUnitFields.MaxItemLevel); }
        }

        public uint[] VirtualItemSlotIds
        {
            get
            {
                uint[] result = new uint[3];
                for (uint i = 0U; i < 3U; i += 1U)
                {
                    result[i] = GetStorageField<uint>((uint)WoWUnitFields.VirtualItemID + i);
                }

                return result;
            }
        }

        #endregion

        #region Stats

        public uint Strength
        {
            get { return GetStorageField<uint>(WoWUnitFields.Stats); }
        }

        public uint StrengthPositiveModifier
        {
            get { return GetStorageField<uint>(WoWUnitFields.StatPosBuff); }
        }

        public uint StrengthNegativeModifier
        {
            get { return GetStorageField<uint>(WoWUnitFields.StatNegBuff); }
        }

        public uint Agility
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.Stats + 4); }
        }

        public uint AgilityPositiveModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatPosBuff + 4); }
        }

        public uint AgilityNegativeModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatNegBuff + 4); }
        }

        public uint Stamina
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.Stats + 2 * 4); }
        }

        public uint StaminaPositiveModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatPosBuff + 2 * 4); }
        }

        public uint StaminaNegativeModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatNegBuff + 2 * 4); }
        }

        public uint Intellect
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.Stats + 3 * 4); }
        }

        public uint IntellectPositiveModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatPosBuff + 3 * 4); }
        }

        public uint IntellectNegativeModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatNegBuff + 3 * 4); }
        }

        public uint Spirit
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.Stats + 4 * 4); }
        }

        public uint SpiritPositiveModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatPosBuff + 4 * 4); }
        }

        public uint SpiritNegativeModifier
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.StatNegBuff + 4 * 4); }
        }

        public uint BaseMana
        {
            get { return GetStorageField<uint>(WoWUnitFields.BaseMana); }
        }

        public uint BaseHealth
        {
            get { return GetStorageField<uint>(WoWUnitFields.BaseHealth); }
        }

        #region Melee/Ranged

        public uint AttackPower
        {
            get { return GetStorageField<uint>(WoWUnitFields.AttackPower); }
        }

        public float AttackPowerMultiplier
        {
            get { return GetStorageField<float>(WoWUnitFields.AttackPowerMultiplier); }
        }

        public uint BaseAttackTime
        {
            get { return GetStorageField<uint>(WoWUnitFields.AttackRoundBaseTime); }
        }

        public uint BaseOffHandAttackTime
        {
            get { return GetStorageField<uint>((uint)WoWUnitFields.AttackRoundBaseTime + 4); }
        }

        public uint BaseRangedAttackTime
        {
            get { return GetStorageField<uint>(WoWUnitFields.RangedAttackRoundBaseTime); }
        }

        public uint RangedAttackPower
        {
            get { return GetStorageField<uint>(WoWUnitFields.RangedAttackPower); }
        }

        public float RangedAttackPowerMultiplier
        {
            get { return GetStorageField<float>(WoWUnitFields.RangedAttackPowerMultiplier); }
        }

        public float MinRangedDamage
        {
            get { return GetStorageField<float>(WoWUnitFields.MinRangedDamage); }
        }

        public float MaxRangedDamage
        {
            get { return GetStorageField<float>(WoWUnitFields.MinRangedDamage); }
        }

        public float CombatReach
        {
            get { return GetStorageField<float>(WoWUnitFields.CombatReach); }
        }

        public uint MinDamage
        {
            get { return GetStorageField<uint>(WoWUnitFields.MinDamage); }
        }

        public float MaxDamage
        {
            get { return GetStorageField<float>(WoWUnitFields.MaxDamage); }
        }

        public float MinOffHandDamage
        {
            get { return GetStorageField<float>(WoWUnitFields.MinOffHandDamage); }
        }

        public float MaxOffHandDamage
        {
            get { return GetStorageField<float>(WoWUnitFields.MaxOffHandDamage); }
        }

        #endregion

        #region Resists

        private uint GetResist(uint index)
        {
            return GetStorageField<uint>((uint)WoWUnitFields.Resistances + index * 4U);
        }

        public uint Armor
        {
            get { return GetResist(0U); }
        }

        public uint ResistHoly
        {
            get { return GetResist(1U); }
        }

        public uint ResistFire
        {
            get { return GetResist(2U); }
        }

        public uint ResistNature
        {
            get { return GetResist(3U); }
        }

        public uint ResistFrost
        {
            get { return GetResist(4U); }
        }

        public uint ResistShadow
        {
            get { return GetResist(5U); }
        }

        public uint ResistArcane
        {
            get { return GetResist(6U); }
        }

        #endregion

        public float MaxHealthModifier
        {
            get { return GetStorageField<float>(WoWUnitFields.MaxHealthModifier); }
        }

        #endregion

        #region Pets

        public uint PetNumber
        {
            get { return GetStorageField<uint>(WoWUnitFields.PetNumber); }
        }

        public uint PetNameTimestamp
        {
            get { return GetStorageField<uint>(WoWUnitFields.PetNameTimestamp); }
        }

        public uint PetExperience
        {
            get { return GetStorageField<uint>(WoWUnitFields.PetExperience); }
        }

        public uint PetNextLevelExperience
        {
            get { return GetStorageField<uint>(WoWUnitFields.PetNextLevelExperience); }
        }

        public virtual WoWUnit Pet
        {
            get
            {
                ulong guid = CharmedUnitGuid;
                if (guid == 0UL)
                {
                    guid = SummonedUnitGuid;
                }

                if (guid == 0UL)
                {
                    return null;
                }

                return ObjectManager.GetObjectByGuid<WoWUnit>(guid);
            }
        }

        public bool IsPet
        {
            get { return SummonedByUnitGuid != 0UL || CharmedByUnitGuid != 0UL; }
        }

        public bool IsTargetingMeOrPet
        {
            get
            {
                WoWUnit pet = StyxWoW.Me.Pet;
                ulong currentTargetGuid = CurrentTargetGuid;
                return (pet != null && pet.Guid == currentTargetGuid) || StyxWoW.Me.Guid == currentTargetGuid || (OwnedByRoot != null && Guid == CurrentTargetGuid);
            }
        }

        public bool IsTargetingAnyMinion
        {
            get { return GotTarget && StyxWoW.Me.Minions.Any(minion => minion.Guid == CurrentTargetGuid); }
        }

        public bool GotAlivePet
        {
            get
            {
                WoWUnit pet = Pet;
                return pet != null && pet.IsAlive;
            }
        }

        public bool IsTargetingPet
        {
            get
            {
                WoWUnit pet = Pet;
                return pet != null && CurrentTargetGuid == pet.Guid;
            }
        }

        #endregion

        #region Descriptors

        public int Level
        {
            get { return GetStorageField<int>(WoWUnitFields.Level); }
        }

        public uint FactionId
        {
            get { return GetStorageField<uint>(WoWUnitFields.FactionTemplate); }
        }

        public Faction Faction
        {
            get { return Styx.WoWInternals.DBC.Faction.FromId(FactionId); }
        }

        public FactionTemplate FactionTemplate
        {
            get
            {
                if (FactionId == 0U)
                {
                    return null;
                }

                return Styx.WoWInternals.DBC.FactionTemplate.FromId(FactionId);
            }
        }

        public float BoundingRadius
        {
            get { return GetStorageField<float>(WoWUnitFields.BoundingRadius); }
        }

        public uint DisplayId
        {
            get { return GetStorageField<uint>(WoWUnitFields.DisplayID); }
        }

        public uint NativeDisplayId
        {
            get { return GetStorageField<uint>(WoWUnitFields.NativeDisplayID); }
        }

        public uint MountDisplayId
        {
            get { return GetStorageField<uint>(WoWUnitFields.MountDisplayID); }
        }

        public virtual bool Mounted
        {
            get { return MountDisplayId > 0U; }
        }

        public float HoverHeight
        {
            get { return GetStorageField<float>(WoWUnitFields.HoverHeight); }
        }

        public float CastSpeedModifier
        {
            get { return GetStorageField<float>(WoWUnitFields.ModCastingSpeed); }
        }

        public float SpellHasteModifier
        {
            get { return GetStorageField<float>(WoWUnitFields.ModSpellHaste); }
        }

        public float HasteModifier
        {
            get { return GetStorageField<float>(WoWUnitFields.ModHaste); }
        }

        public float HasteRegenModifier
        {
            get { return GetStorageField<float>(WoWUnitFields.ModHasteRegen); }
        }

        public uint CreatedBySpellId
        {
            get { return GetStorageField<uint>(WoWUnitFields.CreatedBySpell); }
        }

        public uint Flags
        {
            get { return GetStorageField<uint>(WoWUnitFields.Flags); }
        }

        public uint Flags2
        {
            get { return GetStorageField<uint>(WoWUnitFields.Flags2); }
        }

        public uint NpcFlags
        {
            get { return GetStorageField<uint>(WoWUnitFields.NpcFlags); }
        }

        public uint NpcFlags2
        {
            get { return GetStorageField<uint>(352U); } // NpcFlags + 4, no named WoWUnitFields entry
        }

        public uint AuraState
        {
            get { return GetStorageField<uint>(WoWUnitFields.AuraState); }
        }

        private byte[] DisplayPower
        {
            get { return BitConverter.GetBytes(GetStorageField<uint>(WoWUnitFields.DisplayPower)); }
        }

        private byte[] Sex
        {
            get { return BitConverter.GetBytes(GetStorageField<uint>(WoWUnitFields.Sex)); }
        }

        public WoWClass Class
        {
            get { return (WoWClass)Sex[1]; }
        }

        public WoWRace Race
        {
            get
            {
                if (Sex[0] != 26 && Sex[0] != 25)
                {
                    return (WoWRace)Sex[0];
                }

                return WoWRace.Pandaren;
            }
        }

        public WoWPowerType PowerType
        {
            get
            {
                uint overridePowerId = GetStorageField<uint>(WoWUnitFields.OverrideDisplayPowerID);
                if (overridePowerId == 0U)
                {
                    return (WoWPowerType)DisplayPower[0];
                }

                return (WoWPowerType)overridePowerId;
            }
        }

        public WoWGender Gender
        {
            get { return (WoWGender)Sex[3]; }
        }

        private byte[] Bytes2
        {
            get { return BitConverter.GetBytes(GetStorageField<uint>(WoWUnitFields.ShapeshiftForm)); }
        }

        public ShapeshiftForm Shapeshift
        {
            get { return (ShapeshiftForm)Bytes2[3]; }
        }

        #endregion

        #region Flags

        public bool Combat
        {
            get { return HasFlag(UnitFlags.Combat); }
        }

        public bool Skinnable
        {
            get { return HasFlag(UnitFlags.Skinnable); }
        }

        public bool Dazed
        {
            get { return HasFlag(UnitFlags.Dazed); }
        }

        public bool Disarmed
        {
            get { return HasFlag(UnitFlags.Disarmed); }
        }

        public bool Attackable
        {
            get { return !HasFlag(UnitFlags.NotAttackable); }
        }

        public bool PvpFlagged
        {
            get { return HasFlag(UnitFlags.PvPFlagged); }
        }

        public bool Fleeing
        {
            get { return HasFlag(UnitFlags.Fleeing); }
        }

        public bool Pacified
        {
            get { return HasFlag(UnitFlags.Pacified); }
        }

        public bool Stunned
        {
            get { return HasFlag(UnitFlags.Stunned); }
        }

        public bool Rooted
        {
            get { return HasFlag(UnitFlags.Influenced); }
        }

        public bool CanSelect
        {
            get { return !HasFlag(UnitFlags.NotSelectable); }
        }

        public bool Silenced
        {
            get { return HasFlag(UnitFlags.Silenced); }
        }

        public bool Possessed
        {
            get { return HasFlag(UnitFlags.Possessed); }
        }

        public bool Elite
        {
            get { return HasFlag(UnitFlags.PlusMob); }
        }

        public bool Looting
        {
            get { return HasFlag(UnitFlags.Looting); }
        }

        public bool PetInCombat
        {
            get { return HasFlag(UnitFlags.PetInCombat); }
        }

        public bool OnTaxi
        {
            get { return HasFlag(UnitFlags.TaxiFlight); }
        }

        public bool PlayerControlled
        {
            get { return HasFlag(UnitFlags.PlayerControlled); }
        }

        public bool IsTagged
        {
            get { return HasFlag(UnitDynamicFlags.TaggedByOther); }
        }

        public bool TaggedByOther
        {
            get { return IsTagged && !TaggedByMe; }
        }

        public bool TaggedByMe
        {
            get { return HasFlag(UnitDynamicFlags.TaggedByMe); }
        }

        public bool Lootable
        {
            get { return HasFlag(UnitDynamicFlags.Lootable); }
        }

        public bool Tracked
        {
            get { return HasFlag(UnitDynamicFlags.TrackUnit); }
        }

        public bool RafLinked
        {
            get { return HasFlag(UnitDynamicFlags.ReferAFriendLinked); }
        }

        public bool TappedByAllThreatLists
        {
            get { return HasFlag(UnitDynamicFlags.IsTappedByAllThreatLists); }
        }

        public bool CanGossip
        {
            get { return HasFlag(UnitNPCFlags.Gossip) || GetCursorOverride() == "speak"; }
        }

        public bool IsQuestGiver
        {
            get { return HasFlag(UnitNPCFlags.Questgiver); }
        }

        public bool IsTrainer
        {
            get { return HasFlag(UnitNPCFlags.Trainer); }
        }

        public bool IsClassTrainer
        {
            get { return HasFlag(UnitNPCFlags.ClassTrainer); }
        }

        public bool IsProfessionTrainer
        {
            get { return HasFlag(UnitNPCFlags.ProfessionTrainer); }
        }

        public bool IsAnyTrainer
        {
            get { return HasFlag(UnitNPCFlags.AnyTrainer); }
        }

        public bool IsAnyVendor
        {
            get { return HasFlag(UnitNPCFlags.AnyVendor); }
        }

        public bool IsVendor
        {
            get { return HasFlag(UnitNPCFlags.Vendor); }
        }

        public bool IsAmmoVendor
        {
            get { return HasFlag(UnitNPCFlags.AmmoVendor); }
        }

        public bool IsFoodVendor
        {
            get { return HasFlag(UnitNPCFlags.FoodVendor); }
        }

        public bool IsPoisonVendor
        {
            get { return HasFlag(UnitNPCFlags.PoisonVendor); }
        }

        public bool IsPetitioner
        {
            get { return HasFlag(UnitNPCFlags.Petitioner); }
        }

        public bool IsReagentVendor
        {
            get { return HasFlag(UnitNPCFlags.ReagentVendor); }
        }

        public bool IsRepairMerchant
        {
            get { return HasFlag(UnitNPCFlags.Repair); }
        }

        public bool IsFlightMaster
        {
            get { return HasFlag(UnitNPCFlags.Flightmaster); }
        }

        public bool IsSpiritHealer
        {
            get { return HasFlag(UnitNPCFlags.Spirithealer); }
        }

        public bool IsSpiritGuide
        {
            get { return HasFlag(UnitNPCFlags.Spiritguide); }
        }

        public bool IsInnkeeper
        {
            get { return HasFlag(UnitNPCFlags.Innkeeper); }
        }

        public bool IsBanker
        {
            get { return HasFlag(UnitNPCFlags.Banker); }
        }

        public bool IsTabardDesigner
        {
            get { return HasFlag(UnitNPCFlags.TarbardDesigner); }
        }

        public bool IsBattleMaster
        {
            get { return HasFlag(UnitNPCFlags.Battlemaster); }
        }

        public bool IsAuctioneer
        {
            get { return HasFlag(UnitNPCFlags.Auctioneer); }
        }

        public bool IsStableMaster
        {
            get { return HasFlag(UnitNPCFlags.Stablemaster); }
        }

        [Obsolete("Always returns false. If you need this, check IDs.")]
        public bool IsGuard
        {
            get { return false; }
        }

        public bool IsGuildBanker
        {
            get { return HasFlag(UnitNPCFlags.GuildBanker); }
        }

        public bool IsMailCourier
        {
            get { return HasFlag(UnitNPCFlags.MailCourier); }
        }

        public bool IsReforger
        {
            get { return HasFlag(UnitNPCFlags.Reforger); }
        }

        public bool IsTransmogrifier
        {
            get { return HasFlag(UnitNPCFlags.Transmogrifier); }
        }

        public bool IsVaultKeeper
        {
            get { return HasFlag(UnitNPCFlags.VaultKeeper); }
        }

        public bool IsPetBattleCritter
        {
            get { return HasFlag(UnitNPCFlags.PetBattle); }
        }

        public bool IsBlackMarketAuctioneer
        {
            get { return HasFlag(UnitNPCFlags.BlackMarketAuctioneer); }
        }

        internal bool HasFlag(UnitNPCFlags flags)
        {
            return (NpcFlags & (uint)flags) != 0U;
        }

        internal bool HasFlag(UnitDynamicFlags flags)
        {
            return (DynamicFlags & (int)flags) != 0;
        }

        internal bool HasFlag(UnitFlags flags)
        {
            return (Flags & (uint)flags) != 0U;
        }

        internal bool HasFlag(UnitFlags2 flags)
        {
            return (Flags2 & (uint)flags) != 0U;
        }

        public string GetCursorOverride()
        {
            IntPtr cursorOverride = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitCursorOverride);
            if (cursorOverride == IntPtr.Zero)
            {
                return null;
            }

            IntPtr cursorOverrideStr = StyxWoW.Memory.Read<IntPtr>(cursorOverride + 16);
            if (cursorOverride == IntPtr.Zero)
            {
                return null;
            }

            string text = StyxWoW.Memory.ReadStringUTF8(cursorOverrideStr);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return text.ToLowerInvariant();
        }

        #endregion

        #region CreatureCache

        public CreatureFamily CreatureFamilyInfo
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                if (!GetCachedInfo(out entry))
                {
                    return null;
                }

                return CreatureFamily.FromId(entry.FamilyID);
            }
        }

        public string SubName
        {
            get
            {
                try
                {
                    Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                    if (!GetCachedInfo(out entry) || entry.SubName.PtrStr == IntPtr.Zero)
                    {
                        return null;
                    }

                    return StyxWoW.Memory.ReadString(entry.SubName.PtrStr, Encoding.UTF8);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public WoWCreatureSkinType SkinType
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                if (!GetCachedInfo(out entry))
                {
                    return WoWCreatureSkinType.None;
                }

                if ((entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.HerbSkin) != 0)
                {
                    return WoWCreatureSkinType.Herb;
                }

                if ((entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.MineSkin) != 0)
                {
                    return WoWCreatureSkinType.Rock;
                }

                if ((entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.Salvageable) != 0)
                {
                    return WoWCreatureSkinType.Bolts;
                }

                return WoWCreatureSkinType.Leather;
            }
        }

        public WoWCreatureType CreatureType
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                if (GetCachedInfo(out entry))
                {
                    return (WoWCreatureType)entry.TypeID;
                }

                return WoWCreatureType.NotSpecified;
            }
        }

        public bool IsBeast
        {
            get { return CreatureType == WoWCreatureType.Beast; }
        }

        public bool IsCritter
        {
            get { return CreatureType == WoWCreatureType.Critter; }
        }

        public bool IsDemon
        {
            get { return CreatureType == WoWCreatureType.Demon; }
        }

        public bool IsDragon
        {
            get { return CreatureType == WoWCreatureType.Dragon; }
        }

        public bool IsElemental
        {
            get { return CreatureType == WoWCreatureType.Elemental; }
        }

        public bool IsGasCloud
        {
            get { return CreatureType == WoWCreatureType.GasCloud; }
        }

        public bool IsGiant
        {
            get { return CreatureType == WoWCreatureType.Giant; }
        }

        public bool IsHumanoid
        {
            get { return CreatureType == WoWCreatureType.Humanoid; }
        }

        public bool IsMechanical
        {
            get { return CreatureType == WoWCreatureType.Mechanical; }
        }

        public bool IsNonCombatPet
        {
            get { return CreatureType == WoWCreatureType.NonCombatPet; }
        }

        public bool IsTotem
        {
            get { return CreatureType == WoWCreatureType.Totem; }
        }

        public bool IsUndead
        {
            get { return CreatureType == WoWCreatureType.Undead; }
        }

        public WoWUnitClassificationType CreatureRank
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                if (GetCachedInfo(out entry))
                {
                    return (WoWUnitClassificationType)entry.Rank;
                }

                return WoWUnitClassificationType.NotSpecified;
            }
        }

        public bool IsTameable
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                return GetCachedInfo(out entry) && (entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.Tameable) != 0;
            }
        }

        public bool IsGhostVisible
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                return GetCachedInfo(out entry) && (entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.GhostVisible) != 0;
            }
        }

        public bool IsExotic
        {
            get
            {
                Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry entry;
                return GetCachedInfo(out entry) && (entry.TypeFlags[0] & WoWCache.CreatureCacheTypeFlags.Exotic) != 0;
            }
        }

        private static readonly Dictionary<ulong, Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry> _creatureCacheBuffer = new Dictionary<ulong, Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry>();

        public bool GetCachedInfo(out Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry info)
        {
            ulong guid = Guid;
            if (_creatureCacheBuffer.TryGetValue(guid, out info))
            {
                return true;
            }

            IntPtr ptr = StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitUnitCacheEntry);
            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            info = StyxWoW.Memory.Read<Styx.WoWInternals.WoWCache.WoWCache.CreatureCacheEntry>(ptr);
            _creatureCacheBuffer.Add(guid, info);
            return true;
        }

        #endregion

        #region Threat-Info

        private PerFrameCachedValue<UnitThreatInfo> _threatInfoCache;

        public UnitThreatInfo ThreatInfo
        {
            get
            {
                if (_threatInfoCache == null)
                {
                    _threatInfoCache = new PerFrameCachedValue<UnitThreatInfo>(() => GetThreatInfoFor(StyxWoW.Me));
                }

                return _threatInfoCache;
            }
        }

        public bool Aggro
        {
            get
            {
                if (IsPlayer && PvpFlagged && IsHostile && (IsTargetingMeOrPet || IsTargetingAnyMinion))
                {
                    return Combat;
                }

                return ThreatInfo.ThreatStatus >= ThreatStatus.NoobishTank;
            }
        }

        public bool PetAggro
        {
            get
            {
                WoWUnit myPet = StyxWoW.Me.Pet;
                if (myPet != null && myPet.GotTarget)
                {
                    WoWUnit petTarget = myPet.CurrentTarget;
                    return petTarget.Combat && IsTargetingMeOrPet;
                }

                return false;
            }
        }

        #endregion

        #region Reaction

        public WoWUnitReaction MyReaction
        {
            get { return StyxWoW.Me.GetReactionTowards(this); }
        }

        public bool IsFriendly
        {
            get { return MyReaction >= WoWUnitReaction.Friendly; }
        }

        public bool IsHostile
        {
            get
            {
                if (IsPlayer)
                {
                    return ToPlayer().IsHorde == StyxWoW.Me.IsAlliance;
                }

                return MyReaction <= WoWUnitReaction.Unfriendly;
            }
        }

        public bool IsNeutral
        {
            get { return MyReaction == WoWUnitReaction.Neutral; }
        }

        #endregion

        #region Misc

        private PerFrameCachedValue<int> _totalAbsorbsCache;

        public int TotalAbsorbs
        {
            get
            {
                if (_totalAbsorbsCache == null)
                {
                    _totalAbsorbsCache = new PerFrameCachedValue<int>(GetTotalAbsorbs);
                }

                return _totalAbsorbsCache;
            }
        }

        public int GetTotalAbsorbs()
        {
            EnsureValid("GetTotalAbsorbs");
            return StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.CGUnit_C__GetTotalAbsorb, CallingConvention.ThisCall, BaseAddress);
        }

        public bool IsAutoAttacking
        {
            get { return StyxWoW.Memory.Read<ulong>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWUnitIsAutoAttacking) != 0UL; }
        }

        public bool IsPlayer
        {
            get { return Type == WoWObjectType.Player; }
        }

        public bool IsUnit
        {
            get { return Type == WoWObjectType.Unit; }
        }

        public virtual bool IsAlive
        {
            get { return !IsDead; }
        }

        public bool KilledByMe
        {
            get { return TaggedByMe && IsDead; }
        }

        public float MyAggroRange
        {
            get { return GetAggroRange(StyxWoW.Me); }
        }

        public float MyStealthDetectionRange
        {
            get { return StyxWoW.Me.GetStealthDetectionRange(this); }
        }

        public bool CanSkin
        {
            get
            {
                if (Level <= 2)
                {
                    return false;
                }

                if (!Skinnable)
                {
                    return false;
                }

                WoWCreatureSkinType skinType = SkinType;
                WoWSkill skill;
                switch (CreatureType)
                {
                    case WoWCreatureType.Beast:
                    case WoWCreatureType.Dragon:
                    case WoWCreatureType.Demon:
                    case WoWCreatureType.Undead:
                    case WoWCreatureType.Humanoid:
                        if (skinType != WoWCreatureSkinType.Leather)
                        {
                            return false;
                        }

                        skill = StyxWoW.Me.GetSkill(SkillLine.Skinning);
                        break;

                    case WoWCreatureType.Elemental:
                    case WoWCreatureType.Giant:
                        SkillLine requiredSkill;
                        switch (skinType)
                        {
                            case WoWCreatureSkinType.Herb:
                                requiredSkill = SkillLine.Herbalism;
                                break;
                            case WoWCreatureSkinType.Rock:
                                requiredSkill = SkillLine.Mining;
                                break;
                            default:
                                return false;
                        }

                        skill = StyxWoW.Me.GetSkill(requiredSkill);
                        break;

                    case WoWCreatureType.Mechanical:
                        if (skinType != WoWCreatureSkinType.Bolts)
                        {
                            return false;
                        }

                        skill = StyxWoW.Me.GetSkill(SkillLine.Engineering);
                        break;

                    default:
                        return false;
                }

                if (skill == null)
                {
                    return false;
                }

                if (skill.MaxValue == 0)
                {
                    return false;
                }

                long value = (long)skill.CurrentValue + skill.Bonus;
                if (value < 100L)
                {
                    return Level <= value / 10L + 10L;
                }

                return Level <= value / 5L;
            }
        }

        public bool IsTargetingMyPartyMember
        {
            get
            {
                ulong guid = CurrentTargetGuid;
                if (guid == 0UL)
                {
                    return false;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (StyxWoW.Me.GetPartyMemberGuid(i) == guid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsTargetingMyRaidMember
        {
            get
            {
                ulong guid = CurrentTargetGuid;
                if (guid == 0UL)
                {
                    return false;
                }

                for (int i = 0; i < 40; i++)
                {
                    if (StyxWoW.Me.GetRaidMemberGuid(i) == guid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsStealthed
        {
            get { return (StyxWoW.Memory.Read<byte>(false, BaseAddress + 228, (IntPtr)274) & 2) == 1; }
        }

        public DifficultyColor Difficulty
        {
            get
            {
                int myLevel = StyxWoW.Me.Level;
                int level = Level;
                int delta = myLevel - level;
                if (delta <= -10 || IsBoss)
                {
                    return DifficultyColor.Skull;
                }

                if (delta < -4)
                {
                    return DifficultyColor.Red;
                }

                if (delta <= -3)
                {
                    return DifficultyColor.Orange;
                }

                if (delta >= -2 && delta <= 2)
                {
                    return DifficultyColor.Yellow;
                }

                if (myLevel < 10 && delta <= 4)
                {
                    return DifficultyColor.Green;
                }

                if (myLevel < 20 && delta <= 5)
                {
                    return DifficultyColor.Green;
                }

                if (myLevel < 30 && delta <= 6)
                {
                    return DifficultyColor.Green;
                }

                if (myLevel < 40 && delta <= 7)
                {
                    return DifficultyColor.Green;
                }

                if (delta <= 8)
                {
                    return DifficultyColor.Green;
                }

                return DifficultyColor.Gray;
            }
        }

        public bool HasQuestCursor
        {
            get
            {
                switch (QuestGiverStatus)
                {
                    case QuestGiverStatus.LowLevelAvailable:
                    case QuestGiverStatus.LowLevelTurnInRepeatable:
                    case QuestGiverStatus.LowLevelAvailableRepeatable:
                    case QuestGiverStatus.Incomplete:
                    case QuestGiverStatus.TurnInRepeatable:
                    case QuestGiverStatus.AvailableRepeatable:
                    case QuestGiverStatus.Available:
                    case QuestGiverStatus.TurnInInvisible:
                    case QuestGiverStatus.TurnIn:
                    case QuestGiverStatus.UnkB:
                    case QuestGiverStatus.UnkC:
                    case QuestGiverStatus.UnkD:
                        return true;
                    default:
                        string cursorOverride = GetCursorOverride();
                        return cursorOverride != null && (cursorOverride == "quest" || cursorOverride == "questrepeatable" || cursorOverride == "questturnin");
                }
            }
        }

        public bool CanLoot
        {
            get { return Lootable; }
        }

        private PerFrameCachedValue<bool> _canInteractCache;

        public bool CanInteract
        {
            get
            {
                if (_canInteractCache == null)
                {
                    _canInteractCache = new PerFrameCachedValue<bool>(() =>
                    {
                        EnsureValid("CanInteract");
                        return StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.CGPlayer_C__CanInteract, CallingConvention.ThisCall, StyxWoW.Me.BaseAddress, BaseAddress) != 0;
                    });
                }

                return _canInteractCache;
            }
        }

        private PerFrameCachedValue<bool> _canInteractNowCache;

        public bool CanInteractNow
        {
            get
            {
                if (_canInteractNowCache == null)
                {
                    _canInteractNowCache = new PerFrameCachedValue<bool>(() =>
                    {
                        EnsureValid("CanInteractNow");
                        return StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.CGPlayer_C__CanAutoInteract, CallingConvention.ThisCall, StyxWoW.Me.BaseAddress, BaseAddress) != 0;
                    });
                }

                return _canInteractNowCache;
            }
        }

        private PerFrameCachedValue<bool> _inLineOfSightCache;

        public override bool InLineOfSight
        {
            get
            {
                if (_inLineOfSightCache == null)
                {
                    _inLineOfSightCache = new PerFrameCachedValue<bool>(() => TraceLine(TraceLineHitFlags.Collision));
                }

                return _inLineOfSightCache;
            }
        }

        private TimeCachedValue<bool> _inLineOfSpellSightCache;

        public bool InLineOfSpellSight
        {
            get
            {
                if (_inLineOfSpellSightCache == null)
                {
                    _inLineOfSpellSightCache = new TimeCachedValue<bool>(TimeSpan.FromMilliseconds(400.0), () => TraceLine(TraceLineHitFlags.SpellLineOfSight));
                }

                return _inLineOfSpellSightCache;
            }
        }

        #endregion

        #region Reaction / Interaction

        public bool KnowsSpell(int id)
        {
            EnsureValid("KnowsSpell");
            byte result = StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.CGUnit_C__KnowsSpell, CallingConvention.ThisCall, BaseAddress, id);
            return result != 0;
        }

        public bool Behind(WoWUnit obj)
        {
            return obj != null && WoWMathHelper.IsBehind(this, obj);
        }

        public float GetStealthDetectionRange(WoWUnit to)
        {
            WoWAuraCollection allAuras = GetAllAuras();
            float visibleDistance = 10.5f - allAuras.GetTotalAuraModifier(WoWApplyAuraType.ModStealth) / 100f;
            visibleDistance += to.Level - Level;
            int stealthMod = allAuras.GetTotalAuraModifier(WoWApplyAuraType.ModStealthLevel);
            if (stealthMod < 0)
            {
                stealthMod = 0;
            }

            WoWAuraCollection toAuras = to.GetAllAuras();
            visibleDistance += (toAuras.GetTotalAuraModifier(WoWApplyAuraType.ModStealthDetect) - stealthMod) / 5f;
            return MathEx.Clamp(visibleDistance, 0f, 45f);
        }

        public float GetAggroRange(WoWUnit to)
        {
            if (to.IsStealthed)
            {
                return GetStealthDetectionRange(to);
            }

            WoWUnitReaction reaction = GetReactionTowards(to);
            if (reaction < WoWUnitReaction.Neutral && !IsDead)
            {
                float radius = 20f;
                int levelDiff = Level - to.Level;
                radius += levelDiff * 1f;
                if (Elite)
                {
                    radius += 3f;
                }

                if (CreatureType == WoWCreatureType.Beast)
                {
                    radius -= 3f;
                }
                else if (CreatureType == WoWCreatureType.Mechanical)
                {
                    radius += 3f;
                }

                return MathEx.Clamp(radius, 5f, 45f);
            }

            return 0f;
        }

        public void Face()
        {
            StyxWoW.Me.SetFacing(this);
        }

        public void Target()
        {
            if (CanSelect && StyxWoW.Me.CurrentTarget != this)
            {
                StyxWoW.ResetAfk();
                TargetInternal(Guid);
            }
        }

        internal void TargetInternal(ulong guid)
        {
            StyxWoW.CallInjected(StyxWoW.Offsets.Functions.CGGameUI__Target, CallingConvention.Cdecl, guid);
        }

        private readonly WaitTimer _reactionTimer = new WaitTimer(TimeSpan.FromSeconds(2.0));

        private readonly Dictionary<ulong, WoWUnitReaction> _reactionCache = new Dictionary<ulong, WoWUnitReaction>();

        public WoWUnitReaction GetReactionTowards(WoWUnit otherUnit)
        {
            EnsureValid("GetReactionTowards");
            if (otherUnit == null)
            {
                throw new ArgumentNullException("otherUnit");
            }

            if (!otherUnit.IsValid)
            {
                throw new InvalidOperationException("Cannot call GetReactionTowards with an invalid unit");
            }

            if (otherUnit is WoWPlayer && Battlegrounds.IsInsideBattleground)
            {
                WoWPlayer player = otherUnit.ToPlayer();
                if (StyxWoW.Me.IsHorde != player.IsHorde)
                {
                    return WoWUnitReaction.Hostile;
                }

                return WoWUnitReaction.Friendly;
            }

            if (_reactionTimer.IsFinished)
            {
                _reactionCache.Clear();
                _reactionTimer.Reset();
            }

            ulong guid = otherUnit.Guid;
            WoWUnitReaction cachedReaction;
            if (_reactionCache.TryGetValue(guid, out cachedReaction))
            {
                return cachedReaction;
            }

            int reaction = StyxWoW.CallInjected<int>(StyxWoW.Offsets.Functions.CGUnit_C__UnitReaction, CallingConvention.ThisCall, (uint)(int)BaseAddress, otherUnit.BaseAddress);
            _reactionCache.Add(guid, (WoWUnitReaction)reaction);
            return (WoWUnitReaction)reaction;
        }

        internal bool GetForcedReaction(uint factionId, out WoWUnitReaction reaction)
        {
            uint numForcedReactions = StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.NumForcedReactions);
            ForcedReaction[] forcedReactions = StyxWoW.Memory.ReadArray<ForcedReaction>(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.ForcedReactions), (int)numForcedReactions);
            for (int i = 0; i < forcedReactions.Length; i++)
            {
                if (forcedReactions[i].FactionId == factionId)
                {
                    reaction = (WoWUnitReaction)forcedReactions[i].Reaction;
                    return true;
                }
            }

            reaction = WoWUnitReaction.Hated;
            return false;
        }

        public UnitThreatInfo GetThreatInfoFor(WoWUnit otherUnit)
        {
            return UnitThreatInfo.GetForUnit(this, otherUnit);
        }

        public bool IsWithinMeleeRangeOf(WoWUnit otherUnit)
        {
            if (otherUnit == null)
            {
                throw new ArgumentNullException("otherUnit");
            }

            EnsureValid("IsWithinMeleeRangeOf");
            if (!otherUnit.IsValid)
            {
                throw new ArgumentException("Other unit is invalid!", "otherUnit");
            }

            byte result = StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.CGUnit_C__IsInMeleeRange, CallingConvention.ThisCall, BaseAddress, otherUnit.BaseAddress);
            return result != 0;
        }

        internal WoWFactionGroup GetFactionGroup()
        {
            WoWFactionGroup group;
            if (this is WoWPlayer && GetFactionGroupName(true, FactionTemplate.Record.FactionGroupId, out group))
            {
                return group;
            }

            return WoWFactionGroup.None;
        }

        internal bool GetFactionGroupName(bool checkNeutralMask, uint factionGroupMask, out WoWFactionGroup factionGroup)
        {
            factionGroup = WoWFactionGroup.None;
            if (checkNeutralMask && (factionGroupMask & 1U) != 0U && (factionGroupMask & 6U) == 0U)
            {
                factionGroup = WoWFactionGroup.Neutral;
                return true;
            }

            foreach (WoWDb.Row row in StyxWoW.Db[ClientDb.FactionGroup])
            {
                int id = row.GetField<int>(0U);
                string localized = row.GetStringField(3U);
                if (((uint)(1 << (row.GetField<int>(1U) & 31)) & factionGroupMask) != 0U && !string.IsNullOrEmpty(localized))
                {
                    factionGroup = (WoWFactionGroup)id;
                    return true;
                }
            }

            return false;
        }

        public RaidTargetMarker GetMark()
        {
            ulong guid = Guid;
            IntPtr targetsPtr;
            if (StyxWoW.Me.GroupInfo.IsInParty)
            {
                targetsPtr = StyxWoW.Me.GroupInfo.TargetMarkerList;
            }
            else
            {
                targetsPtr = StyxWoW.Offsets.Statics.NonPartyRaidMarkTargets;
            }

            ulong[] guids = StyxWoW.Memory.ReadArray<ulong>(targetsPtr, 28);
            for (int i = 0; i < 28; i++)
            {
                if (guids[i] == guid)
                {
                    return (RaidTargetMarker)i;
                }
            }

            return RaidTargetMarker.None;
        }

        public void Mark(RaidTargetMarker mark)
        {
            using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(8))
            {
                allocatedMemory.Write(0, Guid);
                StyxWoW.Memory.CallInjected(StyxWoW.Offsets.Functions.SetRaidTargetMarker, CallingConvention.Cdecl, (int)allocatedMemory.Address, (int)mark);
            }
        }

        private unsafe bool TraceLine(TraceLineHitFlags hitFlags)
        {
            if (Entry == 56754U)
            {
                return true;
            }

            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool result;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(40))
                {
                    byte* ptr = stackalloc byte[28];
                    *(float*)(ptr + 24) = 1f;
                    StyxWoW.Memory.WriteByteBuffer(allocatedMemory.Address, (void*)ptr, 28);
                    executor.Clear();
                    executor.AddLine("push {0}", allocatedMemory.Address);
                    executor.AddLine("push {0}", (uint)(int)StyxWoW.Me.BaseAddress);
                    executor.AddLine("call GetTracelinePosition");
                    executor.AddLine("push {0}", allocatedMemory.Address + 12);
                    executor.AddLine("push {0}", (uint)(int)BaseAddress);
                    executor.AddLine("call GetTracelinePosition");
                    executor.AddLine("push 0");
                    executor.AddLine("push {0}", (uint)hitFlags);
                    executor.AddLine("push {0}", allocatedMemory.Address + 24);
                    executor.AddLine("push {0}", allocatedMemory.Address + 28);
                    executor.AddLine("push {0}", allocatedMemory.Address + 12);
                    executor.AddLine("push {0}", allocatedMemory.Address + 0);
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions.CGWorldFrame__Intersect);
                    executor.AddLine("add esp, 0x18");
                    executor.AddLine("retn");
                    EmitGetTracelinePositionRoutine(executor);
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        result = executor.Memory.Read<byte>(executor.ReturnPointer) == 0;
                    }
                }
            }

            return result;
        }

        private static void EmitGetTracelinePositionRoutine(Executor executor)
        {
            executor.AddLine("GetTracelinePosition:");
            executor.AddLine("push ebp");
            executor.AddLine("mov ebp, esp");
            executor.AddLine("sub esp, 24");
            executor.AddLine("mov ecx, [ebp + 8]");
            executor.AddLine("mov eax, [ecx]");
            executor.AddLine("add eax, {0}", (int)StyxWoW.Offsets.VFTableIndex.GetPosition * 4);
            executor.AddLine("mov eax, [eax]");
            executor.AddLine("push dword [ebp + 12]");
            executor.AddLine("call eax");
            executor.AddLine("mov ecx, dword [ebp + 8]");
            executor.AddLine("lea edx, [ebp - 12]");
            executor.AddLine("push edx");
            executor.AddLine("mov eax, [ecx]");
            executor.AddLine("add eax, {0}", 144);
            executor.AddLine("mov eax, [eax]");
            executor.AddLine("call eax");
            executor.AddLine("mov ecx, dword [ebp + 12]");
            executor.AddLine("fld dword [eax + 8]");
            executor.AddLine("fsub dword [ecx + 8]");
            executor.AddLine("push 0.70");
            executor.AddLine("fld dword [esp]");
            executor.AddLine("fmulp");
            executor.AddLine("fld dword [ecx + 8]");
            executor.AddLine("faddp");
            executor.AddLine("fstp dword [ecx + 8]");
            executor.AddLine("add esp, 4");
            executor.AddLine("mov esp, ebp");
            executor.AddLine("pop ebp");
            executor.AddLine("retn 8");
        }

        #endregion

        #region Nested Types

        public struct PowerInfo
        {
            internal PowerInfo(WoWPowerType type, uint current, uint max, float regenFlatModifier, float regenInterruptedFlatModifier, uint costModifier, float costMultiplier)
                : this()
            {
                Type = type;
                Current = current;
                Max = max;
                RegenFlatModifier = regenFlatModifier;
                RegenInterruptedFlatModifier = regenInterruptedFlatModifier;
                CostModifier = costModifier;
                CostMultiplier = costMultiplier;
                Percent = max != 0U ? current * 100f / max : 0f;
            }

            public WoWPowerType Type { get; private set; }

            public uint Current { get; private set; }

            public int CurrentI
            {
                get { return BitConverter.ToInt32(BitConverter.GetBytes(Current), 0); }
            }

            public uint Max { get; private set; }

            public float Percent { get; private set; }

            public float RegenFlatModifier { get; private set; }

            public float RegenInterruptedFlatModifier { get; private set; }

            public uint CostModifier { get; private set; }

            public float CostMultiplier { get; private set; }
        }

        private struct ForcedReaction
        {
            public uint FactionId;

            public uint Reaction;
        }

        #endregion
    }
}
