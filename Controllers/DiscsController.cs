using AudioStreamingApi.Components;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class DiscsController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public DiscsController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [HttpGet("getdisc/{discId}")]
        public ActionResult<string> GetSong(int discId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var disc = DbMethods.GetDiscById(discId, npgsqlConnection);

                if (disc != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.ConvertDiscToFormatForUsers(disc)));
                }
                else
                {
                    return BadRequest("The disc doesn't exist");
                }
            }
        }

        [HttpGet("getmostpopulardiscography")]
        public ActionResult<string> GetMostPopularDiscographyGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromItemsFromEnum(DbMethods.GetTheMostPopularDiscography(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getmostpopulardiscographyforweek")]
        public ActionResult<string> GetMostPopularDiscographyForWeekGet()
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromItemsFromEnum(DbMethods.GetTheMostPopularDiscographyFor7Days(npgsqlConnection), npgsqlConnection)));
            }
        }

        [HttpGet("getallartistsingles/{artistId}")]
        public ActionResult<string> GetAllArtistSingles(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                if (DbMethods.GetUserByIdWithoutJoins(artistId, npgsqlConnection) != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromDiscs(DbMethods.GetAllArtistSingles(artistId, npgsqlConnection), npgsqlConnection)));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }

        [HttpGet("getallartistalbums/{artistId}")]
        public ActionResult<string> GetAllArtistAlbums(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                if (DbMethods.GetUserByIdWithoutJoins(artistId, npgsqlConnection) != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromDiscs(DbMethods.GetAllArtistAlbums(artistId, npgsqlConnection), npgsqlConnection)));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }

        [HttpGet("getallartistmostpopulardiscography/{artistId}")]
        public ActionResult<string> GetAllArtistMostPopularDiscography(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                if (DbMethods.GetUserByIdWithoutJoins(artistId, npgsqlConnection) != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromDiscs(DbMethods.GetArtistTheMostPopularDiscography(artistId, npgsqlConnection), npgsqlConnection)));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }

        [HttpGet("getallartistdiscography/{artistId}")]
        public ActionResult<string> GetAllArtistDiscography(int artistId)
        {
            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                if (DbMethods.GetUserByIdWithoutJoins(artistId, npgsqlConnection) != null)
                {
                    return Ok(JsonConvert.SerializeObject(HttpControllersMethods.GetDiscographyForUserFromDiscs(DbMethods.GetUserDiscography(artistId, npgsqlConnection), npgsqlConnection)));
                }
                else
                {
                    return BadRequest("The user doesn't exist");
                }
            }
        }
    }
}

