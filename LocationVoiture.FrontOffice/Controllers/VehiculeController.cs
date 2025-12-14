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
        public async Task<IActionResult> Index(string? searchString, int? typeId)
        {
            var vehiculesQuery = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
                                    .ThenInclude(t => t.Tarifs)
                                    .Where(v => v.Disponible == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                vehiculesQuery = vehiculesQuery.Where(v =>
                    v.Marque.Contains(searchString) ||
                    v.Modele.Contains(searchString)
                );
            }

            if (typeId.HasValue)
            {
                vehiculesQuery = vehiculesQuery.Where(v => v.TypeVehiculeID == typeId.Value);
            }

            var types = await _context.TypesVehicules.AsNoTracking().ToListAsync();
            ViewBag.Types = new SelectList(types, "TypeVehiculeID", "Nom", typeId);
            ViewBag.CurrentSearch = searchString;

            var vehicules = await vehiculesQuery.ToListAsync();
            return View(vehicules);
        }

        // ==========================================================
        // || ACTION "DETAILS"
        // ==========================================================
        public IActionResult Details(int id)
        {
            var vehicule = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
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
                EmployeID = null
            };

            vehicule.Disponible = false;

            _context.Locations.Add(newLocation);
            await _context.SaveChangesAsync();

            Log.Information("Nouvelle location créée : ID {LocationID} par Client ID {ClientID}", newLocation.LocationID, client.ClientID);

            // Send confirmation email asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var emailHtml = GenerateConfirmationEmailHtml(newLocation, vehicule, client);
                    await _emailSender.SendEmailAsync(
                        client.Email,
                        $"Confirmation de réservation #{newLocation.LocationID}",
                        emailHtml
                    );
                    Log.Information("Email de confirmation envoyé à {Email} pour la location {LocationID}", client.Email, newLocation.LocationID);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erreur lors de l'envoi de l'email de confirmation pour la location {LocationID}", newLocation.LocationID);
                }
            });

            return RedirectToAction("ConfirmationLocation", new { id = newLocation.LocationID });
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
        // || HELPER: Generate Confirmation Email HTML
        // ==========================================================
        private string GenerateConfirmationEmailHtml(
            LocationVoiture.Core.Models.Location location, 
            Vehicule vehicule, 
            Client client)
        {
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
            <h1>Location<span class='gold'>Voiture</span></h1>
            <p style='margin: 10px 0 0; opacity: 0.8;'>Confirmation de Réservation</p>
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
                <div class='total-amount'>{location.MontantTotal:C}</div>
            </div>
            
            <p style='margin-top: 20px; color: #6c757d; font-size: 0.9rem;'>
                Votre réservation est en attente de confirmation. Vous recevrez un email dès qu'elle sera validée.
            </p>
        </div>
        <div class='footer'>
            <p>© 2025 LocationVoiture Premium</p>
            <p>Contact: +33 1 23 45 67 89</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}