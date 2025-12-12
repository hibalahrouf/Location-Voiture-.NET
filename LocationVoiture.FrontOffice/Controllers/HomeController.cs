using System.Diagnostics;
using LocationVoiture.FrontOffice.Models;
using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Core.Data; // Access DB
using Microsoft.EntityFrameworkCore; // For Include

namespace LocationVoiture.FrontOffice.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // Use DB Context

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch top 3 available premium cars
            var featuredCars = await _context.Vehicules
                .Include(v => v.TypeVehicule)
                .ThenInclude(t => t.Tarifs)
                .Where(v => v.Disponible == true)
                .Take(3)
                .ToListAsync();

            return View(featuredCars);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
