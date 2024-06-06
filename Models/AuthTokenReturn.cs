namespace AudioStreamingApi.Models
{
	public class AuthTokenReturn
	{
		public AuthTokenReturn(string token, DateTime expiresAt, bool isLongTerm)
		{
			Token = token;
			ExpiresAt = expiresAt;
			IsLongTerm = isLongTerm;
		}

        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

		public bool IsLongTerm { get; set; }
    }
}

