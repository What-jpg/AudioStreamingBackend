using System;
namespace AudioStreamingApi.DependencyInjections
{
	public class NpgsqlConnectionString : INpgsqlConnectionString
	{
		public string ConnectionString { get { return WebApplication.CreateBuilder().Configuration.GetConnectionString("Postgresql"); } }

	}
}

