namespace AudioStreamingApi.Models.DbModels
{
	public class SongFollower
	{
        public int Id { get; set; }

        public int FollowerId { get; set; }

        public int SongId { get; set; }

        public User? Follower { get; set; }

        public Song? Song { get; set; }
    }
}

