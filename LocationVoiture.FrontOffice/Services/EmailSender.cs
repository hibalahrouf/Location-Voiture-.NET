using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;
using MailKit.Net.Smtp; // MailKit
using MimeKit; // MailKit

namespace LocationVoiture.FrontOffice.Services
{
    // Nous implémentons l'interface par défaut d'Identity pour l'envoi d'e-mails
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public EmailSender(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        // C'est la méthode asynchrone requise
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // On récupère les "faux" paramètres de appsettings.json
            var host = _config.GetValue<string>("EmailSettings:Host");
            var port = _config.GetValue<int>("EmailSettings:Port");
            var fromEmail = _config.GetValue<string>("EmailSettings:FromEmail");

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Admin LocationVoiture", fromEmail));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = htmlMessage
                };

                // C'est notre "fausse" implémentation
                // Au lieu d'envoyer, nous allons sauvegarder l'email dans un fichier
                // dans le dossier "wwwroot/sent_emails"

                // 1. S'assurer que le dossier existe
                var emailDirectory = Path.Combine(_env.WebRootPath, "sent_emails");
                if (!Directory.Exists(emailDirectory))
                {
                    Directory.CreateDirectory(emailDirectory);
                }

                // 2. Créer un nom de fichier unique
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.eml";
                var filePath = Path.Combine(emailDirectory, fileName);

                // 3. Écrire le fichier (c'est une opération I/O, donc on la fait en asynchrone)
                await message.WriteToAsync(filePath);

                // Dans un vrai projet, vous remplaceriez le code ci-dessus par ceci :
                /*
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("votre_username", "votre_mot_de_passe");
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                */
            }
            catch (Exception ex)
            {
                // Gérer l'erreur (nous pourrions utiliser le logger ici)
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'envoi de l'email : {ex.Message}");
            }
        }
    }
}