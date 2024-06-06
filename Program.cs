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

        var jwtIssuer = builder.Configuration.GetSection("Jwt:Issuer").Get<string>();
        var jwtKey = builder.Configuration.GetSection("Jwt:Key").Get<string>();

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

        var requiredVarsAppSettings =
            new string[] {
          "Port",
          "JWT:Key",
          "JWT:Issuer",
          "ConnectionStrings:Postgresql",
          "ConnectionStrings:Redis",
          "Smtp:Client",
          "Smtp:Port",
          "Smtp:UserNameCredential",
          "Smtp:PasswordCredential",
          "Smtp:Email",
            };

        foreach (var key in requiredVarsAppSettings)
        {
            var value = app.Configuration.GetSection(key).Get<string>();

            if (value == "" || value == null)
            {
                throw new Exception($"AppSetings config variable missing: {key}.");
            }
        }

        app.Urls.Add(
            $"https://+:{app.Configuration.GetSection("Port").Get<string>()}");

        // Configure the HTTP request pipeline.

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