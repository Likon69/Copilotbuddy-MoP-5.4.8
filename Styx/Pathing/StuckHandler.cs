using System;

namespace Styx.Pathing
{
    public abstract class StuckHandler : IStuckHandler
    {
        public abstract bool IsStuck();

        public abstract void Unstick();

        public virtual void Reset()
        {
        }

        public virtual void OnSetAsCurrent()
        {
        }

        public virtual void OnRemoveAsCurrent()
        {
        }
    }
}
