using System;

namespace Styx.Helpers
{
    /// <summary>
    /// Gives a setting a default value it will be initialized with.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DefaultValueAttribute : Attribute
    {
        public readonly object Value;

        public DefaultValueAttribute(object val)
        {
            Value = val;
        }
    }
}
