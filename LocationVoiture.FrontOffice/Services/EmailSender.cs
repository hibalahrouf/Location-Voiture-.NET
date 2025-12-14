using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace LocationVoiture.FrontOffice.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailSender(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpHost = _config.GetValue<string>("Email:SmtpHost") ?? "smtp.gmail.com";
            var smtpPort = _config.GetValue<int>("Email:SmtpPort");
            if (smtpPort == 0) smtpPort = 587;
            var smtpUser = _config.GetValue<string>("Email:SmtpUser");
            var smtpPassword = _config.GetValue<string>("Email:SmtpPassword");
            var fromEmail = _config.GetValue<string>("Email:FromEmail") ?? smtpUser;

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("LuxeCar Premium", fromEmail));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                // Send via real SMTP if credentials are configured
                if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPassword))
                {
                    using var client = new SmtpClient();
                    
                    // Accept SSL certificates (fixes certificate revocation check errors)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Email envoyé à {email}: {subject}");
                }
                else
                {
                    // Fallback: save to file if no SMTP credentials
                    var emailDirectory = Path.Combine(_env.WebRootPath, "sent_emails");
                    if (!Directory.Exists(emailDirectory))
                    {
                        Directory.CreateDirectory(emailDirectory);
                    }
                    var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.eml";
                    var filePath = Path.Combine(emailDirectory, fileName);
                    await message.WriteToAsync(filePath);
                    
                    System.Diagnostics.Debug.WriteLine($"📧 Email sauvegardé: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur email: {ex.Message}");
                throw; // Re-throw to let caller know about the failure
            }
        }
    }
}