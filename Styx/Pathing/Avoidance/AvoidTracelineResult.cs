using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class AvoidTracelineResult
    {
        public AvoidTracelineResult(int hits, Vector3 enterPoint, Vector3 exitPoint, Avoid avoid)
        {
            Hits = hits;
            Enter = enterPoint;
            Exit = exitPoint;
            Avoid = avoid;
        }

        public int Hits { get; private set; }
        public Vector3 Enter { get; private set; }
        public Vector3 Exit { get; private set; }
        public Avoid Avoid { get; private set; }
    }
}
