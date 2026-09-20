#region License

// A simplistic Behavior Tree implementation in C#
// Copyright (C) 2010  Anonymous Contributor
//
// This file is part of TreeSharp.

#endregion

using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   The base selector class. This will attempt to execute all branches of logic, until one succeeds.
    ///   This composite will fail only if all branches fail as well.
    /// </summary>
    public abstract class Selector : GroupComposite
    {
        [DebuggerStepThrough]
        protected Selector(params Composite[] children)
            : base(children)
        {
        }

        protected abstract override IEnumerable<RunStatus> Execute(object context);
    }
}
