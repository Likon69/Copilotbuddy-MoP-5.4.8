using System;
using CommonBehaviors.Resources;
using Styx.Common;
using Styx.CommonBot.POI;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class ActionSetPoi : Styx.TreeSharp.Action
    {
        private readonly RetrieveBotPoiDelegate _getPoi;
        private readonly bool _overridePrevious;

        public ActionSetPoi(RetrieveBotPoiDelegate poiRetrievalFunc)
        {
            _getPoi = poiRetrievalFunc;
        }

        public ActionSetPoi(bool ignorePreviousPoi, RetrieveBotPoiDelegate poiRetrievalFunc)
            : this(poiRetrievalFunc)
        {
            _overridePrevious = ignorePreviousPoi;
        }

        protected override RunStatus Run(object context)
        {
            BotPoi poi = null;
            if (context != null && _getPoi == null && context is BotPoi)
            {
                poi = context as BotPoi;
            }
            else if (_getPoi != null)
            {
                try
                {
                    poi = _getPoi(context);
                }
                catch (Exception ex)
                {
                    Logging.Write(CommonBehaviorsResources.ActionSetPoi_Run_Error__ + ex);
                }
            }

            if (poi != null)
            {
                if (BotPoi.Current.Type != poi.Type)
                    BotPoi.Current = poi;
                else if (_overridePrevious)
                    BotPoi.Current = poi;
            }

            if (Parent is Selector)
                return RunStatus.Failure;

            return RunStatus.Success;
        }
    }
}
