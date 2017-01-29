using System;
using System.Threading;
using StackExchange.Redis;

namespace tex.net
{
    public class RedisMutexService : RedisServiceBase, IMutexService
    {
        private static RedisMutexService _instance;
        private static readonly object Locker = new object();
        protected RedisMutexService()
        {
        }

        public static RedisMutexService GetInstance()
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance = new RedisMutexService();
            }
            return _instance;
        }

        public override void Dispose()
        {
            _instance = null;
            base.Dispose();
        }

        public bool Lock(Guid key, out byte[] uniqueKey)
        {
            if (Connections.Count <= 0)
            {
                throw new Exception("No Servers Provided");
            }
            var redisKey = (RedisKey)key.ToString();
            uniqueKey = Guid.NewGuid().ToByteArray();
            var timeToLive = new TimeSpan(0, 0, 0, 0, 500);
            var success = false;
            var internalKey = uniqueKey;
            success = Retry(() =>
            {
                InvokeLocker(conn =>
                {
                    success = GetInstanceLock(conn, redisKey, internalKey, timeToLive);
                });
                return success;
            });
            return success;
        }

        public bool Lock(Guid key, TimeSpan ttl, out byte[] uniqueKey)
        {
            if (Connections.Count <= 0)
            {
                throw new Exception("No Servers Provided");
            }
            uniqueKey = Guid.NewGuid().ToByteArray();
            var redisKey = (RedisKey)key.ToString();
            var success = true;
            var internalKey = uniqueKey;
            success = Retry(() =>
            {
                InvokeLocker(conn =>
                {
                    success = GetInstanceLock(conn, redisKey, internalKey, ttl);
                });
                return success;
            });
            return success;
        }

        public void Unlock(Guid key, byte[] uniqueKey)
        {
            if (Connections.Count <= 0)
            {
                throw new Exception("No Servers Provided");
            }
            InvokeUnlocker(conn => UnlockInstanceLock(conn, key, uniqueKey));
        }

        private static void InvokeLocker(Action<ConnectionMultiplexer> action)
        {
            Connections.ForEach(action);
        }

        private static void InvokeUnlocker(Action<ConnectionMultiplexer> action)
        {
            Connections.ForEach(action);
        }

        private bool Retry(Func<bool> action)
        {
            var currentRetry = 0;
            while (currentRetry++ < RetryCount)
            {
                if (action()) return true;
                var delay = GetDelay(currentRetry);
                Thread.Sleep(delay);
            }
            return false;
        }

        private int GetDelay(int currentRetry)
        {
            return (int)RetryDelay.TotalMilliseconds * (currentRetry + 1);
        }

        private static bool GetInstanceLock(IConnectionMultiplexer conn, RedisKey key, byte[] uniqueKey, TimeSpan timeToLive)
        {
            try
            {
                return conn.GetDatabase().StringSet(key, uniqueKey, timeToLive, When.NotExists);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void UnlockInstanceLock(IConnectionMultiplexer conn, Guid key, byte[] uniqueKey)
        {
            RedisKey[] redisKeys = { key.ToString() };
            RedisValue[] values = { uniqueKey };
            conn.GetDatabase().ScriptEvaluate(UnlockScript, redisKeys, values);
        }
    }
}