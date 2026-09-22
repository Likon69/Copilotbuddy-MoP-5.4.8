using System;
using System.Diagnostics;

namespace Styx.CommonBot.CharacterManagement
{
    internal class DisregardEntry
    {
        public ulong Guid { get; private set; }
        public Stopwatch CheckTimer { get; private set; }

        public DisregardEntry(ulong guid)
        {
            this.Guid = guid;
            this.CheckTimer = new Stopwatch();
        }
    }
}
