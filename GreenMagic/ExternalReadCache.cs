using System;

namespace GreenMagic
{
    public class ExternalReadCache : IDisposable
    {
        public ExternalReadCache(ExternalProcessMemory mem, bool tempCacheState)
        {
            _mem = mem;
            _cacheState = mem.CacheEnabled;
            if (tempCacheState)
            {
                _mem.EnableCache();
            }
            else
            {
                _mem.DisableCache();
            }
        }

        public ExternalReadCache(ExternalProcessMemory mem)
        {
            _mem = mem;
            _cacheState = mem.CacheEnabled;
        }

        private void RestoreCache()
        {
            if (!_disposed)
            {
                if (_cacheState)
                {
                    _mem.EnableCache();
                }
                else
                {
                    _mem.DisableCache();
                }
                _disposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                RestoreCache();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        private bool _cacheState;

        private ExternalProcessMemory _mem;
    }
}
