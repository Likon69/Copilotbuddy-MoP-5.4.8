using Tripper.Tools.Math;

namespace Styx.Pathing.Avoidance
{
    public class AvoidPathResult
    {
        public AvoidPathResult(PathResult result, Vector3[] path)
        {
            Result = result;
            Path = path;
            Index = 0;
        }

        public readonly PathResult Result;
        public readonly Vector3[] Path;
        public int Index;
    }
}
