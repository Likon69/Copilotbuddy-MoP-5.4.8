namespace Styx.TreeSharp
{
    /// <summary>
    ///   Values that can be returned from composites and the like.
    /// </summary>
    public enum RunStatus
    {
        /// <summary>
        /// Returned by a behavior when it has thrown an exception or failed to run.
        /// </summary>
        Failure,
        /// <summary>
        /// Returned by a behavior when it's running or child nodes is running.
        /// </summary>
        Running,
        /// <summary>
        /// Returned by a behavior when it has succeeded.
        /// </summary>
        Success,
    }
}
