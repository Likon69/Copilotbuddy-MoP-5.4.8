using System;

namespace Styx.Offsets
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct StaticOffsets
    {
        public IntPtr GameObjectDataSlots;
        public IntPtr LuaState;
        public IntPtr TimerStruct;
        public IntPtr GxApi;
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
    }
}
