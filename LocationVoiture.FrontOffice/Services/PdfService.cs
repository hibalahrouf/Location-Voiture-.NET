using LocationVoiture.Core.Models;
using System.Text;

namespace LocationVoiture.FrontOffice.Services
{
    public interface IPdfService
    {
        byte[] GenerateReservationPdf(Location location);
    }

    public class PdfService : IPdfService
    {
        /// <summary>
        /// Generates a simple HTML-based PDF for the reservation.
        /// Note: For production, consider using libraries like iTextSharp, PDFsharp, or QuestPDF.
        /// This implementation creates an HTML file that can be printed as PDF.
        /// </summary>
        public byte[] GenerateReservationPdf(Location location)
        {
            var html = GenerateHtmlContent(location);
            return Encoding.UTF8.GetBytes(html);
        }

        private string GenerateHtmlContent(Location location)
        {
            var vehiculeName = $"{location.Vehicule?.Marque} {location.Vehicule?.Modele}";
            var clientName = $"{location.Client?.Prenom} {location.Client?.Nom}";
            var dateDebut = location.DateDebut.ToString("dd/MM/yyyy");
            var dateFin = location.DateFin.ToString("dd/MM/yyyy");
            var montant = location.MontantTotal.ToString("C");
            var statut = location.Statut.ToString();

            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <title>Confirmation de Réservation #{location.LocationID}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #f8f9fa;
            padding: 40px;
            color: #1a1a2e;
        }}
        .container {{
            max-width: 700px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #0A1A2F, #132B4A);
            color: white;
            padding: 40px;
            text-align: center;
        }}
        .header h1 {{
            font-size: 2rem;
            margin-bottom: 10px;
        }}
        .header .gold {{
            color: #C9A44C;
        }}
        .header p {{
            opacity: 0.8;
        }}
        .content {{
            padding: 40px;
        }}
        .badge {{
            display: inline-block;
            background: rgba(201, 164, 76, 0.2);
            color: #C9A44C;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 0.9rem;
            font-weight: 600;
            margin-bottom: 20px;
        }}
        .section {{
            margin-bottom: 30px;
        }}
        .section-title {{
            font-size: 1.1rem;
            color: #64748b;
            margin-bottom: 15px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 15px 0;
            border-bottom: 1px solid #e2e8f0;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            color: #64748b;
        }}
        .info-value {{
            font-weight: 600;
            color: #1a1a2e;
        }}
        .total-row {{
            background: linear-gradient(135deg, #0A1A2F, #132B4A);
            color: white;
            padding: 20px;
            border-radius: 12px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-top: 20px;
        }}
        .total-amount {{
            font-size: 1.8rem;
            font-weight: 700;
            color: #C9A44C;
        }}
        .footer {{
            background: #f8f9fa;
            padding: 30px 40px;
            text-align: center;
            color: #64748b;
            font-size: 0.9rem;
        }}
        .footer a {{
            color: #C9A44C;
            text-decoration: none;
        }}
        @@media print {{
            body {{ padding: 0; background: white; }}
            .container {{ box-shadow: none; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Location<span class='gold'>Voiture</span></h1>
            <p>Confirmation de Réservation</p>
        </div>
        
        <div class='content'>
            <span class='badge'>Réservation #{location.LocationID}</span>
            
            <div class='section'>
                <div class='section-title'>Informations Client</div>
                <div class='info-row'>
                    <span class='info-label'>Nom</span>
                    <span class='info-value'>{clientName}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Email</span>
                    <span class='info-value'>{location.Client?.Email ?? "N/A"}</span>
                </div>
            </div>
            
            <div class='section'>
                <div class='section-title'>Détails du Véhicule</div>
                <div class='info-row'>
                    <span class='info-label'>Véhicule</span>
                    <span class='info-value'>{vehiculeName}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Type</span>
                    <span class='info-value'>{location.Vehicule?.TypeVehicule?.Nom ?? "N/A"}</span>
                </div>
            </div>
            
            <div class='section'>
                <div class='section-title'>Période de Location</div>
                <div class='info-row'>
                    <span class='info-label'>Date de début</span>
                    <span class='info-value'>{dateDebut}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Date de fin</span>
                    <span class='info-value'>{dateFin}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Statut</span>
                    <span class='info-value'>{statut}</span>
                </div>
            </div>
            
            <div class='total-row'>
                <span>Montant Total</span>
                <span class='total-amount'>{montant}</span>
            </div>
        </div>
        
        <div class='footer'>
            <p>Merci d'avoir choisi LocationVoiture Premium</p>
            <p>Contact: <a href='tel:+33123456789'>+33 1 23 45 67 89</a> | <a href='mailto:contact@locationvoiture.com'>contact@locationvoiture.com</a></p>
            <p style='margin-top: 15px; font-size: 0.8rem;'>Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
