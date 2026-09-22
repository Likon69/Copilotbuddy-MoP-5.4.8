using System;
using System.Collections.Generic;
using System.Linq;

using Styx;

using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Tripper.Tools.Math;

namespace Bots.BGBuddy
{
    public class RaidHelper
    {
        public Vector2 GetMapLocation(Vector2 areaA, Vector2 areaB, Vector2 worldPoint)
        {
            Vector2 areaPos = new Vector2(0, 0);
            areaPos.X = (worldPoint.Y - areaA.Y) / (areaB.Y - areaA.Y);
            areaPos.Y = (worldPoint.X - areaA.X) / (areaB.X - areaA.X);

            return areaPos;
        }

        /// <summary>Gets a count of 'positions' within a given box. (This is for determining which 'box' has the highest concentration of players)</summary>
        /// <remarks>Created 12/19/2010.</remarks>
        /// <param name="topLeft">The top left.</param>
        /// <param name="bottomRight">The bottom right.</param>
        /// <param name="playerPositions">The player positions.</param>
        /// <returns>The count within.</returns>
        public static int GetCountWithin(Vector3 topLeft, Vector3 bottomRight, IEnumerable<Vector2> playerPositions)
        {
            //Logger.Write("Getting players within " + topLeft + " - " + bottomRight);
            //Logger.Write(playerPositions.Count() + " players to check.");
            var bb = BoundingBox2.CreateFromPoints(new[] { new Vector2(topLeft.X, topLeft.Y),  new Vector2(bottomRight.X, bottomRight.Y) });
            var count  = playerPositions.Count(bb.Contains);
            //Logger.Write("Found " + count + " here.");
            return count;
        }
        internal static int GetEnemyCountWithin(MapBox box)
        {
            var isHorde = StyxWoW.Me.IsHorde;
            return GetCountWithin(
                box.TopLeft, box.BottomRight,
                ObjectManager.GetObjectsOfType<WoWPlayer>(false, false).Where(p => p.IsHorde != isHorde && p.IsAlive).Select(p => new Vector2(p.X, p.Y)));
        }

        internal static int GetCountWithin(MapBox box)
        {
            var pos = (from partyMember in StyxWoW.Me.RaidMembers
                       where !partyMember.IsDead && !partyMember.IsGhost
                       let loc = partyMember.Location
                       select new Vector2(loc.X, loc.Y)).ToList();

            return GetCountWithin(box.TopLeft, box.BottomRight, pos);
        }
    }
}
