using System;
using AudioStreamingApi.RedisHelper;

namespace AudioStreamingApi.DependencyInjections
{
	public class RedisConnection : IRedisConnection
	{
		public RedisDBAccessor Connection { get { return new RedisDBAccessor(WebApplication.CreateBuilder().Configuration.GetConnectionString("Redis")); } }
	}
}

