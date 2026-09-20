using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary>A small action composite to force any part of the tree to not execute for a given amount of time.</summary>
    /// <remarks>Created 3/26/2011.</remarks>
    public class Sleep : Composite
    {
        private readonly Func<object, TimeSpan> _endRetriever;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sleep"/> class.
        /// </summary>
        /// <param name="retriever">The retriever.</param>
        /// <remarks>Created 2012-02-28</remarks>
        public Sleep(Func<object, TimeSpan> retriever)
        {
            _endRetriever = retriever;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sleep"/> class.
        /// </summary>
        /// <param name="milliseconds">The milliseconds.</param>
        /// <remarks>Created 2012-02-28</remarks>
        public Sleep(int milliseconds)
            : this(ctx => TimeSpan.FromMilliseconds(milliseconds))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sleep"/> class.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <remarks>Created 2012-02-28</remarks>
        public Sleep(TimeSpan time)
            : this(ctx => time)
        {
        }

        private DateTime _end;
        /// <summary>
        /// Initializes the Task into a runnable state by clearing the LastStatus (if any),
        /// clearing the cleanup handlers remaining (if any -- note that this may also trigger the
        /// IDisposable behavior of any remaining cleanup handlers on the cleanup stack), and reinitializing
        /// the execution enumerator.
        /// </summary>
        /// <param name="context"></param>
        /// <remarks>Created 2012-02-28</remarks>
        public override void Start(object context)
        {
            Debug.Assert(_endRetriever != null, "_endRetriever was null");

            _end = DateTime.Now + _endRetriever(context);
            base.Start(context);
        }

        /// <summary>
        /// Actually executes one behavior tick
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>Created 2012-02-28</remarks>
        protected override IEnumerable<RunStatus> Execute(object context)
        {
            while (DateTime.Now < _end)
                yield return RunStatus.Running;

            if (Parent is Selector)
                yield return RunStatus.Failure;

            else
                yield return RunStatus.Success;
        }
    }
}
