using System;
using Styx.Patchables;
using Styx.WoWInternals;

namespace Styx.CommonBot.Inventory
{
    public class LootRoll
    {
        public static event LootRoll.LootRollEventDelegate OnLootRoll;

        internal static void Initialize()
        {
            Styx.WoWInternals.Lua.Events.AttachEvent("START_LOOT_ROLL", new Styx.WoWInternals.LuaEventHandlerDelegate(LootRoll.HandleStartLootRoll));
        }

        private static void HandleStartLootRoll(object sender, Styx.WoWInternals.LuaEventArgs e)
        {
            int rollId = (int)((double)e.Args[0]);
            Styx.Patchables.LootRollItemInfo? lootRollItemInfo = LootRoll.FindLootRollItem(rollId);
            if (lootRollItemInfo != null && LootRoll.OnLootRoll != null)
            {
                LootRoll.OnLootRoll(lootRollItemInfo.Value);
            }
        }

        private static Styx.Patchables.LootRollItemInfo? FindLootRollItem(int rollId)
        {
            for (Styx.Patchables.LootRollItemInfo? next = new Styx.Patchables.LootRollItemInfo?(StyxWoW.Memory.Read<Styx.Patchables.LootRollItemInfo>(StyxWoW.Memory.Read<IntPtr>(StyxWoW.Offsets.Statics.LootRollInfo))); next != null; next = next.Value.Next)
            {
                if ((ulong)next.Value.RollId == (ulong)((long)rollId))
                {
                    return new Styx.Patchables.LootRollItemInfo?(next.Value);
                }
            }
            return null;
        }

        public delegate void LootRollEventDelegate(Styx.Patchables.LootRollItemInfo info);

        public class LootRollItem
        {
            internal LootRollItem(Styx.Patchables.LootRollItemInfo info)
            {
                this.lootRollItemInfo = info;
            }

            public uint RollId
            {
                get
                {
                    return this.lootRollItemInfo.RollId;
                }
            }

            public bool CanNeed
            {
                get
                {
                    return (this.lootRollItemInfo.RollFlags & 2U) != 0U;
                }
            }

            public bool CanGreed
            {
                get
                {
                    return (this.lootRollItemInfo.RollFlags & 4U) != 0U;
                }
            }

            public bool CanDisenchant
            {
                get
                {
                    return (this.lootRollItemInfo.RollFlags & 8U) != 0U;
                }
            }

            private Styx.Patchables.LootRollItemInfo lootRollItemInfo;
        }
    }
}
