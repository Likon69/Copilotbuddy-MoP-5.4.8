using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionAlwaysFail : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            return RunStatus.Failure;
        }
    }
}
