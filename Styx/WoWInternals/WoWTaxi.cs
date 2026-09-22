using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GreenMagic;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals.DBC;

namespace Styx.WoWInternals
{
    // This class is not ready for public consumption. Leave it as internal. They don't need to know it even exists yet.
    internal class WoWTaxi
    {
        public TaxiMapInfo GetTaxiMapInfo(uint index)
        {
            IntPtr intPtr = StyxWoW.Offsets.Statics.TaxiMap_NodeInfos;
            int size = MarshalCache<TaxiMapInfo>.Size;
            long num = (long)size * (long)((ulong)index);
            intPtr = StyxWoW.Memory.Read<IntPtr>(intPtr);
            return StyxWoW.Memory.Read<TaxiMapInfo>(intPtr + (int)num);
        }

        public TaxiNodes GetNode(uint index)
        {
            return TaxiNodes.FromId((uint)GetTaxiMapInfo(index).NodeRecord.Id);
        }

        public bool GetQwordAndBitIndex(uint pathRecordId, ref uint bitIndex, ref uint taxiNodeIndex)
        {
            if (TaxiNodes.FromId(pathRecordId) != null)
            {
                uint num = pathRecordId - 1U;
                uint num2 = pathRecordId - 1U >> 6;
                bitIndex = num2;
                taxiNodeIndex = num - (num2 << 6);
                return true;
            }
            return false;
        }

        private ulong GetKnownNodeBitQword(uint index)
        {
            IntPtr intptr_ = StyxWoW.Offsets.Statics.TaxiMap_KnownNodes;
            return StyxWoW.Memory.Read<ulong>(intptr_);
        }

        private TaxiNodeType GetNodeType(uint taxiNodeIndex)
        {
            if (taxiNodeIndex < NodeCount)
            {
                TaxiMapInfo taxiMapInfo = GetTaxiMapInfo(taxiNodeIndex);
                uint num = StyxWoW.Memory.Read<uint>(false, new IntPtr[]
                {
                    taxiMapInfo.NodeRecordPtr,
                    IntPtr.Zero
                });
                if (num == CurrentNodeId)
                {
                    return TaxiNodeType.Current;
                }
                uint num2 = 0U;
                if (taxiMapInfo.byteD != 0 && GetQwordAndBitIndex(num, ref num2, ref taxiNodeIndex))
                {
                    if ((GetKnownNodeBitQword(num2) & (1UL << (int)taxiNodeIndex)) != 0UL)
                    {
                        return TaxiNodeType.Reachable;
                    }
                    return TaxiNodeType.Distant;
                }
            }
            return TaxiNodeType.None;
        }

        public uint NodeCount
        {
            get
            {
                return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.TaxiMap_NumNodes);
            }
        }

        public uint CurrentNodeId
        {
            get
            {
                return StyxWoW.Memory.Read<uint>(StyxWoW.Offsets.Statics.TaxiMap_CurrentNodeId);
            }
        }

        public TaxiNodes CurrentNode
        {
            get
            {
                return TaxiNodes.FromId(StyxWoW.Memory.Read<uint>(false, new IntPtr[]
                {
                    StyxWoW.Offsets.Statics.TaxiMap_CurrentNodeId,
                    IntPtr.Zero
                }));
            }
        }

        public List<TaxiNodes> GetKnownNodes()
        {
            int maxIndex = StyxWoW.Db[ClientDb.TaxiNodes].MaxIndex;
            List<TaxiNodes> list = new List<TaxiNodes>();
            uint num = 1U;
            uint num2 = 0U;
            uint num3 = 1U;
            ulong num4 = GetKnownNodeBitQword(num2);
            do
            {
                int num5 = 0;
                do
                {
                    if ((num4 & (1UL << num5)) != 0UL)
                    {
                        TaxiNodes taxiNodes = TaxiNodes.FromId(num);
                        if (taxiNodes != null)
                        {
                            list.Add(taxiNodes);
                        }
                    }
                    num5++;
                    num += 1U;
                }
                while (num5 < 64);
                num4 = GetKnownNodeBitQword(num3++);
            }
            while ((ulong)num < (ulong)((long)maxIndex));
            return list;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TaxiMapInfo
        {
            internal TaxiNodes.TaxiNodesRecord NodeRecord
            {
                get
                {
                    return StyxWoW.Memory.Read<TaxiNodes.TaxiNodesRecord>(NodeRecordPtr);
                }
            }

            public override string ToString()
            {
                return string.Format("PathRecordPtr: {0:X8}, Dword4: {1:X8}, Dword8: {2:X8}, ByteC: {3:X2}, ByteD: {4:X2}, ByteE: {5:X2}, ByteF: {6:X2}, Dword10: {7:X8}, Dword14: {8:X8}, Dword18: {9:X8}, Dword1C: {10:X8}, Dword20: {11:X8}, Nodes: {12:X8}, NumNodes: {13:X8}, Dword2C: {14:X8}", NodeRecordPtr, PosY, PosX, byteC, byteD, byteE, byteF, dword10, dword14, dword18, dword1C, dword20, Nodes, NumRoutes, dword2C);
            }

            public IntPtr NodeRecordPtr;

            public float PosX;

            public float PosY;

            public byte byteC;

            public byte byteD;

            public byte byteE;

            public byte byteF;

            public uint dword10;

            public uint dword14;

            public uint dword18;

            public uint dword1C;

            public uint dword20;

            public uint Nodes;

            public uint NumRoutes;

            public uint dword2C;
        }
    }
}
