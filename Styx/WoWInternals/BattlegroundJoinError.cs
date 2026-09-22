namespace Styx.WoWInternals
{
    public enum BattlegroundJoinError
    {
        None, Nothing = -1, Deserter = -2, NotSameTeam = -3, Max3Battles = -4,
        StillEnqueued = -5, InRatedMatch = -6, TeamLeftQueue = -7, GroupJoinedNotEligible = -8
    }
}
