using System;

namespace JetBrains.Annotations
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class AspMvcControllerAttribute : Attribute
    {
        public AspMvcControllerAttribute()
        {
        }

        public AspMvcControllerAttribute([NotNull] string anonymousProperty)
        {
            AnonymousProperty = anonymousProperty;
        }

        [NotNull]
        public string AnonymousProperty { get; private set; }
    }
}
