using System;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    public struct WoWQuestCurrentStep
    {
        public override string ToString()
        {
            return string.Format("MapId: {0}, ObjectiveIndex: {1}, FloorId: {2}, Pos: {3}", MapID, ObjectiveIndex, FloorID, Pos);
        }

        private uint uint_0;

        public uint MapID;

        public uint ObjectiveIndex;

        public uint FloorID;

        public Vector2i Pos;
    }
}
