using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct StaticOffsets
    {
        public IntPtr LuaDummyNode;
        public IntPtr GameObjectDataSlots;
        public IntPtr LuaState;
        public IntPtr TimerStruct;
        public IntPtr GxApi;
        public IntPtr Base_DbCache;
        public IntPtr Base_DbGameObjectCache;
        public IntPtr QuestCache;
        public IntPtr DbItemCache;
        public IntPtr DbItemSparseCache;
        public IntPtr SpellReagentsCacheObj;
        public IntPtr SpellCacheMaxId;
        public IntPtr SpellCacheBase;
        public IntPtr LocalPlayerAutoRepeatingSpell;
        public IntPtr LocalPlayerPendingCursorSpell;
        public IntPtr HomeBindAreaId;
        public IntPtr LocalPlayerDualWieldSpellId;
        public IntPtr UnitPowerIndices;
        public IntPtr MinimapZoneText;
        public IntPtr SubZoneText;
        public IntPtr ZoneText;
        public IntPtr RealZoneText;
        public IntPtr IsInGame;
        public IntPtr LocalPlayerFocusGuid;
        public IntPtr LocalPlayerCurrentTargetGuid;
        public IntPtr LocalPlayerCurrentZoneId;
        public IntPtr LocalPlayerCurrentSubZoneId;
        public IntPtr LocalPlayerCurrentContinentId;
        public IntPtr LocalPlayerCorpsePoint;
        public IntPtr LocalPlayerCorpseMapId;
        public IntPtr TotemInfos;
        public IntPtr MirrorTimers;
        public IntPtr NonPartyRaidMarkTargets;
        public IntPtr LocalPlayerNumKnownSpells;
        public IntPtr LocalPlayerKnownSpells;
        public IntPtr LocalPlayerTotemBars;
        public IntPtr LocalPlayerBattlefieldInstanceType;
        public IntPtr LocalPlayerComboPoints;
        public IntPtr LocalPlayerComboPointsTarget;
        public IntPtr PetSpells;
        public IntPtr StabledPets;
        public IntPtr LocalPlayerBagArray;
        public IntPtr LocalPlayerRuneTypes;
        public IntPtr LocalPlayerRunes;
        public IntPtr NumQuestsInQuestFrame;
        public IntPtr AbandoningQuest;
        public IntPtr FactionStandings;
        public IntPtr NumForcedReactions;
        public IntPtr ForcedReactions;
        public IntPtr ClientConnection;
        public IntPtr CurrentNetClient;
        public IntPtr LocalPlayerName;
        public IntPtr LocalPlayerSpellsOnCooldown;
        public IntPtr LocalPlayerIsAfk;
        public IntPtr SpellOverrideListNext;
        public IntPtr SpellOverrideListStart;
        public IntPtr LastHardwareAction;
        public IntPtr CurrencyInfo;
        public IntPtr RaidMemberInfoListPtr2;
        public IntPtr RaidMemberInfoListPtr;
        public IntPtr QuestsFlaggedCompletedList;
        public IntPtr PlayerCurrentQuests;
        public IntPtr CurrentQueuedBGs;
        public IntPtr ClickToMove_Base;
        public IntPtr ActiveMover;
        public IntPtr ActiveInputControl;
        public IntPtr BattlefieldInstanceStartTime;
        public IntPtr BattlefieldEnded;
        public IntPtr BattlefieldWinner;
        public IntPtr NumMapLandmarks;
        public IntPtr LandMarksArray;
        public IntPtr BattlefieldStatusMapId;
    }
}
