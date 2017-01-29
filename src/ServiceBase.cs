using System;

namespace tex.net
{
    public abstract class ServiceBase
    {
        public virtual int RetryCount => 5;
        public virtual TimeSpan RetryDelay => new TimeSpan(0, 0, 0, 0, 500);
    }
}