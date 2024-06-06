using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AudioStreamingApi.Components;
using AudioStreamingApi.Models.PseudoDbModels;
using Npgsql;
using AudioStreamingApi.Models.DbModels;
using Dapper;
using Newtonsoft.Json;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class FollowController : Controller
    {
        public readonly string mainDbConnectionString;
        private readonly RedisDBAccessor redisHelper;

        public FollowController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisHelper)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;

            this.redisHelper = redisHelper.Connection;
        }

        [Authorize]
        [HttpGet("song/toggle/{id}")]
        public ActionResult<bool> SongToggleGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongByIdWithoutJoins(id, npgsqlConnection);

                if (song != null)
                {
                    var songFollower = DbMethods.GetSongFollowerByIdsWithoutJoins(userId, song.Id, npgsqlConnection);

                    if (songFollower != null)
                    {
                        var sql = $"DELETE FROM \"SongsFollowers\" sf WHERE sf.id = '{songFollower.Id}'";

                        npgsqlConnection.Query(sql);

                        return Ok(false);
                    } else
                    {
                        var sql = $"INSERT INTO \"SongsFollowers\" (\"followerId\", \"elementId\") Values ('{userId}', '{song.Id}')";

                        npgsqlConnection.Query(sql);

                        return Ok(true);
                    }
                } else
                {
                    return BadRequest("No such a song has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("disc/toggle/{id}")]
        public ActionResult<bool> DiscToggleGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var disc = DbMethods.GetDiscByIdWithoutJoins(id, npgsqlConnection);

                if (disc != null)
                {
                    var discFollower = DbMethods.GetDiscFollowerByIdsWithoutJoins(userId, disc.Id, npgsqlConnection);

                    if (discFollower != null)
                    {
                        var sql = $"DELETE FROM \"DiscographyFollowers\" df WHERE df.id = '{discFollower.Id}'";

                        npgsqlConnection.Query(sql);

                        return Ok(false);
                    }
                    else
                    {
                        var sql = $"INSERT INTO \"DiscographyFollowers\" (\"followerId\", \"elementId\") Values ('{userId}', '{disc.Id}')";

                        npgsqlConnection.Query(sql);

                        return Ok(true);
                    }
                }
                else
                {
                    return BadRequest("No such a disc has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("artist/toggle/{id}")]
        public ActionResult<bool> ArtistToggleGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var artist = DbMethods.GetUserByIdWithoutJoins(id, npgsqlConnection);

                if (artist != null)
                {
                    var artistFollower = DbMethods.GetArtistFollowerByIdsWithoutJoins(userId, artist.Id, npgsqlConnection);

                    if (artistFollower != null)
                    {
                        var sql = $"DELETE FROM \"ArtistsFollowers\" af WHERE af.id = '{artistFollower.Id}'";

                        npgsqlConnection.Query(sql);

                        return Ok(false);
                    }
                    else
                    {
                        var sql = $"INSERT INTO \"ArtistsFollowers\" (\"followerId\", \"elementId\") Values ('{userId}', '{artist.Id}')";

                        npgsqlConnection.Query(sql);

                        return Ok(true);
                    }
                }
                else
                {
                    return BadRequest("No such an artist has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("song/{id}")]
        public ActionResult<bool> SongGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongByIdWithoutJoins(id, npgsqlConnection);

                if (song != null)
                {
                    var songFollower = DbMethods.GetSongFollowerByIdsWithoutJoins(userId, song.Id, npgsqlConnection);

                    if (songFollower != null)
                    {
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(false);
                    }
                }
                else
                {
                    return BadRequest("No such a song has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("disc/{id}")]
        public ActionResult<bool> DiscGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var disc = DbMethods.GetDiscByIdWithoutJoins(id, npgsqlConnection);

                if (disc != null)
                {
                    var discFollower = DbMethods.GetDiscFollowerByIdsWithoutJoins(userId, disc.Id, npgsqlConnection);

                    if (discFollower != null)
                    {
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(false);
                    }
                }
                else
                {
                    return BadRequest("No such a disc has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("artist/{id}")]
        public ActionResult<bool> ArtistGet(int id)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var artist = DbMethods.GetUserByIdWithoutJoins(id, npgsqlConnection);

                if (artist != null)
                {
                    var artistFollower = DbMethods.GetArtistFollowerByIdsWithoutJoins(userId, artist.Id, npgsqlConnection);

                    if (artistFollower != null)
                    {
                        return Ok(true);
                    }
                    else
                    {
                        return Ok(false);
                    }
                }
                else
                {
                    return BadRequest("No such an artist has been found");
                }
            }
        }

        [Authorize]
        [HttpGet("songs")]
        public ActionResult<string> SongsGet()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var sql = $"SELECT sf.\"elementId\" FROM \"SongsFollowers\" sf WHERE sf.\"followerId\" = '{userId}'";

                List<int> songIds = npgsqlConnection.Query<int>(sql).ToList();

                List<SongForUsers> songsForUsers = new List<SongForUsers>();

                foreach (var songId in songIds)
                {
                    Song song = DbMethods.GetSongById(songId, npgsqlConnection);

                    songsForUsers.Add(HttpControllersMethods.ConvertSongToFormatForUsers(song));
                }

                return Ok(JsonConvert.SerializeObject(songsForUsers));
            }
        }

        [Authorize]
        [HttpGet("discs")]
        public ActionResult<string> DiscsGet()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var sql = $"SELECT df.\"elementId\" FROM \"DiscographyFollowers\" df WHERE df.\"followerId\" = '{userId}'";

                List<int> discIds = npgsqlConnection.Query<int>(sql).ToList();

                List<DiscForUsers> discsForUsers = new List<DiscForUsers>();

                foreach (var songId in discIds)
                {
                    Disc disc = DbMethods.GetDiscById(songId, npgsqlConnection);

                    discsForUsers.Add(HttpControllersMethods.ConvertDiscToFormatForUsers(disc));
                }

                return Ok(JsonConvert.SerializeObject(discsForUsers));
            }
        }

        [Authorize]
        [HttpGet("artists")]
        public ActionResult<string> ArtistsGet()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            using (NpgsqlConnection npgsqlConnection = new NpgsqlConnection(mainDbConnectionString))
            {
                var sql = $"SELECT af.\"elementId\" FROM \"ArtistsFollowers\" af WHERE af.\"followerId\" = '{userId}'";

                List<int> artistIds = npgsqlConnection.Query<int>(sql).ToList();

                List<UserForUsers> artistsForUsers = new List<UserForUsers>();

                foreach (var artistId in artistIds)
                {
                    User artist = DbMethods.GetUserById(artistId, npgsqlConnection);

                    artistsForUsers.Add(HttpControllersMethods.ConvertUserToFormatForUsers(artist));
                }

                return Ok(JsonConvert.SerializeObject(artistsForUsers));
            }
        }
    }
}

