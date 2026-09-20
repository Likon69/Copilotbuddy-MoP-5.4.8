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
            Functions.WoWClientDb__GetRow = newBase + 0x203D64;
            Functions.DbCreatureCache_GetInfoBlockById = newBase + 0x22E1B8;
            Functions.DbGameObjectCache_GetInfoBlockById = newBase + 0x22E1DA;
            Functions.DbNpcCache_GetInfoBlockById = newBase + 0x22E1FC;
            Functions.DbNameCache_GetInfoBlockById = newBase + 0x22E21E;
            Functions.DbGuildCache_GetInfoBlockById = newBase + 0x22E6CF;
            Functions.DbQuestCache_GetInfoBlockById = newBase + 0x22E6F1;
            Functions.DbPageTextCache_GetInfoBlockById = newBase + 0x22E713;
            Functions.DbPetNameCache_GetInfoBlockById = newBase + 0x22E735;
            Functions.DbPetitionCache_GetInfoBlockById = newBase + 0x22EB8B;
            Functions.DbItemTextCache_GetInfoBlockById = newBase + 0x22EBAD;
            Functions.DBItemCache_GetInfoBlockByID = newBase + 0x24250B;
            Functions.DBItemCache_GetClassInfoBlockByID = newBase + 0x3CE9E4;

            Misc.WoWObjDescriptorFieldsOffset = (IntPtr)0x4;
            Misc.LuaStateGlobals = (IntPtr)0x50;
            Misc.WoWObjType = (IntPtr)0xC;
            Misc.WoWObjGUID = (IntPtr)0x28;
            Misc.WoWObjNextObject = (IntPtr)0x34;
            Misc.AreaTrigger_Timestamp = (IntPtr)0x6C;
            Misc.AreaTriggerMovementData = (IntPtr)0xE8;
            Misc.WoWObjQuestGiverStatus = (IntPtr)0xBC;
            Misc.WoWObjInteractType = (IntPtr)0xC0;
            Misc.GObjectAnimationState = (IntPtr)0xC4;
            Misc.FirstObject = (IntPtr)0xCC;
            Misc.WoWObjObjFlags = (IntPtr)0xD0;
            Misc.LocalGuidOffset = (IntPtr)0xE8;
            Misc.GObjectX = (IntPtr)0xF8;
            Misc.GObjectSubObj = (IntPtr)0x1BC;
            Misc.GObjectCachedInfo = (IntPtr)0x1C0;
            Misc.GObjectWorldMatrix = (IntPtr)0x1C4;
            Misc.ContainerBagOffset = (IntPtr)0x7A8;
            Misc.NetStatsStart = (IntPtr)0x534;
            Misc.WoWUnitMovementDataDirect = (IntPtr)0x828;
            Misc.WoWUnitBoundingHeight = (IntPtr)0x8F8;
            Misc.WoWUnitCursorOverride = (IntPtr)0x9B4;
            Misc.WoWUnitUnitCacheEntry = (IntPtr)0x9B4;
            Misc.WoWUnitIsAutoAttacking = (IntPtr)0xC58;
            Misc.WoWUnitUnitCastId = (IntPtr)0xC9C;
            Misc.WoWUnitNonChanneledCastingSpellId = (IntPtr)0xCB8;
            Misc.WoWUnitUnitCastStart = (IntPtr)0xCC8;
            Misc.WoWUnitUnitCastEnd = (IntPtr)0xCCC;
            Misc.WoWUnitUnitChannelId = (IntPtr)0xCD0;
            Misc.WoWUnitChannelStart = (IntPtr)0xCD4;
            Misc.WoWUnitChannelEnd = (IntPtr)0xCD8;
            Misc.WoWUnitRenderFacing = (IntPtr)0xCE4;
            Misc.WoWUnitAuraTable = (IntPtr)0xE18;
            Misc.WoWUnitAuraCount = (IntPtr)0x1218;
            Misc.PredictedHealsCount = (IntPtr)0x1494;
            Misc.PredictedHealsArray = (IntPtr)0x1498;
            Misc.WoWUnitThreatInfo = (IntPtr)0x14A8;
            Misc.InventoryOffset = (IntPtr)0x3BC8;
            Misc.ClientConnectionOffset = (IntPtr)0x462C;

            Statics.LuaDummyNode = newBase + 0x941D80;
            Statics.GameObjectDataSlots = newBase + 0xB578B0;
            Statics.LuaState = newBase + 0xBA6178;
            Statics.TimerStruct = newBase + 0xBBE928;
            Statics.GxApi = newBase + 0xC85C3C;
            Statics.Base_DbCache = newBase + 0xC86520;
            Statics.Base_DbGameObjectCache = newBase + 0xC86628;
            Statics.QuestCache = newBase + 0xC86A48;
            Statics.DbItemCache = newBase + 0xC88348;
            Statics.DbItemSparseCache = newBase + 0xC88408;
            Statics.SpellReagentsCacheObj = newBase + 0xC89608;
            Statics.SpellCacheMaxId = newBase + 0xC872C4;
            Statics.SpellCacheBase = newBase + 0xC872C8;
            Statics.LocalPlayerAutoRepeatingSpell = newBase + 0xC99108;
            Statics.LocalPlayerPendingCursorSpell = newBase + 0xCA2514;
            Statics.HomeBindAreaId = newBase + 0xCFF0BC;
            Statics.LocalPlayerDualWieldSpellId = newBase + 0xCFF460;
            Statics.UnitPowerIndices = newBase + 0xD0F01C;
            Statics.MinimapZoneText = newBase + 0xD65B04;
            Statics.SubZoneText = newBase + 0xD65B08;
            Statics.ZoneText = newBase + 0xD65B0C;
            Statics.RealZoneText = newBase + 0xD65B0C;
            Statics.IsInGame = newBase + 0xD65B16;
            Statics.LocalPlayerFocusGuid = newBase + 0xD65B60;
            Statics.LocalPlayerCurrentTargetGuid = newBase + 0xD65B40;
            Statics.LocalPlayerCurrentZoneId = newBase + 0xD65BB4;
            Statics.LocalPlayerCurrentSubZoneId = newBase + 0xD65BB8;
            Statics.LocalPlayerCurrentContinentId = newBase + 0xD65C40;
            Statics.LocalPlayerCorpsePoint = newBase + 0xD65ED8;
            Statics.LocalPlayerCorpseMapId = newBase + 0xD65BC8;
            Statics.TotemInfos = newBase + 0xD65EF8;
            Statics.MirrorTimers = newBase + 0xD65F98;
            Statics.NonPartyRaidMarkTargets = newBase + 0xDC1CB8;
            Statics.LocalPlayerNumKnownSpells = newBase + 0xDC25E0;
            Statics.LocalPlayerKnownSpells = newBase + 0xDC25E4;
            Statics.LocalPlayerTotemBars = newBase + 0xDC26D8;
            Statics.LocalPlayerBattlefieldInstanceType = newBase + 0xDC3028;
            Statics.LocalPlayerComboPoints = newBase + 0xD65BF9;
            Statics.LocalPlayerComboPointsTarget = newBase + 0xD65C68;
            Statics.PetSpells = newBase + 0xDD4A30;
            Statics.StabledPets = newBase + 0xDD4B68;
            Statics.LocalPlayerBagArray = newBase + 0xDD7328;
            Statics.LocalPlayerRuneTypes = newBase + 0xDD7388;
            Statics.LocalPlayerRunes = newBase + 0xDD73EC;
            Statics.NumQuestsInQuestFrame = newBase + 0xE06740;
            Statics.AbandoningQuest = newBase + 0xE06750;
            Statics.FactionStandings = newBase + 0xE0B988;
            Statics.NumForcedReactions = newBase + 0xE0CBA4;
            Statics.ForcedReactions = newBase + 0xE0CBA8;
            Statics.ClientConnection = newBase + 0xEC4628;
            Statics.CurrentNetClient = newBase + 0xEC463C;
            Statics.LocalPlayerName = newBase + 0xEC4668;

            VFTableIndex.GetPosition = (IntPtr)0x27;
            VFTableIndex.GetScale = (IntPtr)0x2E;
            VFTableIndex.OnRightClick = (IntPtr)0x45;
            VFTableIndex.GetMatrix = (IntPtr)0x47;
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
