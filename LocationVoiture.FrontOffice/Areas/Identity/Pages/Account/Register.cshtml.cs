#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using LocationVoiture.Core.Data; // <-- NOTRE AJOUT (pour le DbContext)
using LocationVoiture.Core.Models; // <-- NOTRE AJOUT (pour le modèle Client)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

// Assurez-vous que le namespace correspond à votre structure de dossiers
namespace LocationVoiture.FrontOffice.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        // --- NOTRE AJOUT : Le DbContext ---
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context) // <-- NOTRE AJOUT
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context; // <-- NOTRE AJOUT
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // C'est le "modèle" du formulaire
        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "Le mot de passe doit faire entre {2} et {1} caractères.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmer le mot de passe")]
            [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas.")]
            public string ConfirmPassword { get; set; }

            // --- NOS AJOUTS (les champs du formulaire) ---
            [Required]
            [Display(Name = "Prénom")]
            public string Prenom { get; set; }

            [Required]
            [Display(Name = "Nom")]
            public string Nom { get; set; }

            [Display(Name = "Téléphone")]
            public string? Telephone { get; set; }

            [Display(Name = "Adresse")]
            public string? Adresse { get; set; }
        }

        // Se déclenche quand la page est chargée (GET)
        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // Se déclenche quand l'utilisateur clique sur "S'inscrire" (POST)
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid) // Si le formulaire est valide...
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // 1. Crée l'utilisateur 'IdentityUser' (table AspNetUsers)
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded) // Si la création du compte a réussi...
                {
                    _logger.LogInformation("L'utilisateur a créé un nouveau compte.");

                    // ==========================================================
                    // || --- NOTRE LOGIQUE AJOUTÉE ---
                    // =Gen: [2]
                    // ==========================================================
                    try
                    {
                        // 2. On crée l'objet 'Client' dans notre table 'Clients'
                        var newClient = new Client
                        {
                            Prenom = Input.Prenom,
                            Nom = Input.Nom,
                            Email = Input.Email, // On utilise l'email comme lien
                            Telephone = Input.Telephone,
                            Adresse = Input.Adresse,
                            MotDePasseHash = "LinkedToIdentityAccount" // Placeholder
                        };
                        _context.Clients.Add(newClient);
                        await _context.SaveChangesAsync(); // On sauvegarde en BDD
                    }
                    catch (Exception ex)
                    {
                        // Si la création du 'Client' échoue, on supprime le 'IdentityUser'
                        // pour éviter un compte "fantôme". C'est une transaction manuelle.
                        _logger.LogError(ex, "Erreur lors de la création du Client lié.");
                        await _userManager.DeleteAsync(user); // Annulation
                        ModelState.AddModelError(string.Empty, "Erreur lors de la finalisation de l'inscription.");
                        return Page();
                    }
                    // ==========================================================
                    // || --- FIN DE NOTRE LOGIQUE ---
                    // ==========================================================

                    // Le reste est le code par défaut (envoi d'email de confirmation, connexion)
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    // Note : L'envoi d'email (étape 31) n'est pas encore configuré.
                     await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                         $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si on arrive ici, quelque chose a échoué, on ré-affiche le formulaire
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}