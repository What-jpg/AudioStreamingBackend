using StackExchange.Redis;

namespace AudioStreamingApi.RedisHelper
{
    public class HashKeyAccessor
    {
        public HashKeyAccessor(string hash, IDatabase db)
        {
            this.hash = hash;
            this.db = db;
        }

        private string hash;

        private IDatabase db;

        private string ConnectHashKeyToSecondHashKey(string key)
        {
            return hash + "|::|" + key;
        }

        public string? GetHashValue(string key, string hash)
        {
            return db.HashGet(ConnectHashKeyToSecondHashKey(key), hash);
        }

        public bool SetHashValue(string key, string hash, string value)
        {
            return db.HashSet(ConnectHashKeyToSecondHashKey(key), hash, value);
        }

        public bool DeleteHashValue(string key, string hash)
        {
            return db.HashDelete(ConnectHashKeyToSecondHashKey(key), hash);
        }

        public bool DeleteHashKey(string key)
        {
            return db.KeyDelete(ConnectHashKeyToSecondHashKey(key));
        }

        public HashEntry[] GetAllHashesAndValues(string key)
        {
            return db.HashGetAll(ConnectHashKeyToSecondHashKey(key));
        }
    }
}
