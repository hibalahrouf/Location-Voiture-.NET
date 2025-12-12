using LocationVoiture.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LocationVoiture.FrontOffice.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientID == userId);

            if (client == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Check email verification
            if (!client.IsEmailVerified)
            {
                TempData["ErrorMessage"] = "Veuillez verifier votre email avant d'acceder au tableau de bord.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return View(client);
        }
    }
}
