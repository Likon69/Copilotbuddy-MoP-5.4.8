using System.Windows;

namespace Styx.Pathing
{
    public struct LandBoxLocation
    {
        public LandBoxLocation(int mapId, int zoneId, int distance, WoWPoint landLocation, WoWPoint topLeft,
            WoWPoint bottomRight, float minZ, float maxZ, bool shouldDismount)
        {
            MapID = mapId;
            ZoneID = zoneId;
            Distance = distance;
            TopLeft = topLeft;
            BottomRight = bottomRight;
            LandLocation = landLocation;
            MinZ = minZ;
            MaxZ = maxZ;
            ShouldDismount = shouldDismount;
            Landed = false;
        }

        public bool IsWithinBox(WoWPoint point)
        {
            var rectangle = new Rect(new Point(TopLeft.X, TopLeft.Y), new Point(BottomRight.X, BottomRight.Y));
            return rectangle.Contains(point.X, point.Y) && point.Z >= MinZ && point.Z <= MaxZ;
        }

        public WoWPoint LandLocation;
        public WoWPoint TopLeft;
        public WoWPoint BottomRight;
        public int MapID;
        public int ZoneID;
        public int Distance;
        public float MinZ;
        public float MaxZ;
        public bool ShouldDismount;
        public bool Landed;
    }
}
