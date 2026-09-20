using System;
using System.Collections.Generic;

using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.MeshMisc;
using Tripper.Tools.Math;

namespace Styx.Pathing
{
    public static class Navigator
    {
        static Navigator()
        {
            FlightorLandSystem.InitializeLandLocations();
        }

        internal static void Init()
        {
            NavigationProvider = new MeshNavigator();
        }

        public static FlightorLandSystem FlightorLandSystem { get; set; } = new FlightorLandSystem();

        private static INavigationProvider _navigationProvider;

        public static INavigationProvider NavigationProvider
        {
            get { return _navigationProvider; }
            set
            {
                if (ReferenceEquals(_navigationProvider, value))
                {
                    return;
                }

                NavigationProvider newProvider = value as NavigationProvider;
                if (newProvider != null)
                {
                    newProvider.OnSetAsCurrent();
                }

                NavigationProvider oldProvider = _navigationProvider as NavigationProvider;
                if (oldProvider != null)
                {
                    oldProvider.OnRemoveAsCurrent();
                }

                INavigationProvider previous = _navigationProvider;
                _navigationProvider = value;
                OnNavigationProviderChanged?.Invoke(null, new NavigationProviderChangedEventArgs<INavigationProvider>(previous, value));
            }
        }

        private static ITerrainHeightProvider _heightProvider = new DefaultHeightProvider();

        public static ITerrainHeightProvider HeightProvider
        {
            get { return _heightProvider; }
            set
            {
                if (!ReferenceEquals(_navigationProvider, value))
                {
                    ITerrainHeightProvider previous = _heightProvider;
                    _heightProvider = value;
                    OnHeightProviderChanged?.Invoke(null, new NavigationProviderChangedEventArgs<ITerrainHeightProvider>(previous, value));
                }
            }
        }

        private static IPlayerMover _playerMover = new ClickToMoveMover();

        public static IPlayerMover PlayerMover
        {
            get { return _playerMover; }
            set
            {
                if (value != _playerMover)
                {
                    IPlayerMover previous = _playerMover;
                    _playerMover = value;
                    OnPlayerMoverChanged?.Invoke(null, new NavigationProviderChangedEventArgs<IPlayerMover>(previous, value));
                }
            }
        }

        /// <summary>Gets the path precision.</summary>
        /// <value>The path precision.</value>
        public static float PathPrecision
        {
            get
            {
                if (NavigationProvider == null)
                    throw new InvalidOperationException("NavigationProvider cannot be null!");
                return NavigationProvider.PathPrecision;
            }
            set
            {
                if (NavigationProvider == null)
                    throw new InvalidOperationException("NavigationProvider cannot be null!");
                NavigationProvider.PathPrecision = value;
            }
        }

        /// <summary> Event queue for all listeners interested in OnNavigationProviderChanged events. </summary>
        public static event EventHandler<NavigationProviderChangedEventArgs<INavigationProvider>> OnNavigationProviderChanged;

        /// <summary> Event queue for all listeners interested in OnPlayerMoverChanged events. </summary>
        public static event EventHandler<NavigationProviderChangedEventArgs<IPlayerMover>> OnPlayerMoverChanged;

        /// <summary> Event queue for all listeners interested in OnHeightProviderChanged events. </summary>
        public static event EventHandler<NavigationProviderChangedEventArgs<ITerrainHeightProvider>> OnHeightProviderChanged;

        /// <summary>Gets the navigation provider as. </summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <returns>The navigation provider as T</returns>
        public static T GetNavigationProviderAs<T>() where T : class, INavigationProvider
        {
            return NavigationProvider as T;
        }

        /// <summary>Move to. </summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when the navigation provider is null.</exception>
        /// <param name="location">The location.</param>
        /// <returns>.</returns>
        public static MoveResult MoveTo(WoWPoint location)
        {
            if (NavigationProvider == null)
                throw new InvalidOperationException("NavigationProvider cannot be null!");

            return NavigationProvider.MoveTo(location);
        }

        /// <summary>Clears the current path to its blank/initial state. </summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when the requested operation is invalid.</exception>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public static bool Clear()
        {
            if (NavigationProvider == null)
                throw new InvalidOperationException("NavigationProvider cannot be null!");

            return NavigationProvider.Clear();
        }

        /// <summary>Generates a path. </summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when the requested operation is invalid.</exception>
        /// <param name="from">Source for the.</param>
        /// <param name="to">to.</param>
        /// <returns>The path.</returns>
        public static WoWPoint[] GeneratePath(WoWPoint from, WoWPoint to)
        {
            if (NavigationProvider == null)
                throw new InvalidOperationException("NavigationProvider cannot be null!");

            return NavigationProvider.GeneratePath(from, to);
        }

        /// <summary>Queries if we can navigate fully 'from' one location 'to' another.</summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when the requested operation is invalid.</exception>
        /// <param name="from">Source for the.</param>
        /// <param name="to">to.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public static bool CanNavigateFully(WoWPoint from, WoWPoint to)
        {
            if (NavigationProvider == null)
                throw new InvalidOperationException("NavigationProvider cannot be null!");

            return NavigationProvider.CanNavigateFully(from, to, 8192);
        }

        /// <summary>Queries if we can navigate fully 'from' one location 'to' another.</summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <param name="from">Source for the.</param>
        /// <param name="to">to.</param>
        /// <param name="maxHops"></param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        [Obsolete("Use the version without maxHops instead. The maxHops parameter is ignored.")]
        public static bool CanNavigateFully(WoWPoint from, WoWPoint to, int maxHops)
        {
            return CanNavigateFully(from, to);
        }

        public static bool CanNavigateWithin(WoWPoint from, WoWPoint to, float distanceTolerancy)
        {
            NavigationProvider provider = NavigationProvider as NavigationProvider;
            if (provider != null)
            {
                return provider.CanNavigateWithin(from, to, distanceTolerancy);
            }

            WoWPoint[] path = GeneratePath(from, to);
            return path != null && path.Length > 0 && path[path.Length - 1].DistanceSqr(to) < distanceTolerancy * distanceTolerancy;
        }

        public static float? PathDistance(WoWPoint from, WoWPoint to, float maxDistance = float.MaxValue)
        {
            NavigationProvider provider = NavigationProvider as NavigationProvider;
            if (provider != null)
            {
                return provider.PathDistance(from, to, float.MaxValue);
            }

            WoWPoint[] path = GeneratePath(from, to);
            if (path.Length <= 0)
            {
                return null;
            }

            float distance = from.Distance(path[0]);
            distance += path[path.Length - 1].Distance(to);
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (distance > maxDistance)
                {
                    return maxDistance;
                }

                distance += path[i].Distance(path[i + 1]);
            }

            return distance;
        }

        /// <summary>Gets a run status from move result. </summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when one or more arguments are outside
        ///                                               the required range.</exception>
        /// <param name="moveResult">The move result.</param>
        /// <returns>The run status from move result.</returns>
        public static RunStatus GetRunStatusFromMoveResult(MoveResult moveResult)
        {
            switch (moveResult)
            {
                case MoveResult.PathGenerationFailed:
                case MoveResult.Failed:
                    return RunStatus.Failure;
                case MoveResult.ReachedDestination:
                case MoveResult.PathGenerated:
                case MoveResult.UnstuckAttempt:
                case MoveResult.Moved:
                    return RunStatus.Success;
                default:
                    throw new ArgumentOutOfRangeException("moveResult");
            }
        }

        /// <summary>Finds all heights at a specified X and Y value.</summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <exception cref="InvalidOperationException">Thrown when the requested operation is invalid.</exception>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The found mesh heights.</returns>
        public static List<float> FindHeights(float x, float y)
        {
            if (HeightProvider == null)
                throw new InvalidOperationException("HeightProvider cannot be null");

            return HeightProvider.FindHeights(x, y);
        }

        /// <summary>Finds a height of a specified point and assigns the vector passed-in's Z value with the height (if successful).</summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <param name="v">[in,out] The vector containing the X and Y value. The Z value will be assigned if this function succeeds.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public static bool FindHeight(ref Vector3 v)
        {
            return FindHeight(v.X, v.Y, out v.Z);
        }

        /// <summary>Finds a height at a specified X and Y coordinate.</summary>
        /// <remarks>Created 12/1/2010.</remarks>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">[out] The z coordinate.</param>
        /// <returns>true if it succeeds, false if it fails.</returns>
        public static bool FindHeight(float x, float y, out float z)
        {
            List<float> heights = FindHeights(x, y);
            if (heights.Count == 0)
            {
                z = 0;
                return false;
            }

            z = heights[0];
            return true;
        }

        public static bool AtLocation(WoWPoint point1, WoWPoint point2)
        {
            if (NavigationProvider == null)
                throw new InvalidOperationException("NavigationProvider cannot be null!");

            return NavigationProvider.AtLocation(point1, point2);
        }

        public static bool AtLocation(WoWPoint point)
        {
            WoWUnit mover = WoWMovement.ActiveMover ?? StyxWoW.Me;
            return AtLocation(mover.Location, point);
        }

        #region Nested type: CustomAreaType

        private enum CustomAreaType : byte
        {
            BlackSpot = AreaType.Misc1,
        }

        #endregion
    }
}
