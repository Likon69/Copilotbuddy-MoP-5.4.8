using Tripper.Tools.Math;

namespace Bots.DungeonBuddy.Avoidance;

public class AvoidPathResult
{
	public readonly Vector3[] Path;

	public readonly PathResult Result;

	public int Index;

	public AvoidPathResult(PathResult result, Vector3[] path)
	{
		Result = result;
		Path = path;
		Index = 0;
	}
}
