using System.Diagnostics;

namespace Styx.TreeSharp
{
    public class SwitchArgument<T>
    {
        [DebuggerStepThrough]
        public SwitchArgument(Composite branch, T requiredValue)
        {
            Branch = branch;
            RequiredValue = requiredValue;
        }

        public SwitchArgument(T requiredValue, Composite branch):this(branch,requiredValue){}

        /// <summary>
        ///   A branch of logic that will be executed if this argument is the correct switch.
        /// </summary>
        public Composite Branch { get; set; }

        /// <summary>
        ///   The value required for this logic branch to be executed.
        /// </summary>
        public T RequiredValue { get; set; }
    }
}
