using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cache.Tests
{
    [TestClass]

    public class AutoRefreshingCacheTests
    {
        private const string key = "key";
        [TestMethod]
        public void OnlyOneInParallel()
        {
            var i = 0;
            var cache = new AutoRefreshingCache<string>((k) => { i++; return "test"; }, 1);
            Parallel.ForEach(Enumerable.Range(1, 5), (a) => cache.Get("key"));
            Assert.IsTrue(i == 1);
        }

        [TestMethod]
        public void OnlyOneInParallel_LongGetOperation()
        {
            var i = 0;
            var cache = new AutoRefreshingCache<string>((k) => { i++; Thread.Sleep(1000); return "test"; }, 1);
            Parallel.ForEach(Enumerable.Range(1, 3), (a) => cache.Get("key"));
            Assert.IsTrue(i == 1);
        }

        [TestMethod]
        public void DontCacheExceptions()
        {
            var i = 0;
            var cache = new AutoRefreshingCache<string>((k) =>
            {
                i++;
                if (i == 1)
                {
                    throw new Exception();
                }
                return "test";
            }, 1);

            try
            {
                var first = cache.Get("key");
            }
            catch (Exception)
            {

            }

            var second = cache.Get("key");
            Assert.IsTrue(second == "test");
        }

        [TestMethod]
        public void CacheIsAutoRefreshing()
        {
            var i = 0;
            var cache = new AutoRefreshingCache<string>((k) =>
            {
                i++;
                if (i == 2)
                {
                    Thread.Sleep(250);
                }
                return "test";
            }, 1.0 / 60);

            var first = cache.Get("key");
            while (i < 2)
            {
                var duration = Duration(() =>
                {
                    var second = cache.Get("key");
                });
                Assert.IsTrue(duration.TotalMilliseconds < 200);
            }
        }

        [TestMethod]
        public void ExceptionInAsyinc()
        {
            var i = 0;
            var exceptionThrown = false;
            var cache = new AutoRefreshingCache<string>((k) =>
            {
                i++;
                if (i == 2)
                {
                    exceptionThrown = true;
                    throw new Exception();
                }
                return "test";
            }, 1.0 / 60);

            var first = cache.Get("key");
            Thread.Sleep(1500);

            var second = cache.Get("key");

            Assert.IsTrue(exceptionThrown);
            Assert.IsTrue(second == "test");
        }

        private TimeSpan Duration(Action action)
        {
            var start = DateTime.Now;
            action.Invoke();
            var stop = DateTime.Now;
            return stop - start;
        }
    }
}
