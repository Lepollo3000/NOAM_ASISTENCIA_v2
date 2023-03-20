using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using NOAM_ASISTENCIA_v2.Server.Models;
using NOAM_ASISTENCIA_v2.Server.Data.Mail;
//using NOAM_ASISTENCIA_v2.Shared.Utils.Account;

namespace NOAM_ASISTENCIA_v2.Server.Utills
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<MailService> _logger;

        public MailService(IOptions<MailSettings> mailSettings, ILogger<MailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
            _logger.LogInformation("Se creó mail service");
        }

        public async Task SendEmailAsync(string username, string userEmail, string redirectUrl)
        {
            _logger.LogInformation("Se ejecuto sendEmail del mail service");
            string FilePath = Directory.GetCurrentDirectory() + "/Data/Mail/Templates/RegisterTemplate.html";
            StreamReader str = new StreamReader(FilePath);
            string MailText = str.ReadToEnd();

            str.Close();
            MailText = MailText
                .Replace("[Username]", username)
                .Replace("UrlRedirect", redirectUrl);

            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
            email.To.Add(MailboxAddress.Parse(userEmail));
            email.Subject = $"Confirmación de cuenta para {username}";

            var builder = new BodyBuilder();
            builder.HtmlBody = MailText;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            _logger.LogInformation("Informacion de correo: {mailSettings} {mailSettings2}", _mailSettings.Host, _mailSettings.Port);
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.None);

            //smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);

            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        /*public async Task SendRegisterEmailAsync(RegisterRequest request, string redirectUrl)
        {
            await SendEmailAsync(request.Username, request.Email, redirectUrl);
        }*/

        public async Task ResendConfirmationEmailAsync(ApplicationUser request, string redirectUrl)
        {
            await SendEmailAsync(request.UserName, request.Email, redirectUrl);
        }
    }
}
