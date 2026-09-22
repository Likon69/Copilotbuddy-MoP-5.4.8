using CommonBehaviors.Resources;
using Styx;
using Styx.Common;
using Styx.CommonBot.POI;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace CommonBehaviors.Actions
{
    public class ActionMoveToPoi : NavigationAction
    {
        private WoWPoint _lastLocation;
        private ulong _lastGuid;

        public ActionMoveToPoi()
            : base((NavTypeDelegate)(ctx => BotPoi.Current.NavType))
        {
        }

        protected override RunStatus Run(object context)
        {
            BotPoi poi = BotPoi.Current;
            if (poi.Location == WoWPoint.Zero)
            {
                Logging.Write(CommonBehaviorsResources.ActionMoveToPoi_Run_I_don_t_want_to_move_to__0__0__0_);
                return RunStatus.Failure;
            }

            WoWObject asObject = poi.AsObject;
            WoWUnit unit = asObject != null ? asObject.ToUnit() : null;
            if (unit != null)
            {
                ulong guid = unit.Guid;
                WoWPoint location = unit.Location;
                if (unit.IsMoving)
                {
                    if (_lastLocation == WoWPoint.Empty || _lastGuid != guid || _lastLocation.DistanceSqr(StyxWoW.Me.Location) < 900f)
                    {
                        _lastGuid = guid;
                        _lastLocation = location;
                    }
                }
                else if (_lastGuid != guid || _lastLocation != location)
                {
                    _lastGuid = guid;
                    _lastLocation = location;
                }
            }
            else
            {
                _lastGuid = 0UL;
                _lastLocation = poi.Location;
            }

            Logging.Write(CommonBehaviorsResources.ActionMoveToPoi_Run_Moving_to_ + BotPoi.Current);

            float height = 40f;
            switch (poi.Type)
            {
                case PoiType.Hotspot:
                case PoiType.Kill:
                case PoiType.Loot:
                case PoiType.Skin:
                case PoiType.Harvest:
                case PoiType.Corpse:
                    height = 15f;
                    break;
            }

            return base.Run(new NavigationInfo(_lastLocation, height));
        }
    }
}
