using System;

namespace Styx.WoWInternals.World
{
    public class WorldScene
    {
        internal WorldScene(IntPtr baseAddress) { BaseAddress = baseAddress; }
        public IntPtr BaseAddress { get; private set; }
        public WorldMap WorldMap => new WorldMap(StyxWoW.Memory.Read<IntPtr>(BaseAddress + (int)StyxWoW.Offsets.Misc.WorldSceneWorldMap));
    }
}
