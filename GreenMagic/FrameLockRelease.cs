using System;
using System.Diagnostics;
using System.Threading;

namespace GreenMagic
{
    public class FrameLockRelease : IDisposable
    {
        public FrameLockRelease(Executor executor, bool reacquireAsHardLock)
        {
            _executor = executor;
            _reacquireAsHardLock = reacquireAsHardLock;
            bool lockTaken = false;
            bool enterSucceeded = false;
            try
            {
                Monitor.Enter(executor.AssemblyLock, ref enterSucceeded);
                if (executor.IsExecutingContinuously)
                {
                    executor.EndExecute();
                    _lastCacheState = executor.Memory.CacheEnabled;
                    executor.Memory.DisableCache();
                    _wasReleased = true;
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

        private void Restore()
        {
            if (!_disposed)
            {
                try
                {
                    if (_wasReleased)
                    {
                        Debug.Assert(!_executor.IsExecutingContinuously, "We're still executing synchronously! Some nesting of FrameLock classes is horribly wrong..");
                        if (_lastCacheState)
                        {
                            _executor.Memory.EnableCache();
                            _executor.Memory.ClearCache();
                        }
                        else
                        {
                            _executor.Memory.DisableCache();
                        }
                        _executor.BeginExecute();
                        if (_reacquireAsHardLock)
                        {
                            _executor.GrabFrame();
                        }
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
                Restore();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private Executor _executor;

        private bool _disposed;

        private bool _wasReleased;

        private bool _lastCacheState;

        private bool _reacquireAsHardLock;
    }
}
