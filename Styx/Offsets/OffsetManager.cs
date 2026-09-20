using System;
using Styx.Offsets;

namespace Styx
{
    internal class OffsetManager
    {
        internal FunctionOffsets Functions;
        internal MiscOffsets Misc;
        internal StaticOffsets Statics;
        internal VFTableIndex VFTableIndex;

        internal void Rebase(IntPtr newBase)
        {
            Functions.ClntObjMgrGetActivePlayerObj = newBase + 0x4F84;
            Functions.FrameScript__Execute = newBase + 0x4FD12;
            Functions._lua_gettop = newBase + 0xD5E84;
            Functions.FrameScript__SetTop = newBase + 0xD5E95;
            Functions._lua_tolstring = newBase + 0xD6404;
            Functions._lua_pcall = newBase + 0xD6EFC;
            Functions._lua_load = newBase + 0xD6F62;
            Functions._luaL_loadbuffer = newBase + 0xD7962;
            Functions.GetSpellMiscRecord = newBase + 0x38380C;
            Functions.GetSpellReagentsRecord = newBase + 0x37F083;
            Functions.ClntObjMgrObjectPtr = newBase + 0x39BA5B;
            Functions.CGPlayer_C__CanUseItem = newBase + 0x3B3DE3;
            Functions.CGItem_C__BuildItemName = newBase + 0x405D56;
            Functions.CGItem_C__UseItem = newBase + 0x40782C;
            Functions.CGUnit_C__IsCastNotInterruptible = newBase + 0x409717;
            Functions.CGUnit_C__GetCreatureClassification = newBase + 0x40C7FD;
            Functions.CGUnit_C__IsBoss = newBase + 0x40E88E;
            Functions.CGUnit_C__KnowsSpell = newBase + 0x415870;
            Functions.CGUnit_C__IsInMeleeRange = newBase + 0x40EAF9;
            Functions.CGUnit_C__GetTotalAbsorb = newBase + 0x4243FD;
            Functions.CGPlayer_C__CanAutoInteract = newBase + 0x41F546;
            Functions.CGObject_C__IsOutdoors = newBase + 0x4142AC;
            Functions.CGUnit_C__UnitReaction = newBase + 0x4153C3;
            Functions.CGPlayer_C__CanInteract = newBase + 0x417889;
            Functions.GetGUIDByKeyword = newBase + 0x49E653;
            Functions.CGItem_C__CreateItemLink2 = newBase + 0x4C2B16;
            Functions.SetRaidTargetMarker = newBase + 0x4DE099;
            Functions.CGWorldFrame__Intersect = newBase + 0x5EEF7B;
            Functions.GetCurrentMapAreaID = newBase + 0x510B32;
            Functions.CGQuestLog__AbandonSelectedQuest = newBase + 0x580E4F;
            Functions.CGGameUI__SetFocus = newBase + 0x8CC3AB;
            Functions.CGGameUI__Target = newBase + 0x8CE510;

            Misc.WoWObjType = (IntPtr)0xC;
            Misc.WoWObjGUID = (IntPtr)0x28;
            Misc.WoWObjNextObject = (IntPtr)0x34;
            Misc.FirstObject = (IntPtr)0xCC;
            Misc.LocalGuidOffset = (IntPtr)0xE8;
            Misc.WoWUnitMovementDataDirect = (IntPtr)0x828;
            Misc.ClientConnectionOffset = (IntPtr)0x462C;

            Statics.LuaState = newBase + 0xBA6178;
            Statics.GxApi = newBase + 0xC85C3C;
            Statics.IsInGame = newBase + 0xD65B16;
            Statics.ClientConnection = newBase + 0xEC4628;
            Statics.LocalPlayerName = newBase + 0xEC4668;

            VFTableIndex.GetPosition = (IntPtr)0x27;
            VFTableIndex.GetName = (IntPtr)0x4D;
        }

        public void Clear()
        {
            Functions = new FunctionOffsets();
            Misc = new MiscOffsets();
            Statics = new StaticOffsets();
            VFTableIndex = new VFTableIndex();
        }
    }
}
