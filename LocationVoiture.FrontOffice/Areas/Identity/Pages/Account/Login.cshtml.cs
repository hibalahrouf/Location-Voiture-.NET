using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using LocationVoiture.Core.Data;
using LocationVoiture.FrontOffice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LocationVoiture.FrontOffice.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailVerificationService _emailService;
        private readonly SignInManager<IdentityUser> _signInManager;

        public LoginModel(ApplicationDbContext context, IEmailVerificationService emailService, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _emailService = emailService;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public bool ShowResendVerification { get; set; } = false;

        public class InputModel
        {
            [Required(ErrorMessage = "L'email est requis")]
            [EmailAddress(ErrorMessage = "Format d'email invalide")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le mot de passe est requis")]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Se souvenir de moi")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalize email: Trim and ToLower (same as Register)
            var normalizedEmail = Input.Email.Trim().ToLower();

            // Hash password using SHA256 (same as Register)
            var passwordHash = HashPassword(Input.Password);

            // Find user with matching email and password
            var client = await _context.Clients
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.MotDePasseHash == passwordHash);

            if (client == null)
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                return Page();
            }

            // Check email verification
            if (!client.IsEmailVerified)
            {
                ModelState.AddModelError(string.Empty, "Veuillez verifier votre email avant de vous connecter.");
                ShowResendVerification = true;
                return Page();
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, client.ClientID.ToString()),
                new Claim(ClaimTypes.Name, $"{client.Prenom} {client.Nom}"),
                new Claim(ClaimTypes.Email, client.Email),
                new Claim(ClaimTypes.Role, "Client")
            };

            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> OnPostResendVerificationAsync()
        {
            var normalizedEmail = Input.Email.Trim().ToLower();

            var client = await _context.Clients
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (client != null && !client.IsEmailVerified)
            {
                // Generate new token
                var newToken = _emailService.GenerateVerificationToken();
                client.EmailVerificationToken = newToken;
                await _context.SaveChangesAsync();

                // Send verification email
                try
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    await _emailService.SendVerificationEmailAsync(normalizedEmail, newToken, baseUrl);
                    TempData["SuccessMessage"] = "Email de verification renvoye avec succes.";
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Erreur lors de l'envoi de l'email.");
                }
            }

            return Page();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
