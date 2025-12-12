using LocationVoiture.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using Serilog; // <-- AJOUTER
namespace LocationVoiture.BackOffice
{
    public partial class App : Application
    {
        private static IHost? _host;

        public App()
        {
            Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug() // Enregistre tout (Debug, Info, Warning, Error)
        .WriteTo.File("logs/backoffice_log.txt", rollingInterval: RollingInterval.Day) // Crée un nouveau fichier chaque jour
        .CreateLogger();
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Lire appsettings.json
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Récupérer la chaîne de connexion
                    string connectionString = context.Configuration.GetConnectionString("DefaultConnection")!;

                    // Injecter le DbContext
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(connectionString)
                    );

                    // Injecter notre fenêtre principale
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        // Se lance au démarrage (car on a supprimé StartupUri)
        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host!.StartAsync();

            // Injecter le LoginWindow (manuellement pour l'instant car pas dans DI container comme service)
            // Ou mieux : on l'instancie ici.
            
            // Note: On n'a pas enregistré LoginWindow dans DI, on le fait manuellement.
            var context = _host!.Services.GetRequiredService<ApplicationDbContext>();
            var mainWindow = _host!.Services.GetRequiredService<MainWindow>(); // MainWindow est Singleton
            
            // Instancier LoginWindow
            var loginWindow = new LoginWindow(context, mainWindow);
            loginWindow.Show();

            // Ne PAS afficher MainWindow tout de suite. Le LoginWindow s'en chargera.
            // mainWindow.Show();

            base.OnStartup(e);
        }

        // S'assurer que l'hôte s'arrête en même temps que l'app
        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host!.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }
    }
}