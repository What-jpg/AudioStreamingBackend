namespace AudioStreamingApi.Models
{
	public class ContinueStreamingSongInfo
	{
		public ContinueStreamingSongInfo(string songContentPart, TimeSpan thisPartDuration, TimeSpan? whenToUpdate)
        {
            SongContentPart = songContentPart;

            ThisPartDuration = thisPartDuration.TotalMicroseconds;

            if (whenToUpdate != null)
            {
                WhenToUpdate = whenToUpdate.Value.TotalMicroseconds;
            } else
            {
                WhenToUpdate = null;
            }
        }

        public string SongContentPart { get; set; }

        public double ThisPartDuration { get; set; }

        public double? WhenToUpdate { get; set; }
    }
}

