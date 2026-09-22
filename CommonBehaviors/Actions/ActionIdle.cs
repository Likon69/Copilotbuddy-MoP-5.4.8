using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionIdle : ActionAlwaysSucceed
    {
        protected override RunStatus Run(object context)
        {
            StyxWoW.ResetAfk();
            InactivityDetector.Reset();
            return base.Run(context);
        }
    }
}
