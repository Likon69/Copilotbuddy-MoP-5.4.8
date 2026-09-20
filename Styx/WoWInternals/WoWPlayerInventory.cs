using Styx.WoWInternals.WoWObjects;

namespace Styx.WoWInternals
{
    public class WoWPlayerInventory : WoWBag
    {
        internal WoWPlayerInventory(CGBag bag)
            : base(bag)
        {
            Equipped = new WoWPaperDoll(bag, 0U, 23U);
            Backpack = new WoWBag(bag, 23U, 16U);
            Bank = new WoWBag(bag, 39U, 28U);
            BankBags = new WoWBag(bag, 67U, 7U);
            Buyback = new WoWBag(bag, 74U, 12U);
        }

        public WoWPaperDoll Equipped { get; private set; }

        public WoWBag Backpack { get; private set; }

        public WoWBag Bank { get; private set; }

        public WoWBag BankBags { get; private set; }

        public WoWBag Buyback { get; private set; }
    }
}
