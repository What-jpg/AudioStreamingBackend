namespace AudioStreamingApi.Models.DbModels
{
	public class Song
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public TimeSpan TotalTime { get; set; }

        public int ContentId { get; set; }

        public int DiscId { get; set; }

        public DbFile? Content { get; set; }

		public Disc? Disc { get; set; }
	}
}

