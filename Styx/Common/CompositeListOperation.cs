using System.Collections.Generic;
using Styx.TreeSharp;

namespace Styx.Common
{
    public abstract class CompositeListOperation
    {
        protected CompositeListOperation(Composite composite)
        {
            _composite = composite;
        }

        public Composite Composite
        {
            get { return _composite; }
        }

        public abstract void ApplyTo(List<Composite> compositeList);

        private readonly Composite _composite;
    }
}
