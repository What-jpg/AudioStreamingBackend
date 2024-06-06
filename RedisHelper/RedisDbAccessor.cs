using StackExchange.Redis;

namespace AudioStreamingApi.RedisHelper
{
    public class RedisDBAccessor
    {
        public RedisDBAccessor(string connectionString)
        {
            var db = ConnectionMultiplexer.Connect(connectionString).GetDatabase();

            AuthCodes = new KeyAccessor("authCodes", db);
            ChangeCodes = new KeyAccessor("changeCodes", db);
            CurrentlyListening = new KeyAccessor("currentlyListening", db);
        }

        public KeyAccessor AuthCodes;
        public KeyAccessor ChangeCodes;
        public KeyAccessor CurrentlyListening;
    }
}
