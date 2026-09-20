using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Common
{
    public class ReplaceCompositeListOperation : CompositeListOperation
    {
        public ReplaceCompositeListOperation(Composite composite)
            : base(composite)
        {
        }

        public override void ApplyTo(List<Composite> compositeList)
        {
            compositeList.Clear();
            if (Composite != null)
                compositeList.Add(Composite);
        }
    }
}
