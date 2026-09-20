using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GreenMagic;
using Styx.Common;
using Styx.Resources;
using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public static class ObjectManager
    {
        public static event ObjectListUpdateFinishedDelegate OnObjectListUpdateFinished;

        internal static LocalPlayer Me { get; private set; }

        public static ulong LocalGuid
        {
            get
            {
                IntPtr curMgrPtr = CurMgrPtr;
                if (curMgrPtr == IntPtr.Zero)
                {
                    return 0UL;
                }

                return StyxWoW.Memory.Read<ulong>(curMgrPtr + (int)StyxWoW.Offsets.Misc.LocalGuidOffset);
            }
        }

        private static IntPtr CurMgrPtr
        {
            get { return StyxWoW.Memory.Read<IntPtr>(false, StyxWoW.Offsets.Statics.ClientConnection, StyxWoW.Offsets.Misc.ClientConnectionOffset); }
        }

        public static List<WoWObject> ObjectList
        {
            get { return _cachedObjects.Values.ToList(); }
        }

        public static unsafe void Update()
        {
            if (StyxWoW.Memory == null)
            {
                throw new Exception(StyxResources.BlackMagic_was_not_Initialized_);
            }

            lock (_updateLock)
            {
                IntPtr curMgrPtr;
                if (StyxWoW.WowExited || (curMgrPtr = CurMgrPtr) == IntPtr.Zero)
                {
                    _cachedObjects.Clear();
                    return;
                }

                foreach (KeyValuePair<ulong, WoWObject> kvp in _cachedObjects)
                {
                    kvp.Value.UpdatePointer(IntPtr.Zero);
                }

                int guidOffset = (int)StyxWoW.Offsets.Misc.WoWObjGUID;
                int typeOffset = (int)StyxWoW.Offsets.Misc.WoWObjType;
                int nextObjOffset = (int)StyxWoW.Offsets.Misc.WoWObjNextObject;

                int @base = Math.Min(Math.Min(guidOffset, typeOffset), nextObjOffset);
                int max = Math.Max(Math.Max(guidOffset, typeOffset), nextObjOffset);
                int bufferSize = max - @base + 4;
                byte* buffer = stackalloc byte[bufferSize];

                guidOffset -= @base;
                typeOffset -= @base;
                nextObjOffset -= @base;

                ulong localGuid = StyxWoW.Memory.Read<ulong>(curMgrPtr + (int)StyxWoW.Offsets.Misc.LocalGuidOffset);
                IntPtr curObj = StyxWoW.Memory.Read<IntPtr>(curMgrPtr + (int)StyxWoW.Offsets.Misc.FirstObject);

                while ((int)curObj != 0 && ((int)curObj & 1) == 0)
                {
                    StyxWoW.Memory.ReadByteBuffer(curObj + @base, buffer, bufferSize);
                    ulong curGuid = (ulong)(*(long*)(buffer + guidOffset));

                    if (curGuid == localGuid && Me != null)
                    {
                        Me.UpdatePointer(curObj);
                    }

                    WoWObject obj;
                    if (_cachedObjects.TryGetValue(curGuid, out obj))
                    {
                        obj.UpdatePointer(curObj);
                    }
                    else
                    {
                        uint objType = *(uint*)(buffer + typeOffset);
                        WoWObject newObj = ConstructObject(curObj, (WoWObjectType)objType, curGuid, localGuid);
                        _cachedObjects.Add(curGuid, newObj);
                    }

                    IntPtr nextObj = new IntPtr(*(int*)(buffer + nextObjOffset));
                    if (nextObj == curObj)
                    {
                        break;
                    }

                    curObj = nextObj;
                }

                _invalidObjects.Clear();
                foreach (KeyValuePair<ulong, WoWObject> kvp in _cachedObjects)
                {
                    if (kvp.Value.BaseAddress == IntPtr.Zero)
                    {
                        _invalidObjects.Add(Tuple.Create(kvp.Key, kvp.Value));
                    }
                }

                foreach (Tuple<ulong, WoWObject> tuple in _invalidObjects)
                {
                    _cachedObjects.Remove(tuple.Item1);
                }

                foreach (Tuple<ulong, WoWObject> tuple in _invalidObjects)
                {
                    tuple.Item2.FireInvalidated();
                }
            }

            if (Me != null && OnObjectListUpdateFinished != null)
            {
                OnObjectListUpdateFinished(Me);
            }
        }

        private static WoWObject ConstructObject(IntPtr baseAddress, WoWObjectType? objType, ulong objGuid, ulong myGuid)
        {
            if (!objType.HasValue)
            {
                objType = (WoWObjectType)StyxWoW.Memory.Read<uint>(baseAddress + 0x14);
            }

            switch (objType)
            {
                case WoWObjectType.Item:
                    return new WoWItem(baseAddress);
                case WoWObjectType.Container:
                    return new WoWContainer(baseAddress);
                case WoWObjectType.Unit:
                    return new WoWUnit(baseAddress);
                case WoWObjectType.Player:
                    if (objGuid == myGuid)
                    {
                        if (Me == null)
                        {
                            Me = new LocalPlayer(baseAddress);
                        }
                        else
                        {
                            Me.UpdatePointer(baseAddress);
                        }

                        return Me;
                    }

                    return new WoWPlayer(baseAddress);
                case WoWObjectType.GameObject:
                    return new WoWGameObject(baseAddress);
                case WoWObjectType.DynamicObject:
                    return new WoWDynamicObject(baseAddress);
                case WoWObjectType.Corpse:
                    return new WoWCorpse(baseAddress);
                case WoWObjectType.AreaTrigger:
                    return new WoWAreaTrigger(baseAddress);
                default:
                    return new WoWObject(baseAddress);
            }
        }

        public static List<T> GetObjectsOfType<T>() where T : WoWObject
        {
            return GetObjectsOfType<T>(false, false);
        }

        public static List<T> GetObjectsOfType<T>(bool allowInheritance) where T : WoWObject
        {
            return GetObjectsOfType<T>(allowInheritance, false);
        }

        public static List<T> GetObjectsOfType<T>(bool allowInheritance, bool includeMeIfFound) where T : WoWObject
        {
            return EnumerateObjectsOfType<T>(allowInheritance, includeMeIfFound).ToList();
        }

        public static List<T> GetObjectsOfTypeFast<T>() where T : WoWObject
        {
            return EnumerateObjectsOfType<T>(true, true).ToList();
        }

        internal static IEnumerable<T> EnumerateObjectsOfType<T>(bool allowInheritance = false, bool includeMeIfFound = false) where T : WoWObject
        {
            if (allowInheritance)
            {
                if (includeMeIfFound)
                {
                    return _cachedObjects.Values.OfType<T>();
                }

                return _cachedObjects.Values.Where(o => !ReferenceEquals(o, Me)).OfType<T>();
            }

            Type type = typeof(T);
            if (!includeMeIfFound && type == typeof(WoWPlayer))
            {
                return _cachedObjects.Values.Where(o => o.GetType() == type && !ReferenceEquals(o, Me)).Select(o => (T)(object)o);
            }

            return _cachedObjects.Values.Where(o => o.GetType() == type).Select(o => (T)(object)o);
        }

        private static bool GetGuidByKeyword(string keyword, out ulong guid, int unk)
        {
            Guard.AgainstInvalidExecutor();

            IntPtr pGuid = StyxWoW.Memory.Executor.Memory.AllocateMemory(8);
            IntPtr pszString = StyxWoW.Memory.Executor.Memory.AllocateMemory(512);
            StyxWoW.Memory.Executor.Memory.WriteString(pszString, keyword, Encoding.ASCII);

            bool success = false;
            try
            {
                Executor executor = StyxWoW.Memory.Executor;
                lock (executor.AssemblyLock)
                {
                    executor.Clear();
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.ClntObjMgrGetActivePlayerObj);
                    executor.AddRandomLine("test eax, eax");
                    executor.AddRandomLine("jz @OUT");
                    executor.AddRandomLine("push {0}", unk);
                    executor.AddRandomLine("push {0}", pGuid);
                    executor.AddRandomLine("push {0}", pszString);
                    executor.AddRandomLine("call {0}", StyxWoW.Offsets.Functions.GetGUIDByKeyword);
                    executor.AddRandomLine("add esp, 0xC");
                    executor.AddRandomLine("@OUT:");
                    executor.AddRandomLine("retn");
                    executor.Execute();

                    using (StyxWoW.Memory.TemporaryCacheState(false))
                    {
                        success = StyxWoW.Memory.Executor.Memory.Read<bool>(StyxWoW.Memory.Executor.ReturnPointer);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(StyxResources.GetGuidByKeyword_failed_);
                Logging.WriteException(ex);
            }
            finally
            {
                using (StyxWoW.Memory.TemporaryCacheState(false))
                {
                    guid = StyxWoW.Memory.Executor.Memory.Read<ulong>(pGuid);
                }

                if (pGuid != IntPtr.Zero)
                {
                    StyxWoW.Memory.Executor.Memory.FreeMemory(pGuid);
                }

                if (pszString != IntPtr.Zero)
                {
                    StyxWoW.Memory.Executor.Memory.FreeMemory(pszString);
                }
            }

            return success;
        }

        internal static T GetUnitByKeyword<T>(string unit) where T : WoWUnit
        {
            ulong guid;
            if (GetGuidByKeyword(unit, out guid, 0) && guid != 0 && guid != 0xFFFFFFFFFFFFFFFE)
            {
                return GetObjectByGuid<T>(guid);
            }

            return default(T);
        }

        internal static WoWObject GetObjectByGuid(ulong guid)
        {
            if (guid == 0)
            {
                return null;
            }

            WoWObject obj;
            if (!_cachedObjects.TryGetValue(guid, out obj))
            {
                obj = null;
            }

            if (obj != null && obj.BaseAddress != IntPtr.Zero && obj.IsValid)
            {
                return obj;
            }

            return null;
        }

        [Obsolete("Use GetObjectByGuid instead")]
        public static T GetAnyObjectByGuid<T>(ulong guid) where T : WoWObject
        {
            return GetObjectByGuid<T>(guid);
        }

        public static T GetObjectByGuid<T>(ulong guid) where T : WoWObject
        {
            WoWObject obj = GetObjectByGuid(guid);
            return obj as T;
        }

        private static readonly Dictionary<ulong, WoWObject> _cachedObjects = new Dictionary<ulong, WoWObject>();

        private static readonly List<Tuple<ulong, WoWObject>> _invalidObjects = new List<Tuple<ulong, WoWObject>>();

        private static readonly object _updateLock = new object();
    }
}
