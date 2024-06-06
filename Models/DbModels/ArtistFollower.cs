namespace AudioStreamingApi.Models.DbModels
{
	public class ArtistFollower
	{
		public int Id { get; set; }

		public int FollowerId { get; set; }

        public int ArtistId { get; set; }

        public User? Follower { get; set; }

		public User? Artist { get; set; }
	}
}

