using System;

using Styx.CommonBot;
using Styx.CommonBot.CharacterManagement;
using Styx.CommonBot.Database;
using Styx.CommonBot.Routines;
using Styx.Plugins;
using Styx.WoWInternals;

namespace Styx
{
    public static class Pulsator
    {
        public static void Pulse(PulseFlags flags)
        {
            if ((flags & PulseFlags.Objects) != 0)
            {
                ObjectManager.Update();
                NpcCacher.Pulse();
                WoWMovement.Pulse();
            }

            if ((flags & PulseFlags.Lua) != 0)
            {
                Lua.Pulse();
            }

            if ((flags & PulseFlags.InfoPanel) != 0)
            {
                GameStats.Pulse();
            }

            if ((flags & PulseFlags.CharacterManager) != 0)
            {
                CharacterManager.Pulse();
            }

            if ((flags & PulseFlags.Looting) != 0)
            {
                LootTargeting.Instance.Pulse();
            }

            if ((flags & PulseFlags.Targeting) != 0)
            {
                Targeting.Instance.Pulse();
                HealTargeting.Instance.Pulse();
            }

            if ((flags & PulseFlags.BotEvents) != 0)
            {
                BotEvents.PulseEvents();
            }

            if ((flags & PulseFlags.Plugins) != 0)
            {
                PluginManager.Pulse();
            }

            if (RoutineManager.Current != null)
            {
                RoutineManager.Current.Pulse();
            }
        }
    }
}
