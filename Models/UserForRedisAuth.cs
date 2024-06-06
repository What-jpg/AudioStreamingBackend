namespace AudioStreamingApi.Models
{
	public class UserForRedisAuth
	{
        public int? Id { get; set; }

        public string HashedPassword { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string? AvatarContent { get; set; }

        public string? AvatarContentType { get; set; }

        public string? AvatarFileName { get; set; }

        public bool NeedToRemember30Days { get; set; }

        public string AuthCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

