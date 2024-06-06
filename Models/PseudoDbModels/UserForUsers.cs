namespace AudioStreamingApi.Models.PseudoDbModels
{
	public class UserForUsers
	{
        public UserForUsers(int id, string name, DbFileWithContent? avatar = null)
        {
            Id = id;
            Name = name;
            Avatar = avatar;
        }

        public UserForUsers()
        {

        }

        public int Id { get; set; }

        public string Name { get; set; }

        public DbFileWithContent? Avatar { get; set; }
    }
}

