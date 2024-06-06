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

		public static string CreateToken(IEnumerable<Claim> claims, DateTime expiresAt)
		{
			var builderConfig = WebApplication.CreateBuilder().Configuration;

			string jwtKey = builderConfig.GetSection("Jwt:Key").Get<string>();
			string jwtIssuer = builderConfig.GetSection("Jwt:Issuer").Get<string>();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var secToken = new JwtSecurityToken(
				jwtIssuer,
				jwtIssuer,
				claims,
				expires: expiresAt,
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(secToken);
        }

		public static bool ValidateToken(string token, out List<Claim> claims)
		{
            var builderConfig = WebApplication.CreateBuilder().Configuration;

            string jwtKey = builderConfig.GetSection("Jwt:Key").Get<string>();
            string jwtIssuer = builderConfig.GetSection("Jwt:Issuer").Get<string>();

			try
			{
				var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtIssuer,
					ValidAudience = jwtIssuer,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
				}, out SecurityToken validatedToken);

				claims = claimsPrincipal.Claims.ToList();
				return true;
			} catch(Exception e)
			{
				claims = new List<Claim>() { };
				return false;
			}
		}

		public static string? GetTokenFromHeaders(IHeaderDictionary headers)
		{
			try
			{
				var tokenRaw = (string)headers["Authorization"];

				return (tokenRaw).Split(" ")[1];
			} catch (Exception e)
			{
				return null;
			}
		}
	}
}

