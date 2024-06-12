using System;
using AudioStreamingApi.RedisHelper;

namespace AudioStreamingApi.DependencyInjections
{
	public class RedisConnection : IRedisConnection
	{
		public RedisDBAccessor Connection { get { 
			var builderConfig = WebApplication.CreateBuilder().Configuration;

			return new RedisDBAccessor(Environment.GetEnvironmentVariable("REDIS_URL") ?? builderConfig.GetConnectionString("Redis")); 
		} }
	}
}

