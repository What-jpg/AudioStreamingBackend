namespace AudioStreamingApi.Models
{
	public class FileForRedisAuth
	{
		public FileForRedisAuth(string content, string contentType, string fileName)
		{
			Content = content;
			ContentType = contentType;
			FileName = fileName;
		}

		public string Content { get; set; }

		public string ContentType { get; set; }

		public string FileName { get; set; }
	}
}

