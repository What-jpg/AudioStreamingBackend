using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AudioStreamingApi.Components;
using AudioStreamingApi.RedisHelper;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.Models;
using Npgsql;
using Dapper;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class CreateController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public CreateController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [RequestSizeLimit(2_010_000_000)]
        [Authorize]
        [HttpPost("disc")]
        public ActionResult<int> DiscPost([FromForm] string name, [FromForm] string[] songNames, [FromForm] List<IFormFile> songs, [FromForm] IFormFile? cover)
        {

            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(name, songNames, songs))
            {
                return NotFound();
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            List<CreateRequestSong> songsConverted = new List<CreateRequestSong>();

            if (songNames.Length != songs.Count)
            {
                return BadRequest("Songs and song names count are unequal");
            }

            if (songNames.Length == 0 || songs.Count == 0)
            {
                return BadRequest("The song list must have at least one song");
            }

            TimeSpan[] totalTimeArray = new TimeSpan[songs.Count];

            for (int i = 0; i < songs.Count; i++)
            {
                IFormFile song = songs[i];
                    
                try
                {
                    HttpControllersMethods.CheckIfSongContentIsinRightFormatOrThrowError(song, out totalTimeArray[i]);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            int? imageIdInDb = null;

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                try
                {
                    HttpControllersMethods.CheckIfImageDataIsNotNullAndSaveToDb(cover, connection, out imageIdInDb);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }

                int discId = DbMethods.CreateAnAlbum(userId, name, imageIdInDb, connection);

                for (int i = 0; i < songs.Count; i++)
                {
                    var item = songs[i];
                    string songName = songNames[i];

                    DbMethods.CreateASong(songName, discId, totalTimeArray[i], item, connection);
                }

                return Ok(discId);
            }
        }

        [RequestSizeLimit(100_000_000)]
        [Authorize]
        [HttpPost("song")]
        public ActionResult<int> SongPost([FromForm] int discId, [FromForm] string name, [FromForm] IFormFile song)
        {
            if (!HttpControllersMethods.CheckIfVariablesAreNotNull(discId, name, song))
            {
                return NotFound();
            }

            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            TimeSpan totalTime;

            try
            {
                HttpControllersMethods.CheckIfSongContentIsinRightFormatOrThrowError(song, out totalTime);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var getAlbumSql = $"SELECT * FROM \"Discography\" WHERE \"id\" = '{discId}' AND \"artistId\" = '{userId}' LIMIT 1";
                if (connection.QueryFirstOrDefault(getAlbumSql) != null)
                {
                    int songId = DbMethods.CreateASong(name, (int)discId, totalTime, song, connection);

                    return Ok(songId);
                } else
                {
                    return BadRequest("The album isn't correct");
                }
            }
        }
    }
}

