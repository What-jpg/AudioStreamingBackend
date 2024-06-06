namespace AudioStreamingApi.Models.PseudoDbModels
{
	public class UserForHimself : UserForUsers
    {
		public UserForHimself(int id, string name, string email, bool isTwoFactorAuthActive, DbFileWithContent? avatar = null)
        {
            Id = id;
            Name = name;
            Avatar = avatar;
            Email = email;
            IsTwoFactorAuthActive = isTwoFactorAuthActive;
        }

        public string Email { get; set; }
        public bool IsTwoFactorAuthActive { get; set; }
    }
}

