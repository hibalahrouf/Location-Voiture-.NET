using System.Net;
using System.Net.Mail;

namespace LocationVoiture.FrontOffice.Services
{
    public interface IEmailVerificationService
    {
        string GenerateVerificationToken();
        Task SendVerificationEmailAsync(string email, string token, string baseUrl);
    }

    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IConfiguration _configuration;

        public EmailVerificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateVerificationToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        public async Task SendVerificationEmailAsync(string email, string token, string baseUrl)
        {
            var verificationLink = $"{baseUrl}/Identity/Account/VerifyEmail?token={token}&email={WebUtility.UrlEncode(email)}";

            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"] ?? "";
            var smtpPass = _configuration["Email:SmtpPassword"] ?? "";
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, "Location Voiture"),
                Subject = "Verifiez votre adresse email",
                Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>Bienvenue sur Location Voiture!</h2>
                        <p>Merci de vous etre inscrit. Veuillez cliquer sur le lien ci-dessous pour verifier votre adresse email:</p>
                        <p><a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verifier mon email</a></p>
                        <p>Ou copiez ce lien dans votre navigateur:</p>
                        <p>{verificationLink}</p>
                        <p>Ce lien expirera dans 24 heures.</p>
                    </body>
                    </html>",
                IsBodyHtml = true
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
