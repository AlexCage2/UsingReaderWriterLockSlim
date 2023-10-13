using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchronizationPrimitives
{
    public sealed class SynchronizedCache
    {
        private ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private Dictionary<int, string> _innerCache = new Dictionary<int, string>();

        public int Count { get => _innerCache.Count; }

        public string Read(int key)
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _innerCache[key];
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        public void Add(int key, string value)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _innerCache[key] = value;
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public bool AddWithTimeout(int key, string value, int timeout)
        {
            if (!_cacheLock.TryEnterWriteLock(timeout))
            {
                return false;
            }

            try
            {
                _innerCache.Add(key, value);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }

            return true;
        }

        public AddOrUpdateStatus AddOrUpdate(int key, string value)
        {
            _cacheLock.EnterUpgradeableReadLock();
            try
            {
                string result = "";
                if (_innerCache.TryGetValue(key, out result))
                {
                    if (result == value)
                    {
                        return AddOrUpdateStatus.Unchanged;
                    }

                    _cacheLock.EnterWriteLock();
                    try
                    {
                        _innerCache[key] = value;
                    }
                    finally
                    {
                        _cacheLock.ExitWriteLock();
                    }

                    return AddOrUpdateStatus.Updated;
                }

                _cacheLock.EnterWriteLock();
                try
                {
                    _innerCache.Add(key, value);
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock(); 
            }

            return AddOrUpdateStatus.Added;
        }

        public void Delete(int key)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                _innerCache.Remove(key);
            }
            finally
            {
                _cacheLock.ExitWriteLock(); 
            }
        }

        public enum AddOrUpdateStatus
        {
            Added,
            Updated,
            Unchanged
        };

        ~SynchronizedCache()
        {
            if (_cacheLock is not null) _cacheLock.Dispose();
        }
    }
}
