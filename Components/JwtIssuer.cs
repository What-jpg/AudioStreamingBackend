using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace AudioStreamingApi.Components
{
	public class JwtIssuer
	{
		public JwtIssuer()
		{
		}

        public static ConfigurationManager BuilderConfig = WebApplication.CreateBuilder().Configuration;

        public static string JwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? BuilderConfig.GetSection("Jwt:Key").Get<string>();
        public static string JwtIssuerStr = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? BuilderConfig.GetSection("Jwt:Issuer").Get<string>();

        public static string CreateToken(IEnumerable<Claim> claims, DateTime expiresAt)
		{
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var secToken = new JwtSecurityToken(
				JwtIssuerStr,
				JwtIssuerStr,
				claims,
				expires: expiresAt,
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(secToken);
        }

		public static bool ValidateToken(string token, out List<Claim> claims)
		{
			try
			{
				var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = JwtIssuerStr,
					ValidAudience = JwtIssuerStr,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey))
				}, out SecurityToken validatedToken);

				claims = claimsPrincipal.Claims.ToList();
				return true;
			} catch(Exception e)
			{
				claims = new List<Claim>() { };
				return false;
			}
		}

		public static string GetTokenFromBearerString(string bearerToken)
		{
            return (bearerToken).Split(" ")[1] ?? (bearerToken).Split(" ")[0];
        }

		public static string? GetTokenFromHeaders(IHeaderDictionary headers)
		{
			try
			{
				var tokenRaw = (string)headers["Authorization"];

				return GetTokenFromBearerString(tokenRaw);
			} catch (Exception e)
			{
				return null;
			}
		}
	}
}

