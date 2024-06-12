using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AudioStreamingApi.DependencyInjections;

namespace AudioStreamingApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();

        builder.Services.AddSingleton<INpgsqlConnectionString, NpgsqlConnectionString>();
        builder.Services.AddSingleton<IRedisConnection, RedisConnection>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration.GetSection("Jwt:Issuer").Get<string>();
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration.GetSection("Jwt:Key").Get<string>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetPreflightMaxAge(TimeSpan.FromSeconds(86400));
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(options =>
         {
             options.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidateLifetime = true,
                 ValidateIssuerSigningKey = true,
                 ValidIssuer = jwtIssuer,
                 ValidAudience = jwtIssuer,
                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
             };
         });

        var app = builder.Build();

        var requiredVarsEnviromentAndAppSettings =
            new string[][] {
              new string[] {"PORT", "Port"},
              new string[] {"JWT_KEY", "Jwt:Key"},
              new string[] {"JWT_ISSUER", "Jwt:Issuer"},
              new string[] {"CONNECTION_STRING", "ConnectionStrings:Postgresql"},
              new string[] {"REDIS_URL", "ConnectionStrings:Redis"},
              new string[] {"SMTP_CLIENT", "Smtp:Client"},
              new string[] {"SMTP_PORT", "Smtp:Port"},
              new string[] {"SMTP_USER_NAME_CREDENTIAL", "Smtp:UserNameCredential"},
              new string[] {"SMTP_PASSWORD_CREDENTIAL", "Smtp:PasswordCredential"},
              new string[] {"SMTP_EMAIL", "Smtp:Email"}
            };

        foreach (var key in requiredVarsEnviromentAndAppSettings)
        {
            var valueEnv = Environment.GetEnvironmentVariable(key[0]);
            var valueAppSettings = app.Configuration.GetSection(key[1]).Get<string>();

            if ((valueAppSettings == "" || valueAppSettings == null) && (valueEnv == "" || valueEnv == null))
            {
                throw new Exception($"Config variable is missing you can either add it to .env ({key[0]}) or to appsettings.json ({key[1]})");
            }
        }

        var port = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration.GetSection("Port").Get<string>();
        app.Urls.Add("http://*:" + port);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseCors();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}