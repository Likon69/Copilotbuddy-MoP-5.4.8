using System;
using Styx.CommonBot.ObjectDatabase;
using Styx.WoWInternals.DBC;

namespace Styx.CommonBot.Database
{
    public static class NpcQueries
    {
        [Obsolete("Use Query.GetNpcByName instead")]
        public static NpcResult GetNpcByName(string name)
        {
            return Query.GetNpcByName(name);
        }

        [Obsolete("Use Query.GetNpcById instead")]
        public static NpcResult GetNpcById(uint id)
        {
            return Query.GetNpcById(id);
        }

        [Obsolete("Use Query.GetNearestNpcById instead")]
        public static NpcResult GetNearestNpcById(uint id, WoWPoint searchLocation)
        {
            return Query.GetNearestNpcById(id, searchLocation);
        }

        [Obsolete("Use Query.GetNearestTrainer instead")]
        public static NpcResult GetNearestTrainer(uint mapId, WoWPoint searchLocation, WoWClass searchClass)
        {
            return Query.GetNearestTrainer(mapId, searchLocation, searchClass);
        }

        [Obsolete("Use Query.GetNearestNpc instead")]
        public static NpcResult GetNearestNpc(uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags)
        {
            return Query.GetNearestNpc(mapId, searchLocation, npcFlags);
        }

        [Obsolete("Use Query.GetNearestNpc(uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags) overload instead.")]
        public static NpcResult GetNearestNpc(FactionTemplate myFaction, uint mapId, WoWPoint searchLocation, UnitNPCFlags npcFlags)
        {
            return Query.GetNearestNpc(myFaction, mapId, searchLocation, npcFlags);
        }
    }
}
