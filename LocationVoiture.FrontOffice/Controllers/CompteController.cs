using LocationVoiture.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LocationVoiture.FrontOffice.Controllers
{
    [Authorize] // TRÈS IMPORTANT : Seuls les utilisateurs connectés peuvent voir cette page
    public class CompteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CompteController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Action principale : /Compte/Index
        // Elle affichera la liste des locations du client
        public async Task<IActionResult> Index()
        {
            // 1. Trouver l'identifiant du client dans les claims (ClientID)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int clientId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // 2. Trouver le "Client" lié à cet identifiant
            var client = await _context.Clients
                                       .FirstOrDefaultAsync(c => c.ClientID == clientId);

            if (client == null)
            {
                ViewData["Error"] = "Impossible de trouver votre profil client.";
                return View(new List<LocationVoiture.Core.Models.Location>());
            }

            ViewData["ClientName"] = client.Prenom + " " + client.Nom;

            // 3. Récupérer toutes les locations de ce client
            var mesLocations = await _context.Locations
                .Where(l => l.ClientID == client.ClientID)
                .Include(l => l.Vehicule) // Inclure les infos de la voiture
                .OrderByDescending(l => l.DateDebut) // Trier par la plus récente
                .ToListAsync();

            // 4. Envoyer la liste à la Vue
            return View(mesLocations);
        }
    }
}