using System;

namespace Styx.WoWInternals
{
    public enum WoWPetBattleState
    {
        Created = 0,
        WaitingPreBattle = 1,
        RoundInProgress = 2,
        WaitingForRoundPlayback = 3,
        WaitingForFrontPets = 4,
        CreatedFailed = 5,
        FinalRound = 6,
        Finished = 7,
    }
}
