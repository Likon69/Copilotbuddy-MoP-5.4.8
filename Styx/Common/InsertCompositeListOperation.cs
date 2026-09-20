using System;
using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Common
{
    public class InsertCompositeListOperation : CompositeListOperation
    {
        public InsertCompositeListOperation(Composite composite, int index)
            : base(composite)
        {
            Index = index;
        }

        public int Index { get; private set; }

        public override void ApplyTo(List<Composite> compositeList)
        {
            compositeList.Insert(Math.Max(0, Math.Min(compositeList.Count, Index)), Composite);
        }
    }
}
