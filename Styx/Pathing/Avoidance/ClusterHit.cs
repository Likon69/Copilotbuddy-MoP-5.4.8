using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class ClusterHit
    {
        public ClusterHit(AvoidCluster cluster, Vector3 point, float hitAngle)
        {
            Point = point;
            Cluster = cluster;
            HitAngle = hitAngle;
        }

        public AvoidCluster Cluster { get; private set; }
        public Vector3 Point { get; private set; }
        public float HitAngle { get; private set; }
    }
}
