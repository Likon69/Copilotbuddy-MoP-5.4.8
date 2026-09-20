using System;

namespace Styx.Pathing
{
    public abstract class NavigationProvider : INavigationProvider
    {
        bool INavigationProvider.CanNavigateFully(WoWPoint from, WoWPoint to, int maxHops)
        {
            return CanNavigateFully(from, to);
        }

        public abstract MoveResult MoveTo(WoWPoint location);

        public abstract float PathPrecision { get; set; }

        public abstract WoWPoint[] GeneratePath(WoWPoint from, WoWPoint to);

        public abstract bool AtLocation(WoWPoint point1, WoWPoint point2);

        public bool IsCurrent
        {
            get { return Navigator.NavigationProvider == this; }
        }

        private IStuckHandler _stuckHandler;

        public virtual IStuckHandler StuckHandler
        {
            get { return _stuckHandler; }
            set
            {
                if (ReferenceEquals(value, _stuckHandler))
                {
                    return;
                }

                if (IsCurrent)
                {
                    StuckHandler stuckHandler = value as StuckHandler;
                    if (stuckHandler != null)
                    {
                        stuckHandler.OnSetAsCurrent();
                    }

                    StuckHandler previousStuckHandler = _stuckHandler as StuckHandler;
                    if (previousStuckHandler != null)
                    {
                        previousStuckHandler.OnRemoveAsCurrent();
                    }
                }

                _stuckHandler = value;
            }
        }

        public virtual bool Clear()
        {
            return true;
        }

        public virtual bool CanNavigateWithin(WoWPoint from, WoWPoint to, float distanceTolerancy)
        {
            WoWPoint[] path = GeneratePath(from, to);
            return path != null && path.Length > 0 && path[path.Length - 1].DistanceSqr(to) < distanceTolerancy * distanceTolerancy;
        }

        public virtual bool CanNavigateFully(WoWPoint from, WoWPoint to)
        {
            WoWPoint[] path = GeneratePath(from, to);
            return path != null && path.Length > 0 && AtLocation(path[path.Length - 1], to);
        }

        public virtual float? PathDistance(WoWPoint from, WoWPoint to, float maxDistance = float.MaxValue)
        {
            WoWPoint[] path = GeneratePath(from, to);
            if (path.Length <= 0)
            {
                return null;
            }

            float distance = from.Distance(path[0]);
            distance += path[path.Length - 1].Distance(to);
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (distance > maxDistance)
                {
                    return maxDistance;
                }

                distance += path[i].Distance(path[i + 1]);
            }

            return distance;
        }

        public virtual void OnSetAsCurrent()
        {
            StuckHandler stuckHandler = StuckHandler as StuckHandler;
            if (stuckHandler != null)
            {
                stuckHandler.OnSetAsCurrent();
            }
        }

        public virtual void OnRemoveAsCurrent()
        {
            StuckHandler stuckHandler = StuckHandler as StuckHandler;
            if (stuckHandler != null)
            {
                stuckHandler.OnRemoveAsCurrent();
            }
        }
    }
}
