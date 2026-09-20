using System;

namespace Styx.WoWInternals.WoWObjects
{
    public struct WoWGlyphInfo
    {
        internal WoWGlyphInfo(int slot, bool enabled, uint itemId)
        {
            Slot = slot;
            Enabled = enabled;
            ItemId = itemId;
        }

        public int Slot;

        public bool Enabled;

        public uint ItemId;
    }
}
