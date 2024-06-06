namespace AudioStreamingApi.Models.PseudoDbModels
{
	public class DiscForUsers
	{
		public DiscForUsers(int id, string name, DateTime createdAt, int artistId, UserForUsers? artist = null, DbFileWithContent? cover = null, List<SongForUsers>? songs = null)
		{
            Id = id;
            Name = name;
            CreatedAt = createdAt;
            ArtistId = artistId;
            Artist = artist;
            Cover = cover;

            if (songs != null)
            {
                Songs = songs;
            }
		}

        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ArtistId { get; set; }

        public UserForUsers? Artist { get; set; }

        public DbFileWithContent? Cover { get; set; }

        public List<SongForUsers> Songs { get; set; } = new List<SongForUsers>();
    }
}

