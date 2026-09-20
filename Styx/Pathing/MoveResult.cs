using System;

namespace Styx.Pathing
{
    public enum MoveResult
    {
        /// <summary> .  </summary>
        Failed,
        /// <summary> .  </summary>
        ReachedDestination,
        /// <summary> .  </summary>
        PathGenerationFailed,
        /// <summary> .  </summary>
        PathGenerated,
        /// <summary> .  </summary>
        UnstuckAttempt,
        /// <summary> .  </summary>
        Moved
    }
}
