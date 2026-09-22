using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Styx.Common
{
    public static class Beta
    {
        [Conditional("NIGHTLY_BUILD")]
        [Conditional("BETA_BUILD")]
        [Conditional("DEBUG")]
        [JetBrains.Annotations.ContractAnnotation("condition:false => halt")]
        public static void Assert(bool condition)
        {
            Trace.Assert(condition);
        }

        [Conditional("BETA_BUILD")]
        [JetBrains.Annotations.ContractAnnotation("condition:false => halt")]
        [Conditional("DEBUG")]
        [Conditional("NIGHTLY_BUILD")]
        public static void Assert(bool condition, string message)
        {
            Trace.Assert(condition, message);
        }

        [Conditional("NIGHTLY_BUILD")]
        [JetBrains.Annotations.ContractAnnotation("condition:false => halt")]
        [Conditional("DEBUG")]
        [Conditional("BETA_BUILD")]
        public static void Assert(bool condition, string message, string detailMessage)
        {
            Trace.Assert(condition, message, detailMessage);
        }

        [Conditional("BETA_BUILD")]
        [JetBrains.Annotations.ContractAnnotation("=> halt")]
        [Conditional("NIGHTLY_BUILD")]
        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            Trace.Fail(message);
        }

        [Conditional("DEBUG")]
        [Conditional("BETA_BUILD")]
        [Conditional("NIGHTLY_BUILD")]
        [JetBrains.Annotations.ContractAnnotation("=> halt")]
        public static void Fail(string message, string detailMessage)
        {
            Trace.Fail(message, detailMessage);
        }
    }
}
