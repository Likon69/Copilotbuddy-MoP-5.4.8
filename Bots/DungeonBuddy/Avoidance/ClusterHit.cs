using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Avoidance;

public class ClusterHit
{
	public AvoidCluster Cluster { get; private set; }

	public Avoid Avoid { get; private set; }

	public Vector3 Point { get; private set; }

	public float HitAngle { get; private set; }

	public ClusterHit(AvoidCluster cluster, Avoid avoid, Vector3 point, float hitAngle)
	{
		Point = point;
		Cluster = cluster;
		Avoid = avoid;
		HitAngle = hitAngle;
	}
}
