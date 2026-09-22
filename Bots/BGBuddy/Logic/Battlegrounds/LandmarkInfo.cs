using System;
using Bots.BGBuddy.Helpers;
using Styx;
using Styx.WoWInternals;

using Tripper.Tools.Math;

namespace Bots.BGBuddy.Logic.Battlegrounds
{
    public class LandmarkInfo
    {
        public LandmarkInfo(int lmType, LandmarkControlType ctrl, MapBox box, Battleground bg)
        {
            Type = lmType;
            Control = ctrl;
            LastProcessed = DateTime.Now;
            Box = box;
            _bg = bg;
        }

        private Battleground _bg;
        public MapBox Box { get; set; }
        public int Type { get; set; }
        public int FriendlyPlayersAround { get; set; }
        public int EnemyPlayersAround { get; set; }
        public DateTime LastProcessed { get; set; }
        public LandmarkControlType Control { get; set; }

        public bool TooManyFriendlies
        {
            get
            {
                var count = RaidHelper.GetCountWithin(Box);
                return count <= _bg.MaxPlayersInGroup;
            }
        }

        public bool IsPlayerInside
        {
            get
            {
                var playerLoc = StyxWoW.Me.Location;
                var count = RaidHelper.GetCountWithin(Box.TopLeft,
                    Box.BottomRight,
                    new[] {new Vector2(playerLoc.X, playerLoc.Y)});
                return count != 0;
            }
        }

        public bool IsBoxAvailable
        {
            get
            {
                var friendlyCount = RaidHelper.GetCountWithin(Box) - 3;
                var enemyCount = RaidHelper.GetEnemyCountWithin(Box);

                if (enemyCount > friendlyCount)
                    return true;

                return false;
            }
        }

        public bool ControlledByUs
        {
            get
            {
                bool isHorde = StyxWoW.Me.IsHorde;
                switch (Control)
                {
                    case LandmarkControlType.AllianceControlled:
                        return !isHorde;
                    case LandmarkControlType.HordeControlled:
                        return isHorde;
                    default:
                        return false;
                }
            }
        }

        public bool ControlledByEnemy
        {
            get
            {
                bool isHorde = StyxWoW.Me.IsHorde;
                switch (Control)
                {
                    case LandmarkControlType.AllianceControlled:
                        return isHorde;
                    case LandmarkControlType.HordeControlled:
                        return !isHorde;
                    default:
                        return false;
                }
            }
        }

        public bool IsNeutral
        {
            get
            {
                return !ControlledByEnemy && !ControlledByUs;
            }
        }

        public void Process()
        {
            FriendlyPlayersAround = RaidHelper.GetCountWithin(Box);
            EnemyPlayersAround = RaidHelper.GetEnemyCountWithin(Box);
            LastProcessed = DateTime.Now;
        }
    }
}
