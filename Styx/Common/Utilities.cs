using System;
using System.IO;
using System.Reflection;

namespace Styx.Common
{
    public static class Utilities
    {
        public static string AssemblyDirectory
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string GetObjectString(object value, string nullValue)
        {
            return value != null ? value.ToString() : nullValue;
        }
    }
}
