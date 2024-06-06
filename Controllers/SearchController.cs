using Microsoft.AspNetCore.Mvc;
using AudioStreamingApi.Components;
using Newtonsoft.Json;
using Npgsql;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Authorization;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public SearchController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [HttpGet("songs")]
        public ActionResult<string> SearchSongsGet(string searchString)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.SearchForSongs(searchString, npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("discs")]
        public ActionResult<string> SearchAlbumsGet(string searchString)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromItemsFromEnum(DbMethods.SearchForDiscs(searchString, npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("artists")]
        public ActionResult<string> SearchArtistsGet(string searchString)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetUsersForUserFromItemsFromEnum(DbMethods.SearchForArtists(searchString, npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("songslibrary")]
        public ActionResult<string> SearchSongsInLibraryGet(string searchString)
        {
            var userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetSongsForUserFromItemsFromEnum(DbMethods.SearchForSongsInLibrary(userId, searchString, npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("discslibrary")]
        public ActionResult<string> SearchAlbumsInLibraryGet(string searchString)
        {
            var userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromItemsFromEnum(DbMethods.SearchForDiscsInLibrary(userId, searchString, npgsqlConnection), npgsqlConnection)));
            }
        }

        [Authorize]
        [HttpGet("artistslibrary")]
        public ActionResult<string> SearchArtistsInLibraryGet(string searchString)
        {
            var userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetUsersForUserFromItemsFromEnum(DbMethods.SearchForArtistsInLibrary(userId, searchString, npgsqlConnection), npgsqlConnection)));
            }
        }
    }
}

