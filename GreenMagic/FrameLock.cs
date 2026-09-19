using System;
using System.Threading;

namespace GreenMagic
{
    public class FrameLock : IDisposable
    {
        public FrameLock(Executor executor)
        {
            _executor = executor;
            bool lockTaken = false;
            bool enterSucceeded = false;
            try
            {
                Monitor.Enter(executor.AssemblyLock, ref enterSucceeded);
                if (!executor.IsExecutingContinuously)
                {
                    executor.BeginExecute();
                    _wasAcquired = true;
                }
                lockTaken = true;
            }
            finally
            {
                if (!lockTaken && enterSucceeded)
                {
                    Monitor.Exit(executor.AssemblyLock);
                }
            }
        }

        private void ReleaseLock()
        {
            if (!_disposed)
            {
                try
                {
                    if (_executor.IsExecutingContinuously && _wasAcquired)
                    {
                        _executor.EndExecute();
                    }
                }
                catch (Exception)
                {
                }
                Monitor.Exit(_executor.AssemblyLock);
                _disposed = true;
            }
        }

        public Executor Executor
        {
            get { return _executor; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseLock();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private Executor _executor;

        private bool _disposed;

        private bool _wasAcquired;
    }
}
