using System;
using System.Runtime.InteropServices;
using GreenMagic;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWSubObject
    {
        public IntPtr BaseAddress { get; private set; }

        internal WoWSubObject(IntPtr baseAddress)
        {
            BaseAddress = baseAddress;
        }

        public float InteractDistance
        {
            get { return GetField<float>(SubObjectOffsets.InteractDistance); }
        }

        public WoWGameObject OwnerObject
        {
            get
            {
                WoWGameObject woWGameObject;
                try
                {
                    IntPtr intPtr = GetField<IntPtr>(SubObjectOffsets.OwnerObj);
                    woWGameObject = ObjectManager.GetObjectByGuid<WoWGameObject>(new WoWObject(intPtr).Guid);
                }
                catch
                {
                    woWGameObject = null;
                }
                return woWGameObject;
            }
        }

        #region Virtual Functions

        public bool CanUse()
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot call CanUse on an invalid object.");
            }
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool flag2;
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                executor.AddRandomLine("mov ecx, {0}", BaseAddress);
                executor.AddRandomLine("test ecx, ecx");
                executor.AddRandomLine("jz @OUT");
                executor.AddRandomLine("mov eax, [ecx]");
                executor.AddRandomLine("mov eax, [eax+{0}]", 24U);
                executor.AddRandomLine("call eax");
                executor.AddRandomLine("@OUT:");
                executor.AddRandomLine("retn");
                executor.Execute();
                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    byte b = StyxWoW.Memory.Read<byte>(executor.ReturnPointer);
                    flag2 = b != 0;
                }
            }
            return flag2;
        }

        public bool CanUseNow()
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot call CanUseNow on an invalid object.");
            }
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool flag2;
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                CanUseNowAsm(executor, (IntPtr)0, 0U, 0U);
                executor.AddRandomLine("retn");
                executor.Execute();
                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    byte b = StyxWoW.Memory.Read<byte>(executor.ReturnPointer);
                    flag2 = b != 0;
                }
            }
            return flag2;
        }

        public bool CanUseNow(out GameError reason)
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot call CanUseNow on an invalid object.");
            }
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool flag2;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(4))
                {
                    executor.Clear();
                    CanUseNowAsm(executor, allocatedMemory.Address, 0U, 0U);
                    executor.AddRandomLine("retn");
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        reason = (GameError)StyxWoW.Memory.Read<uint>(allocatedMemory.Address);
                        byte b = StyxWoW.Memory.Read<byte>(executor.ReturnPointer);
                        flag2 = b != 0;
                    }
                }
            }
            return flag2;
        }

        public void Use()
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot call Use on an invalid object.");
            }
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            lock (executor.AssemblyLock)
            {
                executor.Clear();
                executor.AddRandomLine("mov ecx, {0}", BaseAddress);
                executor.AddRandomLine("test ecx, ecx");
                executor.AddRandomLine("jz @OUT");
                executor.AddRandomLine("mov eax, [ecx]");
                executor.AddRandomLine("mov eax, [eax+{0}]", 36U);
                executor.AddRandomLine("call eax");
                executor.AddRandomLine("@OUT:");
                executor.AddRandomLine("retn");
                executor.Execute();
            }
        }

        private void CanUseNowAsm(Executor executor, IntPtr reason, uint interactDistance, uint a4)
        {
            executor.AddRandomLine("mov ecx, {0}", BaseAddress);
            executor.AddRandomLine("test ecx, ecx");
            executor.AddRandomLine("jz @OUT");
            executor.AddRandomLine("mov eax, [ecx]");
            executor.AddRandomLine("mov eax, [eax+{0}]", 28U);
            executor.AddRandomLine("push {0}", a4);
            executor.AddRandomLine("push {0}", interactDistance);
            executor.AddRandomLine("push {0}", reason);
            executor.AddRandomLine("call eax");
            executor.AddRandomLine("@OUT:");
        }

        public bool CheckRange()
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("This SubObj is in an invalid state");
            }
            IntPtr vftableEntry = StyxWoW.Memory.GetVFTableEntry(BaseAddress, 8);
            byte b = StyxWoW.CallInjected<byte>(vftableEntry, CallingConvention.ThisCall, BaseAddress);
            return b != 0;
        }

        #endregion

        private T GetField<T>(SubObjectOffsets offset) where T : struct
        {
            return StyxWoW.Memory.Read<T>(BaseAddress + (int)offset);
        }

        private enum SubObjectOffsets
        {
            OwnerObj = 4,
            InteractDistance = 12
        }

        private enum SubObjectVFTableIndex
        {
            GetCursor = 5,
            CanUse,
            CanUseNow,
            CheckRange,
            Use
        }
    }
}
