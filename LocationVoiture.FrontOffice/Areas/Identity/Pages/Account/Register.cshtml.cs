using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using LocationVoiture.Core.Data;
using LocationVoiture.Core.Models;
using LocationVoiture.FrontOffice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LocationVoiture.FrontOffice.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailVerificationService _emailService;
        private readonly IConfiguration _configuration;

        public RegisterModel(ApplicationDbContext context, IEmailVerificationService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Le nom est requis")]
            [Display(Name = "Nom")]
            public string Nom { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le prenom est requis")]
            [Display(Name = "Prenom")]
            public string Prenom { get; set; } = string.Empty;

            [Required(ErrorMessage = "L'email est requis")]
            [EmailAddress(ErrorMessage = "Format d'email invalide")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le mot de passe est requis")]
            [StringLength(100, ErrorMessage = "Le {0} doit contenir au moins {2} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirmer le mot de passe")]
            [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Format de telephone invalide")]
            [Display(Name = "Telephone")]
            public string? Telephone { get; set; }

            [Display(Name = "Adresse")]
            public string? Adresse { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalize email: Trim and ToLower
            var normalizedEmail = Input.Email.Trim().ToLower();

            // Check if user already exists
            var existingUser = await _context.Clients
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Cet email est deja utilise.");
                return Page();
            }

            // Hash password using SHA256
            var passwordHash = HashPassword(Input.Password);

            // Check if email verification is enabled
            var emailVerificationEnabled = _configuration.GetValue<bool>("Email:VerificationEnabled", false);
            
            // Generate verification token
            var verificationToken = _emailService.GenerateVerificationToken();

            var client = new Client
            {
                Nom = Input.Nom.Trim(),
                Prenom = Input.Prenom.Trim(),
                Email = normalizedEmail,
                MotDePasseHash = passwordHash,
                Telephone = Input.Telephone?.Trim(),
                Adresse = Input.Adresse?.Trim(),
                IsEmailVerified = !emailVerificationEnabled, // Auto-verify if email verification is disabled
                EmailVerificationToken = emailVerificationEnabled ? verificationToken : null
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Send verification email if enabled
            if (emailVerificationEnabled)
            {
                try
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    await _emailService.SendVerificationEmailAsync(normalizedEmail, verificationToken, baseUrl);
                    TempData["SuccessMessage"] = "Inscription reussie! Veuillez verifier votre email avant de vous connecter.";
                }
                catch (Exception ex)
                {
                    // If email fails, auto-verify user so they can still login
                    client.IsEmailVerified = true;
                    client.EmailVerificationToken = null;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Inscription reussie! Vous pouvez maintenant vous connecter.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Inscription reussie! Vous pouvez maintenant vous connecter.";
            }

            return RedirectToPage("./Login", new { returnUrl });
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
