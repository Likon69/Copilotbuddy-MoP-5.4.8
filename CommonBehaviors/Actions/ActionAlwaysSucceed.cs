using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionAlwaysSucceed : Styx.TreeSharp.Action
    {
        protected override RunStatus Run(object context)
        {
            return RunStatus.Success;
        }
    }
}
