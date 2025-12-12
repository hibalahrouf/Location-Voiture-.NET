using LocationVoiture.Core.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Important pour .Where() et .ToList()
using System.Threading.Tasks;
using LocationVoiture.Core.Models; // Pour Location
using Serilog; // (On le laisse pour plus tard)
using System.Collections.Generic; // Pour List<>
using Microsoft.AspNetCore.Mvc.Rendering; // Pour SelectList
using Serilog; // <-- AJOUTER
namespace LocationVoiture.FrontOffice.Controllers
{
    public class VehiculeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VehiculeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================================
        // || ACTION "INDEX" (MISE À JOUR AVEC FILTRES)
        // ==========================================================
        // string? searchString -> pour la barre de recherche
        // int? typeId -> pour le filtre par type
        public async Task<IActionResult> Index(string? searchString, int? typeId)
        {
            // --- 1. Préparer la requête de base ---
            // On récupère les véhicules, mais on n'exécute pas la requête tout de suite (IQueryable)
            var vehiculesQuery = _context.Vehicules
                                    .Include(v => v.TypeVehicule)
                                    .ThenInclude(t => t.Tarifs)
                                    .Where(v => v.Disponible == true);

            // --- 2. Appliquer le filtre de recherche (Marque/Modèle) ---
            if (!string.IsNullOrEmpty(searchString))
            {
                vehiculesQuery = vehiculesQuery.Where(v =>
                    v.Marque.Contains(searchString) ||
                    v.Modele.Contains(searchString)
                );
            }

            // --- 3. Appliquer le filtre par Type ---
            if (typeId.HasValue)
            {
                vehiculesQuery = vehiculesQuery.Where(v => v.TypeVehiculeID == typeId.Value);
            }

            // --- 4. Préparer les données pour les filtres (le menu déroulant) ---

            // Récupérer tous les types pour le menu déroulant
            var types = await _context.TypesVehicules.AsNoTracking().ToListAsync();

            // "ViewBag" permet d'envoyer des données temporaires à la Vue
            ViewBag.Types = new SelectList(types, "TypeVehiculeID", "Nom", typeId); // typeId est la valeur sélectionnée
            ViewBag.CurrentSearch = searchString; // Garder le texte dans la barre de recherche

            // --- 5. Exécuter la requête finale et envoyer à la Vue ---
            var vehicules = await vehiculesQuery.ToListAsync();

            return View(vehicules);
        }

        // ==========================================================
        // || ACTION "DETAILS" (Existante)
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
        // || ACTION "CREERLOCATION" (Existante)
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

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Challenge();
            }

            var client = _context.Clients.FirstOrDefault(c => c.Email == identityUser.Email);
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

            return RedirectToAction("ConfirmationLocation", new { id = newLocation.LocationID });
        }

        // ==========================================================
        // || ACTION "CONFIRMATION" (Existante)
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
    }
}