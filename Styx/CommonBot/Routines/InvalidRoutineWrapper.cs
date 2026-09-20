using System;
using Styx.Resources;

namespace Styx.CommonBot.Routines
{
    public sealed class InvalidRoutineWrapper : CombatRoutine
    {
        public override string Name
        {
            get
            {
                return StyxResources.Invalid_Routine;
            }
        }

        public override WoWClass Class
        {
            get
            {
                return StyxWoW.Me.Class;
            }
        }

        public override void Initialize()
        {
        }
    }
}
