using System.Collections.Generic;
using Bots.DungeonBuddy.Avoidance;
using Bots.DungeonBuddy.Helpers;
using Styx.TreeSharp;

namespace Bots.DungeonBuddy.Behaviors
{
    internal class CombatRoutineDecorator : Decorator
    {
        public CombatRoutineDecorator(Composite child)
            : base(child)
        {
        }

        protected override bool CanRun(object context)
        {
            if (AvoidanceManager.IsRunningOutOfAvoid)
            {
                return DungeonBuddySettings.Instance.CombatMovement;
            }

            return true;
        }

        protected override IEnumerable<RunStatus> Execute(object context)
        {
            if (!CanRun(context))
            {
                yield return RunStatus.Failure;
                yield break;
            }

            DecoratedChild.Start(context);
            while (DecoratedChild.Tick(context) == RunStatus.Running && CanRun(context))
            {
                yield return RunStatus.Running;
            }

            DecoratedChild.Stop(context);
            if (DecoratedChild.LastStatus == RunStatus.Failure)
            {
                yield return RunStatus.Failure;
            }
            else
            {
                yield return RunStatus.Success;
            }
        }
    }
}
