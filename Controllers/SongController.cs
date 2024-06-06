using Microsoft.AspNetCore.Mvc;
using AudioStreamingApi.Components;
using Npgsql;
using AudioStreamingApi.Models;
using AudioStreamingApi.DependencyInjections;
using AudioStreamingApi.RedisHelper;
using Newtonsoft.Json;
using AudioStreamingApi.Models.DbModels;
using Microsoft.AspNetCore.Authorization;

namespace AudioStreamingApi.Controllers
{
    [Route("api/[controller]")]
    public class SongController : Controller
    {
        public readonly string mainDbConnectionString;
        public readonly RedisDBAccessor redisHelper;

        public SongController(INpgsqlConnectionString npgsqlConnectionString, IRedisConnection redisConnection)
        {
            mainDbConnectionString = npgsqlConnectionString.ConnectionString;
            redisHelper = redisConnection.Connection;
        }

        [HttpGet("getsongcontenttype/{id}")]
        public ActionResult<string> GetSongContentTypeGet(int id)
        {
            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongById(id, connection);

                if (song != null)
                {
                    return Ok(song.Content.Type);
                }
                else
                {
                    return BadRequest("No such a song");
                }
            }
        }

        [Authorize]
        [HttpGet("startstreamingsong/{id}")]
        public ActionResult<string> StartStreamingSongGet(int id, TimeSpan? startAt)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            TimeSpan startAtNotNullable = new TimeSpan();
            if (startAt != null)
            {
                startAtNotNullable = startAt!.Value;
            }

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongById(id, connection);

                if (song != null)
                {
                    try
                    {
                        StopStreamingSong(userId, redisHelper, mainDbConnectionString);
                    }
                    catch (Exception ex)
                    {

                    }
                    try
                    {
                        ContinueStreamingSongInfo songInfo = ContinueStreamingSong(song, startAtNotNullable, userId, redisHelper);

                        redisHelper.CurrentlyListening.SetValue(Convert.ToString(userId), JsonConvert.SerializeObject(new CurrentlyListeningRedisValue(song.Id, DateTime.Now)));

                        return Ok(JsonConvert.SerializeObject(songInfo));
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex);
                    }
                }
                else
                {
                    return BadRequest("No such a song");
                }
            }
        }

        [Authorize]
        [HttpGet("continuestreamingsong")]
        public ActionResult<string> ContinueStreamingSongGet(TimeSpan continueAt)
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            string? currentlyListeningStr = redisHelper.CurrentlyListening.GetValue(Convert.ToString(userId));

            if (currentlyListeningStr == null)
            {
                return BadRequest("User isn't currently listening anything");
            }

            CurrentlyListeningRedisValue? currentlyListening = JsonConvert.DeserializeObject<CurrentlyListeningRedisValue>(currentlyListeningStr);

            using (var connection = new NpgsqlConnection(mainDbConnectionString))
            {
                var song = DbMethods.GetSongById(currentlyListening.SongId, connection);

                if (song != null)
                {
                    try
                    {
                        return Ok(JsonConvert.SerializeObject(ContinueStreamingSong(song, continueAt, userId, redisHelper)));
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(ex);
                    }
                }
                else
                {
                    return BadRequest("No such a song");
                }
            }
        }

        [Authorize]
        [HttpGet("stopstreamingsong")]
        public ActionResult StopStreamingSongGet()
        {
            int userId = HttpControllersMethods.GetUserIdFromHttpContextIfAuthorized(HttpContext);

            try
            {
                StopStreamingSong(userId, redisHelper, mainDbConnectionString);
                return Ok();
            } catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static ContinueStreamingSongInfo ContinueStreamingSong(Song song, TimeSpan startAt, int userId, RedisDBAccessor redisHelper)
        {
            int partDurationInt = 60;

            TimeSpan partDuration = TimeSpan.FromSeconds(partDurationInt);
            
            AudioStringContentAndIsLastSongPart contentAndIsLastSongPart = HttpControllersMethods.TrimAudioFileForStreaming(song.Content, startAt, partDuration + startAt);
            
            redisHelper.CurrentlyListening.SetValue(Convert.ToString(userId), JsonConvert.SerializeObject(new CurrentlyListeningRedisValue(song.Id, DateTime.Now)));

            TimeSpan? whenToUpdate = TimeSpan.FromSeconds(Convert.ToDouble(partDurationInt) / 12.0);

            if (contentAndIsLastSongPart.IsLastSongPart)
            {
                whenToUpdate = null;
            }

            return new ContinueStreamingSongInfo(contentAndIsLastSongPart.AudioStringContent, partDuration, whenToUpdate);
        }

        private static void StopStreamingSong(int userId, RedisDBAccessor redisHelper, string npgsqlConnectionString)
        {
            if (redisHelper.CurrentlyListening.GetValue(Convert.ToString(userId)) != null)
            {
                DateTime stoppedListeningAt = DateTime.Now;
                CurrentlyListeningRedisValue? currentlyListening = JsonConvert.DeserializeObject<CurrentlyListeningRedisValue>(redisHelper.CurrentlyListening.GetValue(Convert.ToString(userId)));

                using (var connection = new NpgsqlConnection(npgsqlConnectionString))
                {
                    var song = DbMethods.GetSongById(currentlyListening.SongId, connection);

                    TimeSpan songTotalTime = HttpControllersMethods.GetSongTotalTime(song);

                    TimeSpan minWatchTime = TimeSpan.FromMinutes(1);

                    if (songTotalTime < TimeSpan.FromMinutes(2))
                    {
                        minWatchTime = songTotalTime * 0.33;
                    }

                    if (stoppedListeningAt - currentlyListening.StartedListeningAt >= minWatchTime)
                    {
                        DbMethods.CreateSongListened(song.Id, userId, connection);
                    }

                    redisHelper.CurrentlyListening.DeleteValue(Convert.ToString(userId));
                }
            } else
            {
                throw new Exception("User isn't currently listening anything");
            }
        }
    }
}

