using System;

namespace Styx.Pathing
{
    /// <summary>Interface for stuck handler. </summary>
    /// <remarks>Created 12/1/2010.</remarks>
    [Obsolete("If deriving from this class, use StuckHandler instead.")]
    public interface IStuckHandler
    {
        /// <summary>Query if this object is stuck. </summary>
        /// <returns>true if stuck, false if not.</returns>
        bool IsStuck();

        /// <summary>Unsticks this object. </summary>
        void Unstick();

        /// <summary>Resets this object. Called when the navigation provider reaches its end location.</summary>
        void Reset();
    }
}
