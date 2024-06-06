namespace AudioStreamingApi.Models
{
	public class CurrentlyListeningRedisValue
	{
		public CurrentlyListeningRedisValue(int songId, DateTime startedWatchingAt)
		{
			SongId = songId;

            StartedListeningAt = startedWatchingAt;
		}

		public int SongId { get; set; }

		public DateTime StartedListeningAt { get; set; }
	}
}

