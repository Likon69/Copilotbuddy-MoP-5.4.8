using System;
using Styx.CommonBot.POI;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionClearPoi : Styx.TreeSharp.Action
    {
        private string _reason;

        public ActionClearPoi(string reason)
        {
            _reason = reason;
        }

        public ActionClearPoi()
            : this(string.Empty)
        {
        }

        protected override RunStatus Run(object context)
        {
            if (BotPoi.Current.Type != PoiType.None)
                BotPoi.Clear(_reason);

            return RunStatus.Success;
        }
    }
}
