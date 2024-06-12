using System;
namespace AudioStreamingApi.DependencyInjections
{
	public class NpgsqlConnectionString : INpgsqlConnectionString
	{
		public string ConnectionString { get { 
			var builderConfig = WebApplication.CreateBuilder().Configuration;
			
			return Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? builderConfig.GetConnectionString("Postgresql"); 
		} }

	}
}

