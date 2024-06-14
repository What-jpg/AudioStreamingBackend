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


		public static string? Client = Environment.GetEnvironmentVariable("SMTP_CLIENT") ?? BuilderConfig.GetSection("Smtp:Client").Get<string>();

        public static int Port = Convert.ToInt32(Environment.GetEnvironmentVariable("SMTP_PORT") ?? BuilderConfig.GetSection("Smtp:Port").Get<string>());

        public static string? UserNameCredential = Environment.GetEnvironmentVariable("SMTP_USER_NAME_CREDENTIAL") ?? BuilderConfig.GetSection("Smtp:UserNameCredential").Get<string>();

        public static string? PasswordCredential = Environment.GetEnvironmentVariable("SMTP_PASSWORD_CREDENTIAL") ?? BuilderConfig.GetSection("Smtp:PasswordCredential").Get<string>();

        public static string? FromEmail = Environment.GetEnvironmentVariable("SMTP_MAIL_FROM_EMAIL") ?? BuilderConfig.GetSection("Smtp:MailFromEmail").Get<string>();

		public static string? FromDisplayName = Environment.GetEnvironmentVariable("SMTP_MAIL_FROM_DISPLAY_NAME") ?? BuilderConfig.GetSection("Smtp:MailFromDisplayName").Get<string>();


        private static SmtpClient GetSmtpClient()
		{
			return new SmtpClient(Client)
			{
				Port = Port,
				Credentials = new NetworkCredential(UserNameCredential, PasswordCredential),
				EnableSsl = true,
			};
		}

		private static MailMessage GetMailMessage(string recipient, string subject, string body)
		{
			if (FromDisplayName != null) 
			{
				var from = new MailAddress(FromEmail, FromDisplayName);

				var mail = new MailMessage();

				mail.From = from;
				mail.To.Add(recipient);
				mail.Subject = subject;
				mail.Body = body;

				return mail;
			} else {
				return new MailMessage(FromEmail, recipient, subject, body);
			}
        }

		public static void GetMailMessageAndSendIt(string recipient, string subject, string body)
		{
			if (Client != null &&
				Port != 0 &&
                UserNameCredential != null &&
                PasswordCredential != null &&
                FromEmail != null)
			{
                var smtpClient = GetSmtpClient();
				var mail = GetMailMessage(recipient, subject, body);

				smtpClient.Send(mail);
            } else
			{
				Console.WriteLine($"The smtp client is not configured properly, so it won't be sent, the mail recipient is: {recipient}, the mail subject is: \"{subject}\", the mail body: \"{body}\"");
			}
		}
	}
}

