using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Avoidance;

public class LineClusterTangentPoints : LineCircleTangentPoints
{
	public readonly Avoid LeftAvoid;

	public readonly Avoid RightAvoid;

	public LineClusterTangentPoints(Vector3 rightPoint, Vector3 leftPoint, Avoid rightAvoid, Avoid leftAvoid)
		: base(rightPoint, leftPoint)
	{
		RightAvoid = rightAvoid;
		LeftAvoid = leftAvoid;
	}
}
