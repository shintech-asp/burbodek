using System.Net;
using System.Net.Mail;

namespace burbodek.Services
{
    public class EmailServices
    {
        private readonly IConfiguration _config;

        public EmailServices(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailConfig = _config.GetSection("EmailSettings");

            using (var smtp = new SmtpClient())
            {
                smtp.Host = emailConfig["Host"];
                smtp.Port = int.Parse(emailConfig["Port"]);
                smtp.EnableSsl = bool.Parse(emailConfig["EnableSSL"]);
                smtp.Credentials = new NetworkCredential(emailConfig["SenderEmail"], emailConfig["SenderPassword"]);

                var message = new MailMessage();
                message.From = new MailAddress(emailConfig["SenderEmail"], "Cruise Ship Jobs Ph");
                message.To.Add(to);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true; // enable HTML template

                await smtp.SendMailAsync(message);
            }
        }
    }
}
