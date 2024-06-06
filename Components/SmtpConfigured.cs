using System.Net.Mail;
using System.Net;

namespace AudioStreamingApi.Components
{
	public class SmtpConfigured
	{
		public SmtpConfigured()
		{
		}

		public static SmtpClient GetSmtpClient()
		{
			var builderConfiguration = WebApplication.CreateBuilder().Configuration;

			return new SmtpClient(builderConfiguration.GetSection("Smtp:Client").Get<string>())
			{
				Port = builderConfiguration.GetSection("Smtp:Port").Get<int>(),
				Credentials = new NetworkCredential(builderConfiguration.GetSection("Smtp:UserNameCredential").Get<string>(), builderConfiguration.GetSection("Smtp:PasswordCredential").Get<string>()),
				EnableSsl = true,
			};
		}

		public static MailMessage GetMailMessage(string recipient, string subject, string body)
		{
            var builderConfiguration = WebApplication.CreateBuilder().Configuration;

			return new MailMessage(builderConfiguration.GetSection("Smtp:Email").Get<string>(), recipient, subject, body);
        }
	}
}

