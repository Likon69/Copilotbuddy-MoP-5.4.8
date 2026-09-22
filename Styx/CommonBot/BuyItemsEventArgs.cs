using System.Collections.Generic;

namespace Styx.CommonBot
{
    public class BuyItemsEventArgs
    {
        public BuyItemsEventArgs()
        {
            BuyItemsIds = new Dictionary<uint, int>();
        }

        public Dictionary<uint, int> BuyItemsIds { get; private set; }
    }
}
