namespace AudioStreamingApi.Models.DbModels
{
	public class Disc
	{
		public int Id { get; set; }

		public string Name { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ArtistId { get; set; }

        public int? CoverId { get; set; }

        public User? Artist { get; set; }

		public DbFile? Cover { get; set; }

		public List<Song> Songs { get; set; }
	}
}

