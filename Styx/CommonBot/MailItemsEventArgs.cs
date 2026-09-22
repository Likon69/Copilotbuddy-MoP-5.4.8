using System.Collections.Generic;
using Styx.WoWInternals.WoWObjects;

namespace Styx.CommonBot
{
    public class MailItemsEventArgs
    {
        public MailItemsEventArgs()
        {
            AdditionalItems = new List<WoWItem>();
        }

        public List<WoWItem> AdditionalItems { get; private set; }
    }
}
