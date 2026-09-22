using System;

namespace JetBrains.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class AspMvcActionAttribute : Attribute
    {
        public AspMvcActionAttribute()
        {
        }

        public AspMvcActionAttribute([NotNull] string anonymousProperty)
        {
            AnonymousProperty = anonymousProperty;
        }

        [NotNull]
        public string AnonymousProperty { get; private set; }
    }
}
