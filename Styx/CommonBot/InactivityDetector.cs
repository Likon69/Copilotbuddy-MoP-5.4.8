using System;
using System.Linq;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.Resources;
using Styx.WoWInternals;

namespace Styx.CommonBot
{
    /// <summary>
    /// Handles logging out when the player remains inactive for the configured period.
    /// </summary>
    public static class InactivityDetector
    {
        private static WaitTimer _inactivityTimer;
        private static WoWPoint _lastLocation;
        private static int _pulseCount;

        static InactivityDetector()
        {
            BotEvents.OnPulse += BotEvents_OnPulse;
            BotEvents.OnBotStarted += args => Reset();
        }

        public static void Initialize()
        {
            _inactivityTimer = new WaitTimer(new TimeSpan(0, GlobalSettings.Instance.LogoutInactivityTimer, 0));
            _lastLocation = StyxWoW.Me.Location;
            _inactivityTimer.Reset();
        }

        public static void Reset()
        {
            _inactivityTimer.Reset();
        }

        public static TimeSpan TimeUntilLogout
        {
            get { return _inactivityTimer.TimeLeft; }
        }

        public static DateTime LogoutTime
        {
            get { return _inactivityTimer.EndTime; }
        }

        private static void ForceLogout(bool useForceQuit)
        {
            if (!useForceQuit)
                Lua.DoString("Logout()", "WoW.lua");
            else
                Lua.DoString("ForceQuit()", "WoW.lua");
            TreeRoot.Shutdown(12, false);
        }

        private static void BotEvents_OnPulse(BotEvents.PulseEventArgs args)
        {
            _pulseCount++;
            if (_pulseCount % 10 != 0)
                return;

            if (GlobalSettings.Instance.LogoutForInactivity && TreeRoot.IsRunning)
            {
                using (StyxWoW.Memory.AcquireFrame(true))
                {
                    if (_lastLocation.DistanceSqr(StyxWoW.Me.Location) > 225f)
                    {
                        _inactivityTimer.Reset();
                        _lastLocation = StyxWoW.Me.Location;
                    }
                    else if (StyxWoW.Me.HasAura("Resurrection Sickness"))
                    {
                        _inactivityTimer.Reset();
                    }
                    else if (Battlegrounds.BattlegroundStatuses.Any(status => status != BattlegroundStatus.None))
                    {
                        _inactivityTimer.Reset();
                    }
                    else
                    {
                        if (StyxWoW.Me.LfgMode(LfgCategory.LookingForDungeon) != LfgState.InQueue &&
                            StyxWoW.Me.LfgMode(LfgCategory.Scenario) != LfgState.InQueue &&
                            StyxWoW.Me.LfgMode(LfgCategory.RaidFinder) != LfgState.InQueue)
                        {
                            if (_inactivityTimer.IsFinished)
                            {
                                Logging.Write(StyxResources.INACTIVITY_TIMER_TRIPPED__LOGGING_OUT_);
                                ForceLogout(GlobalSettings.Instance.LogoutInactivityUseForceQuit);
                            }
                            return;
                        }
                        _inactivityTimer.Reset();
                    }
                }
                return;
            }

            _inactivityTimer.Reset();
        }
    }
}
