using AudioStreamingApi.Components;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.Models.DbModels;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using System;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class DeleteController : Controller
    {
        public readonly string mainDbConnectionString;
        private readonly RedisDBAccessor redisHelper;

        public DeleteController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisHelper)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;

            this.redisHelper = redisHelper.Connection;
        }


        [Authorize]
        [HttpDelete("song/{songId}")]
        public ActionResult DeleteSongDelete(int songId, [FromForm] string currentPassword)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(currentPassword, songId))
            {
                return NotFound();
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
                        DeleteSong(song, npgsqlConnection);
                        return Ok();
                    }
                    else
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
        [HttpDelete("disc/{discId}")]
        public ActionResult DeleteDiscDelete(int discId, [FromForm] string currentPassword)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(currentPassword, discId))
            {
                return NotFound();
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                User user = DbMethods.GetUserByIdWithoutJoins(userId, npgsqlConnection);
                Disc? disc = DbMethods.GetDiscByIdWithoutJoins(discId, npgsqlConnection);

                if (disc == null)
                {
                    return BadRequest("The disc doesn't exist");
                }

                if (PasswordHasher.VerifyHashedPassword(user.HashedPassword, currentPassword))
                {
                    if (user.Id == disc.ArtistId)
                    {
                        List<Song> songs = DbMethods.GetDiscSongs(disc.Id, npgsqlConnection);

                        foreach (var item in songs)
                        {
                            DeleteSong(item, npgsqlConnection);
                        }

                        npgsqlConnection.Query($"DELETE FROM \"Discography\" WHERE id = '{disc.Id}'");

                        if (disc.CoverId != null)
                        {
                            DbFile cover = DbMethods.GetDbFileById(disc.CoverId!.Value, npgsqlConnection);

                            npgsqlConnection.Query($"DELETE FROM \"DbFiles\" WHERE id = '{disc.CoverId}'");

                            HttpControllersMethods.DeleteFileFromStorage(cover.Path);
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

        private static void DeleteSongWithId(int songId, NpgsqlConnection npgsqlConnection) 
        {
            Song song = DbMethods.GetSongByIdWithoutJoins(songId, npgsqlConnection);

            DeleteSong(song, npgsqlConnection);
        }

        private static void DeleteSong(Song song, NpgsqlConnection npgsqlConnection)
        {
            npgsqlConnection.Query($"DELETE FROM \"SongsFollowers\" sf WHERE sf.\"elementId\" = '{song.Id}'");
            npgsqlConnection.Query($"DELETE FROM \"SongsListened\" sf WHERE sf.\"songId\" = '{song.Id}'");
            npgsqlConnection.Query($"DELETE FROM \"Songs\" WHERE id = '{song.Id}'");

            DbFile content = DbMethods.GetDbFileById(song.ContentId, npgsqlConnection);

            npgsqlConnection.Query($"DELETE FROM \"DbFiles\" WHERE id = '{song.ContentId}'");

            HttpControllersMethods.DeleteFileFromStorage(content.Path);
        }
    }
}

