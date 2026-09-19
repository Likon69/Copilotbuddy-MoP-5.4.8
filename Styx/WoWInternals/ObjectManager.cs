using System;
using System.Collections.Generic;
using System.Linq;
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
            get { return StyxWoW.Memory.Read<ulong>(CurMgrPtr + (int)StyxWoW.Offsets.Misc.LocalGuidOffset); }
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
                case WoWObjectType.Container:
                    return new WoWContainer(baseAddress);
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

        private static readonly Dictionary<ulong, WoWObject> _cachedObjects = new Dictionary<ulong, WoWObject>();

        private static readonly List<Tuple<ulong, WoWObject>> _invalidObjects = new List<Tuple<ulong, WoWObject>>();

        private static readonly object _updateLock = new object();
    }
}
