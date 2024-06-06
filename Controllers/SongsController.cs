using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using AudioStreamingApi.Components;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class SongsController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public SongsController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [HttpGet("getsong/{songId}")]
        public ActionResult<string> GetSong(int songId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongById(songId, npgsqlConnection);

                if (song != null)
                {
                    HttpControllersMethods.GetSongTotalTime(song);

                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.ConvertSongToFormatForUsers(song)));
                }
                else
                {
                    return BadRequest("The song doesn't exist");
                }
            }
        }

        [HttpGet("getmostpopularsongs")]
        public ActionResult<string> GetMostPopularSongsGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetTheMostPopularSongs(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getmostpopularsongsforweek")]
        public ActionResult<string> GetMostPopularSongsForWeekGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetTheMostPopularSongsFor7Days(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getrecommendedsongs")]
        public ActionResult<string> GetRecommendedSongs()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetRecommendedSongs(npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("getlistenerpopularsongs")]
        public ActionResult<string> GetListenerPopularSongs()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetListenerPopularSongs(userId, npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("getlistenerforgottenpopularsongs")]
        public ActionResult<string> GetListenerForgottenPopularSongs()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetListenerForgottenPopularSongs(userId, npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("getlistenerhistorysongs")]
        public ActionResult<string> GetListenerHistorySongs()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetListenerHistorySongs(userId, npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getartistmostpopularsongs/{artistId}")]
        public ActionResult<string> GetArtistMostPopularSongs(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                if (DbMethods.GetUserByIdWithoutJoins(artistId, npgsqlConnection) != null) {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetArtistMostPopularSongs(artistId, npgsqlConnection), npgsqlConnection)));
                } else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }

        [HttpGet("getrandomsongs")]
        public ActionResult<string> GetRandomSongs()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.GetRandomSongs(npgsqlConnection), npgsqlConnection)));
            }
        }
    }
}

