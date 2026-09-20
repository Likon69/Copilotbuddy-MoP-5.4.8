using System;

namespace Styx.WoWInternals.World
{
    internal struct TerrainFacetResult
    {
        private readonly IntPtr _unk0;

        public readonly int FacetArrayLength;

        public readonly IntPtr FacetArrayPtr;

        public readonly int FacetArrayAllottedSize;

        public readonly int Unk10;

        public readonly int Array2Length;

        public readonly IntPtr Array2Ptr;

        public readonly int Array2AllottedSize;

        public readonly int Unk20;

        public readonly int FacetTypesLength;

        public readonly IntPtr FacetTypesPtr;

        public readonly int FacetTypesAllottedSize;

        public readonly int Unk30;

        public readonly int Flags;

        public override string ToString()
        {
            return string.Format("FacetArrayLength: {0}, FacetArrayAllottedSize: {1}, Unk10: {2}, Array2Length: {3}, Array2AllottedSize: {4}, Unk20: {5}, FacetTypesLength: {6}, FacetTypesAllottedSize: {7}, Unk30: {8}, Flags: {9}", FacetArrayLength, FacetArrayAllottedSize, Unk10, Array2Length, Array2AllottedSize, Unk20, FacetTypesLength, FacetTypesAllottedSize, Unk30, Flags);
        }
    }
}
