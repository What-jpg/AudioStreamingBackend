namespace AudioStreamingApi.Models
{
	public class AudioStringContentAndIsLastSongPart
	{
		public AudioStringContentAndIsLastSongPart(string audioStringContent, bool isLastSongPart)
		{
			AudioStringContent = audioStringContent;
			IsLastSongPart = isLastSongPart;
		}

		public string AudioStringContent { get; set; }

		public bool IsLastSongPart { get; set; }
    }
}

