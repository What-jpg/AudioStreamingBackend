using AudioStreamingApi.Components;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Npgsql;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class ArtistsController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public ArtistsController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [Authorize]
        [HttpGet("getthisuserinfo")]
        public ActionResult<string> GetThisUserInfo()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.ConvertUserToFormatForHimself(DbMethods.GetUserById(userId, npgsqlConnection))));
            }
        }

        [HttpGet("getartist/{artistId}")]
        public ActionResult<string> GetSong(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var artist = DbMethods.GetUserById(artistId, npgsqlConnection);

                if (artist != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.ConvertUserToFormatForUsers(artist)));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }

        [HttpGet("getmostpopularartists")]
        public ActionResult<string> GetMostPopularArtistsGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetUsersForUserFromItemsFromEnum(DbMethods.GetTheMostPopularArtists(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getmostpopularartistsforweek")]
        public ActionResult<string> GetMostPopularArtistsForWeekGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetUsersForUserFromItemsFromEnum(DbMethods.GetTheMostPopularArtistsFor7Days(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getartistlisteners/{artistId}")]
        public ActionResult<long> GetArtistListenersGet(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var artist = DbMethods.GetUserById(artistId, npgsqlConnection);

                if (artist != null)
                {
                    return Ok(DbMethods.GetArtistMouthlyListeners(artist.Id, npgsqlConnection));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }
    }
}

