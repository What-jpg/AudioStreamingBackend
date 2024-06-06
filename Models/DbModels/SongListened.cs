namespace AudioStreamingApi.Models.DbModels
{
	public class SongListened
	{
        public int Id { get; set; }

        public DateTime ListenedAt { get; set; }

        public int ListenerId { get; set; }

        public int SongId { get; set; }

        public User? Listener { get; set; }

        public Song? Song { get; set; }
    }
}

