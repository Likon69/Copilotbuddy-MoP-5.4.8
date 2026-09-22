using System.Collections.Generic;
using Styx.CommonBot.POI;
using Styx.TreeSharp;

namespace CommonBehaviors.Decorators
{
    public class DecoratorIsNotPoiType : DecoratorIsPoiType
    {
        public DecoratorIsNotPoiType(PoiType type, Composite child)
            : base(type, child)
        {
        }

        public DecoratorIsNotPoiType(IEnumerable<PoiType> types, Composite child)
            : base(types, child)
        {
        }

        protected override bool CanRun(object context)
        {
            return !base.CanRun(context);
        }
    }
}
