// --- USINGS NÉCESSAIRES ---
using LocationVoiture.Core.Data; // <-- MODIFIÉ : Pour trouver notre DbContext
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using LocationVoiture.FrontOffice.Services;
using Serilog; // <-- AJOUTER
// --- REMPLACER "var builder = ..." PAR CECI ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/frontoffice_log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Dire à l'application d'utiliser Serilog
builder.Host.UseSerilog();
// --- FIN DU REMPLACEMENT ---

// --- CONFIGURATION DES SERVICES ---

// 1. Récupérer la chaîne de connexion
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Configurer le DbContext
//    Nous pointons vers le DbContext dans 'LocationVoiture.Core'
builder.Services.AddDbContext<ApplicationDbContext>(options => // <-- MODIFIÉ : Utilise notre DbContext
    options.UseSqlServer(connectionString,
        // Dites-lui de chercher les migrations dans le projet 'Core'
        b => b.MigrationsAssembly("LocationVoiture.Core") // <-- MODIFIÉ
    ));

// 3. Configurer Identity
//    Nous lui disons d'utiliser notre 'ApplicationDbContext'
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>(); // <-- MODIFIÉ : Utilise notre DbContext
// Injecte notre EmailSender personnalisé (en mode Singleton)
builder.Services.AddSingleton<IEmailSender, EmailSender>();
// Email Verification Service
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
// PDF Generation Service
builder.Services.AddScoped<IPdfService, PdfService>();
// 4. Ajouter les services standards
builder.Services.AddControllersWithViews();
// Ajout des Razor Pages (nécessaire pour les pages de connexion/inscription par défaut d'Identity)
builder.Services.AddRazorPages();

// ==========================================================
// || CONSTRUCTION DE L'APPLICATION
// ==========================================================

var app = builder.Build();

// ==========================================================
// || CONFIGURATION DU PIPELINE HTTP
// ==========================================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Outil utile pour les migrations pendant le développement
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Pour servir les fichiers CSS, JS, images

app.UseRouting(); // Active le routage

// IMPORTANT : L'authentification DOIT être avant l'autorisation
app.UseAuthentication();
app.UseAuthorization();

// Définit la route par défaut (ex: /Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mappe les pages Razor (pour les pages d'authentification)
app.MapRazorPages();

// Démarre l'application
app.Run();