using System;
using AudioStreamingApi.RedisHelper;

namespace AudioStreamingApi.DependencyInjections
{
	public interface IRedisConnection
	{
		public RedisDBAccessor Connection { get; }
	}
}

