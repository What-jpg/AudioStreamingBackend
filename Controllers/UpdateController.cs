using Microsoft.AspNetCore.Mvc;
using AudioStreamingApi.Components;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Newtonsoft.Json;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using AudioStreamingApi.Models.DbModels;
using AudioStreamingApi.Models;
using Dapper;
using System.IO;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class UpdateController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public UpdateController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [RequestSizeLimit(10_000_000)]
        [Authorize]
        [HttpPut("updateuserinfo")]
        public ActionResult UpdateUserInfoPost([FromForm] string currentPassword, [FromForm] string? newName, [FromForm] string? newEmail, [FromForm] string? newPassword, [FromForm] bool? newIsTwoFactorAuthActive, [FromForm] bool? needToSetAvatarToDefault, [FromForm] IFormFile? newAvatar)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(currentPassword))
            {
                return BadRequest("You must fill the current password");
            }

            Console.WriteLine(newIsTwoFactorAuthActive);

            if (newName == null && newPassword == null && newIsTwoFactorAuthActive == null && needToSetAvatarToDefault == null && newAvatar == null && newEmail == null)
            {
                return BadRequest("You must do at least one change in your account");
            }

            bool needToSetAvatarToDefaultBool = needToSetAvatarToDefault ?? false;

            if (needToSetAvatarToDefaultBool && newAvatar != null)
            {
                return BadRequest("You can't add new avatar and set avatar to default, choose one option");
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                User user = DbMethods.GetUserByIdWithoutJoins(userId, npgsqlConnection);

                if (PasswordHasher.VerifyHashedPassword(user.HashedPassword, currentPassword))
                {
                    if (newName != null)
                    {
                        user.Name = newName;
                    }
                    if (newEmail != null)
                    {
                        user.Email = newEmail;
                    }
                    if (newPassword != null)
                    {
                        user.HashedPassword = PasswordHasher.HashPassword(newPassword);
                    }
                    if (newIsTwoFactorAuthActive != null)
                    {
                        user.IsTwoFactorAuthActive = newIsTwoFactorAuthActive!.Value;
                    }

                    int? oldAvatarId = null;

                    if (needToSetAvatarToDefaultBool)
                    {
                        if (user.AvatarId != null)
                        {
                            oldAvatarId = user.AvatarId;
                        }

                        user.AvatarId = null;
                    } else if (newAvatar != null)
                    {
                        try
                        {
                            HttpControllersMethods.CheckIfImageDataIsNotNull(newAvatar);

                            if (user.AvatarId != null)
                            {
                                oldAvatarId = user.AvatarId;
                            }
                        } catch (Exception ex)
                        {
                            return BadRequest(ex.Message);
                        }
                    }
                    if (newEmail == null)
                    {

                        if (newAvatar != null)
                        {
                            user.AvatarId = DbMethods.CreateAFile(newAvatar, npgsqlConnection);
                        }

                        DbMethods.UpdateUser(user, npgsqlConnection);

                        User newUser = DbMethods.GetUserByIdWithoutJoins(user.Id, npgsqlConnection);

                        return Ok();
                    } else
                    {
                        UpdateUserRedis userForRedis = new UpdateUserRedis
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Email = newEmail,
                            HashedPassword = user.HashedPassword,
                            IsTwoFactorAuthActive = user.IsTwoFactorAuthActive,
                            UpdateCode = new Random().Next(100000, 999999).ToString(),
                            AvatarHasChanged = false
                        };

                        if (newAvatar != null)
                        {
                            userForRedis.AvatarContent = HttpControllersMethods.GetContentInStrFromFormFile(newAvatar);
                            userForRedis.AvatarContentType = newAvatar.ContentType;
                            userForRedis.AvatarFileName = newAvatar.FileName;

                            userForRedis.AvatarHasChanged = true;
                        }

                        CreateChangeCodeInDbAndSendIt(userForRedis, TimeSpan.FromMinutes(10));

                        return Ok(newEmail);
                    }
                } else
                {
                    return BadRequest("The password is incorrect");
                }
            }
        }

        [Authorize]
        [HttpPost("useupdatecode/{updateCode}")]
        public ActionResult UseCodePost(string updateCode, [FromForm] string email)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(email))
            {
                return NotFound();
            }

            string? changeCodeValue = redisHelper.ChangeCodes.GetValue(email);

            if (changeCodeValue != null)
            {
                UpdateUserRedis userFromRedis = JsonConvert.DeserializeObject<UpdateUserRedis>(changeCodeValue);

                if (userFromRedis.UpdateCode == updateCode) {
                    redisHelper.ChangeCodes.DeleteValue(email);

                    User user = new User
                    {
                        Id = userFromRedis.Id,
                        Name = userFromRedis.Name,
                        Email = userFromRedis.Email,
                        HashedPassword = userFromRedis.HashedPassword,
                        IsTwoFactorAuthActive = userFromRedis.IsTwoFactorAuthActive,
                    };

                    using (var npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
                    {
                        if (userFromRedis.AvatarHasChanged != false)
                        {
                            if (userFromRedis.AvatarContent != null)
                            {
                                user.AvatarId = DbMethods.CreateAFile(new FileForRedisAuth(userFromRedis.AvatarContent, userFromRedis.AvatarContentType, userFromRedis.AvatarFileName), npgsqlConnection);
                            }
                        } else
                        {
                            User oldUser = DbMethods.GetUserByIdWithoutJoins(user.Id, npgsqlConnection);

                            user.AvatarId = oldUser.AvatarId;
                        }

                        int? avatarIdFromDb = DbMethods.GetUserByIdWithoutJoins(user.Id, npgsqlConnection).AvatarId;

                        DbMethods.UpdateUser(user, npgsqlConnection);
                    }

                    return Ok();
                }
            }
            return BadRequest("Invalid code");
        }

        [HttpPost("resendupdatecode")]
        public ActionResult ResendUpdateCodePost([FromForm] string email)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(email))
            {
                return NotFound();
            }

            string? authCodeValue = redisHelper.AuthCodes.GetValue(email);

            if (authCodeValue != null)
            {
                UpdateUserRedis userFromRedis = JsonConvert.DeserializeObject<UpdateUserRedis>(authCodeValue);
                TimeSpan timeExpired = DateTime.Now - userFromRedis.CreatedAt;

                if (timeExpired >= TimeSpan.FromMinutes(2))
                {
                    userFromRedis.UpdateCode = new Random().Next(100000, 999999).ToString();
                    userFromRedis.CreatedAt = DateTime.Now;

                    CreateChangeCodeInDbAndSendIt(userFromRedis, TimeSpan.FromMinutes(10));
                    return Ok();
                }
                else
                {
                    return BadRequest($"To resend code you must wait {(TimeSpan.FromMinutes(2) - timeExpired).TotalSeconds} seconds");
                }
            }
            else
            {
                return BadRequest("No one is currently trying to register with this email");
            }
        }

        [RequestSizeLimit(100_000_000)]
        [Authorize]
        [HttpPut("updatesonginfo/{sondId}")]
        public ActionResult UpdateSongInfoPost(int songId, [FromForm] string currentPassword, [FromForm] string? newName)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(currentPassword, songId))
            {
                return NotFound();
            }

            if (newName == null)
            {
                return BadRequest("You must do at least one change in your song");
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                User user = DbMethods.GetUserByIdWithoutJoins(userId, npgsqlConnection);
                Song? song = DbMethods.GetSongByIdWithoutJoins(songId, npgsqlConnection);

                if (song == null)
                {
                    return BadRequest("The song doesn't exist");
                }

                Disc disc = DbMethods.GetDiscByIdWithoutJoins(song.DiscId, npgsqlConnection);

                if (PasswordHasher.VerifyHashedPassword(user.HashedPassword, currentPassword))
                {
                    if (user.Id == disc.ArtistId)
                    {
                        song.Name = newName;

                        DbMethods.UpdateSong(song, npgsqlConnection);

                        return Ok();
                    } else
                    {
                        return BadRequest("The song doesn't belong to user");
                    }
                }
                else
                {
                    return BadRequest("The password is incorrect");
                }
            }
        }

        [Authorize]
        [HttpPut("updatediscinfo/{discId}")]
        public ActionResult UpdateDiscInfoPost(int discId, [FromForm] string currentPassword, [FromForm] string? newName, [FromForm] IFormFile? newCover, [FromForm] bool needNewCover)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(currentPassword, discId) && !needNewCover)
            {
                return NotFound();
            }

            if (newName == null && newCover == null && !needNewCover)
            {
                return BadRequest("You must do at least one change in your disc");
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                User user = DbMethods.GetUserByIdWithoutJoins(userId, npgsqlConnection);
                Disc disc = DbMethods.GetDiscByIdWithoutJoins(discId, npgsqlConnection);

                if (disc == null)
                {
                    return BadRequest("The disc doesn't exist");
                }

                if (PasswordHasher.VerifyHashedPassword(user.HashedPassword, currentPassword))
                {
                    if (user.Id == disc.ArtistId)
                    {
                        int? oldCoverId = disc.CoverId;

                        if (newName != null)
                        {
                            disc.Name = newName;
                        }
                        if (needNewCover)
                        {
                            if (newCover != null)
                            {
                                try
                                {
                                    HttpControllersMethods.CheckIfImageDataIsNotNullAndSaveToDb(newCover, npgsqlConnection, out int? imageIdInDb);

                                    disc.CoverId = imageIdInDb;
                                }
                                catch (Exception ex)
                                {
                                    return BadRequest(ex);
                                }
                            } else
                            {
                                disc.CoverId = null;
                            }
                        }

                        DbMethods.UpdateDisc(disc, npgsqlConnection);

                        if (needNewCover)
                        {
                            if (oldCoverId != null)
                            {
                                DbFile cover = DbMethods.GetDbFileById(oldCoverId!.Value, npgsqlConnection);

                                npgsqlConnection.Query($"DELETE FROM \"DbFiles\" df WHERE df.id = '{oldCoverId}'");

                                HttpControllersMethods.DeleteFileFromStorage(cover.Path);
                            }
                        }

                        return Ok();
                    }
                    else
                    {
                        return BadRequest("The disc doesn't belong to user");
                    }
                }
                else
                {
                    return BadRequest("The password is incorrect");
                }
            }
        }

        public void CreateChangeCodeInDbAndSendIt(UpdateUserRedis updateCodeValue, TimeSpan expirationTime)
        {
            redisHelper.ChangeCodes.SetValue(updateCodeValue.Email, JsonConvert.SerializeObject(updateCodeValue), expirationTime);

            Console.WriteLine($"The code is: {updateCodeValue.UpdateCode}");

            var smtpClient = SmtpConfigured.GetSmtpClient();
            var mailMessage = SmtpConfigured.GetMailMessage(updateCodeValue.Email, "Code for user info update", $"Your code is {updateCodeValue.UpdateCode}");

            // Only for release, comment if in development

            smtpClient.Send(mailMessage);
        }
    }
}

