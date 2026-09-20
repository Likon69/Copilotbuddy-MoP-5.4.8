using System;
using GreenMagic;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWDoor : WoWAnimatedSubObject
    {
        internal WoWDoor(IntPtr baseAddress)
            : base(baseAddress)
        {
        }

        public bool IsClosed
        {
            get
            {
                WoWGameObject ownerObject = OwnerObject;
                return ownerObject == null || ownerObject.State != WoWGameObjectState.Active;
            }
        }

        public bool IsOpen
        {
            get { return !IsClosed; }
        }

        public bool CanOpenNow()
        {
            uint num;
            return CanOpenNow(out num);
        }

        public bool CanOpenNow(out uint reason)
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("Cannot call CanOpenNow on an invalid object.");
            }
            Guard.AgainstInvalidExecutor();
            Executor executor = StyxWoW.Memory.Executor;
            bool flag2;
            lock (executor.AssemblyLock)
            {
                using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(4))
                {
                    executor.Clear();
                    CanOpenNowAsm(executor, allocatedMemory.Address, 0U);
                    executor.AddRandomLine("retn");
                    executor.Execute();
                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        reason = StyxWoW.Memory.Read<uint>(allocatedMemory.Address);
                        byte b = StyxWoW.Memory.Read<byte>(executor.ReturnPointer);
                        flag2 = b != 0;
                    }
                }
            }
            return flag2;
        }

        private void CanOpenNowAsm(Executor executor, IntPtr reason, uint interactDistance)
        {
            executor.AddRandomLine("mov ecx, {0}", BaseAddress);
            executor.AddRandomLine("test ecx, ecx");
            executor.AddRandomLine("jz @OUT");
            executor.AddRandomLine("push {0}", interactDistance);
            executor.AddRandomLine("push {0}", reason);
            executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.CGDoor_C__CanOpenNow);
            executor.AddRandomLine("@OUT:");
        }
    }
}
