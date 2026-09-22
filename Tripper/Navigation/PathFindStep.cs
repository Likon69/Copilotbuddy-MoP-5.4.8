namespace Tripper.Navigation
{
    public enum PathFindStep
    {
        None,
        FindStartPoly,
        FindEndPoly,
        InitPathFind,
        UpdatePathFind,
        FinalizePathFind,
        SnapPartialPathToEnd,
        FindStraightPath
    }
}
