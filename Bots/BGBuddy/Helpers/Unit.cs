using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Vector2 = Tripper.Tools.Math.Vector2;

namespace Bots.BGBuddy.Helpers
{
    internal static class Unit
    {
        public static int PlayersAttacking(this WoWPlayer player)
        {
            return
                ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).Count(
                    p => p.CurrentTarget != null && p.CurrentTargetGuid == player.Guid);
        }

        public static int PlayersDefending(this WoWPlayer player, double distance)
        {
            return
                ObjectManager.GetObjectsOfType<WoWPlayer>(false).Count(
                    p => p.Location.Distance(player.Location) < distance && p.IsFriendly);
        }

        public static bool WithinBox(this WoWPlayer player, MapBox box)
        {
            var pos = (from partyMember in StyxWoW.Me.RaidMembers
                       where !partyMember.IsDead && !partyMember.IsGhost
                       let loc = partyMember.Location
                       select new Vector2(loc.X, loc.Y)).ToList();

            var count = RaidHelper.GetCountWithin(box.TopLeft, box.BottomRight, pos);

            return count != 0;
        }
    }
}
