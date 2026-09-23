using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Bots.GatherBuddy;

public static class BagHelper
{
	public static uint EmptyMineSlots
	{
		get
		{
			uint emptySlots = StyxWoW.Me.Inventory.Backpack.FreeSlots;
			for (uint index = 0u; index < 4; index++)
			{
				WoWContainer bag = StyxWoW.Me.GetBagAtIndex(index);
				if (bag != null && (bag.BagType == BagType.MiningBag || bag.BagType == BagType.NormalBag))
				{
					emptySlots += bag.FreeSlots;
				}
			}
			return emptySlots;
		}
	}

	public static uint EmptyHerbSlots
	{
		get
		{
			uint emptySlots = StyxWoW.Me.Inventory.Backpack.FreeSlots;
			for (uint index = 0u; index < 4; index++)
			{
				WoWContainer bag = StyxWoW.Me.GetBagAtIndex(index);
				if (bag != null && (bag.BagType == BagType.HerbBag || bag.BagType == BagType.NormalBag))
				{
					emptySlots += bag.FreeSlots;
				}
			}
			return emptySlots;
		}
	}
}
