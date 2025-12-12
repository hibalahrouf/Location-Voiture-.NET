using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Core.Data;
using LocationVoiture.Core.Models;
using System.Threading.Tasks;

namespace LocationVoiture.FrontOffice.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                _context.ContactMessages.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Merci ! Votre message a bien été envoyé.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", model);
        }
    }
}
