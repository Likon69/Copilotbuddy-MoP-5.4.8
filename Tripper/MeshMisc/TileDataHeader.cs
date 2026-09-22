using System;
using System.IO;

namespace Tripper.MeshMisc
{
    public class TileDataHeader
    {
        public TileDataHeader(string mapName, int tileX, int tileY, DateTime utcCreateTime)
        {
            MapName = mapName;
            TileX = tileX;
            TileY = tileY;
            UtcCreateTime = utcCreateTime;
        }

        public string MapName { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public DateTime UtcCreateTime { get; set; }

        public void WriteToStream(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(MapName);
            writer.Write(TileX);
            writer.Write(TileY);
            writer.Write(UtcCreateTime.ToFileTimeUtc());
        }

        public static TileDataHeader ReadFromStream(Stream stream)
        {
            var reader = new BinaryReader(stream);
            string mapName = reader.ReadString();
            int tileX = reader.ReadInt32();
            int tileY = reader.ReadInt32();
            DateTime utcCreateTime = DateTime.FromFileTimeUtc(reader.ReadInt64());
            return new TileDataHeader(mapName, tileX, tileY, utcCreateTime);
        }
    }
}
