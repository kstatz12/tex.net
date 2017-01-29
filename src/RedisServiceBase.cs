using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace tex.net
{
    public abstract class RedisServiceBase : ServiceBase
    {
        public static List<ConnectionMultiplexer> Connections { get; set; }
        protected const string UnlockScript = @"
            if redis.call(""get"",KEYS[1]) == ARGV[1] then
                return redis.call(""del"",KEYS[1])
            else
                return 0
            end";

        public virtual void Init(params string[] servers)
        {
            ResolveConnections(s => ConnectionMultiplexer.Connect(s), servers);
        }

        private static void ResolveConnections(Func<string, ConnectionMultiplexer> connect, IEnumerable<string> inputs)
        {
            Connections = new List<ConnectionMultiplexer>();
            inputs.ToList().ForEach(x => Connections.Add(connect(x)));
        }


        public virtual void Dispose()
        {
            foreach (var connection in Connections)
            {
                connection.Close();
            }
            Connections.Clear();
        }
    }
}