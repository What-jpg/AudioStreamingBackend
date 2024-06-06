using AudioStreamingApi.RedisHelper;
using AudioStreamingApi.Models.DbModels;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using AudioStreamingApi.Components;
using AudioStreamingApi.Models;
using Newtonsoft.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AudioStreamingApi.DependencyInjections;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public readonly string mainDbConnectionString;
        private readonly RedisDBAccessor redisHelper;

        public AuthController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisHelper)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;

            this.redisHelper = redisHelper.Connection;
        }

        [RequestSizeLimit(10_000_000)]
        [HttpPost("signup")]
        public ActionResult SignUpPost([FromForm] string userName, [FromForm] string password, [FromForm] string email, [FromForm] bool? needToRemember30Days, [FromForm] IFormFile? avatar)
        {
            Console.WriteLine("Got it on server");
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(userName, password, email))
            {
                return NotFound();
            }

            bool needToRemember30DaysBool = needToRemember30Days ?? false;

            try
            {
                HttpControllersMethods.CheckIfImageDataIsNotNull(avatar);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var getUserSql = $"SELECT * FROM \"Users\" WHERE \"email\" = '{email}' LIMIT 1";

                User? userInDb = connection.QueryFirstOrDefault<User>(getUserSql);

                if (userInDb != null)
                {
                    return BadRequest("Email is already in use");
                }

                string hashedPassword = PasswordHasher.HashPassword(password);

                CreateAuthCodeAndSendIt(null, userName, hashedPassword, email, needToRemember30DaysBool, avatar);

                return Ok();
            }
        }

        [HttpPost("signin")]
        public ActionResult SignInPost([FromForm] string email, [FromForm] string password, [FromForm] bool? needToRemember30Days)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(password, email))
            {
                return NotFound();
            }

            bool needToRemember30DaysBool = needToRemember30Days ?? false;

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var getUserSql = $"SELECT * FROM \"Users\" WHERE \"email\" = '{email}' LIMIT 1";

                User? userInDb = connection.QueryFirstOrDefault<User>(getUserSql);

                if (userInDb != null && PasswordHasher.VerifyHashedPassword(userInDb.HashedPassword, password))
                {
                    if (!userInDb.IsTwoFactorAuthActive)
                    {
                        return Ok(JsonConvert.SerializeObject(
                            CreateAuthToken(userInDb.Id, needToRemember30DaysBool)
                        ));
                    } else
                    {
                        CreateAuthCodeAndSendIt(null, userInDb.Name, userInDb.HashedPassword, userInDb.Email, needToRemember30DaysBool);
                        return Ok(null);
                    }
                } else
                {
                    return BadRequest("The user or password is invalid");
                }
            }
        }

        [HttpPost("useauthcode/{authCode}")]
        public ActionResult<string> UseCodePost(string authCode, [FromForm] string email)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(email))
            {
                return NotFound();
            }

            string? authCodeValue = redisHelper.AuthCodes.GetValue(email);

            if (authCodeValue != null)
            {
                UserForRedisAuth userFromRedis = JsonConvert.DeserializeObject<UserForRedisAuth>(authCodeValue);

                if (userFromRedis.AuthCode == authCode) {
                    redisHelper.AuthCodes.DeleteValue(email);

                    if (userFromRedis.Id != null)
                    {
                        return Ok(CreateAuthToken((int)(userFromRedis.Id), userFromRedis.NeedToRemember30Days));
                    } else
                    {
                        using (var connection = new NpgsqlConnection(mainDbConnectionString))
                        {
                            int? avatarId = null;

                            if (userFromRedis.AvatarContent != null)
                            {
                                avatarId = DbMethods.CreateAFile(new FileForRedisAuth(userFromRedis.AvatarContent, userFromRedis.AvatarContentType, userFromRedis.AvatarFileName), connection);
                            }

                            int userId = DbMethods.CreateAnUser(userFromRedis.Name, userFromRedis.Email, userFromRedis.HashedPassword, avatarId, connection);

                            return Ok(JsonConvert.SerializeObject(
                                CreateAuthToken(userId, userFromRedis.NeedToRemember30Days)
                            ));
                        }
                    }
                }
            }
            return BadRequest("Invalid code");
        }

        [HttpPost("resendauthcode")]
        public ActionResult ResendAuthCodePost([FromForm] string email)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(email))
            {
                return NotFound();
            }

            string? authCodeValue = redisHelper.AuthCodes.GetValue(email);

            if (authCodeValue != null)
            {
                UserForRedisAuth userFromRedis = JsonConvert.DeserializeObject<UserForRedisAuth>(authCodeValue);
                TimeSpan timeExpired = DateTime.Now - userFromRedis.CreatedAt;

                if (timeExpired >= TimeSpan.FromMinutes(2))
                {
                    userFromRedis.AuthCode = new Random().Next(100000, 999999).ToString();
                    userFromRedis.CreatedAt = DateTime.Now;

                    CreateAuthCodeInDbAndSendIt(userFromRedis, TimeSpan.FromMinutes(10));
                    return Ok();
                } else
                {
                    return StatusCode(406, $"Wait {Convert.ToInt32((TimeSpan.FromMinutes(2) - timeExpired).TotalSeconds)} seconds");
                }
            } else
            {
                return BadRequest("No one is currently trying to authenticate with this email");
            }
        }

        [Authorize]
        [HttpGet("getnewtoken")]
        public ActionResult<MessageAndValueReturn> GetNewTokenGet()
        {
            string oldToken = JwtIssuer.GetTokenFromHeaders(HttpContext.Request.Headers);

            JwtIssuer.ValidateToken(oldToken, out List<Claim> claims);

            DateTime expiresAt = DateTime.Now.AddMinutes(11);

            return Ok(new AuthTokenReturn(
                JwtIssuer.CreateToken(new List<Claim>
                {
                    new Claim("UserId", claims[0].Value)
                }, DateTime.Now.AddMinutes(11)),
                expiresAt,
                false
            ));
        }



        public void CreateAuthCodeAndSendIt(int? id, string userName, string hashedPassword, string email, bool needToRemember30Days, IFormFile? avatar = null)
        {
            string signUpCode = "";


            string avatarContent = null;
            string avatarContentType = null;
            string avatarFileName = null;

            if (avatar != null)
            {
                avatarContent = HttpControllersMethods.GetContentInStrFromFormFile(avatar);
                avatarContentType = avatar.ContentType;
                avatarFileName = avatar.FileName;
            }

            string newCode = new Random().Next(100000, 999999).ToString();
            var authCodeValue = new UserForRedisAuth
            {
                Id = id,
                Name = userName,
                Email = email,
                HashedPassword = hashedPassword,
                NeedToRemember30Days = needToRemember30Days,
                AvatarContent = avatarContent,
                AvatarContentType = avatarContentType,
                AvatarFileName = avatarFileName,
                AuthCode = newCode
            };

            CreateAuthCodeInDbAndSendIt(authCodeValue, TimeSpan.FromMinutes(10));
        }

        public void CreateAuthCodeInDbAndSendIt(UserForRedisAuth authCodeValue, TimeSpan expirationTime)
        {
            redisHelper.AuthCodes.SetValue(authCodeValue.Email, JsonConvert.SerializeObject(authCodeValue), expirationTime);

            Console.WriteLine($"The code is: {authCodeValue.AuthCode}");

            var smtpClient = SmtpConfigured.GetSmtpClient();
            var mailMessage = SmtpConfigured.GetMailMessage(authCodeValue.Email, "Code for authentication", $"Your code is {authCodeValue.AuthCode}");

            // Commented, only for release

            // smtpClient.Send(mailMessage);
        }

        public List<AuthTokenReturn> CreateAuthToken(int userId, bool needToRemember30Days)
        {
            List<Claim> claims = new List<Claim> { new Claim("UserId", userId.ToString()) };
            DateTime shortTermExpiresAt = DateTime.Now.AddMinutes(11);

            List<AuthTokenReturn> authTokens = new List<AuthTokenReturn> { new AuthTokenReturn(JwtIssuer.CreateToken(claims, shortTermExpiresAt), shortTermExpiresAt, false) };

            if (needToRemember30Days)
            {
                DateTime longTermExpiresAt = DateTime.Now.AddDays(30);
                authTokens.Add(new AuthTokenReturn(JwtIssuer.CreateToken(claims, longTermExpiresAt), longTermExpiresAt, true));
            }

            return authTokens;
        }
    }
}

