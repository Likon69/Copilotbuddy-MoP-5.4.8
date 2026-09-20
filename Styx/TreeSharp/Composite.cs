using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Styx.TreeSharp
{
    /// <summary>
    ///   The base class of the entire behavior tree system.
    ///   Nearly all branches derive from this class.
    /// </summary>
    public abstract class Composite : IEquatable<Composite>
    {
        /// <summary>
        /// Provides a lockable object so we can gain a mutex over the operations done in Tick(...)
        /// </summary>
        protected static readonly object Locker = new object();

        /// <summary>
        /// The current behavior (as a enumerator of Status codes)
        /// </summary>
        private IEnumerator<RunStatus> _current;

        protected Composite()
        {
            Guid = Guid.NewGuid();
            CleanupHandlers = new Stack<CleanupHandler>();
        }

        /// <summary>
        /// Contains the execution status (if any) provided by the last Tick()
        /// </summary>
        public RunStatus? LastStatus { get; set; }

        protected ContextChangeHandler ContextChanger { get; set; }
        protected Stack<CleanupHandler> CleanupHandlers { get; set; }

        public Composite Parent { get; set; }
        public Guid Guid { get; private set; }

        /// <summary>
        /// Returns true if the current behavior is running
        /// </summary>
        public bool IsRunning { get { return _current != null && LastStatus.HasValue && LastStatus.Value == RunStatus.Running; } }

        /// <summary>
        /// Actually executes one behavior tick
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<RunStatus> Execute(object context);

        /// <summary>
        /// Executes the user code of the function, as expressed in Execute()
        /// </summary>
        /// <returns>Status code indicating Success, Failure, or (still) Running</returns>
        /// <remarks>Should not usually be overridden.  Once a status of Success or Failure
        /// is returned, Tick() will no longer call Execute().  This allows status hysteresis.
        /// Note that exceptions are not handled; a Status.Failure is a "clean failure"
        /// (recoverable), whereas an exception indicates a "dirty failure" which we probably
        /// can't recover from.</remarks>
        [DebuggerStepThrough]
        public virtual RunStatus Tick(object context)
        {
            if (LastStatus.HasValue && LastStatus != RunStatus.Running)
                return LastStatus.Value;

            if (_current == null)
                throw new ApplicationException("Cannot run Tick before running Start first!");

            Stopwatch stopwatch = Stopwatch.StartNew();

            if (_current.MoveNext())
            {
                LastStatus = _current.Current;
                stopwatch.Stop();

                if (LastStatus != RunStatus.Running)
                    Stop(context);

                return LastStatus.GetValueOrDefault(RunStatus.Failure);
            }

            throw new ApplicationException("Nothing to run? Somethings gone terribly, terribly wrong!");
        }

        /// <summary>
        /// Initializes the Task into a runnable state by clearing the LastStatus (if any),
        /// clearing the cleanup handlers remaining (if any -- note that this may also trigger the
        /// IDisposable behavior of any remaining cleanup handlers on the cleanup stack), and reinitializing
        /// the execution enumerator.
        /// </summary>
        [DebuggerStepThrough]
        public virtual void Start(object context)
        {
            LastStatus = null;
            _current = Execute(context).GetEnumerator();
        }

        /// <summary>
        /// Stop will call all cleanup handlers!
        /// </summary>
        [DebuggerStepThrough]
        public virtual void Stop(object context)
        {
            Cleanup();
            if (_current != null)
            {
                _current.Dispose();
                _current = null;
            }

            if (LastStatus.HasValue && LastStatus.Value == RunStatus.Running)
            {
                LastStatus = RunStatus.Failure;
            }
        }

        [DebuggerStepThrough]
        protected void Cleanup()
        {
            if (CleanupHandlers.Count != 0)
            {
                while (CleanupHandlers.Count != 0)
                {
                    CleanupHandlers.Pop().Dispose();
                }
            }
        }

        #region Nested type: CleanupHandler

        /// <summary>
        /// Typed cleanup class
        /// </summary>
        protected abstract class CleanupHandler : IDisposable
        {
            private bool _disposed;

            [DebuggerStepThrough]
            protected CleanupHandler(Composite owner, object context)
            {
                Owner = owner;
                Context = context;
            }

            public Composite Owner { get; private set; }
            public object Context { get; private set; }
            public bool IsDisposed { get { return _disposed; } }

            #region IDisposable Members

            [DebuggerStepThrough]
            public void Dispose()
            {
                if (!IsDisposed)
                {
                    _disposed = true;
                    DoCleanup(Context);
                }
            }

            #endregion

            [DebuggerStepThrough]
            protected abstract void DoCleanup(object context);
        }

        #endregion

        #region IEquatable<Composite> Members

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///   true if the current object is equal to the <paramref name = "other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name = "other">An object to compare with this object.</param>
        [DebuggerStepThrough]
        public bool Equals(Composite other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.Guid.Equals(Guid);
        }

        #endregion

        #region Overrides of object

        /// <summary>
        ///   Determines whether the specified <see cref = "T:System.Object" /> is equal to the current <see cref = "T:System.Object" />.
        /// </summary>
        /// <returns>
        ///   true if the specified <see cref = "T:System.Object" /> is equal to the current <see cref = "T:System.Object" />; otherwise, false.
        /// </returns>
        /// <param name = "obj">The <see cref = "T:System.Object" /> to compare with the current <see cref = "T:System.Object" />. </param>
        /// <filterpriority>2</filterpriority>
        [DebuggerStepThrough]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(Composite))
                return false;
            return Equals((Composite)obj);
        }

        /// <summary>
        ///   Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        ///   A hash code for the current <see cref = "T:System.Object" />.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        [DebuggerStepThrough]
        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        #endregion

        #region Operators

        [DebuggerStepThrough]
        public static bool operator ==(Composite left, Composite right)
        {
            return Equals(left, right);
        }

        [DebuggerStepThrough]
        public static bool operator !=(Composite left, Composite right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
