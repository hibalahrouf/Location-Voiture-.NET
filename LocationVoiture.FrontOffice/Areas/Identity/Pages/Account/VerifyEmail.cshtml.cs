using LocationVoiture.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LocationVoiture.FrontOffice.Areas.Identity.Pages.Account
{
    public class VerifyEmailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VerifyEmailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string? Message { get; set; }
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                Message = "Lien de verification invalide.";
                IsSuccess = false;
                return Page();
            }

            var normalizedEmail = email.Trim().ToLower();

            var client = await _context.Clients
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.EmailVerificationToken == token);

            if (client == null)
            {
                Message = "Lien de verification invalide ou expire.";
                IsSuccess = false;
                return Page();
            }

            if (client.IsEmailVerified)
            {
                Message = "Votre email a deja ete verifie.";
                IsSuccess = true;
                return Page();
            }

            client.IsEmailVerified = true;
            client.EmailVerificationToken = null;
            await _context.SaveChangesAsync();

            Message = "Votre email a ete verifie avec succes! Vous pouvez maintenant vous connecter.";
            IsSuccess = true;
            return Page();
        }
    }
}
