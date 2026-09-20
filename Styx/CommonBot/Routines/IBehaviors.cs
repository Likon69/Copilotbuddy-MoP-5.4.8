using System;
using Styx.TreeSharp;

namespace Styx.CommonBot.Routines
{
    [Obsolete("Do not use")]
    public interface IBehaviors
    {
        /// <summary>
        /// Behavior used when resting
        /// </summary>
        Composite RestBehavior { get; }
        /// <summary>
        /// Behavior used for buffing, regular buffs like 'Power Word: Fortitude', 'MotW' etc..
        /// </summary>
        Composite PreCombatBuffBehavior { get; }
        /// <summary>
        /// Behavior used when buffing prior to pulling
        /// </summary>
        Composite PullBuffBehavior { get; }
        /// <summary>
        /// Behavior used when engaging mobs in combat
        /// </summary>
        Composite PullBehavior { get; }
        /// <summary>
        /// Behavior used for combat buffs. eg; 'Horn of Winter', 'Power Infusion' etc..
        /// </summary>
        Composite CombatBuffBehavior { get; }
        /// <summary>
        /// Behavior used in combat
        /// </summary>
        Composite CombatBehavior { get; }
        /// <summary>
        /// Behavior used when healing
        /// </summary>
        Composite HealBehavior { get; }
        /// <summary>
        /// Behavior used for moving to targets
        /// </summary>
        Composite MoveToTargetBehavior { get; }

        /// <summary>
        /// Custom handling for when you are dead.
        /// </summary>
        Composite DeathBehavior { get; }
    }
}
