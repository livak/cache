using System;
using System.Runtime.Caching;

namespace Cache
{
    public class LazyCache<T> 
    {
        private readonly Func<string, T> updateCacheFunc;
        private readonly double refreshMinutes;

        private readonly MemoryCache cache = new MemoryCache(typeof(T).ToString());
        private readonly object lockObject = new object();
        public LazyCache(Func<string, T> updateCache, double refreshMinutes)
        {
            this.updateCacheFunc = updateCache;
            this.refreshMinutes = refreshMinutes;
        }
        public T Get(string key)
        {
            var item = cache.Get(key);
            if (item == null) // double-check locking (prevent double initialisation)
            {
                lock (lockObject)
                {
                    item = cache.Get(key);
                    if (item == null)
                    {
                        item = updateCacheFunc(key);
                        cache.Add(key, item, DateTimeOffset.Now.AddMinutes(refreshMinutes));
                    }
                }
            }
            return (T)item;
        }
    }
}
