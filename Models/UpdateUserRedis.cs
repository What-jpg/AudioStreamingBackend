namespace AudioStreamingApi.Models
{
	public class UpdateUserRedis
	{

        public int Id { get; set; }

        public string HashedPassword { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public bool IsTwoFactorAuthActive { get; set; }

        public string? AvatarContent { get; set; }

        public string? AvatarContentType { get; set; }

        public string? AvatarFileName { get; set; }

        public string UpdateCode { get; set; }

        public bool AvatarHasChanged { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}

