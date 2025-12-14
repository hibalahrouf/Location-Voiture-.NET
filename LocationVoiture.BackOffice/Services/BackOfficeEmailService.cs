using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LocationVoiture.BackOffice.Services
{
    /// <summary>
    /// Email service for BackOffice notifications.
    /// Reuses same logic as FrontOffice EmailSender - saves emails to file system.
    /// </summary>
    public class BackOfficeEmailService
    {
        private readonly IConfiguration _config;
        private readonly string _emailDirectory;

        public BackOfficeEmailService(IConfiguration config)
        {
            _config = config;
            // Save emails to BackOffice's output directory
            _emailDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sent_emails");
            if (!Directory.Exists(_emailDirectory))
            {
                Directory.CreateDirectory(_emailDirectory);
            }
        }

        /// <summary>
        /// Send email notification asynchronously (non-blocking).
        /// Call with Task.Run() to avoid blocking UI thread.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var fromEmail = _config.GetValue<string>("Email:FromEmail") ?? "noreply@luxecar.com";
                var fromName = _config.GetValue<string>("Email:FromName") ?? "LuxeCar BackOffice";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlContent };

                // Save email to file (fake SMTP - same as FrontOffice)
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}.eml";
                var filePath = Path.Combine(_emailDirectory, fileName);
                await message.WriteToAsync(filePath);

                Log.Information("Email envoyé: {Subject} -> {ToEmail} (sauvegardé: {FilePath})", subject, toEmail, filePath);

                // In production, uncomment below to actually send via SMTP:
                /*
                var smtpHost = _config.GetValue<string>("Email:SmtpHost");
                var smtpPort = _config.GetValue<int>("Email:SmtpPort");
                var smtpUser = _config.GetValue<string>("Email:SmtpUser");
                var smtpPassword = _config.GetValue<string>("Email:SmtpPassword");

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                if (!string.IsNullOrEmpty(smtpUser))
                {
                    await client.AuthenticateAsync(smtpUser, smtpPassword);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                Log.Information("Email SMTP envoyé: {Subject} -> {ToEmail}", subject, toEmail);
                */
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de l'envoi d'email: {Subject} -> {ToEmail}", subject, toEmail);
            }
        }

        /// <summary>
        /// Send rental confirmation email to client.
        /// </summary>
        public Task SendRentalConfirmationAsync(string clientEmail, string clientName, 
            string vehicleInfo, DateTime dateDebut, DateTime dateFin, decimal montantTotal)
        {
            var subject = "🚗 Confirmation de votre location - LuxeCar";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #0B1F3F 0%, #1a3a5c 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .gold {{ color: #C9A44C; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #f8fafc; border-left: 4px solid #C9A44C; padding: 15px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
        .info-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e2e8f0; }}
        .info-row:last-child {{ border-bottom: none; }}
        .label {{ color: #64748b; font-size: 14px; }}
        .value {{ color: #1a202c; font-weight: 600; }}
        .total {{ background: #0B1F3F; color: white; padding: 20px; text-align: center; margin-top: 20px; border-radius: 8px; }}
        .total-amount {{ font-size: 32px; font-weight: bold; color: #C9A44C; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚗 <span class='gold'>LuxeCar</span></h1>
            <p>Confirmation de Location</p>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Votre location a été confirmée avec succès !</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <span class='label'>Véhicule</span>
                    <span class='value'>{vehicleInfo}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Date de début</span>
                    <span class='value'>{dateDebut:dd/MM/yyyy}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Date de fin</span>
                    <span class='value'>{dateFin:dd/MM/yyyy}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Durée</span>
                    <span class='value'>{(dateFin - dateDebut).Days} jours</span>
                </div>
            </div>
            
            <div class='total'>
                <p style='margin:0 0 10px 0;'>Montant Total</p>
                <div class='total-amount'>{montantTotal:N2} €</div>
            </div>
        </div>
        <div class='footer'>
            <p>Merci pour votre confiance.</p>
            <p>© {DateTime.Now.Year} LuxeCar - Tous droits réservés</p>
        </div>
    </div>
</body>
</html>";

            return SendEmailAsync(clientEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send payment confirmation email to client.
        /// </summary>
        public Task SendPaymentConfirmationAsync(string clientEmail, string clientName,
            string vehicleInfo, decimal montantPaye, string methodePaiement, DateTime datePaiement)
        {
            var subject = "💳 Confirmation de paiement - LuxeCar";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .success-icon {{ font-size: 48px; text-align: center; margin-bottom: 20px; }}
        .info-box {{ background: #f0fdf4; border-left: 4px solid #22c55e; padding: 15px; margin: 20px 0; border-radius: 0 8px 8px 0; }}
        .info-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #dcfce7; }}
        .info-row:last-child {{ border-bottom: none; }}
        .label {{ color: #64748b; font-size: 14px; }}
        .value {{ color: #1a202c; font-weight: 600; }}
        .amount {{ font-size: 28px; font-weight: bold; color: #22c55e; text-align: center; margin: 20px 0; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💳 Paiement Confirmé</h1>
        </div>
        <div class='content'>
            <div class='success-icon'>✅</div>
            <p>Bonjour <strong>{clientName}</strong>,</p>
            <p>Votre paiement a été reçu et confirmé.</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <span class='label'>Véhicule</span>
                    <span class='value'>{vehicleInfo}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Méthode</span>
                    <span class='value'>{methodePaiement}</span>
                </div>
                <div class='info-row'>
                    <span class='label'>Date</span>
                    <span class='value'>{datePaiement:dd/MM/yyyy HH:mm}</span>
                </div>
            </div>
            
            <div class='amount'>{montantPaye:N2} €</div>
        </div>
        <div class='footer'>
            <p>Merci pour votre paiement.</p>
            <p>© {DateTime.Now.Year} LuxeCar - Tous droits réservés</p>
        </div>
    </div>
</body>
</html>";

            return SendEmailAsync(clientEmail, subject, htmlContent);
        }
    }
}
