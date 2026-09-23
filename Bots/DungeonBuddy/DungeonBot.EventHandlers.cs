using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Styx;
using Styx.Common.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals.DBC;
using Tripper.Navigation;

using Bots.DungeonBuddy.Enums;
using Bots.DungeonBuddy.Helpers;
using Bots.DungeonBuddy.Profiles;
using Bots.DungeonBuddy.Profiles.Handlers;

namespace Bots.DungeonBuddy
{
    public partial class DungeonBot
    {
        internal void LoadDungeonScriptAndProfile(uint lfgDungeonId)
        {
            Dungeon dungeon = null;
            if (DungeonManager.CustomDungeons != null)
            {
                dungeon = DungeonManager.CustomDungeons.FirstOrDefault(candidate => candidate.DungeonId == lfgDungeonId);
            }

            bool usingCustomDungeon = dungeon != null;
            if (dungeon == null)
            {
                dungeon = DungeonManager.DefaultDungeons.FirstOrDefault(candidate => candidate.DungeonId == lfgDungeonId);
            }

            if (DungeonManager.SelectedDungeon != null && DungeonManager.SelectedDungeon == dungeon)
            {
                return;
            }

            _root = null;
            if (DungeonManager.SelectedDungeon != null)
            {
                HandleDungeonLeft();
            }

            DungeonManager.SelectedDungeon = dungeon;
            if (dungeon != null)
            {
                if (IsFarming)
                {
                    LfgDungeons lfgDungeon = dungeon.LfgDungeon;
                    if (lfgDungeon.SubType == LfgDungeons.LfgType.Dungeon && LfgQueue.DungeonDifficultyId != lfgDungeon.DifficultyId)
                    {
                        LfgQueue.SetDungeonDifficultyId(lfgDungeon.DifficultyId);
                    }
                    else if (lfgDungeon.SubType == LfgDungeons.LfgType.Raid && LfgQueue.RaidDifficultyId != lfgDungeon.DifficultyId)
                    {
                        LfgQueue.SetRaidDifficultyId(lfgDungeon.DifficultyId);
                    }
                }

                dungeon.Enter();
                DungeonCompleted = CompleteReason.None;
                _dungeonTimer.Reset();
                Logger.Write("Entered {0} dungeon and using {1} script", dungeon.Name, usingCustomDungeon ? "custom" : "default");
            }
            else
            {
                Logger.Write(Colors.Red, "No script found for dungeonId: {0}", lfgDungeonId);
            }

            if ((ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.DungeonId != lfgDungeonId) && lfgDungeonId != 0)
            {
                ProfileManager.LoadProfileForDungeon(lfgDungeonId);
            }
        }

        private Composite _dungeonRoot;

        private static List<WoWPoint> _cachedPathToBoss;

        private static readonly WaitTimer _pathToBossCacheTimer = new WaitTimer(TimeSpan.FromSeconds(8));

        internal void HandleDungeonEntered(uint dungeonId)
        {
            Dungeon dungeon = null;
            if (DungeonManager.CustomDungeons != null)
                dungeon = DungeonManager.CustomDungeons.FirstOrDefault(d => d.DungeonId == dungeonId);

            bool usingCustomDungeon = dungeon != null;

            if (dungeon == null)
                dungeon = DungeonManager.DefaultDungeons.FirstOrDefault(d => d.DungeonId == dungeonId);

            if (DungeonManager.SelectedDungeon != null && DungeonManager.SelectedDungeon == dungeon)
                return;

            _dungeonRoot = null;

            if (DungeonManager.SelectedDungeon != null)
                HandleDungeonLeft();

            DungeonManager.SelectedDungeon = dungeon;

            if (dungeon != null)
            {
                if (IsFarming)
                {
                    LfgDungeons lfgDungeon = dungeon.LfgDungeon;
                    if (lfgDungeon.SubType == LfgDungeons.LfgType.Dungeon && LfgQueue.DungeonDifficultyId != lfgDungeon.DifficultyId)
                        LfgQueue.SetDungeonDifficultyId(lfgDungeon.DifficultyId);
                    else if (lfgDungeon.SubType == LfgDungeons.LfgType.Raid && LfgQueue.RaidDifficultyId != lfgDungeon.DifficultyId)
                        LfgQueue.SetRaidDifficultyId(lfgDungeon.DifficultyId);
                }

                dungeon.Enter();
                DungeonCompleted = CompleteReason.None;
                _dungeonTimer.Reset();
                _inactivityTimer.Reset();
                Logger.Write("Entered {0} dungeon and using {1} script", dungeon.Name, usingCustomDungeon ? "custom" : "default");
            }
            else
            {
                Logger.Write(Colors.Red, "No script found for dungeonId: {0}", dungeonId);
            }

            if ((ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.DungeonId != dungeonId) && dungeonId != 0)
                ProfileManager.LoadProfileForDungeon(dungeonId);
        }

        internal void HandleDungeonLeft()
        {
            var selectedDungeon = DungeonManager.SelectedDungeon;
            if (selectedDungeon != null)
            {
                selectedDungeon.Exit();
                Logger.Write("Left dungeon: {0}", selectedDungeon.Name);
            }
            DungeonCompleted = CompleteReason.None;
            DungeonManager.SelectedDungeon = null;
            ProfileManager.Unload();
        }

        private static void UpdatePathToBoss(WoWPoint location)
        {
            Boss currentBoss = BossManager.CurrentBoss;
            if (currentBoss == null)
                return;

            if (_cachedPathToBoss != null && !_pathToBossCacheTimer.IsFinished)
            {
                float bestDistSqr = float.MaxValue;
                int bestSegmentIndex = 0;
                WoWPoint bestPoint = WoWPoint.Empty;
                for (int i = 1; i < _cachedPathToBoss.Count; i++)
                {
                    WoWPoint segmentStart = _cachedPathToBoss[i - 1];
                    WoWPoint segmentEnd = _cachedPathToBoss[i];
                    WoWPoint nearestPointOnSegment = location.GetNearestPointOnSegment(segmentStart, segmentEnd);
                    float distSqr = nearestPointOnSegment.DistanceSqr(location);
                    if (distSqr < bestDistSqr)
                    {
                        bestSegmentIndex = i - 1;
                        bestDistSqr = distSqr;
                        bestPoint = nearestPointOnSegment;
                    }
                }
                if (bestSegmentIndex > 0)
                    _cachedPathToBoss.RemoveRange(0, bestSegmentIndex);
                _cachedPathToBoss[0] = bestPoint;
                return;
            }

            PathFindResult pathFindResult = DungeonNavigator.FindPath(location, currentBoss.Location);
            _pathToBossCacheTimer.Reset();
            if (pathFindResult.Succeeded)
                _cachedPathToBoss = pathFindResult.Points.Select(p => (WoWPoint)p).ToList();
        }
    }
}
