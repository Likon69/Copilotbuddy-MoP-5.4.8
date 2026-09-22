using Styx;
using Styx.TreeSharp;

namespace Levelbot.Actions.Death
{
    public class ActionSuceedIfDeadOrGhost : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            if (!StyxWoW.Me.IsDead && !StyxWoW.Me.IsGhost)
                return RunStatus.Failure;

            return RunStatus.Success;
        }
    }
}
