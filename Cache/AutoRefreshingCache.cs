using System;
using System.Runtime.Caching;
using System.Threading;

namespace Cache
{
    public class AutoRefreshingCache<T>
    {
        private readonly Func<string, T> updateCacheFunc;
        private readonly double refreshMinutes;
        private readonly MemoryCache cache = new MemoryCache(typeof(T).ToString());
        public AutoRefreshingCache(Func<string, T> updateCache, double refreshMinutes)
        {
            this.updateCacheFunc = updateCache;
            this.refreshMinutes = refreshMinutes;
        }
        public T Get(string key)
        {
            var item = cache.Get(key);
            if (item == null) // double-check locking (prevent double initialisation)
            {
                lock (this)
                {
                    item = cache.Get(key);
                    if (item == null)
                    {
                        item = updateCacheFunc(key);
                        cache.Set(new CacheItem(key, item), GetPolicy());
                    }
                }
            }
            return (T)item;
        }
        private CacheItemPolicy GetPolicy()
        {
            return new CacheItemPolicy
            {
                UpdateCallback = CacheItemRemoved,
                SlidingExpiration = TimeSpan.FromMinutes(refreshMinutes),  //set your refresh interval
            };
        }
        private void CacheItemRemoved(CacheEntryUpdateArguments args)
        {
            if (args.RemovedReason == CacheEntryRemovedReason.Expired || args.RemovedReason == CacheEntryRemovedReason.Removed)
            {
                var key = args.Key;
                args.UpdatedCacheItem = new CacheItem(key, updateCacheFunc(key));
                args.UpdatedCacheItemPolicy = GetPolicy();
            }
        }
    }

}
