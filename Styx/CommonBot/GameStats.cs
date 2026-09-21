using System;
using Styx.Common;
using Styx.WoWInternals;

namespace Styx.CommonBot
{
    public static class GameStats
    {
        public delegate void InfoPanelUpdatedDelegate();
        public static event InfoPanelUpdatedDelegate OnInfoPanelUpdated;

        private static uint _addXP;
        private static DateTime _measureStart;
        private static uint _startNeededXP;
        private static uint _startXP;
        private static uint _startHonor;
        private static long _tickCount;
        private static readonly TimedRecordKeeper<long> _tickRecordKeeper = new TimedRecordKeeper<long>(1000, 5000);

        static GameStats()
        {
            BotEvents.OnBotStarted += BotEvents_OnBotStart;
            BotEvents.OnPulse += Pulse_TickCounter;
            BotEvents.Player.OnMobKilled += Player_KilledMob;
            BotEvents.Player.OnMobLooted += Player_OnMobLooted;
            BotEvents.Player.OnLevelUp += Player_LevelUp;
        }

        public static uint MobsKilled { get; private set; }
        public static uint Loots { get; private set; }
        public static uint Deaths { get; private set; }
        public static uint BGsWon { get; private set; }
        public static uint BGsLost { get; private set; }
        public static float XPPerHour { get; private set; }
        public static float MobsPerHour { get; private set; }
        public static float LootsPerHour { get; private set; }
        public static float DeathsPerHour { get; private set; }
        public static float BGsWonPerHour { get; private set; }
        public static float BGsLostPerHour { get; private set; }
        public static uint HonorGained { get; private set; }
        public static float HonorPerHour { get; private set; }
        public static TimeSpan TimeToLevel { get; private set; }
        public static float TicksPerSecond { get; private set; }
        public static uint BGsCompleted { get { return BGsWon + BGsLost; } }
        public static float BGsPerHour { get { return BGsWonPerHour + BGsLostPerHour; } }

        public static bool IsMeasuring { get; private set; }

        private static uint HonorPoints
        {
            get
            {
                var currency = WoWCurrency.GetCurrencyByType(WoWCurrencyType.HonorPoints);
                return currency != null ? currency.Amount : 0;
            }
        }

        internal static void Pulse()
        {
            if (_startHonor != HonorPoints)
            {
                if (HonorPoints > _startHonor)
                {
                    HonorGained += HonorPoints - _startHonor;
                }

                _startHonor = HonorPoints;
            }

            TimeSpan elapsedTime = GetElapsedMeasureTime();
            XPPerHour = CalculatePerHour(StyxWoW.Me.Experience + _addXP - _startXP, elapsedTime);
            MobsPerHour = CalculatePerHour(MobsKilled, elapsedTime);
            LootsPerHour = CalculatePerHour(Loots, elapsedTime);
            DeathsPerHour = CalculatePerHour(Deaths, elapsedTime);
            BGsWonPerHour = CalculatePerHour(BGsWon, elapsedTime);
            BGsLostPerHour = CalculatePerHour(BGsLost, elapsedTime);
            HonorPerHour = CalculatePerHour(HonorGained, elapsedTime);
            TimeToLevel = TimeSpan.FromHours(XPPerHour < 1f ? 0f : (StyxWoW.Me.NextLevelExperience - StyxWoW.Me.Experience) / XPPerHour);
            TicksPerSecond = (float)(CalculateTicksPerHour(_tickCount, _tickRecordKeeper) / 3600.0);

            if (OnInfoPanelUpdated != null)
                OnInfoPanelUpdated();
        }

        private static float CalculatePerHour(uint val, TimeSpan elapsedTime)
        {
            return elapsedTime.TotalHours > 0 ? val / (float)elapsedTime.TotalHours : 0;
        }

        private static double CalculateTicksPerHour(long tickCount, TimedRecordKeeper<long> recordKeeper)
        {
            recordKeeper.AddRecord(tickCount);

            long record;
            TimeSpan elapsed;
            if (!recordKeeper.GetRecord(TimeSpan.FromMilliseconds(recordKeeper.StoreTimeMilliseconds), out record, out elapsed))
                return 0.0;

            return elapsed.TotalHours > 0.0 ? (tickCount - record) / elapsed.TotalHours : 0.0;
        }

        private static void BotEvents_OnBotStart(EventArgs args)
        {
            Reset();
            StartMeasuring();
        }

        private static void Pulse_TickCounter(BotEvents.PulseEventArgs args)
        {
            _tickCount += 1L;
        }

        private static void Player_LevelUp(BotEvents.Player.LevelUpEventArgs e)
        {
            _addXP += _startNeededXP - _startXP;
            _startNeededXP = StyxWoW.Me.NextLevelExperience;
            _startXP = 0;
        }

        private static void Player_KilledMob(BotEvents.Player.MobKilledEventArgs args)
        {
            KilledMob();
        }

        private static void Player_OnMobLooted(BotEvents.Player.MobLootedEventArgs args)
        {
            LootedMob();
        }

        private static TimeSpan GetElapsedMeasureTime()
        {
            return DateTime.Now.Subtract(_measureStart);
        }

        #region Manager functions

        public static void Reset()
        {
            IsMeasuring = false;
            _startXP = StyxWoW.Me.Experience;
            _startNeededXP = StyxWoW.Me.NextLevelExperience;
            _startHonor = HonorPoints;
            _addXP = 0;
            _tickCount = 0;
            HonorGained = 0;
            MobsKilled = 0;
            Loots = 0;
            Deaths = 0;
            BGsWon = 0;
            BGsLost = 0;
            HonorPerHour = 0;
            XPPerHour = 0;
            MobsPerHour = 0;
            LootsPerHour = 0;
            DeathsPerHour = 0;
            BGsWonPerHour = 0;
            BGsLostPerHour = 0;
        }

        public static void StartMeasuring()
        {
            _measureStart = DateTime.Now;
            IsMeasuring = true;
        }

        public static void StopMeasuring()
        {
            IsMeasuring = false;
        }

        public static void Died()
        {
            Deaths++;
        }

        public static void KilledMob()
        {
            MobsKilled++;
            LootTargeting.Instance.Pulse();
        }

        public static void LootedMob()
        {
            Loots++;
        }

        public static void BGWon()
        {
            BGsWon++;
        }

        public static void BGLost()
        {
            BGsLost++;
        }

        #endregion
    }
}
