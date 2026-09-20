using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct MiscOffsets
    {
        public IntPtr WoWObjDescriptorFieldsOffset;
        public IntPtr WoWObjType;
        public IntPtr WoWObjGUID;
        public IntPtr WoWObjNextObject;
        public IntPtr AreaTrigger_Timestamp;
        public IntPtr AreaTriggerMovementData;
        public IntPtr WoWObjQuestGiverStatus;
        public IntPtr WoWObjInteractType;
        public IntPtr GObjectAnimationState;
        public IntPtr FirstObject;
        public IntPtr WoWObjObjFlags;
        public IntPtr LocalGuidOffset;
        public IntPtr GObjectX;
        public IntPtr GObjectSubObj;
        public IntPtr GObjectCachedInfo;
        public IntPtr GObjectWorldMatrix;
        public IntPtr ContainerBagOffset;
        public IntPtr WoWUnitMovementDataDirect;
        public IntPtr WoWUnitBoundingHeight;
        public IntPtr WoWUnitCursorOverride;
        public IntPtr WoWUnitUnitCacheEntry;
        public IntPtr WoWUnitIsAutoAttacking;
        public IntPtr WoWUnitUnitCastId;
        public IntPtr WoWUnitNonChanneledCastingSpellId;
        public IntPtr WoWUnitUnitCastStart;
        public IntPtr WoWUnitUnitCastEnd;
        public IntPtr WoWUnitUnitChannelId;
        public IntPtr WoWUnitChannelStart;
        public IntPtr WoWUnitChannelEnd;
        public IntPtr WoWUnitRenderFacing;
        public IntPtr WoWUnitAuraTable;
        public IntPtr WoWUnitAuraCount;
        public IntPtr PredictedHealsCount;
        public IntPtr PredictedHealsArray;
        public IntPtr WoWUnitThreatInfo;
        public IntPtr InventoryOffset;
        public IntPtr ClientConnectionOffset;
    }
}
