namespace Styx.Pathing
{
    public static class MoveResultExtensions
    {
        public static bool IsSuccessful(this MoveResult moveResult)
        {
            return moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
        }
    }
}
