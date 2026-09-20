using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GreenMagic;
using Styx.Common;
using Styx.Helpers;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    internal static class SpellHelpers
    {
        private static PerFrameCachedValue<Dictionary<int, int>> _spellOverridesCache;

        public static Dictionary<int, int> GetSpellOverrides()
        {
            if (_spellOverridesCache == null)
            {
                _spellOverridesCache = new PerFrameCachedValue<Dictionary<int, int>>(() =>
                {
                    Dictionary<int, int> ret = new Dictionary<int, int>();
                    ExternalProcessMemory mem = StyxWoW.Memory;
                    IntPtr pCurrentOverride = mem.Read<IntPtr>(StyxWoW.Offsets.Statics.SpellOverrideListStart);
                    int talentSpellNext = mem.Read<int>(StyxWoW.Offsets.Statics.SpellOverrideListNext);
                    while (((int)pCurrentOverride & 1) == 0 && pCurrentOverride != IntPtr.Zero)
                    {
                        LocalPlayer.SpellOverrideInfo info = mem.Read<LocalPlayer.SpellOverrideInfo>(pCurrentOverride);
                        IntPtr next = mem.Read<IntPtr>(pCurrentOverride + 4 + talentSpellNext);
                        pCurrentOverride = next;
                        if (info.NewSpellId == 0)
                        {
                            break;
                        }
                        ret[info.OriginalSpellId] = info.NewSpellId;
                    }
                    return ret;
                });
            }
            return _spellOverridesCache;
        }

        private static IEnumerable<WoWSpell> RawKnownSpells
        {
            get { return SpellManager.Spells.Values; }
        }

        public static bool FindSpell(WoWSpell spell, out SpellFindResults findResults)
        {
            return FindSpell(spell.Id, out findResults);
        }

        public static bool FindSpell(int id, out SpellFindResults findResults)
        {
            findResults = null;

            Dictionary<int, int> overrides = GetSpellOverrides();
            int newId = 0;
            int current = id;
            while (overrides.TryGetValue(current, out current))
            {
                newId = current;
            }

            if (newId != 0)
            {
                using (StyxWoW.Memory.AcquireFrame())
                {
                    findResults = new SpellFindResults(WoWSpell.FromId(id), WoWSpell.FromId(newId));
                }
                return true;
            }

            foreach (KeyValuePair<int, int> pair in overrides)
            {
                if (pair.Value == id)
                {
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        findResults = new SpellFindResults(WoWSpell.FromId(pair.Key), WoWSpell.FromId(id));
                    }
                    return true;
                }
            }

            WoWSpell raw = RawKnownSpells.FirstOrDefault(s => s.Id == id);
            if (raw != null)
            {
                findResults = new SpellFindResults(raw, null);
                return true;
            }

            return false;
        }

        public static bool FindSpell(string name, out SpellFindResults findResults)
        {
            WoWSpell spell = RawKnownSpells.FirstOrDefault(s => s.Name == name);
            if (spell != null)
            {
                return FindSpell(spell.Id, out findResults);
            }

            Dictionary<int, int> overrides = GetSpellOverrides();
            foreach (int id in overrides.Keys.Concat(overrides.Values).Distinct())
            {
                spell = WoWSpell.FromId(id);
                if (spell != null && spell.Name == name)
                {
                    return FindSpell(spell.Id, out findResults);
                }
            }

            findResults = null;
            return false;
        }

        public static HashSet<int> CanCastUseOriginals = new HashSet<int>
        {
            102355, // Faerie Swarm
            110309, // Symbiosis
            127538, // Savage Roar (feral)
            106830, // Thrash
            106832, // Thrash (Guardian)
            77758, // Thrash (Guardian) #2
        };
    }

    public static class SpellManager
    {
        private static readonly Random Rand = new Random(Environment.TickCount);

        #region FindSpell

        public static bool FindSpell(string name, out SpellFindResults results)
        {
            return SpellHelpers.FindSpell(name, out results);
        }

        public static bool FindSpell(int id, out SpellFindResults results)
        {
            return SpellHelpers.FindSpell(id, out results);
        }

        #endregion

        #region HasSpell

        public static bool HasSpell(string name)
        {
            SpellFindResults junk;
            return SpellHelpers.FindSpell(name, out junk);
        }

        public static bool HasSpell(int id)
        {
            SpellFindResults junk;
            return SpellHelpers.FindSpell(id, out junk);
        }

        public static bool HasSpell(WoWSpell spell)
        {
            SpellFindResults junk;
            return SpellHelpers.FindSpell(spell, out junk);
        }

        #endregion

        #region CanCast

        #region By Name

        public static bool CanCast(string spellName)
        {
            return CanCast(spellName, false);
        }

        public static bool CanCast(string spellName, WoWUnit target)
        {
            return CanCast(spellName, target, false);
        }

        public static bool CanCast(string spellName, bool checkRange)
        {
            return CanCast(spellName, StyxWoW.Me.CurrentTarget, checkRange);
        }

        public static bool CanCast(string spellName, WoWUnit target, bool checkRange)
        {
            return CanCast(spellName, target, checkRange, true);
        }

        public static bool CanCast(string spellName, WoWUnit target, bool checkRange, bool checkMoving)
        {
            SpellFindResults findResults;
            if (!SpellHelpers.FindSpell(spellName, out findResults))
            {
                return false;
            }

            if (findResults.Override != null && !SpellHelpers.CanCastUseOriginals.Contains(findResults.Override.Id) &&
                findResults.Original.Id != 110309 && findResults.Override.Id != 110309)
            {
                return CanCast(findResults.Override, target, checkRange, checkMoving);
            }

            return CanCast(findResults.Original, target, checkRange, checkMoving);
        }

        #endregion

        #region By ID

        public static bool CanCast(int spellId)
        {
            return CanCast(spellId, false);
        }

        public static bool CanCast(int spellId, WoWUnit target)
        {
            return CanCast(spellId, target, false);
        }

        public static bool CanCast(int spellId, bool checkRange)
        {
            return CanCast(spellId, StyxWoW.Me.CurrentTarget, checkRange);
        }

        public static bool CanCast(int spellId, WoWUnit target, bool checkRange)
        {
            SpellFindResults findResults;
            if (SpellHelpers.FindSpell(spellId, out findResults))
            {
                if (findResults.Override != null && !SpellHelpers.CanCastUseOriginals.Contains(findResults.Original.Id))
                {
                    return CanCast(findResults.Override, target, checkRange, true, true);
                }

                return CanCast(findResults.Original, target, checkRange, true, true);
            }

            WoWSpell spell = WoWSpell.FromId(spellId);
            if (spell == null)
            {
                return false;
            }

            if (!StyxWoW.Me.KnowsSpell(spell.Id))
            {
                WoWUnit pet = StyxWoW.Me.Pet;
                if (pet == null || !pet.KnowsSpell(spell.Id))
                {
                    return false;
                }
            }

            return CanCast(spell, target, checkRange, true, true);
        }

        #endregion

        #region By Spell

        public static bool CanCast(WoWSpell spell)
        {
            return CanCast(spell, false);
        }

        public static bool CanCast(WoWSpell spell, WoWUnit target)
        {
            return CanCast(spell, target, false, true, true);
        }

        public static bool CanCast(WoWSpell spell, bool checkRange)
        {
            return CanCast(spell, StyxWoW.Me.CurrentTarget, checkRange, true, true);
        }

        #endregion

        #region Main CanCast

        private static readonly HashSet<uint> LineOfSightExceptions = new HashSet<uint>
        {
            56754, // Azure Serpent (Cloudstrike fight) Shadow Pan Monastary
        };

        public static bool CanCast(WoWSpell spell, WoWUnit target, bool checkRange, bool checkMoving = true, bool accountForLagTolerance = true)
        {
            if (spell == null)
            {
                return false;
            }

            WoWUnit me = StyxWoW.Me;
            if (!me.KnowsSpell(spell.Id))
            {
                SpellFindResults findResults;
                if (FindSpell(spell.Id, out findResults))
                {
                    spell = findResults.Override ?? findResults.Original;
                }
                else
                {
                    me = StyxWoW.Me.Pet;
                    if (me == null || !me.KnowsSpell(spell.Id))
                    {
                        return false;
                    }
                }
            }

            if (checkRange && target != null && !spell.IsSelfOnlySpell)
            {
                if (spell.IsMeleeSpell && !me.IsWithinMeleeRangeOf(target))
                {
                    return false;
                }

                float distance = me.Location.Distance(target.Location);
                if (spell.HasRange && (distance > spell.SpellMaxRange(target) || distance < spell.SpellMinRange(target)))
                {
                    return false;
                }

                if (!LineOfSightExceptions.Contains(target.Entry))
                {
                    bool isMe;
                    if ((isMe = me.IsMe) && !target.InLineOfSpellSight)
                    {
                        return false;
                    }

                    if (!isMe && !GameWorld.IsInLineOfSpellSight(me.GetTraceLinePos(), target.GetTraceLinePos()))
                    {
                        return false;
                    }
                }
            }

            if (checkMoving && (spell.CastTime != 0U || spell.IsFunnel) && me.IsMoving && !me.HasAura("Spiritwalker's Grace"))
            {
                return false;
            }

            if (accountForLagTolerance && me.ChanneledCastingSpellId == 0)
            {
                uint latency = StyxWoW.WoWClient.Latency * 2U;
                if (spell.CooldownTimeLeft.TotalMilliseconds > latency)
                {
                    return false;
                }

                if (me.IsCasting && me.CurrentCastTimeLeft.TotalMilliseconds > latency)
                {
                    return false;
                }
            }
            else
            {
                if (me.IsCasting)
                {
                    return false;
                }

                if (spell.Cooldown)
                {
                    return false;
                }
            }

            if (spell.Name == "Ravage!")
            {
                return me.HasAura("Stampede");
            }

            return spell.CanCast;
        }

        private static float SpellMaxRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.MaxRange <= 0f)
            {
                return 0f;
            }

            if (unit == null)
            {
                return spell.MaxRange;
            }

            return spell.MaxRange + unit.CombatReach + 1f;
        }

        private static float SpellMinRange(this WoWSpell spell, WoWUnit unit)
        {
            if (spell.MinRange <= 0f)
            {
                return 0f;
            }

            if (unit == null)
            {
                return spell.MinRange;
            }

            return spell.MinRange + unit.CombatReach + 1.66666675f;
        }

        #endregion

        #endregion

        #region CanBuff

        #region By Name

        public static bool CanBuff(string spellName)
        {
            return CanBuff(spellName, false);
        }

        public static bool CanBuff(string spellName, WoWUnit target)
        {
            return CanBuff(spellName, target, false);
        }

        public static bool CanBuff(string spellName, bool checkRange)
        {
            return CanBuff(spellName, StyxWoW.Me, checkRange);
        }

        public static bool CanBuff(string spellName, WoWUnit target, bool checkRange)
        {
            SpellFindResults findResults;
            if (!SpellHelpers.FindSpell(spellName, out findResults))
            {
                return false;
            }

            if (findResults.Override != null)
            {
                return CanBuff(findResults.Override, target, checkRange);
            }

            return CanBuff(findResults.Original, target, checkRange);
        }

        #endregion

        #region By ID

        public static bool CanBuff(int spellId)
        {
            return CanBuff(spellId, false);
        }

        public static bool CanBuff(int spellId, WoWUnit target)
        {
            return CanBuff(spellId, target, false);
        }

        public static bool CanBuff(int spellId, bool checkRange)
        {
            return CanBuff(spellId, StyxWoW.Me, checkRange);
        }

        public static bool CanBuff(int spellId, WoWUnit target, bool checkRange)
        {
            SpellFindResults findResults;
            if (!SpellHelpers.FindSpell(spellId, out findResults))
            {
                return false;
            }

            if (findResults.Override != null)
            {
                return CanBuff(findResults.Override, target, checkRange);
            }

            return CanBuff(findResults.Original, target, checkRange);
        }

        #endregion

        #region By Spell

        public static bool CanBuff(WoWSpell spell)
        {
            return CanBuff(spell, false);
        }

        public static bool CanBuff(WoWSpell spell, WoWUnit target)
        {
            return CanBuff(spell, target, false);
        }

        public static bool CanBuff(WoWSpell spell, bool checkRange)
        {
            return CanBuff(spell, StyxWoW.Me, checkRange);
        }

        #endregion

        #region Main Check

        public static bool CanBuff(WoWSpell spell, WoWUnit target, bool checkRange)
        {
            return CanCast(spell, target, checkRange, true, true) && !target.HasAura(spell.Name);
        }

        #endregion

        #endregion

        #region Cast

        #region By Name

        public static bool Cast(string spellName)
        {
            return Cast(spellName, null);
        }

        public static bool Cast(string spellName, WoWUnit target)
        {
            SpellFindResults findResults;
            return SpellHelpers.FindSpell(spellName, out findResults) && Cast(findResults.Original, target);
        }

        #endregion

        #region By ID

        public static bool Cast(int spellId)
        {
            return Cast(spellId, null);
        }

        public static bool Cast(int spellId, WoWUnit target)
        {
            SpellFindResults findResults;
            return SpellHelpers.FindSpell(spellId, out findResults) && Cast(findResults.Original, target);
        }

        #endregion

        #region By Spell

        public static bool Cast(WoWSpell spell)
        {
            return Cast(spell, null);
        }

        #endregion

        #region Main Cast

        public static bool Cast(WoWSpell spell, WoWUnit target)
        {
            return spell != null && CastSpellById(spell.Id, target != null ? target.Guid : 0UL);
        }

        #endregion

        #endregion

        #region Buff

        #region By Name

        public static bool Buff(string spellName)
        {
            return Buff(spellName, StyxWoW.Me);
        }

        public static bool Buff(string spellName, WoWUnit target)
        {
            SpellFindResults findResults;
            if (!SpellHelpers.FindSpell(spellName, out findResults))
            {
                return false;
            }

            if (findResults.Override != null)
            {
                return Buff(findResults.Override, target);
            }

            return Buff(findResults.Original, target);
        }

        #endregion

        #region By ID

        public static bool Buff(int spellId)
        {
            return Buff(spellId, StyxWoW.Me);
        }

        public static bool Buff(int spellId, WoWUnit target)
        {
            SpellFindResults findResults;
            if (!SpellHelpers.FindSpell(spellId, out findResults))
            {
                return false;
            }

            if (findResults.Override != null)
            {
                return Buff(findResults.Override, target);
            }

            return Buff(findResults.Original, target);
        }

        #endregion

        #region By Spell

        public static bool Buff(WoWSpell spell)
        {
            return Buff(spell, StyxWoW.Me);
        }

        #endregion

        #region Main Cast

        public static bool Buff(WoWSpell spell, WoWUnit target)
        {
            return Cast(spell, target);
        }

        #endregion

        #endregion

        #region CastRandom

        #region By name

        public static bool CastRandom(IEnumerable<string> spellNames, WoWUnit target, bool checkRange)
        {
            return CastRandom(spellNames.Select(n => InternalSpells.Values.FirstOrDefault(s => s.Name == n)), target, checkRange);
        }

        public static bool CastRandom(IEnumerable<string> spellNames, WoWUnit target)
        {
            return CastRandom(spellNames, target, false);
        }

        public static bool CastRandom(IEnumerable<string> spellNames, bool checkRange)
        {
            return CastRandom(spellNames, null, checkRange);
        }

        #endregion

        #region By ID

        public static bool CastRandom(IEnumerable<int> spellIds, WoWUnit target, bool checkRange)
        {
            return CastRandom(spellIds.Select(i => InternalSpells.Values.FirstOrDefault(s => s.Id == i)), target, checkRange);
        }

        public static bool CastRandom(IEnumerable<int> spellIds, WoWUnit target)
        {
            return CastRandom(spellIds, target, false);
        }

        public static bool CastRandom(IEnumerable<int> spellIds, bool checkRange)
        {
            return CastRandom(spellIds, StyxWoW.Me.CurrentTarget, checkRange);
        }

        #endregion

        #region By Spell

        public static bool CastRandom(IEnumerable<WoWSpell> spells, WoWUnit target)
        {
            return CastRandom(spells, target, false);
        }

        public static bool CastRandom(IEnumerable<WoWSpell> spells, bool checkRange)
        {
            return CastRandom(spells, StyxWoW.Me.CurrentTarget, checkRange);
        }

        #endregion

        #region Main CastRandom

        public static bool CastRandom(IEnumerable<WoWSpell> spells, WoWUnit target, bool checkRange)
        {
            List<WoWSpell> spellList = spells.ToList();
            while (spellList.Count != 0)
            {
                int randomIndex = Rand.Next(0, spellList.Count);
                if (CanCast(spellList[randomIndex], target, checkRange, true, true))
                {
                    return Cast(spellList[randomIndex], target);
                }

                spellList.RemoveAt(randomIndex);
            }

            return false;
        }

        #endregion

        #endregion

        #region BuffRandom

        #region By name

        public static bool BuffRandom(IEnumerable<string> spellNames, WoWUnit target, bool checkRange)
        {
            return BuffRandom(spellNames.Select(n => InternalSpells.Values.FirstOrDefault(s => s.Name == n)), target, checkRange);
        }

        public static bool BuffRandom(IEnumerable<string> spellNames, WoWUnit target)
        {
            return BuffRandom(spellNames, target, false);
        }

        public static bool BuffRandom(IEnumerable<string> spellNames, bool checkRange)
        {
            return BuffRandom(spellNames, StyxWoW.Me.CurrentTarget, checkRange);
        }

        #endregion

        #region By ID

        public static bool BuffRandom(IEnumerable<int> spellIds, WoWUnit target, bool checkRange)
        {
            return BuffRandom(spellIds.Select(i => InternalSpells.Values.FirstOrDefault(s => s.Id == i)), target, checkRange);
        }

        public static bool BuffRandom(IEnumerable<int> spellIds, WoWUnit target)
        {
            return BuffRandom(spellIds, target, false);
        }

        public static bool BuffRandom(IEnumerable<int> spellIds, bool checkRange)
        {
            return BuffRandom(spellIds, StyxWoW.Me.CurrentTarget, checkRange);
        }

        #endregion

        #region By Spell

        public static bool BuffRandom(IEnumerable<WoWSpell> spells, WoWUnit target)
        {
            return BuffRandom(spells, target, false);
        }

        public static bool BuffRandom(IEnumerable<WoWSpell> spells, bool checkRange)
        {
            return BuffRandom(spells, StyxWoW.Me.CurrentTarget, checkRange);
        }

        #endregion

        #region Main CastRandom

        public static bool BuffRandom(IEnumerable<WoWSpell> spells, WoWUnit target, bool checkRange)
        {
            List<WoWSpell> spellList = spells.ToList();
            while (spellList.Count != 0)
            {
                int randomIndex = Rand.Next(0, spellList.Count);
                if (CanBuff(spellList[randomIndex], target, checkRange))
                {
                    Buff(spellList[randomIndex], target);
                    return true;
                }

                spellList.RemoveAt(randomIndex);
            }

            return false;
        }

        #endregion

        #endregion

        #region Stop Casting

        public static void StopCasting()
        {
            Lua.DoString("SpellStopCasting()", "WoW.lua");
        }

        #endregion

        #region Ctor & Updater

        private static void BotEvents_OnBotStart(EventArgs args)
        {
            Update();
            Lua.Events.DetachEvent("LEARNED_SPELL_IN_TAB", EventUpdate);
            Lua.Events.DetachEvent("ACTIVE_TALENT_GROUP_CHANGED", EventUpdate);
            Lua.Events.DetachEvent("PLAYER_SPECIALIZATION_CHANGED", EventUpdate);
            Lua.Events.AttachEvent("LEARNED_SPELL_IN_TAB", EventUpdate);
            Lua.Events.AttachEvent("ACTIVE_TALENT_GROUP_CHANGED", EventUpdate);
            Lua.Events.AttachEvent("PLAYER_SPECIALIZATION_CHANGED", EventUpdate);
        }

        internal static void Init()
        {
            BotEvents.OnBotStarted += BotEvents_OnBotStart;
            InternalSpells.Clear();
            Update();
        }

        internal static void Shutdown()
        {
            InternalSpells.Clear();
            BotEvents.OnBotStarted -= BotEvents_OnBotStart;
        }

        private static void EventUpdate(object sender, LuaEventArgs e)
        {
            Update();
        }

        private static readonly object UpdateLocker = new object();

        internal static void Update()
        {
            lock (UpdateLocker)
            {
                InternalSpells.Clear();
                List<WoWSpell> list = new List<WoWSpell>(50);
                using (StyxWoW.Memory.AcquireFrame())
                {
                    IEnumerable<LocalPlayer.KnownSpell> knownSpells = StyxWoW.Me.IterateKnownSpells();
                    foreach (WoWSpell spell in knownSpells.Select(s => WoWSpell.FromId(s.SpellId)))
                    {
                        if (spell != null && !InternalSpells.ContainsKey(spell.Name))
                        {
                            InternalSpells[spell.Name] = spell;
                            list.Add(spell);
                        }
                    }

                    for (int i = 0; i < list.Count; i += 3)
                    {
                        int count = Math.Min(3, list.Count - i);
                        Logging.WriteDiagnostic("[SpellManager] Adding known spells: {0}", string.Join(" | ", list.Skip(i).Take(count).Select(s =>
                        {
                            string text = s.Name;
                            if (text.Length > 20)
                            {
                                text = text.Remove(18) + "..";
                            }

                            return string.Format("{0,-20} ({1:000000})", text, s.Id);
                        })));
                    }

                    foreach (KeyValuePair<int, int> pair in SpellHelpers.GetSpellOverrides())
                    {
                        WoWSpell original = WoWSpell.FromId(pair.Key);
                        WoWSpell overrideSpell = WoWSpell.FromId(pair.Value);
                        Logging.WriteDiagnostic("[SpellManager] {0} ({1}) overrides {2} ({3})",
                            overrideSpell != null ? overrideSpell.Name : "(null)",
                            pair.Value,
                            original != null ? original.Name : "(null)",
                            pair.Key);
                    }
                }
            }
        }

        #endregion

        #region Global Cooldown

        public static TimeSpan GlobalCooldownLeft
        {
            get
            {
                long currentTime = (long)StyxWoW.WoWClient.PerformanceCounter();
                IntPtr current = StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LocalPlayerSpellsOnCooldown + 8);
                while (current != IntPtr.Zero && ((int)current & 1) == 0)
                {
                    SpellCooldownEntry entry = StyxWoW.Memory.Read<SpellCooldownEntry>(current);
                    long startTime = (long)(ulong)entry.StartTime;
                    long endTime = startTime + entry.GlobalCooldownDuration;
                    if (endTime > currentTime)
                    {
                        return TimeSpan.FromMilliseconds((double)(endTime - currentTime));
                    }

                    current = entry.Next;
                }

                return TimeSpan.Zero;
            }
        }

        public static bool GlobalCooldown
        {
            get { return GlobalCooldownLeft != TimeSpan.Zero; }
        }

        #region Nested Type: SpellCooldownEntry

        // Reversed from SpellHistory::GetCooldown, get's called from Spell_C_GetSpellCooldown.
        [StructLayout(LayoutKind.Sequential)]
        private struct SpellCooldownEntry
        {
            public uint dword0; // 0
            public IntPtr Next; // 4
            public uint SpellId; // 8
            public uint ItemId; // C
            public uint StartTime; // 10
            public uint SpellOrItemCooldownDuration; // 14
            public uint SpellCategoryId; // 18
            public uint CategoryCooldownStartTime; // 1C
            public uint CategoryCooldownDuration; // 20
            public byte HasCooldown; // 24
            public byte pad1, pad2, pad3;
            public uint GlobalCooldownStartTime; // 28
            public uint StartRecoveryCategoryId; // 2C
            public int GlobalCooldownDuration; // 30
        }

        #endregion

        #endregion

        private static SpellCollection InternalSpells = new SpellCollection();

        public static SpellCollection Spells
        {
            get
            {
                lock (UpdateLocker)
                {
                    return InternalSpells == null ? null : new SpellCollection(InternalSpells);
                }
            }
        }

        public static bool ClickRemoteLocation(WoWPoint location)
        {
            StyxWoW.ResetAfk();
            return HandleTerrainClick(new CTerrainClickEvent { Position = location, GUID = 0, Button = MouseButton.Left });
        }

        private static bool HandleTerrainClick(CTerrainClickEvent click)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool result;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory mem = StyxWoW.Memory.CreateAllocatedMemory(MarshalCache<CTerrainClickEvent>.Size))
                {
                    mem.Write(0, click);

                    executor.Clear();
                    executor.AddRandomLine("push {0}", mem.Address);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.Spell_C__HandleTerrainClick);
                    executor.AddRandomLine("add esp, 4");
                    executor.AddRandomLine("test al, al");
                    executor.AddRandomLine("jnz @Return");
                    executor.AddRandomLine("push {0}", mem.Address);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.Spell_C_HandleSpriteClick);
                    executor.AddRandomLine("add esp, 4");
                    executor.AddRandomLine("@Return:");
                    executor.AddRandomLine("retn");
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        result = executor.Memory.Read<byte>(executor.ReturnPointer) != 0;
                    }
                }
            }

            return result;
        }

        #region CastById

        public static bool CastSpellById(uint spellID)
        {
            return CastSpellById((int)spellID);
        }

        public static bool CastSpellById(int spellID)
        {
            return CastSpellById(spellID, 0UL);
        }

        public static bool CastSpellById(int spellID, WoWUnit target)
        {
            return CastSpellById(spellID, target != null ? target.Guid : 0UL);
        }

        public static bool CastSpellById(int spellID, ulong targetGuid)
        {
            using (StyxWoW.Memory.AcquireFrame(true))
            {
                int overrideSpellId = GetSpellOverrideId(spellID);
                bool isPetAction = false;
                int knownSpellIndex = GetKnownSpellIndex(overrideSpellId, false);
                if (knownSpellIndex < 0)
                {
                    knownSpellIndex = GetKnownSpellIndex(overrideSpellId, true);
                    if (knownSpellIndex < 0)
                    {
                        return Spell_C__CastSpell(overrideSpellId, null, targetGuid, 0, 0, false, false);
                    }

                    isPetAction = true;
                }

                Spell_C__CastSpell2(knownSpellIndex, isPetAction, targetGuid, false);
                return true;
            }
        }

        private static int GetKnownSpellIndex(int overrideSpellId, bool isPetAction)
        {
            // 5.4.8 emulates this one through StyxWoW.Memory.Call<int> (see rule 18).
            return StyxWoW.Memory.CallInjected<int>(StyxWoW.Offsets.Functions.GetKnownSpellIndex, CallingConvention.Cdecl, overrideSpellId, isPetAction ? 1 : 0);
        }

        private static bool Spell_C__CastSpell(int spellId, WoWItem item, ulong targetGuid, int unk1, int unk2, bool bool1, bool bool2)
        {
            StyxWoW.ResetAfk();
            return StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.Spell_C__CastSpell, CallingConvention.Cdecl,
                spellId,
                (item == null || !item.IsValid) ? IntPtr.Zero : item.BaseAddress,
                targetGuid,
                unk1,
                unk2,
                bool1 ? 1 : 0,
                bool2 ? 1 : 0) != 0;
        }

        private static int GetSpellOverrideId(int spellId)
        {
            // 5.4.8 emulates this one through StyxWoW.Memory.Call<int> (see rule 18).
            return StyxWoW.Memory.CallInjected<int>(StyxWoW.Offsets.Functions.GetSpellOverrideId, CallingConvention.Cdecl, spellId);
        }

        private static void Spell_C__CastSpell2(int knownSpellIndex, bool isPetAction, ulong targetGuid, bool unk)
        {
            StyxWoW.ResetAfk();
            StyxWoW.Memory.CallInjected(StyxWoW.Offsets.Functions.Spell_C__CastSpell2, CallingConvention.Cdecl,
                knownSpellIndex,
                isPetAction ? 1 : 0,
                targetGuid,
                unk ? 1 : 0);
        }

        #endregion
    }
}
