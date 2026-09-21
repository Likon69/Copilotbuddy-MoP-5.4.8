using System;

namespace Styx.CommonBot.Profiles
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CustomBehaviorFileNameAttribute : Attribute
    {
        public CustomBehaviorFileNameAttribute(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; private set; }
    }
}
