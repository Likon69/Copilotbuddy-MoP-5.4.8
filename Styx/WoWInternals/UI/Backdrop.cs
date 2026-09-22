using System;

namespace Styx.WoWInternals.UI
{
    public class Backdrop
    {
        public string BackgroundFile { get; set; }

        public string EdgeFile { get; set; }

        public bool Tile { get; set; }

        public int TileSize { get; set; }

        public int EdgeSize { get; set; }

        public int InsetLeft { get; set; }

        public int InsetRight { get; set; }

        public int InsetTop { get; set; }

        public int InsetBottom { get; set; }

        internal string ToLuaTableString()
        {
            return string.Format("{{bgFile=\"{0}\",edgeFile=\"{1}\",tile={2},tileSize={3},edgeSize={4},insets={{left={5},right={6},top={7},bottom={8}}}}}", new object[] { this.BackgroundFile, this.EdgeFile, this.Tile, this.TileSize, this.EdgeSize, this.InsetLeft, this.InsetRight, this.InsetTop, this.InsetBottom });
        }
    }
}
