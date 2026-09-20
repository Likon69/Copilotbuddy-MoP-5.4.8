using System;

namespace Styx.TreeSharp
{
    public class DynamicChildSelector : PrioritySelector
    {
        private readonly Func<object, Composite> _childGetter;

        public DynamicChildSelector(Func<object, Composite> childGetter)
        {
            _childGetter = childGetter;
        }

        public override void Start(object context)
        {
            Children.Clear();
            Children.Add(_childGetter(context));
            base.Start(context);
        }
    }
}
