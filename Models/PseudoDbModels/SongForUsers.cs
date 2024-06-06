namespace AudioStreamingApi.Models.PseudoDbModels
{
	public class SongForUsers
	{
		public SongForUsers(int id, string name, TimeSpan totalTime, int discId, DiscForUsers? disc = null)
		{
            Id = id;
            Name = name;
            TotalTimeInMicroseconds = totalTime.TotalMicroseconds;
            DiscId = discId;
            Disc = disc;
		}

        public int Id { get; set; }

        public string Name { get; set; }

        public double TotalTimeInMicroseconds { get; set; }

        public int DiscId { get; set; }

        public DiscForUsers? Disc { get; set; }
    }
}

