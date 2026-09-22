using System;
using Levelbot.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;

namespace Levelbot.Actions.Death
{
    public class ActionReleaseFromCorpse : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            if (!StyxWoW.IsInGame)
                return RunStatus.Running;

            GameStats.Died();
            Logging.Write(LevelbotResources.ActionReleaseFromCorpse_Run_I_died_);
            Navigator.Clear();
            ReleaseSpirit();

            if (StyxWoW.Me.IsDead)
                return RunStatus.Running;

            if (StyxWoW.Me.IsGhost)
                return RunStatus.Success;

            return RunStatus.Failure;
        }

        private static void ReleaseSpirit()
        {
            WoWMovement.MoveStop();

            if (Lua.GetReturnVal<bool>("return HasSoulstone()", 0U))
            {
                Lua.DoString("UseSoulstone()", "WoW.lua");

                int tickCount = Environment.TickCount;
                while (!StyxWoW.Me.IsAlive && Environment.TickCount - tickCount < 7500)
                    StyxWoW.Sleep(100);

                if (StyxWoW.Me.IsAlive)
                    return;
            }

            if (StyxWoW.Me.IsDead && !StyxWoW.Me.IsGhost)
            {
                Lua.DoString("RepopMe()", "WoW.lua");

                int tickCount2 = Environment.TickCount;
                while (Environment.TickCount - tickCount2 < 5000 && ((StyxWoW.Me.IsDead && !StyxWoW.Me.IsGhost) || !StyxWoW.IsInGame))
                    StyxWoW.Sleep(200);
            }
        }
    }
}
