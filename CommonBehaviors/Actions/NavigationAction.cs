using Styx;
using Styx.Pathing;
using Styx.TreeSharp;

namespace CommonBehaviors.Actions
{
    public class NavigationAction : Action
    {
        private readonly GetPointDelegate _getPointDel;
        private readonly NavTypeDelegate _navTypeDel;

        public NavigationAction(WoWPoint point)
            : this(ctx => point)
        {
        }

        public NavigationAction(NavTypeDelegate navType)
        {
            _navTypeDel = navType;
        }

        public NavigationAction(GetPointDelegate getPointDel)
        {
            _getPointDel = getPointDel;
        }

        public NavigationAction(GetPointDelegate getPointDel = null, NavTypeDelegate navTypeDel = null)
        {
            _getPointDel = getPointDel;
            _navTypeDel = navTypeDel;
        }

        protected override RunStatus Run(object context)
        {
            WoWPoint point = WoWPoint.Zero;
            float height = 40f;
            if (_getPointDel == null)
            {
                if (context == null)
                    return RunStatus.Failure;

                if (context is WoWPoint)
                {
                    point = (WoWPoint)context;
                }
                else
                {
                    NavigationInfo navInfo = context as NavigationInfo;
                    if (navInfo == null)
                        return RunStatus.Failure;

                    point = navInfo.Destination;
                    height = navInfo.Height;
                }
            }
            else
            {
                point = _getPointDel(context);
            }

            if (_navTypeDel != null && _navTypeDel(context) != NavType.Run)
            {
                Flightor.MoveTo(point, height, true);
                return RunStatus.Success;
            }

            return Navigator.GetRunStatusFromMoveResult(Navigator.MoveTo(point));
        }
    }
}
