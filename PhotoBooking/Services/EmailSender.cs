using System.Net;
using System.Net.Mail;

namespace PhotoBooking.Services
{
    public class EmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Lấy thông tin từ appsettings
            string mailServer = _config["EmailSettings:MailServer"];
            int mailPort = int.Parse(_config["EmailSettings:MailPort"]);
            string senderEmail = _config["EmailSettings:SenderEmail"];
            string senderName = _config["EmailSettings:SenderName"];
            string password = _config["EmailSettings:Password"];

            var client = new SmtpClient(mailServer, mailPort)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true // Cho phép gửi nội dung HTML đẹp
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
