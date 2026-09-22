using System.Linq;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace Levelbot.Actions.General
{
    public class ActionMountVendor : Styx.TreeSharp.Action
    {
        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected override RunStatus Run(object context)
        {
            if (Mount.Mounts.FirstOrDefault(mount => mount.CreatureSpellId == 61425) != null
                && Mount.CanMount()
                && !Me.HasAura(61425)
                && !Me.IsIndoors
                && Me.Level >= 20)
            {
                if (BotPoi.Current.Type == PoiType.Sell || BotPoi.Current.Type == PoiType.Repair)
                {
                    if (Me.Mounted && !Me.HasAura(61425))
                    {
                        Mount.Dismount("Using mount vendor");
                        return RunStatus.Running;
                    }

                    if (Me.IsCasting)
                        return RunStatus.Running;

                    if (!Me.Mounted)
                    {
                        while (Me.IsMoving)
                            WoWMovement.MoveStop();

                        SpellManager.CastSpellById(61425);
                        return RunStatus.Running;
                    }

                    return RunStatus.Success;
                }
            }

            return RunStatus.Failure;
        }
    }
}
