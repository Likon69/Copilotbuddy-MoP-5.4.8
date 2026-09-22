using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;

namespace Styx.CommonBot.CharacterManagement
{
    public class TalentSelector
    {
        private Coroutine _coroutine;

        internal TalentSelector()
        {
            this.CheckInterval = 5000;
        }

        public int CheckInterval { get; set; }

        private void LogQuiet(Color color, string message, params object[] args)
        {
            Logging.WriteQuiet(color, "[Talented] " + message, args);
        }

        private void LogQuiet(Color color, string message)
        {
            Logging.WriteQuiet(color, "[Talented] " + message);
        }

        private void Log(Color color, string message, params object[] args)
        {
            Logging.Write(color, "[Talented] " + message, args);
        }

        private void Log(Color color, string message)
        {
            Logging.Write(color, "[Talented] " + message);
        }

        private void DisposeCoroutine()
        {
            if (this._coroutine == null)
            {
                return;
            }
            this._coroutine.Dispose();
            this._coroutine = null;
        }

        private TalentPlacement GetSelectedTalentForTier(int tier)
        {
            for (int i = 0; i < 3; i++)
            {
                List<string> returnValues = Lua.GetReturnValues(string.Format("return GetTalentInfo({0})", tier * 3 + i + 1));
                string name = returnValues[0];
                if (Lua.ParseLuaValue<bool>(returnValues[4]))
                {
                    return new TalentPlacement(tier, i, name);
                }
            }
            return null;
        }

        private TalentPlacementSet GetCurrentTalents()
        {
            List<TalentPlacement> list = new List<TalentPlacement>();
            for (int i = 0; i < 6; i++)
            {
                TalentPlacement talentPlacement = this.GetSelectedTalentForTier(i);
                if (talentPlacement != null)
                {
                    list.Add(talentPlacement);
                }
            }
            return new TalentPlacementSet(list);
        }

        private int GetAvailableTalentTierCount()
        {
            int[] array;
            if (StyxWoW.Me.Class == WoWClass.DeathKnight)
            {
                array = new int[] { 56, 57, 58, 60, 75, 90 };
            }
            else
            {
                array = new int[] { 15, 30, 45, 60, 75, 90 };
            }
            int level = StyxWoW.Me.Level;
            int num = 0;
            while (num < array.Length && array[num] <= level)
            {
                num++;
            }
            return num;
        }

        private async Task SelectTalentsAsync()
        {
            ClassProfile classProfile = CharacterManager.CurrentClassProfile;
            if (StyxWoW.Me.Class != classProfile.Class)
            {
                this.Log(Colors.Red, "Current class is a {0}, but the current class profile is for {1}!", StyxWoW.Me.Class, classProfile.Class);
                return;
            }
            WoWSpec targetSpec = classProfile.GetSpec();
            WoWSpec currentSpec = StyxWoW.Me.Specialization;
            if (currentSpec == WoWSpec.None)
            {
                this.LogQuiet(Colors.LawnGreen, "Setting class specialization to {0}", targetSpec);
                Lua.DoString(string.Format("SetSpecialization({0})", classProfile.Specialization));
                if (!await Coroutine.Wait(5000, () => StyxWoW.Me.Specialization == targetSpec))
                {
                    this.Log(Colors.Red, "Specialization was not properly set - retrying later");
                    return;
                }
                await Coroutine.Sleep(1500);
            }
            else if (currentSpec != targetSpec)
            {
                this.Log(Colors.Red, "Current specialization is wrong! It is {0}, and the class profile expects {1}!", currentSpec, targetSpec);
                return;
            }

            TalentPlacementSet currentTalents = this.GetCurrentTalents();
            TalentPlacementSet desiredTalents = classProfile.Talents;
            int availableTiers = this.GetAvailableTalentTierCount();

            for (int tier = 0; tier < availableTiers; tier++)
            {
                if (currentTalents[tier] != null)
                    continue;
                TalentPlacement desired = desiredTalents[tier];
                if (desired == null)
                    continue;

                this.LogQuiet(Colors.LawnGreen, "Learning talent {0} [Tier: {1} Index: {2}]", desired.Name, desired.Tier + 1, desired.Index + 1);
                int talentIndex = tier * 3 + desired.Index + 1;
                Lua.DoString(string.Format("LearnTalent({0})", talentIndex));
                int selectedTier = tier;
                bool learned = await Coroutine.Wait(5000, () =>
                {
                    TalentPlacement check = this.GetSelectedTalentForTier(selectedTier);
                    return check != null && check.Index == desired.Index;
                });
                if (!learned)
                {
                    this.Log(Colors.Red, "Talent was not learned properly - retrying later");
                    return;
                }
                await Coroutine.Sleep(500);
            }
        }

        private async Task RunAsync()
        {
            while (true)
            {
                Stopwatch timer = Stopwatch.StartNew();
                await this.SelectTalentsAsync();
                int remaining = this.CheckInterval - (int)timer.ElapsedMilliseconds;
                if (remaining > 0)
                    await Coroutine.Sleep(remaining);
            }
        }

        internal void Pulse()
        {
            if (!CharacterSettings.Instance.SelectTalents)
            {
                this.DisposeCoroutine();
                return;
            }
            if (CharacterManager.CurrentClassProfile == null)
            {
                this.Log(Colors.Red, "No class profile is loaded; cannot select talents");
                this.DisposeCoroutine();
                return;
            }
            if (StyxWoW.Me.Level >= 10 && !StyxWoW.Me.Combat && StyxWoW.Me.IsAlive)
            {
                if (this._coroutine == null || this._coroutine.IsFinished)
                {
                    if (this._coroutine != null)
                    {
                        this.Log(Colors.Red, "Coroutine finished? - restarting");
                    }
                    this._coroutine = new Coroutine(this.RunAsync);
                }
                try
                {
                    using (StyxWoW.Memory.AcquireFrame())
                    {
                        this._coroutine.Resume();
                    }
                }
                catch (CoroutineException ex)
                {
                    this.Log(Colors.Red, "Exception was thrown in coroutine!\r\n{0}", new object[] { ex.ToString() });
                }
                return;
            }
        }

        private void OnBotStopped(EventArgs args)
        {
            this.DisposeCoroutine();
        }

        internal void Initialize()
        {
            BotEvents.OnBotStopped += this.OnBotStopped;
        }

        internal void Shutdown()
        {
            BotEvents.OnBotStopped -= this.OnBotStopped;
        }
    }
}
