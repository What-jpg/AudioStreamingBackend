using System.Net.Mail;
using System.Net;

namespace AudioStreamingApi.Components
{
	public class SmtpConfigured
	{
		public SmtpConfigured()
		{
		}

		public static ConfigurationManager BuilderConfig = WebApplication.CreateBuilder().Configuration;

        public static SmtpClient GetSmtpClient()
		{
			return new SmtpClient(Environment.GetEnvironmentVariable("SMTP_CLIENT") ?? BuilderConfig.GetSection("Smtp:Client").Get<string>())
			{
				Port = Convert.ToInt32(Environment.GetEnvironmentVariable("SMTP_PORT") ?? BuilderConfig.GetSection("Smtp:Port").Get<string>()),
				Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("SMTP_USER_NAME_CREDENTIAL") ?? BuilderConfig.GetSection("Smtp:UserNameCredential").Get<string>(), Environment.GetEnvironmentVariable("SMTP_PASSWORD_CREDENTIAL") ?? BuilderConfig.GetSection("Smtp:PasswordCredential").Get<string>()),
				EnableSsl = true,
			};
		}

		public static MailMessage GetMailMessage(string recipient, string subject, string body)
		{
			return new MailMessage(Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? BuilderConfig.GetSection("Smtp:Email").Get<string>(), recipient, subject, body);
        }
	}
}

