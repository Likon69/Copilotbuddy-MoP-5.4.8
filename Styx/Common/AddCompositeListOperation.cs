using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Common
{
    public class AddCompositeListOperation : CompositeListOperation
    {
        public AddCompositeListOperation(Composite composite)
            : base(composite)
        {
        }

        public override void ApplyTo(List<Composite> compositeList)
        {
            compositeList.Add(Composite);
        }
    }
}
