using System;

namespace tex.net
{
    public interface IMutexService : IDisposable
    {
        bool Lock(Guid key, out byte[] uniqueKey);
        bool Lock(Guid key, TimeSpan ttl, out byte[] uniqueKey);
        void Unlock(Guid key, byte[] uniqueKey);
    }
}
