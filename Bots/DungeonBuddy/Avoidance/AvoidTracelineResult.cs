using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidTracelineResult
{
	public int Hits { get; private set; }

	public Vector3 Enter { get; private set; }

	public Vector3 Exit { get; private set; }

	public Avoid Avoid { get; private set; }

	public AvoidTracelineResult(int hits, Vector3 enterPoint, Vector3 exitPoint, Avoid avoid)
	{
		Hits = hits;
		Enter = enterPoint;
		Exit = exitPoint;
		Avoid = avoid;
	}
}
