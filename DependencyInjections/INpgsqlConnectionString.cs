using System;
namespace AudioStreamingApi.DependencyInjections
{
	public interface INpgsqlConnectionString
	{
		public string ConnectionString { get; }
	}
}

