using System.Collections.Generic;

namespace Styx.CommonBot
{
    public class SellItemsEventArgs
    {
        public SellItemsEventArgs()
        {
            NameExceptions = new List<string>();
            IdExceptions = new List<uint>();
        }

        public List<string> NameExceptions { get; private set; }
        public List<uint> IdExceptions { get; private set; }
    }
}
