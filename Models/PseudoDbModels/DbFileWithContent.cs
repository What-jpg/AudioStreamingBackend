namespace AudioStreamingApi.Models.PseudoDbModels
{
	public class DbFileWithContent
	{
        public DbFileWithContent(string content, string type)
        {
            Content = content;
            Type = type;
        }

        public string Content { get; set; }

        public string Type { get; set; }
    }
}

