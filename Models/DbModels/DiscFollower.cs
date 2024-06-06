namespace AudioStreamingApi.Models.DbModels
{
	public class DiscFollower
	{
        public int Id { get; set; }

        public int FollowerId { get; set; }

        public int DiscId { get; set; }

        public User? Follower { get; set; }

        public Disc? Disc { get; set; }
    }
}

