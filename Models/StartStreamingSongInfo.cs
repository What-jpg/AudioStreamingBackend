namespace AudioStreamingApi.Models.DbModels;

public class StartStreamingSongInfo
{
	public StartStreamingSongInfo(Song song, TimeSpan thisPartDuration, TimeSpan? whenToUpdate)
	{
		Song = song;

		ThisPartDuration = thisPartDuration;

		WhenToUpdate = whenToUpdate;
	}

	public Song Song { get; set; }

	public TimeSpan ThisPartDuration { get; set; }

	public TimeSpan? WhenToUpdate { get; set; }
}

