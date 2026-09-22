namespace Styx.Pathing
{
    public struct LandLocation
    {
        public LandLocation(int mapId, int zoneId, int distance, float x, float y, float z, float minZ, float maxZ)
        {
            MapID = mapId;
            ZoneID = zoneId;
            Distance = distance;
            Location = new WoWPoint(x, y, z);
            MinZ = minZ;
            MaxZ = maxZ;
            Landed = false;
        }

        public WoWPoint Location;
        public int MapID;
        public int ZoneID;
        public int Distance;
        public float MinZ;
        public float MaxZ;
        public bool Landed;
    }
}
