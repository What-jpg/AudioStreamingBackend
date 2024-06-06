namespace AudioStreamingApi.Models.DbModels
{
	public class User
	{
        public int Id { get; set; }

		public string HashedPassword { get; set; }

		public string Name { get; set; }

		public string Email { get; set; }

		public bool IsTwoFactorAuthActive { get; set; }

        public int? AvatarId { get; set; }

        public DbFile? Avatar { get; set; }
	}
}

