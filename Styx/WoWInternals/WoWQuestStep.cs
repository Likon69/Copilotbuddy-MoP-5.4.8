using System;
using System.Linq;
using System.Runtime.InteropServices;
using Tripper.Tools.Math;

namespace Styx.WoWInternals
{
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    public struct WoWQuestStep
    {
        public override string ToString()
        {
            return string.Format("PoiObjectiveRelation: {0}, AreaPointsCount: {1}, AreaPointsPtr: {2}, DwordC: {3}, StepPosition: {4}, PoiId: {5}, PoiObjectiveIndex: {6}, PoiMapAreaId: {7}, PoiMapId: {8}, PoiFloorId: {9}, Dword2C: {10}, Dword30: {11}, Dword34: {12}, WorldEffectId: {13}, Dword3C: {14}",
                PoiObjectiveRelation, AreaPointsCount, intptr_0, uint_0, StepPosition, PoiID, PoiObjectiveIndex, PoiMapAreaID, PoiMapID, PoiFloorID,
                uint_1, uint_2, uint_3, uint_4, uint_5);
        }

        public Vector2[] AreaPoints
        {
            get { return GetAreaPoints(); }
        }

        private Vector2[] GetAreaPoints()
        {
            Vector2i[] array;
            if (AreaPointsCount > 0U)
            {
                array = StyxWoW.Memory.ReadArray<Vector2i>(intptr_0, (int)AreaPointsCount);
            }
            else
            {
                array = new Vector2i[10];
            }

            return array.Select(v => new Vector2((float)v.X, (float)v.Y)).ToArray();
        }

        public uint AreaPointsCount;

        private IntPtr intptr_0;

        public uint PoiObjectiveRelation;

        private uint uint_0;

        public Vector2i StepPosition;

        public uint PoiID;

        public int PoiObjectiveIndex;

        public uint PoiMapAreaID;

        public uint PoiMapID;

        public uint PoiFloorID;

        private uint uint_1;

        private uint uint_2;

        private uint uint_3;

        private uint uint_4;

        private uint uint_5;
    }
}
