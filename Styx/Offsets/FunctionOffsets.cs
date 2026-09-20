using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FunctionOffsets
    {
        public IntPtr ClntObjMgrGetActivePlayerObj;
        public IntPtr FrameScript__Execute;
        public IntPtr _lua_gettop;
        public IntPtr FrameScript__SetTop;
        public IntPtr _lua_tolstring;
        public IntPtr _lua_pcall;
        public IntPtr _lua_load;
        public IntPtr _luaL_loadbuffer;
        public IntPtr GetSpellMiscRecord;
        public IntPtr GetSpellReagentsRecord;
        public IntPtr ClntObjMgrObjectPtr;
        public IntPtr CGPlayer_C__CanUseItem;
        public IntPtr CGItem_C__BuildItemName;
        public IntPtr CGItem_C__UseItem;
        public IntPtr CGUnit_C__IsCastNotInterruptible;
        public IntPtr CGUnit_C__GetCreatureClassification;
        public IntPtr CGUnit_C__IsBoss;
        public IntPtr CGUnit_C__KnowsSpell;
        public IntPtr CGUnit_C__IsInMeleeRange;
        public IntPtr CGUnit_C__GetTotalAbsorb;
        public IntPtr CGPlayer_C__CanAutoInteract;
        public IntPtr CGObject_C__IsOutdoors;
        public IntPtr CGUnit_C__UnitReaction;
        public IntPtr CGPlayer_C__CanInteract;
        public IntPtr GetGUIDByKeyword;
        public IntPtr CGItem_C__CreateItemLink2;
        public IntPtr GetItemDps;
        public IntPtr GetItemSellPrice;
        public IntPtr SetRaidTargetMarker;
        public IntPtr CGWorldFrame__Intersect;
        public IntPtr GetCurrentMapAreaID;
        public IntPtr CGQuestLog__AbandonSelectedQuest;
        public IntPtr CGGameUI__SetFocus;
        public IntPtr CGGameUI__Target;
        public IntPtr WoWClientDb__GetRow;
        public IntPtr DbCreatureCache_GetInfoBlockById;
        public IntPtr DbGameObjectCache_GetInfoBlockById;
        public IntPtr DbNpcCache_GetInfoBlockById;
        public IntPtr DbNameCache_GetInfoBlockById;
        public IntPtr DbGuildCache_GetInfoBlockById;
        public IntPtr DbQuestCache_GetInfoBlockById;
        public IntPtr DbPageTextCache_GetInfoBlockById;
        public IntPtr DbPetNameCache_GetInfoBlockById;
        public IntPtr DbPetitionCache_GetInfoBlockById;
        public IntPtr DbItemTextCache_GetInfoBlockById;
        public IntPtr DBItemCache_GetInfoBlockByID;
        public IntPtr DBItemCache_GetClassInfoBlockByID;
        public IntPtr Spell_C__CastSpell2;
        public IntPtr Spell_C_HandleSpriteClick;
        public IntPtr CGPlayer_C__ClearAFK;
        public IntPtr GetKnownSpellIndex;
        public IntPtr GetSpellOverrideId;
        public IntPtr Spell_C__CastSpell;
        public IntPtr Spell_C__HandleTerrainClick;
    }
}
