using System;

namespace Styx.Common
{
    public static class CommandLine
    {
        public static Arguments Arguments { get; private set; } = new Arguments(Environment.GetCommandLineArgs());
    }
}
