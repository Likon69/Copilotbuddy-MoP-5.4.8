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
    }
}
