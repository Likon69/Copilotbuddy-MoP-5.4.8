using System;

namespace Styx.CommonBot.Routines
{
    [Obsolete("Use the CombatRoutine abstract class instead")]
    public interface ICombatRoutine : IDisposable, IBehaviors
    {
        string Name { get; }

        WoWClass Class { get; }

        double? PullDistance { get; }

        bool NeedRest { get; }

        void Rest();

        bool NeedPreCombatBuffs { get; }

        void PreCombatBuff();

        bool NeedPullBuffs { get; }

        void PullBuff();

        void Pull();

        bool NeedCombatBuffs { get; }

        void CombatBuff();

        void Combat();

        bool NeedHeal { get; }

        void Heal();

        bool NeedDeath { get; }

        void Death();

        void Initialize();

        void OnButtonPress();

        bool WantButton { get; }

        string ButtonText { get; }

        void Pulse();

        void ShutDown();
    }
}
