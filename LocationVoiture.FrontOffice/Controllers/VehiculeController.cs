using LocationVoiture.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LocationVoiture.Core.Models;
using Serilog;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity.UI.Services;
using LocationVoiture.FrontOffice.Services;

namespace LocationVoiture.FrontOffice.Controllers
{
    public class VehiculeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IPdfService _pdfService;

        public VehiculeController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            IPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _pdfService = pdfService;
        }

        // ==========================================================
        // || ACTION "INDEX" (WITH FILTERS)
        // ==========================================================
        public async Task<IActionResult> Index(string? searchString, List<int>? typeIds, decimal? minPrice, decimal? maxPrice)
        {
            var vehiculesQuery = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
                                    .ThenInclude(t => t.Tarifs)
                                    .Include(v => v.Images)
                                    .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                vehiculesQuery = vehiculesQuery.Where(v =>
                    v.Marque.Contains(searchString) ||
                    v.Modele.Contains(searchString)
                );
            }

            // Multiple type filter
            if (typeIds != null && typeIds.Any())
            {
                vehiculesQuery = vehiculesQuery.Where(v => typeIds.Contains(v.TypeVehiculeID));
            }

            // Get all vehicles first for price filtering (since PrixJournee is computed)
            var vehicules = await vehiculesQuery.ToListAsync();

            // Price range filter (on computed property)
            if (minPrice.HasValue && minPrice > 0)
            {
                vehicules = vehicules.Where(v => v.PrixJournee >= minPrice.Value).ToList();
            }
            if (maxPrice.HasValue && maxPrice > 0)
            {
                vehicules = vehicules.Where(v => v.PrixJournee <= maxPrice.Value).ToList();
            }

            // Pass data to view
            var allTypes = await _context.TypesVehicules.AsNoTracking().ToListAsync();
            ViewBag.AllTypes = allTypes;
            ViewBag.SelectedTypes = typeIds ?? new List<int>();
            ViewBag.CurrentSearch = searchString;
            ViewBag.MinPrice = minPrice ?? 0;
            ViewBag.MaxPrice = maxPrice ?? 1000;

            return View(vehicules);
        }

        // ==========================================================
        // || ACTION "DETAILS"
        // ==========================================================
        public IActionResult Details(int id)
        {
            var vehicule = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
                                    .ThenInclude(t => t.Tarifs)
                                    .Include(v => v.Images)
                                    .FirstOrDefault(v => v.VehiculeID == id);
            if (vehicule == null)
            {
                return NotFound();
            }
            return View(vehicule);
        }

        // ==========================================================
        // || ACTION "CREERLOCATION" (CREATE RESERVATION)
        // ==========================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreerLocation(int VehiculeID, DateTime DateDebut, DateTime DateFin)
        {
            if (DateDebut < DateTime.Today || DateFin <= DateDebut)
            {
                TempData["Error"] = "Les dates de location sont invalides.";
                return RedirectToAction("Details", new { id = VehiculeID });
            }

            var vehicule = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
                                    .ThenInclude(t => t.Tarifs)
                                    .FirstOrDefault(v => v.VehiculeID == VehiculeID);

            if (vehicule == null || !vehicule.Disponible)
            {
                TempData["Error"] = "Ce véhicule n'est pas disponible.";
                return RedirectToAction("Index");
            }

            var clientIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(clientIdClaim) || !int.TryParse(clientIdClaim, out int clientId))
            {
                return Challenge();
            }

            var client = _context.Clients.FirstOrDefault(c => c.ClientID == clientId);
            if (client == null)
            {
                TempData["Error"] = "Erreur de compte client. Veuillez contacter le support.";
                return RedirectToAction("Details", new { id = VehiculeID });
            }

            var tarif = vehicule.TypeVehicule.Tarifs.FirstOrDefault();
            if (tarif == null)
            {
                TempData["Error"] = "Erreur de tarification pour ce véhicule.";
                return RedirectToAction("Details", new { id = VehiculeID });
            }

            var nombreDeJours = (DateFin - DateDebut).TotalDays;
            var montantTotal = (decimal)nombreDeJours * tarif.PrixParJour;

            var newLocation = new LocationVoiture.Core.Models.Location
            {
                DateDebut = DateDebut,
                DateFin = DateFin,
                MontantTotal = montantTotal,
                Statut = LocationVoiture.Core.Models.LocationStatut.EnAttente,
                ClientID = client.ClientID,
                VehiculeID = vehicule.VehiculeID,
                EmployeID = null,
                ConfirmationToken = Guid.NewGuid().ToString("N") // Generate unique token
            };

            // vehicule.QuantiteDisponible--; // Decrement moved to ConfirmerLocation as requested

            _context.Locations.Add(newLocation);
            await _context.SaveChangesAsync();

            Log.Information("Nouvelle location créée : ID {LocationID} par Client ID {ClientID} - En attente de confirmation email", newLocation.LocationID, client.ClientID);

            // Generate confirmation URL
            var confirmationUrl = Url.Action(
                "ConfirmerLocation",
                "Vehicule",
                new { id = newLocation.LocationID, token = newLocation.ConfirmationToken },
                Request.Scheme
            );

            // Send confirmation email with link asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailHtml = GenerateConfirmationEmailHtml(newLocation, vehicule, client, confirmationUrl);
                    await _emailSender.SendEmailAsync(
                        client.Email,
                        $"⏳ Confirmez votre réservation #{newLocation.LocationID} - LuxeCar",
                        emailHtml
                    );
                    Log.Information("Email de confirmation avec lien envoyé à {Email} pour la location {LocationID}", client.Email, newLocation.LocationID);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erreur lors de l'envoi de l'email de confirmation pour la location {LocationID}", newLocation.LocationID);
                }
            });

            TempData["Success"] = "Réservation créée! Un email de confirmation a été envoyé. Veuillez cliquer sur le lien pour confirmer.";
            return RedirectToAction("ConfirmationLocation", new { id = newLocation.LocationID });
        }

        // ==========================================================
        // || ACTION "CONFIRMERLOCATION" (EMAIL VERIFICATION)
        // ==========================================================
        [HttpGet]
        public async Task<IActionResult> ConfirmerLocation(int id, string token)
        {
            var location = await _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .FirstOrDefaultAsync(l => l.LocationID == id);

            if (location == null)
            {
                TempData["Error"] = "Réservation introuvable.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(token) || location.ConfirmationToken != token)
            {
                TempData["Error"] = "Lien de confirmation invalide ou expiré.";
                return RedirectToAction("Index");
            }

            if (location.Statut == LocationVoiture.Core.Models.LocationStatut.Confirmee)
            {
                TempData["Info"] = "Cette réservation a déjà été confirmée.";
                return RedirectToAction("ConfirmationLocation", new { id = location.LocationID });
            }

            // Confirm the location
            // Check stock again to prevent race condition overbooking
            if (location.Vehicule.QuantiteDisponible > 0)
            {
                location.Vehicule.QuantiteDisponible--;
                location.Statut = LocationVoiture.Core.Models.LocationStatut.Confirmee;
                location.ConfirmationToken = null; // Clear token after use
                await _context.SaveChangesAsync();
            }
            else
            {
                // Edge case where stock ran out between request and confirmation
                TempData["Error"] = "Désolé, ce véhicule n'est plus disponible (stock épuisé).";
                return RedirectToAction("Index");
            }

            Log.Information("Location {LocationID} confirmée par email par Client {ClientID}", location.LocationID, location.ClientID);

            TempData["Success"] = "✅ Votre réservation a été confirmée avec succès!";
            return RedirectToAction("ConfirmationLocation", new { id = location.LocationID });
        }

        // ==========================================================
        // || ACTION "CONFIRMATIONLOCATION"
        // ==========================================================
        [Authorize]
        public IActionResult ConfirmationLocation(int id)
        {
            var location = _context.Locations
                                    .Include(l => l.Client)
                                    .Include(l => l.Vehicule)
                                    .ThenInclude(v => v.TypeVehicule)
                                    .FirstOrDefault(l => l.LocationID == id);

            if (location == null)
            {
                return NotFound();
            }

            return View(location);
        }

        // ==========================================================
        // || ACTION "DOWNLOADPDF" (NEW)
        // ==========================================================
        [Authorize]
        public IActionResult DownloadPdf(int id)
        {
            var location = _context.Locations
                                    .Include(l => l.Client)
                                    .Include(l => l.Vehicule)
                                    .ThenInclude(v => v.TypeVehicule)
                                    .FirstOrDefault(l => l.LocationID == id);

            if (location == null)
            {
                return NotFound();
            }

            var pdfBytes = _pdfService.GenerateReservationPdf(location);
            var fileName = $"Reservation_{location.LocationID}_{DateTime.Now:yyyyMMdd}.html";
            
            return File(pdfBytes, "text/html", fileName);
        }

        // ==========================================================
        // || HELPER: Generate Confirmation Email HTML with Link
        // ==========================================================
        private string GenerateConfirmationEmailHtml(
            LocationVoiture.Core.Models.Location location, 
            Vehicule vehicule, 
            Client client,
            string? confirmationUrl = null)
        {
            var confirmButton = !string.IsNullOrEmpty(confirmationUrl) ? $@"
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{confirmationUrl}' style='display: inline-block; background: linear-gradient(135deg, #C9A44C, #d4a528); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>✅ Confirmer ma réservation</a>
            </div>
            <p style='text-align: center; color: #6c757d; font-size: 0.85rem;'>
                Ou copiez ce lien: <a href='{confirmationUrl}'>{confirmationUrl}</a>
            </p>" : "";

            var statusText = location.Statut == LocationVoiture.Core.Models.LocationStatut.Confirmee 
                ? "<p style='color: #22c55e; font-weight: bold; text-align: center;'>✅ Réservation Confirmée</p>" 
                : $"<p style='color: #f59e0b; font-weight: bold; text-align: center;'>⏳ En attente de confirmation</p>{confirmButton}";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f5f5f5; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #0A1A2F, #132B4A); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 1.5rem; }}
        .gold {{ color: #C9A44C; }}
        .content {{ padding: 30px; }}
        .info {{ background: #f8f9fa; padding: 20px; border-radius: 12px; margin: 20px 0; }}
        .row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e9ecef; }}
        .row:last-child {{ border: none; }}
        .total {{ background: #0A1A2F; color: white; padding: 20px; border-radius: 12px; text-align: center; margin-top: 20px; }}
        .total-amount {{ font-size: 2rem; color: #C9A44C; font-weight: bold; }}
        .footer {{ padding: 20px; text-align: center; color: #6c757d; font-size: 0.9rem; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚗 <span class='gold'>LuxeCar</span></h1>
            <p style='margin: 10px 0 0; opacity: 0.8;'>Demande de Réservation</p>
        </div>
        <div class='content'>
            <p>Bonjour <strong>{client.Prenom}</strong>,</p>
            <p>Nous avons bien reçu votre demande de réservation. Voici les détails :</p>
            
            <div class='info'>
                <div class='row'>
                    <span>Référence</span>
                    <strong>#{location.LocationID}</strong>
                </div>
                <div class='row'>
                    <span>Véhicule</span>
                    <strong>{vehicule.Marque} {vehicule.Modele}</strong>
                </div>
                <div class='row'>
                    <span>Du</span>
                    <strong>{location.DateDebut:dd/MM/yyyy}</strong>
                </div>
                <div class='row'>
                    <span>Au</span>
                    <strong>{location.DateFin:dd/MM/yyyy}</strong>
                </div>
            </div>
            
            <div class='total'>
                <p style='margin: 0 0 10px; opacity: 0.8;'>Montant Total</p>
                <div class='total-amount'>{location.MontantTotal:N2} €</div>
            </div>
            
            {statusText}
        </div>
        <div class='footer'>
            <p>© 2025 LuxeCar Premium</p>
            <p>Contact: +33 1 23 45 67 89</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}