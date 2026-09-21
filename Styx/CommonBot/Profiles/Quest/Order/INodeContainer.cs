using System.Collections.Generic;

namespace Styx.CommonBot.Profiles.Quest.Order
{
    public interface INodeContainer
    {
        IEnumerable<OrderNode> GetNodes();
    }
}
