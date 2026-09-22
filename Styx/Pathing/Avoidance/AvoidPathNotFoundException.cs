using System;

namespace Styx.Pathing.Avoidance
{
    public class AvoidPathNotFoundException : Exception
    {
        public AvoidPathNotFoundException(string message)
            : base(message)
        {
        }
    }
}
