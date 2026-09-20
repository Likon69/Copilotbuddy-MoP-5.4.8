using System;

namespace Styx.WoWInternals.WoWObjects
{
    public struct WoWArenaTeamInfo
    {
        public WoWArenaTeamInfo(uint id, uint type, uint member, uint gamesThisWeek, uint gamesThisSeason, uint seasonWins, uint personalRating)
        {
            Id = id;
            Type = type;
            Member = member;
            GamesThisWeek = gamesThisWeek;
            GamesThisSeason = gamesThisSeason;
            SeasonWins = seasonWins;
            PersonalRating = personalRating;
        }

        public uint Id;

        public uint Type;

        public uint Member;

        public uint GamesThisWeek;

        public uint GamesThisSeason;

        public uint SeasonWins;

        public uint PersonalRating;
    }
}
