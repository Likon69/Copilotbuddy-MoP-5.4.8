using System;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWObject
    {
        public event ObjectInvalidateDelegate OnInvalidate;

        internal WoWObject(IntPtr baseAdress)
        {
            BaseAddress = baseAdress;
        }

        public IntPtr BaseAddress { get; private set; }

        public bool IsValid
        {
            get { return BaseAddress != IntPtr.Zero; }
        }

        public virtual WoWObjectType Type
        {
            get { return (WoWObjectType)StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjType); }
        }

        private ulong _guid;

        public virtual ulong Guid
        {
            get
            {
                if (_guid == 0)
                {
                    _guid = StyxWoW.Memory.Read<ulong>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjGUID);
                }

                return _guid;
            }
        }

        protected void EnsureValid(string funcName)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot call " + funcName + " on an invalid object.");
            }
        }

        public virtual string Name
        {
            get { return GetObjectName() ?? "Unknown"; }
        }

        private string _name;

        public string GetObjectName()
        {
            if (!string.IsNullOrEmpty(_name) && _name != "Unknown")
            {
                return _name;
            }

            EnsureValid("GetObjectName");
            Guard.AgainstInvalidExecutor();

            Executor executor = StyxWoW.Memory.Executor;
            string curName;
            try
            {
                lock (executor.AssemblyLock)
                {
                    WoWGuid wowGuid = new WoWGuid(Guid);

                    executor.Clear();
                    executor.AddRandomLine("push {0}", 1);
                    executor.AddRandomLine("push {0}", wowGuid.High);
                    executor.AddRandomLine("push {0}", wowGuid.Low);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrObjectPtr);
                    executor.AddRandomLine("add esp, 12");
                    executor.AddRandomLine("test eax, eax");
                    executor.AddRandomLine("jz @OUT");

                    executor.AddRandomLine("mov ecx, eax");
                    executor.AddRandomLine("test ecx, ecx");
                    executor.AddRandomLine("jz @OUT");

                    executor.AddRandomLine("mov eax, [ecx]");
                    executor.AddRandomLine("add eax, {0}", (int)StyxWoW.Offsets.VFTableIndex.GetName * 4);
                    executor.AddRandomLine("mov eax, [eax]");
                    executor.AddRandomLine("test eax, eax");
                    executor.AddRandomLine("jz @OUT");
                    executor.AddRandomLine("call eax");
                    executor.AddRandomLine("@OUT:");
                    executor.AddRandomLine("retn");
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        IntPtr curNamePtr = executor.Memory.Read<IntPtr>(executor.ReturnPointer);
                        try
                        {
                            curName = curNamePtr != IntPtr.Zero ? executor.Memory.ReadString(curNamePtr, Encoding.UTF8) : "Unknown";
                        }
                        catch (Exception)
                        {
                            curName = "Unknown";
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "Unknown";
            }

            _name = curName;
            return _name;
        }

        internal void UpdatePointer(IntPtr ptr)
        {
            BaseAddress = ptr;
        }

        internal void FireInvalidated()
        {
            if (OnInvalidate != null)
            {
                OnInvalidate();
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct WoWGuid
    {
        [FieldOffset(0)]
        public uint Low;

        [FieldOffset(4)]
        public uint High;

        [FieldOffset(0)]
        public ulong Full;

        public WoWGuid(ulong full)
            : this()
        {
            Full = full;
        }
    }
}
