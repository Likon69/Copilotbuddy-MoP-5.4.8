using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   This composite will perform a 'switch' statement to execute a specific branch of logic.
    ///   This is useful for selecting specific branches, for different types of agents. (e.g. rogue, mage, and warrior branches)
    /// </summary>
    /// <typeparam name = "T"></typeparam>
    public class Switch<T> : GroupComposite
    {
        private readonly Dictionary<T, Composite> _argumentDictionary;

        [DebuggerStepThrough]
        public Switch(RetrieveSwitchParameterDelegate<T> statement, params SwitchArgument<T>[] args)
            : base(args.Select(a => a.Branch).ToArray())
        {
            Statement = statement;
            Arguments = args;

            _argumentDictionary = new Dictionary<T, Composite>();
            foreach (var switchArgument in args)
            {
                _argumentDictionary.Add(switchArgument.RequiredValue, switchArgument.Branch);
            }
        }

        [DebuggerStepThrough]
        public Switch(RetrieveSwitchParameterDelegate<T> statement, Composite defaultArgument, params SwitchArgument<T>[] args)
            : this(statement, args)
        {
            Default = defaultArgument;

            _argumentDictionary = new Dictionary<T, Composite>();
            foreach (var switchArgument in args)
            {
                _argumentDictionary.Add(switchArgument.RequiredValue, switchArgument.Branch);
            }
        }

        /// <summary>
        ///   The statement assigned to this Switch that will determine which logical branch to take.
        /// </summary>
        protected RetrieveSwitchParameterDelegate<T> Statement { get; set; }

        /// <summary>
        ///   The switch arguments.
        /// </summary>
        protected SwitchArgument<T>[] Arguments { get; set; }

        /// <summary>
        ///   The 'default' argument to be carried out if no other switch conditions are met.
        /// </summary>
        protected Composite Default { get; set; }

        [DebuggerStepThrough]
        protected void RunSwitch(object context)
        {
            if (Arguments == null && Default == null)
            {
                throw new NullReferenceException("Switch statement has no arguments, and no default statement. Can not run.");
            }

            if (Statement == null)
                throw new NullReferenceException("Switch statement is null.");

            // Run the statement, and get the value for our switch.
            T value = Statement(context);

            // Since we can't do an *actual* switch statement,
            // this is the best we can do. It works in the same way,
            // except that it's slower, and may cause severe performance
            // hits if there are a large number of switch cases.
            if (Arguments != null)
            {
                Composite select;
                if (_argumentDictionary.TryGetValue(value, out select))
                {
                    Selection = select;
                    return;
                }
            }

            if (Default != null)
            {
                Selection = Default;
            }
        }

        [DebuggerStepThrough]
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            RunSwitch(context);

            if (Selection == null)
            {
                yield return RunStatus.Failure;
                yield break;
            }

            Selection.Start(context);
            while (Selection.Tick(context) == RunStatus.Running)
            {
                yield return RunStatus.Running;
            }

            Selection.Stop(context);

            if (Selection.LastStatus == RunStatus.Failure)
            {
                yield return RunStatus.Failure;
                yield break;
            }

            yield return RunStatus.Success;
            yield break;
        }
    }
}
