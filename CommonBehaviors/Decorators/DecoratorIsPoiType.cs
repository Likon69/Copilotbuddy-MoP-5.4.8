using System.Collections.Generic;
using System.Linq;
using Styx.CommonBot.POI;
using Styx.TreeSharp;

namespace CommonBehaviors.Decorators
{
    public class DecoratorIsPoiType : Decorator
    {
        private IEnumerable<PoiType> _types;

        public DecoratorIsPoiType(PoiType type, Composite child)
            : this(new[] { type }, child)
        {
        }

        public DecoratorIsPoiType(IEnumerable<PoiType> types, Composite child)
            : base(child)
        {
            _types = types;
        }

        protected override bool CanRun(object context)
        {
            return _types.Any(type => type == BotPoi.Current.Type);
        }
    }
}
