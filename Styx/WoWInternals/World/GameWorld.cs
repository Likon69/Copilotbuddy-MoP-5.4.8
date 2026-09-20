using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GreenMagic;
using Styx.Common;

namespace Styx.WoWInternals.World
{
    public static class GameWorld
    {
        public static bool TraceLine(WoWPoint from, WoWPoint to, TraceLineHitFlags flags, out WoWPoint hitPoint)
        {
            return TraceLine(from, to, 1f, flags, out hitPoint);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static bool TraceLine(WoWPoint from, WoWPoint to, CGWorldFrameHitFlags flags, out WoWPoint hitPoint)
        {
            return TraceLine(from, to, 1f, (TraceLineHitFlags)flags, out hitPoint);
        }

        public static bool TraceLine(WoWPoint from, WoWPoint to, TraceLineHitFlags flags)
        {
            WoWPoint hitPoint;
            return TraceLine(from, to, flags, out hitPoint);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static bool TraceLine(WoWPoint from, WoWPoint to, CGWorldFrameHitFlags flags)
        {
            WoWPoint hitPoint;
            return TraceLine(from, to, flags, out hitPoint);
        }

        public static void MassTraceLine(WorldLine[] lines, TraceLineHitFlags flag, out bool[] hitResults, out WoWPoint[] hitPoints)
        {
            TraceLineHitFlags[] flags = new TraceLineHitFlags[lines.Length];
            for (int i = 0; i < flags.Length; i++)
            {
                flags[i] = flag;
            }
            MassTraceLine(lines, flags, out hitResults, out hitPoints);
        }

        public static void MassTraceLine(WorldLine[] lines, TraceLineHitFlags[] flags, out bool[] hitResults)
        {
            WoWPoint[] hitPoints;
            MassTraceLine(lines, flags, out hitResults, out hitPoints);
        }

        public static void MassTraceLine(WorldLine[] lines, TraceLineHitFlags flag, out bool[] hitResults)
        {
            WoWPoint[] hitPoints;
            MassTraceLine(lines, flag, out hitResults, out hitPoints);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static void MassTraceLine(WorldLine[] lines, CGWorldFrameHitFlags flag, out bool[] hitResults, out WoWPoint[] hitPoints)
        {
            MassTraceLine(lines, Enumerable.Repeat(flag, lines.Length).ToArray(), out hitResults, out hitPoints);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static void MassTraceLine(WorldLine[] lines, CGWorldFrameHitFlags[] flags, out bool[] hitResults)
        {
            WoWPoint[] hitPoints;
            MassTraceLine(lines, flags, out hitResults, out hitPoints);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static void MassTraceLine(WorldLine[] lines, CGWorldFrameHitFlags flag, out bool[] hitResults)
        {
            MassTraceLine(lines, Enumerable.Repeat(flag, lines.Length).ToArray(), out hitResults);
        }

        public static bool IsInLineOfSight(WoWPoint from, WoWPoint to)
        {
            return !TraceLine(from, to, TraceLineHitFlags.Collision);
        }

        public static bool IsInLineOfSpellSight(WoWPoint from, WoWPoint to)
        {
            return !TraceLine(from, to, TraceLineHitFlags.SpellLineOfSight);
        }

        [Obsolete("Use the version that takes TraceLineHitFlags instead")]
        public static void MassTraceLine(WorldLine[] lines, CGWorldFrameHitFlags[] flags, out bool[] hitResults, out WoWPoint[] hitPoints)
        {
            TraceLineHitFlags[] newFlags = new TraceLineHitFlags[flags.Length];
            for (int i = 0; i < newFlags.Length; i++)
            {
                newFlags[i] = (TraceLineHitFlags)flags[i];
            }
            MassTraceLine(lines, newFlags, out hitResults, out hitPoints);
        }

        public static unsafe void MassTraceLine(WorldLine[] lines, TraceLineHitFlags[] flags, out bool[] hitResults, out WoWPoint[] hitPoints)
        {
            if (flags.Length != lines.Length)
            {
                throw new ArgumentException("flags.Length is not the same as lines.Length!");
            }

            if (lines.Length == 0)
            {
                hitResults = new bool[0];
                hitPoints = new WoWPoint[0];
                return;
            }

            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;

            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(lines.Length + lines.Length * 12 + lines.Length * 28 + 4))
                {
                    allocatedMemory.AllocateOfChunk("HitResults", lines.Length);
                    allocatedMemory.AllocateOfChunk("HitPoints", lines.Length * 12);
                    allocatedMemory.AllocateOfChunk("InputData", lines.Length * 28);
                    allocatedMemory.AllocateOfChunk("Distance", 4);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        int bas = i * 28;
                        allocatedMemory.Write<uint>("InputData", bas + 0, (uint)flags[i]);
                        allocatedMemory.Write<WoWPoint>("InputData", bas + 4, lines[i].End);
                        allocatedMemory.Write<WoWPoint>("InputData", bas + 16, lines[i].Start);
                    }

                    executor.Clear();
                    executor.AddLine("mov ebx, 0");
                    executor.AddLine("mov esi, {0}", allocatedMemory["HitPoints"]);
                    executor.AddLine("mov edi, {0}", allocatedMemory["InputData"]);
                    executor.AddLine("@loop:");
                    executor.AddLine("mov eax, {0}", allocatedMemory["Distance"]);
                    executor.AddLine("mov edx, {0}", AsmHelper.GetFloatUInt(1f));
                    executor.AddLine("mov [eax], edx");
                    executor.AddLine("push 0");
                    executor.AddLine("mov eax, edi");
                    executor.AddLine("mov eax, [eax]");
                    executor.AddLine("push eax");
                    executor.AddLine("push {0}", allocatedMemory["Distance"]);
                    executor.AddLine("push esi");
                    executor.AddLine("mov eax, edi");
                    executor.AddLine("add eax, 4");
                    executor.AddLine("push eax");
                    executor.AddLine("add eax, 12");
                    executor.AddLine("push eax");
                    executor.AddLine("call {0}", StyxWoW.Offsets.Functions.CGWorldFrame__Intersect);
                    executor.AddLine("add esp, 0x18");
                    executor.AddLine("mov edx, {0}", allocatedMemory["HitResults"]);
                    executor.AddLine("add edx, ebx");
                    executor.AddLine("mov [edx], al");
                    executor.AddLine("inc ebx");
                    executor.AddLine("add esi, 12");
                    executor.AddLine("add edi, {0}", 28);
                    executor.AddLine("cmp ebx, {0}", lines.Length);
                    executor.AddLine("jl @loop");
                    executor.AddLine("retn");
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        hitResults = new bool[lines.Length];
                        fixed (bool* hitResultsPtr = hitResults)
                        {
                            executor.Memory.ReadByteBuffer(allocatedMemory["HitResults"], hitResultsPtr, hitResults.Length);
                        }

                        hitPoints = new WoWPoint[lines.Length];
                        fixed (WoWPoint* hitPointsPtr = hitPoints)
                        {
                            executor.Memory.ReadByteBuffer(allocatedMemory["HitPoints"], hitPointsPtr, hitPoints.Length * 12);
                        }
                    }
                }
            }
        }

        private static unsafe bool TraceLine(WoWPoint from, WoWPoint to, float distance, TraceLineHitFlags flags, out WoWPoint hitPoint)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;

            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(40))
                {
                    byte* inputData = stackalloc byte[28];
                    *(WoWPoint*)inputData = from;
                    *(WoWPoint*)(inputData + 12) = to;
                    *(float*)(inputData + 24) = distance;
                    StyxWoW.Memory.WriteByteBuffer(allocatedMemory.Address, inputData, 28);

                    try
                    {
                        executor.Clear();
                        executor.AddLine("push 0");
                        executor.AddLine("push {0}", (uint)flags);
                        executor.AddLine("push {0}", allocatedMemory.Address + 24);
                        executor.AddLine("push {0}", allocatedMemory.Address + 28);
                        executor.AddLine("push {0}", allocatedMemory.Address + 12);
                        executor.AddLine("push {0}", allocatedMemory.Address + 0);
                        executor.AddLine("call {0}", StyxWoW.Offsets.Functions.CGWorldFrame__Intersect);
                        executor.AddLine("add esp, 0x18");
                        executor.AddLine("retn");
                        executor.Execute();

                        hitPoint = StyxWoW.Memory.Read<WoWPoint>(allocatedMemory.Address + 28);

                        using (StyxWoW.Memory.TemporaryCacheState(false))
                        {
                            return executor.Memory.Read<byte>(executor.ReturnPointer) != 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteDiagnostic("Exception in TraceLine:{0} - {1} - {2}", ex.Message, ex.StackTrace, ex.Source);
                    }
                }
            }

            hitPoint = WoWPoint.Zero;
            return true;
        }

        public static unsafe bool GetTriangles(WoWPoint boundsMin, WoWPoint boundsMax, TraceLineHitFlags flags, out ReadOnlyCollection<Triangle> triangles)
        {
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;

            lock (executor.AssemblyLock)
            {
                IntPtr absolute = StyxWoW.Memory.GetAbsolute((IntPtr)0xD261B8);

                executor.Clear();
                executor.AddLine("push ebp");
                executor.AddLine("mov ebp, esp");
                executor.AddLine("sub esp, 24");
                executor.AddLine("mov [ebp-24], dword {0}", *(uint*)&boundsMin.X);
                executor.AddLine("mov [ebp-20], dword {0}", *(uint*)&boundsMin.Y);
                executor.AddLine("mov [ebp-16], dword {0}", *(uint*)&boundsMin.Z);
                executor.AddLine("mov [ebp-12], dword {0}", *(uint*)&boundsMax.X);
                executor.AddLine("mov [ebp-8], dword {0}", *(uint*)&boundsMax.Y);
                executor.AddLine("mov [ebp-4], dword {0}", *(uint*)&boundsMax.Z);
                executor.AddLine("push {0}", (int)flags);
                executor.AddLine("push {0}", absolute);
                executor.AddLine("lea eax, [ebp-24]");
                executor.AddLine("push eax");
                executor.AddLine("call {0}", StyxWoW.Memory.GetAbsolute((IntPtr)0x5F0C9A));
                executor.AddLine("leave");
                executor.AddLine("retn");
                executor.Execute();

                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    if (executor.Memory.Read<byte>(executor.ReturnPointer) != 0)
                    {
                        TerrainFacetResult result = executor.Memory.Read<TerrainFacetResult>(absolute);
                        Logging.Write(result.ToString());
                        Triangle[] array = executor.Memory.ReadArray<Triangle>(result.FacetArrayPtr, result.FacetArrayLength);
                        triangles = new ReadOnlyCollection<Triangle>(array);
                        return true;
                    }
                }
            }

            triangles = new List<Triangle>().AsReadOnly();
            return false;
        }

        [Obsolete("Use TraceLineHitFlags instead")]
        [Flags]
        public enum CGWorldFrameHitFlags : uint
        {
            HitTestNothing = 0x0,
            HitTestBoundingModels = 0x1,
            HitTestSpellLoS = 0x10,
            HitTestWMO = 0x10,
            HitTestWMONoCam = 0x40,
            HitTestUnknown2 = 0x80,
            HitTestGround = 0x100,
            HitTestLiquid = 0x10000,
            HitTestLiquid2 = 0x20000,
            HitTestMovableObjects = 0x100000,
            HitTestUnknown3 = 0x200000,
            HitTestUnknown4 = 0x400000,
            HitTestLOS = HitTestMovableObjects | HitTestGround | HitTestWMONoCam | HitTestWMO | HitTestBoundingModels,
            HitTestGroundAndStructures = HitTestGround | HitTestWMO | HitTestBoundingModels,
        }
    }
}
