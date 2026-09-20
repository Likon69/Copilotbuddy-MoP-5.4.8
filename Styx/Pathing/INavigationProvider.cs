using System;

namespace Styx.Pathing
{
    /// <summary>Interface for navigation provider. </summary>
    /// <remarks>Created 12/1/2010.</remarks>
    [Obsolete("If deriving from this class, derive from NavigationProvider instead. If using this class, use wrapper functions in Navigator instead.")]
    public interface INavigationProvider
    {
        /// <summary>Moves to the specified coordinate</summary>
        /// <param name="location">The location.</param>
        /// <returns>.</returns>
        MoveResult MoveTo(WoWPoint location);

        /// <summary>Gets or sets the path precision.</summary>
        /// <value>The path precision.</value>
        float PathPrecision { get; set; }

        /// <summary>Clears the current path to its blank/initial state.</summary>
        /// <returns>true if it succeeds, false if it fails.</returns>
        bool Clear();

        /// <summary>Generates a path. </summary>
        /// <param name="from">Source for the path.</param>
        /// <param name="to">to.</param>
        /// <returns>The path.</returns>
        WoWPoint[] GeneratePath(WoWPoint from, WoWPoint to);

        /// <summary>Queries if we can navigate fully 'from'. </summary>
        /// <param name="from">Source for the path.</param>
        /// <param name="to">to.</param>
        /// <param name="maxHops"></param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        bool CanNavigateFully(WoWPoint from, WoWPoint to, int maxHops);

        /// <summary>Gets or sets the stuck handler for this navigation provider.</summary>
        /// <value>The stuck handler.</value>
        IStuckHandler StuckHandler { get; set; }

        bool AtLocation(WoWPoint point1, WoWPoint point2);
    }
}
