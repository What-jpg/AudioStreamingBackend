using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AudioStreamingApi.DependencyInjections;
using Npgsql;
using Dapper;
using AudioStreamingApi.Models.DbModels;
using AudioStreamingApi.Components;

namespace AudioStreamingApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();

        builder.Services.AddHealthChecks();

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

        using (var connection = new NpgsqlConnection(new NpgsqlConnectionString().ConnectionString))
        {
            try
            {
                var testDbFiles = connection.Query<DbFile>("SELECT * FROM \"DbFiles\" LIMIT 1").ToArray();

                if (testDbFiles.Length != 0)
                {
                    try
                    {
                        HttpControllersMethods.GetBytesFromDbFile(testDbFiles[0]);
                    } catch
                    {
                        connection.Query("DROP TABLE \"DbFiles\", \"Users\", \"Discography\", \"Songs\", \"ArtistsFollowers\", \"DiscographyFollowers\", \"SongsFollowers\", \"SongsListened\"");

                        throw new Exception("Exception for calling previous try block");
                    }
                }
            } catch
            {
                connection.Query("CREATE TABLE \"DbFiles\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, path text NOT NULL, type text NOT NULL)");

                connection.Query("CREATE TABLE \"Users\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"avatarId\" integer REFERENCES \"DbFiles\"(id), \"hashedPassword\" text NOT NULL, email text NOT NULL, name text NOT NULL, \"isTwoFactorAuthActive\" boolean NOT NULL DEFAULT false)");

                connection.Query("CREATE TABLE \"Discography\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"artistId\" integer NOT NULL REFERENCES \"Users\"(id), name text NOT NULL, \"coverId\" integer REFERENCES \"DbFiles\"(id), \"createdAt\" timestamp without time zone NOT NULL DEFAULT now())");

                connection.Query("CREATE TABLE \"Songs\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, name text NOT NULL, \"discId\" integer NOT NULL REFERENCES \"Discography\"(id), \"contentId\" integer NOT NULL REFERENCES \"DbFiles\"(id), \"createdAt\" timestamp without time zone NOT NULL DEFAULT now(), \"totalTime\" interval NOT NULL DEFAULT '00:00:00'::interval)");

                connection.Query("CREATE TABLE \"ArtistsFollowers\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"followerId\" integer NOT NULL REFERENCES \"Users\"(id), \"elementId\" integer NOT NULL REFERENCES \"Users\"(id))");

                connection.Query("CREATE TABLE \"DiscographyFollowers\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"followerId\" integer NOT NULL REFERENCES \"Users\"(id), \"elementId\" integer NOT NULL REFERENCES \"Discography\"(id))");

                connection.Query("CREATE TABLE \"SongsFollowers\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"followerId\" integer NOT NULL REFERENCES \"Users\"(id), \"elementId\" integer NOT NULL REFERENCES \"Songs\"(id))");

                connection.Query("CREATE TABLE \"SongsListened\" (id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY, \"listenerId\" integer NOT NULL REFERENCES \"Users\"(id), \"songId\" integer NOT NULL REFERENCES \"Songs\"(id), \"listenedAt\" timestamp without time zone NOT NULL DEFAULT now())");
            }
        }

        string port = Environment.GetEnvironmentVariable("PORT") ?? builder.Configuration.GetSection("Port").Get<string>();

        app.Urls.Add("http://*:" + port);

        var requiredVarsEnviromentAndAppSettings =
            new List<string[]> {
                new string[] {"PORT", "Port"},
                new string[] {"JWT_KEY", "Jwt:Key"},
                new string[] {"JWT_ISSUER", "Jwt:Issuer"},
                new string[] {"CONNECTION_STRING", "ConnectionStrings:Postgresql"},
                new string[] {"REDIS_URL", "ConnectionStrings:Redis"}
            };

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            var requiredVarsEnviromentAndAppSettingsOnlyForProduction = new string[][] {
                new string[] {"SMTP_CLIENT", "Smtp:Client"},
                new string[] {"SMTP_PORT", "Smtp:Port"},
                new string[] {"SMTP_USER_NAME_CREDENTIAL", "Smtp:UserNameCredential"},
                new string[] {"SMTP_PASSWORD_CREDENTIAL", "Smtp:PasswordCredential"},
                new string[] {"SMTP_MAIL_FROM_EMAIL", "Smtp:MailFromEmail"}
            };

            requiredVarsEnviromentAndAppSettings.AddRange(requiredVarsEnviromentAndAppSettingsOnlyForProduction);

            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        foreach (var key in requiredVarsEnviromentAndAppSettings)
        {
            var valueEnv = Environment.GetEnvironmentVariable(key[0], EnvironmentVariableTarget.Machine);
            var valueAppSettings = app.Configuration.GetSection(key[1]).Get<string>();

            if ((valueAppSettings == "" || valueAppSettings == null) && (valueEnv == "" || valueEnv == null))
            {
                throw new Exception($"Config variable is missing you need to add it to launchsettings.json ({key[0]})");
            }
        }

        app.UseHttpsRedirection();

        app.UseCors();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/health");

        app.Run();
    }
}