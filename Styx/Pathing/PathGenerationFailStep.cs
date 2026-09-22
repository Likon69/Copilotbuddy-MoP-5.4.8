namespace Styx.Pathing
{
    public enum PathGenerationFailStep
    {
        None = -1,
        Success,
        FindStartNode,
        FindEndNode,
        FindPath,
        Mesh
    }
}
