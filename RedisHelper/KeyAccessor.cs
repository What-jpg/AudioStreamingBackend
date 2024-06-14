using StackExchange.Redis;

namespace AudioStreamingApi.RedisHelper
{
    public class KeyAccessor
    {
        public KeyAccessor(string keyType, IDatabase db)
        {
            this.keyType = keyType;
            this.db = db;
        }

        private string keyType;

        private IDatabase db;

        private string ConnectKeyToKeyType(string key) 
        {
            return keyType + "|::|" + key;
        }

        public string? GetValue(string key)
        {
            return db.StringGet(ConnectKeyToKeyType(key));
        }

        public bool SetValue(string key, string value, TimeSpan? expirationTime = null)
        {
            return db.StringSet(ConnectKeyToKeyType(key), value, expirationTime);
        }

        public bool DeleteValue(string key)
        {
            return db.KeyDelete(ConnectKeyToKeyType(key));
        }
    }
}