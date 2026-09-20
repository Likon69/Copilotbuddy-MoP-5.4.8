using System;
using System.Runtime.InteropServices;
using System.Text;
using GreenMagic;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.Helpers;
using Styx.Offsets;
using Styx.Resources;
using Styx.WoWInternals.World;
using Tripper.Tools.Math;

namespace Styx.WoWInternals.WoWObjects
{
    public class WoWObject : IEquatable<WoWObject>, IComparable<WoWObject>
    {
        public uint ModelAnimFlags
        {
            get { return StyxWoW.Memory.Read<uint>(BaseAddress + 0x60); }
        }

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

        private PerFrameCachedValue<float> _scaleCache;

        public float Scale
        {
            get
            {
                EnsureValid("Scale");
                if (_scaleCache == null)
                {
                    _scaleCache = new PerFrameCachedValue<float>(() =>
                    {
                        IntPtr getScale = StyxWoW.Memory.Read<IntPtr>(false, BaseAddress, (IntPtr)((int)StyxWoW.Offsets.VFTableIndex.GetScale * 4));
                        return StyxWoW.CallInjected<float>(getScale, CallingConvention.ThisCall, (uint)(int)BaseAddress);
                    });
                }

                return _scaleCache;
            }
        }

        public int DynamicFlags
        {
            get { return GetStorageField<int>((uint)WoWObjectFields.DynamicFlags); }
        }

        private uint _entry;

        public uint Entry
        {
            get
            {
                if (_entry == 0)
                {
                    _entry = GetStorageField<uint>((int)WoWObjectFields.EntryID);
                }

                return _entry;
            }
        }

        public virtual WoWObjectType Type
        {
            get { return (WoWObjectType)StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjType); }
        }

        public WoWObjectTypeFlag TypeFlags
        {
            get { return (WoWObjectTypeFlag)GetStorageField<uint>((int)WoWObjectFields.Type); }
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

        internal WoWGuid WoWGuid
        {
            get { return new WoWGuid { Full = Guid }; }
        }

        protected void EnsureValid(string funcName)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot call " + funcName + " on an invalid object.");
            }
        }

        public uint ObjectFlags
        {
            get
            {
                if (BaseAddress == IntPtr.Zero)
                {
                    return 0;
                }

                return StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjObjFlags);
            }
        }

        public virtual float InteractRange
        {
            get { return 4f; }
        }

        public float InteractRangeSqr
        {
            get { return InteractRange * InteractRange; }
        }

        public virtual bool WithinInteractRange
        {
            get
            {
                float interactRange = InteractRange;
                return DistanceSqr < (double)((interactRange - 1f) * (interactRange - 1f));
            }
        }

        public bool IsDisabled
        {
            get { return false; }
        }

        public virtual ulong DescriptorGuid
        {
            get { return GetStorageField<ulong>((int)WoWObjectFields.Guid); }
        }

        public float ZDiff
        {
            get
            {
                if (StyxWoW.Me == null)
                {
                    return 0f;
                }

                return Math.Abs(Z - StyxWoW.Me.Z);
            }
        }

        public float X
        {
            get { return Location.X; }
        }

        public float Y
        {
            get { return Location.Y; }
        }

        public float Z
        {
            get { return Location.Z; }
        }

        public virtual float Rotation
        {
            get { return 0f; }
        }

        public float RotationDegrees
        {
            get { return WoWMathHelper.RadiansToDegrees(Rotation); }
        }

        public virtual double Distance
        {
            get { return StyxWoW.Me.Location.Distance(Location); }
        }

        public virtual double DistanceSqr
        {
            get { return StyxWoW.Me.Location.DistanceSqr(Location); }
        }

        public virtual double Distance2D
        {
            get { return StyxWoW.Me.Location.Distance2D(Location); }
        }

        public virtual double Distance2DSqr
        {
            get { return StyxWoW.Me.Location.Distance2DSqr(Location); }
        }

        public bool IsMe
        {
            get { return Guid == ObjectManager.LocalGuid; }
        }

        public virtual string Name
        {
            get { return GetObjectName() ?? "Unknown"; }
        }

        public virtual string SafeName
        {
            get { return Name; }
        }

        private PerFrameCachedValue<WoWPoint> _locationCache;

        public virtual WoWPoint Location
        {
            get
            {
                EnsureValid("Location");
                if (_locationCache == null)
                {
                    _locationCache = new PerFrameCachedValue<WoWPoint>(() =>
                    {
                        using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(12))
                        {
                            IntPtr getPosition = StyxWoW.Memory.Read<IntPtr>(false, BaseAddress, (IntPtr)((int)StyxWoW.Offsets.VFTableIndex.GetPosition * 4));
                            StyxWoW.CallInjected<uint>(getPosition, CallingConvention.ThisCall, (uint)(int)BaseAddress, allocatedMemory.Address);
                            return allocatedMemory.Read<WoWPoint>(0);
                        }
                    });
                }

                return _locationCache;
            }
        }

        public WoWPoint WorldLocation
        {
            get { return Location; }
        }

        public virtual WoWPoint RelativeLocation
        {
            get { return WoWPoint.Empty; }
        }

        public QuestGiverStatus QuestGiverStatus
        {
            get { return (QuestGiverStatus)StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjQuestGiverStatus); }
        }

        public WoWInteractType InteractType
        {
            get { return (WoWInteractType)StyxWoW.Memory.Read<uint>(BaseAddress + (int)StyxWoW.Offsets.Misc.WoWObjInteractType); }
        }

        public virtual bool MeIsSafelyBehind
        {
            get { return StyxWoW.Me.IsSafelyBehind(this); }
        }

        public virtual bool MeIsBehind
        {
            get { return StyxWoW.Me.IsBehind(this); }
        }

        public virtual bool IsOutdoors
        {
            get
            {
                EnsureValid("IsOutdoors");
                return GetIsOutdoors();
            }
        }

        public virtual bool IsIndoors
        {
            get
            {
                EnsureValid("IsIndoors");
                return !GetIsOutdoors();
            }
        }

        private PerFrameCachedValue<Matrix> _worldMatrixCache;

        public virtual Matrix GetWorldMatrix()
        {
            if (_worldMatrixCache == null)
            {
                _worldMatrixCache = new PerFrameCachedValue<Matrix>(() =>
                {
                    if (!IsValid)
                    {
                        return Matrix.Identity;
                    }

                    using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(64))
                    {
                        IntPtr getMatrix = StyxWoW.Memory.Read<IntPtr>(false, BaseAddress, (IntPtr)((int)StyxWoW.Offsets.VFTableIndex.GetMatrix * 4));
                        StyxWoW.CallInjected<uint>(getMatrix, CallingConvention.ThisCall, (uint)(int)BaseAddress, allocatedMemory.Address);
                        return allocatedMemory.Read<Matrix>(0);
                    }
                });
            }

            return _worldMatrixCache;
        }

        protected static bool HasFlag(uint flag, uint value)
        {
            return (flag & value) == value;
        }

        internal static ulong ReadGuid(IntPtr baseAddress)
        {
            return StyxWoW.Memory.Read<ulong>(baseAddress + (int)StyxWoW.Offsets.Misc.WoWObjGUID);
        }

        public override int GetHashCode()
        {
            return BaseAddress.GetHashCode();
        }

        internal void UpdatePointer(IntPtr ptr)
        {
            BaseAddress = ptr;
        }

        public override string ToString()
        {
            return string.Format(StyxResources._0__Distance___1_, Name, Math.Floor(Distance));
        }

        public override bool Equals(object obj)
        {
            return obj as WoWObject != null && Equals((WoWObject)obj);
        }

        public bool Equals(WoWObject other)
        {
            return !ReferenceEquals(null, other) && (ReferenceEquals(this, other) || Guid == other.Guid);
        }

        public static bool operator ==(WoWObject left, WoWObject right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WoWObject left, WoWObject right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(WoWObject other)
        {
            if (other == null)
            {
                return 1;
            }

            if (this == other)
            {
                return 0;
            }

            return Distance.CompareTo(other.Distance);
        }

        internal T GetStorageField<T>(uint field) where T : struct
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidObjectPointerException(StyxResources.CannotReadDescriptorOnInvalidObject);
            }

            int descriptorOffset = (int)StyxWoW.Offsets.Misc.WoWObjDescriptorFieldsOffset;
            IntPtr storage = StyxWoW.Memory.Read<IntPtr>(BaseAddress + descriptorOffset);
            return StyxWoW.Memory.Read<T>(storage + (int)field);
        }

        internal T[] GetStorageFieldArray<T>(uint field, int count) where T : struct
        {
            if (BaseAddress == IntPtr.Zero)
            {
                throw new InvalidObjectPointerException(StyxResources.CannotReadDescriptorOnInvalidObject);
            }

            int descriptorOffset = (int)StyxWoW.Offsets.Misc.WoWObjDescriptorFieldsOffset;
            IntPtr storage = StyxWoW.Memory.Read<IntPtr>(BaseAddress + descriptorOffset);
            return StyxWoW.Memory.ReadArray<T>(storage + (int)field, count);
        }

        public WoWDynamicObject ToDynamicObject()
        {
            return this as WoWDynamicObject;
        }

        public WoWUnit ToUnit()
        {
            return this as WoWUnit;
        }

        public WoWPlayer ToPlayer()
        {
            return this as WoWPlayer;
        }

        public WoWItem ToItem()
        {
            return this as WoWItem;
        }

        public WoWContainer ToContainer()
        {
            return this as WoWContainer;
        }

        public WoWGameObject ToGameObject()
        {
            return this as WoWGameObject;
        }

        internal void FireInvalidated()
        {
            if (OnInvalidate != null)
            {
                OnInvalidate();
            }
        }

        private TimeCachedValue<bool> _inLineOfSightCache;

        public virtual bool InLineOfSight
        {
            get
            {
                if (_inLineOfSightCache == null)
                {
                    _inLineOfSightCache = new TimeCachedValue<bool>(TimeSpan.FromMilliseconds(400.0), () => GameWorld.IsInLineOfSight(StyxWoW.Me.GetTraceLinePos(), Location.Add(0f, 0f, 2.132f)));
                }

                return _inLineOfSightCache;
            }
        }

        public bool IsFacing(WoWObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return IsFacing(obj.Location);
        }

        public bool IsFacing(WoWPoint location)
        {
            return WoWMathHelper.IsFacing(Location, Rotation, location, 3.14159274f);
        }

        public bool IsSafelyFacing(WoWObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return IsSafelyFacing(obj.Location);
        }

        public bool IsSafelyFacing(WoWPoint location)
        {
            return WoWMathHelper.IsSafelyFacing(Location, Rotation, location);
        }

        public bool IsSafelyFacing(WoWObject obj, float viewDegrees = 100f)
        {
            return WoWMathHelper.IsFacing(Location, Rotation, obj.Location, WoWMathHelper.DegreesToRadians(viewDegrees));
        }

        public bool IsBehind(WoWObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return WoWMathHelper.IsBehind(Location, obj.Location, obj.Rotation, 3.14159274f);
        }

        public bool IsSafelyBehind(WoWObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return WoWMathHelper.IsSafelyBehind(Location, obj.Location, obj.Rotation);
        }

        private readonly WaitTimer _interactTimer = new WaitTimer(TimeSpan.FromMilliseconds(500.0));

        public virtual void Interact()
        {
            Interact(false);
        }

        public void Interact(bool ignoreTimer)
        {
            if (!ignoreTimer && !_interactTimer.IsFinished)
            {
                return;
            }

            EnsureValid("Interact");
            _interactTimer.Reset();
            StyxWoW.ResetAfk();
            Logging.WriteDiagnostic("Interacting with {0}", SafeName);
            Guard.AgainstInvalidExecutor();

            Executor executor = StyxWoW.Memory.Executor;
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
                executor.AddRandomLine("add eax, {0}", (int)StyxWoW.Offsets.VFTableIndex.OnRightClick * 4);
                executor.AddRandomLine("mov eax, [eax]");
                executor.AddRandomLine("test eax, eax");
                executor.AddRandomLine("jz @OUT");
                executor.AddRandomLine("call eax");
                executor.AddRandomLine("@OUT:");
                executor.AddRandomLine("retn");
                executor.Execute();
            }
        }

        private PerFrameCachedValue<WoWPoint> _positionCache;

        public WoWPoint GetPosition()
        {
            if (_positionCache == null)
            {
                _positionCache = new PerFrameCachedValue<WoWPoint>(() =>
                {
                    EnsureValid("GetPosition");
                    Guard.AgainstInvalidExecutor();

                    Executor executor = StyxWoW.Memory.Executor;
                    lock (executor.AssemblyLock)
                    {
                        using (AllocatedMemory allocatedMemory = StyxWoW.Memory.CreateAllocatedMemory(12))
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

                            executor.AddRandomLine("push {0}", allocatedMemory.Address);
                            executor.AddRandomLine("mov eax, [ecx]");
                            executor.AddRandomLine("add eax, {0}", (int)StyxWoW.Offsets.VFTableIndex.GetPosition * 4);
                            executor.AddRandomLine("mov eax, [eax]");
                            executor.AddRandomLine("call eax");
                            executor.AddRandomLine("@OUT:");
                            executor.AddRandomLine("retn");
                            executor.Execute();

                            using (StyxWoW.Memory.TemporaryCacheState(false))
                            {
                                return executor.Memory.Read<WoWPoint>(allocatedMemory.Address);
                            }
                        }
                    }
                });
            }

            return _positionCache;
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

        private PerFrameCachedValue<bool> _isOutdoorsCache;

        private bool GetIsOutdoors()
        {
            if (_isOutdoorsCache == null)
            {
                _isOutdoorsCache = new PerFrameCachedValue<bool>(() =>
                {
                    EnsureValid("GetIsOutdoors");
                    return StyxWoW.CallInjected<byte>(StyxWoW.Offsets.Functions.CGObject_C__IsOutdoors, CallingConvention.ThisCall, (uint)(int)BaseAddress) != 0;
                });
            }

            return _isOutdoorsCache;
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
